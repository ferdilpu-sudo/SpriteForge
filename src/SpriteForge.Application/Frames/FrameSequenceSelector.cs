using SpriteForge.Core.Models;

namespace SpriteForge.Application.Frames;

public sealed class FrameSequenceSelector
{
    public IReadOnlyList<FrameRecord> Select(ProjectDocument project)
    {
        var enabled = project.Frames.Where(frame => frame.Enabled).OrderBy(frame => frame.Order).ToArray();
        if (enabled.Length == 0) return [];
        if (!project.Loop.Enabled || project.Loop.StartFrameId is null || project.Loop.EndFrameId is null)
            return enabled;

        var startIndex = Array.FindIndex(enabled, frame => frame.Id == project.Loop.StartFrameId.Value);
        var endIndex = Array.FindIndex(enabled, frame => frame.Id == project.Loop.EndFrameId.Value);
        if (startIndex < 0 || endIndex < 0)
            throw new InvalidOperationException("The configured loop range references a disabled or missing frame.");
        if (endIndex < startIndex)
            throw new InvalidOperationException("The configured loop end must follow the loop start.");

        return enabled[startIndex..(endIndex + 1)];
    }
}
