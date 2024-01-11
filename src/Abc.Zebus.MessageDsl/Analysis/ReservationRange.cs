using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Analysis;

internal readonly struct ReservationRange(int startTag, int endTag)
{
    public static ReservationRange None => default;

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

    public override string ToString()
        => IsValid
            ? startTag != endTag
                ? $"{startTag} - {endTag}"
                : $"{startTag}"
            : "None";
}
