using SpriteForge.Core.Models;

namespace SpriteForge.Presentation.Looping;

public sealed class LoopCandidateViewModel(LoopCandidate candidate, int startDisplay, int endDisplay)
{
    public LoopCandidate Candidate { get; } = candidate;
    public int StartDisplay { get; } = startDisplay;
    public int EndDisplay { get; } = endDisplay;
    public double SeamScore => Candidate.SeamScore;
    public string Label => $"{StartDisplay} → {EndDisplay}   score {SeamScore:0.0000}";
}
