using SpriteForge.Core.Models;

namespace SpriteForge.Application.Frames;

public sealed class FrameTimingService
{
    public double GetEnabledTotalDurationMs(ProjectDocument project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return project.Frames
            .Where(frame => frame.Enabled)
            .Sum(frame => frame.DurationMs);
    }

    public void PreserveEnabledTotalDuration(
        ProjectDocument project,
        double targetDurationMs)
    {
        ArgumentNullException.ThrowIfNull(project);
        if (!double.IsFinite(targetDurationMs) || targetDurationMs <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(targetDurationMs),
                "Target animation duration must be positive.");

        var enabled = project.Frames
            .Where(frame => frame.Enabled)
            .ToArray();
        if (enabled.Length == 0)
            throw new InvalidOperationException("At least one enabled frame must remain.");

        var currentTotal = enabled.Sum(frame => frame.DurationMs);
        if (!double.IsFinite(currentTotal) || currentTotal <= 0)
            throw new InvalidOperationException("Enabled frame durations are invalid.");

        var scale = targetDurationMs / currentTotal;
        var enabledIds = enabled.Select(frame => frame.Id).ToHashSet();

        for (var index = 0; index < project.Frames.Count; index++)
        {
            var frame = project.Frames[index];
            if (!enabledIds.Contains(frame.Id)) continue;

            project.Frames[index] = frame with
            {
                DurationMs = frame.DurationMs * scale
            };
        }
    }
}
