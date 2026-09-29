namespace SpriteForge.App.Workflow;

internal sealed partial class DesktopWorkflowController
{
    private static double NormalizeFinite(
        double value,
        double fallback,
        double minimum,
        double maximum = double.MaxValue)
    {
        var safeFallback = double.IsFinite(fallback)
            ? Math.Clamp(fallback, minimum, maximum)
            : minimum;
        if (!double.IsFinite(value)) return safeFallback;
        return Math.Clamp(value, minimum, maximum);
    }

    private static int NormalizeDimension(double value, int fallback)
    {
        var safe = NormalizeFinite(value, Math.Max(1, fallback), 1, 16_384);
        return Math.Clamp((int)Math.Round(safe), 1, 16_384);
    }

    private static int NormalizeNonNegativeInt(double value, int fallback)
    {
        var safe = NormalizeFinite(value, Math.Max(0, fallback), 0, 16_384);
        return Math.Clamp((int)Math.Round(safe), 0, 16_384);
    }

    private static int? NormalizeColumns(double value, int? fallback)
    {
        if (!double.IsFinite(value)) return fallback is > 0 ? fallback : null;
        if (value <= 0) return null;
        return Math.Clamp((int)Math.Round(value), 1, 16_384);
    }

    private static int NormalizeFrameNumber(double value, int fallback, int frameCount)
    {
        var safeFallback = Math.Clamp(fallback, 1, Math.Max(1, frameCount));
        var safe = NormalizeFinite(value, safeFallback, 1, Math.Max(1, frameCount));
        return Math.Clamp((int)Math.Round(safe), 1, Math.Max(1, frameCount));
    }

    private static string NormalizeFrameOptimizationMode(string? value, string? fallback = null)
    {
        static string? Canonicalize(string? candidate)
        {
            var normalized = candidate?.Trim().ToLowerInvariant();
            return normalized is "raw" or "compact" or "balanced" or "smooth"
                ? normalized
                : null;
        }

        return Canonicalize(value) ?? Canonicalize(fallback) ?? "balanced";
    }

    private static string NormalizeFit(string? value, string? fallback = null)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        if (normalized is "contain" or "cover" or "original") return normalized;

        var normalizedFallback = fallback?.Trim().ToLowerInvariant();
        return normalizedFallback is "contain" or "cover" or "original"
            ? normalizedFallback
            : "contain";
    }

    private static string NormalizeAnchor(string? value, string? fallback = null)
    {
        static string? Canonicalize(string? candidate)
        {
            var normalized = candidate?.Trim().ToLowerInvariant().Replace(' ', '_');
            return normalized is
                "top_left" or "top_center" or "top_right" or
                "center_left" or "center" or "center_right" or
                "bottom_left" or "bottom_center" or "bottom_right"
                ? normalized
                : null;
        }

        return Canonicalize(value) ?? Canonicalize(fallback) ?? "bottom_center";
    }
}
