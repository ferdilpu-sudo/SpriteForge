using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace SpriteForge.App.Controls;

internal static class PreviewImageCache
{
    private const int MaxEntries = 32;
    private const int MaxDecodeDimension = 1_024;

    private static readonly Dictionary<string, Task<BitmapImage?>> Entries =
        new(StringComparer.OrdinalIgnoreCase);
    private static readonly Queue<string> InsertionOrder = new();
    private static readonly object Gate = new();

    public static Task<BitmapImage?> GetAsync(string path)
    {
        lock (Gate)
        {
            if (Entries.TryGetValue(path, out var cached))
                return cached;

            var task = LoadAsync(path);
            Entries[path] = task;
            InsertionOrder.Enqueue(path);
            Trim();
            return task;
        }
    }

    public static async Task PreloadAsync(IEnumerable<string> paths)
    {
        var unique = paths
            .Where(path => !string.IsNullOrWhiteSpace(path) && File.Exists(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxEntries)
            .ToArray();

        if (unique.Length == 0) return;
        await Task.WhenAll(unique.Select(GetAsync));
    }

    public static void Clear()
    {
        lock (Gate)
        {
            Entries.Clear();
            InsertionOrder.Clear();
        }
    }

    private static async Task<BitmapImage?> LoadAsync(string path)
    {
        try
        {
            var file = await StorageFile.GetFileFromPathAsync(path);
            var imageProperties = await file.Properties.GetImagePropertiesAsync();
            using var stream = await file.OpenAsync(FileAccessMode.Read);

            var bitmap = CreateDecodeCappedBitmap(imageProperties);
            await bitmap.SetSourceAsync(stream);
            return bitmap;
        }
        catch
        {
            lock (Gate)
            {
                Entries.Remove(path);
            }
            return null;
        }
    }

    private static BitmapImage CreateDecodeCappedBitmap(ImageProperties properties)
    {
        var bitmap = new BitmapImage();
        if (properties.Width <= MaxDecodeDimension &&
            properties.Height <= MaxDecodeDimension)
        {
            return bitmap;
        }

        if (properties.Width >= properties.Height)
            bitmap.DecodePixelWidth = MaxDecodeDimension;
        else
            bitmap.DecodePixelHeight = MaxDecodeDimension;

        return bitmap;
    }

    private static void Trim()
    {
        while (Entries.Count > MaxEntries && InsertionOrder.Count > 0)
        {
            var oldest = InsertionOrder.Dequeue();
            Entries.Remove(oldest);
        }
    }
}
