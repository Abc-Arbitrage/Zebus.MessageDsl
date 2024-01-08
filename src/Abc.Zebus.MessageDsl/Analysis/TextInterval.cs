using System;
using JetBrains.Annotations;

namespace Abc.Zebus.MessageDsl.Analysis;

public readonly struct TextInterval : IEquatable<TextInterval>
{
    public static TextInterval Empty { get; } = new();

    public int Start { get; }
    public int End { get; }

    public int Length => End - Start;
    public bool IsEmpty => Length == 0;

    public TextInterval(int offset)
        : this(offset, offset)
    {
    }

    public TextInterval(int startOffset, int endOffset)
    {
        if (startOffset < 0)
            throw new ArgumentOutOfRangeException(nameof(startOffset), "Start offset cannot be negative");

        if (endOffset < startOffset)
            throw new ArgumentOutOfRangeException(nameof(endOffset), "End offset cannot be less than start offset");

        Start = startOffset;
        End = endOffset;
    }

    [Pure]
    public TextInterval Intersect(TextInterval other)
        => OverlapsOrIsAdjacent(other)
            ? new TextInterval(Math.Max(Start, other.Start), Math.Min(End, other.End))
            : Empty;

    [Pure]
    public bool Contains(TextInterval other)
        => Start <= other.Start && other.End <= End;

    [Pure]
    public bool Contains(int offset)
        => Start <= offset && offset <= End;

    [Pure]
    public bool Overlaps(TextInterval other)
        => other.End > Start && other.Start < End;

    [Pure]
    public bool OverlapsOrIsAdjacent(TextInterval other)
        => other.End >= Start && other.Start <= End;

    public bool Equals(TextInterval other)
        => Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => obj is TextInterval interval && Equals(interval);
    public override int GetHashCode() => unchecked((Start * 397) ^ End);

    public static bool operator ==(TextInterval left, TextInterval right) => left.Equals(right);
    public static bool operator !=(TextInterval left, TextInterval right) => !left.Equals(right);

    public override string ToString()
        => IsEmpty ? Start.ToString() : $"{Start}-{End}";
}
