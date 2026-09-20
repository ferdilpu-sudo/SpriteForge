namespace SpriteForge.Core.Models;

public sealed record LoopCandidate(Guid StartFrameId, Guid EndFrameId, double SeamScore, int FrameCount);
