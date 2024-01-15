using System;
using System.Collections.Generic;
using System.Linq;
using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Analysis;

internal readonly struct ReservationRange(int startTag, int endTag)
{
    public static ReservationRange None => default;

    public int StartTag => startTag;
    public int EndTag => endTag;

    private bool IsValid => startTag > 0;

    public ReservationRange(int tag)
        : this(tag, tag)
    {
    }

    public bool Contains(int tag)
        => IsValid && tag >= startTag && tag <= endTag;

    public bool TryAddTag(int tag, out ReservationRange updatedRange)
    {
        if (IsValid && tag == endTag + 1)
        {
            updatedRange = new ReservationRange(startTag, tag);
            return true;
        }

        updatedRange = default;
        return false;
    }

    public void AddToMessage(MessageDefinition message)
    {
        if (!IsValid)
            return;

        message.Attributes.Add(new AttributeDefinition(KnownTypes.ProtoReservedAttribute, startTag == endTag ? $"{startTag}" : $"{startTag}, {endTag}"));
        message.ReservedRanges.Add(this);
    }

    public static IEnumerable<ReservationRange> Compress(IEnumerable<ReservationRange> ranges)
    {
        var startTag = 0;
        var endTag = 0;

        foreach (var range in ranges.Where(i => i.IsValid).OrderBy(i => i.StartTag))
        {
            if (startTag == 0)
            {
                startTag = range.StartTag;
                endTag = range.EndTag;
            }

            if (range.StartTag > endTag + 1)
            {
                yield return new ReservationRange(startTag, endTag);
                startTag = range.StartTag;
            }

            endTag = Math.Max(endTag, range.EndTag);
        }

        if (startTag != 0)
            yield return new ReservationRange(startTag, endTag);
    }

    public override string ToString()
        => IsValid
            ? startTag != endTag
                ? $"{startTag} - {endTag}"
                : $"{startTag}"
            : "None";
}
