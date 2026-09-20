using System.Globalization;
using System.Text.RegularExpressions;

namespace SpriteForge.Application.Imports;

internal sealed partial class NaturalPathComparer : IComparer<string>
{
    public static NaturalPathComparer Instance { get; } = new();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var left = Tokenize(Path.GetFileName(x));
        var right = Tokenize(Path.GetFileName(y));
        var count = Math.Min(left.Length, right.Length);
        for (var index = 0; index < count; index++)
        {
            var a = left[index];
            var b = right[index];
            var aNumber = long.TryParse(a, NumberStyles.None, CultureInfo.InvariantCulture, out var av);
            var bNumber = long.TryParse(b, NumberStyles.None, CultureInfo.InvariantCulture, out var bv);
            var comparison = aNumber && bNumber
                ? av.CompareTo(bv)
                : StringComparer.OrdinalIgnoreCase.Compare(a, b);
            if (comparison != 0) return comparison;
        }

        return left.Length.CompareTo(right.Length);
    }

    private static string[] Tokenize(string value) => NumberTokenRegex().Split(value);

    [GeneratedRegex("([0-9]+)")]
    private static partial Regex NumberTokenRegex();
}
