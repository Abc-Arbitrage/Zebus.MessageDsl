using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Analysis;

internal class AstProcessor
{
    private readonly ParsedContracts _contracts;

    public AstProcessor(ParsedContracts contracts)
    {
        _contracts = contracts;
    }

    public void PreProcess()
    {
        _contracts.ImportedNamespaces.Add("System");
        _contracts.ImportedNamespaces.Add("ProtoBuf");
        _contracts.ImportedNamespaces.Add("Abc.Zebus");
    }

    public void PostProcess()
    {
        foreach (var message in _contracts.Messages)
        {
            ResolveTags(message);
            AddInterfaces(message);
            AddImplicitNamespaces(message);
            AddReservations(message);
            SetInheritanceModifier(message);
        }

        foreach (var enumDef in _contracts.Enums)
        {
            ResolveEnumValues(enumDef);
            AddImplicitNamespaces(enumDef.Attributes);

            foreach (var memberDef in enumDef.Members)
                AddImplicitNamespaces(memberDef.Attributes);
        }
    }

    public void Cleanup()
    {
        foreach (var message in _contracts.Messages)
            RemoveDiscardedParameters(message);

        foreach (var enumDef in _contracts.Enums)
            RemoveDiscardedMembers(enumDef);
    }

    private static void AddInterfaces(MessageDefinition message)
    {
        switch (message.Type)
        {
            case MessageType.Event:
                message.BaseTypes.Add(KnownTypes.EventInterface);
                break;

            case MessageType.Command:
                message.BaseTypes.Add(KnownTypes.CommandInterface);
                break;

            case MessageType.Custom:
                message.BaseTypes.Add(KnownTypes.MessageInterface);
                break;
        }
    }

    private static void ResolveTags(MessageDefinition message)
    {
        var nextTag = AstValidator.ProtoMinTag;

        foreach (var param in message.Parameters)
        {
            if (param.Tag == 0)
                param.Tag = nextTag;

            nextTag = param.Tag + 1;

            if (nextTag is >= AstValidator.ProtoFirstReservedTag and <= AstValidator.ProtoLastReservedTag)
                nextTag = AstValidator.ProtoLastReservedTag + 1;
        }
    }

    private static void ResolveEnumValues(EnumDefinition enumDef)
    {
        var lastValue = (string?)null;
        var lastValueAsNumber = (long?)null;
        var lastValueIsParsed = false;
        var lastOffset = 0L;

        enumDef.UseInferredValues = enumDef.Members.Any(i => i.IsDiscarded);

        foreach (var member in enumDef.Members)
        {
            if (!string.IsNullOrEmpty(member.Value))
            {
                lastValue = member.InferredValueAsCSharpString = member.Value;
                lastValueAsNumber = null;
                lastValueIsParsed = false;
                lastOffset = 0;
            }
            else if (lastValue is null)
            {
                lastValue = member.InferredValueAsCSharpString = "0";
                lastValueAsNumber = 0;
                lastValueIsParsed = true;
                lastOffset = 0;
            }
            else
            {
                if (lastValueAsNumber is null && !lastValueIsParsed)
                {
                    if (long.TryParse(lastValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var lastValueParsed))
                        lastValueAsNumber = lastValueParsed;
                    else if (lastValue.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && long.TryParse(lastValue.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out lastValueParsed))
                        lastValueAsNumber = lastValueParsed;

                    lastValueIsParsed = true;
                }

                ++lastOffset;

                member.InferredValueAsCSharpString = lastValueAsNumber is { } lastBaseValue
                    ? checked(lastBaseValue + lastOffset).ToString(CultureInfo.InvariantCulture)
                    : $"({lastValue}) + {lastOffset}";
            }

            member.InferredValueAsNumber = enumDef.GetValidUnderlyingValue(member.InferredValueAsCSharpString);
        }
    }

    private void AddImplicitNamespaces(MessageDefinition message)
    {
        AddImplicitNamespaces(message.Attributes);

        foreach (var paramDef in message.Parameters)
        {
            AddImplicitNamespaces(paramDef.Attributes);

            if (paramDef.Type.IsList)
                _contracts.ImportedNamespaces.Add(typeof(List<>).Namespace!);
            else if (paramDef.Type.IsDictionary)
                _contracts.ImportedNamespaces.Add(typeof(Dictionary<,>).Namespace!);
            else if (paramDef.Type.IsHashSet)
                _contracts.ImportedNamespaces.Add(typeof(HashSet<>).Namespace!);
        }
    }

    private void AddImplicitNamespaces(AttributeSet attributes)
    {
        if (attributes.HasAttribute(KnownTypes.DescriptionAttribute))
            _contracts.ImportedNamespaces.Add(typeof(DescriptionAttribute).Namespace!);
    }

    private static void SetInheritanceModifier(MessageDefinition message)
    {
        if (message.InheritanceModifier != InheritanceModifier.Default)
            return;

        var hasInheritedMessages = message.Attributes.Any(attr => Equals(attr.TypeName, KnownTypes.ProtoIncludeAttribute));

        message.InheritanceModifier = hasInheritedMessages
            ? InheritanceModifier.None
            : InheritanceModifier.Sealed;
    }

    private static void RemoveDiscardedParameters(MessageDefinition message)
    {
        for (var i = message.Parameters.Count - 1; i >= 0; --i)
        {
            if (message.Parameters[i].IsDiscarded)
                message.Parameters.RemoveAt(i);
        }
    }

    private static void RemoveDiscardedMembers(EnumDefinition enumDef)
    {
        for (var i = enumDef.Members.Count - 1; i >= 0; --i)
        {
            if (enumDef.Members[i].IsDiscarded)
                enumDef.Members.RemoveAt(i);
        }
    }

    private static void AddReservations(MessageDefinition message)
    {
        var currentReservation = ReservationRange.None;

        foreach (var parameter in message.Parameters)
        {
            if (parameter.IsDiscarded)
            {
                if (currentReservation.TryAddTag(parameter.Tag, out var updatedReservation))
                {
                    currentReservation = updatedReservation;
                }
                else
                {
                    currentReservation.AddToMessage(message);
                    currentReservation = new ReservationRange(parameter.Tag);
                }
            }
            else
            {
                currentReservation.AddToMessage(message);
                currentReservation = ReservationRange.None;
            }
        }

        currentReservation.AddToMessage(message);
    }
}
