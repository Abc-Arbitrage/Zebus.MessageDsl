﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Analysis;

internal class AstProcessor
{
    private static readonly Regex _simpleValueRe = new(@"^\s*(?:-\s*)?(?:0[xX])?\w+\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

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
        var lastValueAsString = (string?)null;
        var lastValueAsNumber = (object?)null;
        var lastValueOffset = 0L;

        enumDef.UseInferredValues = enumDef.Members.Any(i => i.IsDiscarded);

        foreach (var member in enumDef.Members)
        {
            if (!string.IsNullOrEmpty(member.Value) || lastValueAsString is null)
            {
                lastValueAsString = member.InferredValueAsString = !string.IsNullOrEmpty(member.Value) ? member.Value : "0";
                lastValueAsNumber = member.InferredValueAsNumber = enumDef.GetValidUnderlyingValue(member.InferredValueAsString);
                lastValueOffset = 0;
            }
            else
            {
                if (lastValueAsNumber is not null)
                {
                    lastValueAsNumber = checked(lastValueAsNumber switch
                    {
                        byte value   => value + 1,
                        sbyte value  => value + 1,
                        short value  => value + 1,
                        ushort value => value + 1,
                        int value    => value + 1,
                        uint value   => value + 1,
                        long value   => value + 1,
                        ulong value  => value + 1,
                        _            => throw new InvalidOperationException("Unexpected enum underlying value type")
                    });

                    member.InferredValueAsString = lastValueAsNumber.ToString();
                    member.InferredValueAsNumber = lastValueAsNumber;
                }
                else
                {
                    ++lastValueOffset;

                    member.InferredValueAsString = _simpleValueRe.IsMatch(lastValueAsString)
                        ? $"{lastValueAsString} + {lastValueOffset}"
                        : $"({lastValueAsString}) + {lastValueOffset}";

                    member.InferredValueAsNumber = null;
                }
            }
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
