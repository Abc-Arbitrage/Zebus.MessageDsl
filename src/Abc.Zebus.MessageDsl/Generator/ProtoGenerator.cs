﻿using System.Collections.Generic;
using System.Linq;
using Abc.Zebus.MessageDsl.Analysis;
using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Generator;

public sealed class ProtoGenerator : GeneratorBase
{
    private ParsedContracts Contracts { get; }

    private ProtoGenerator(ParsedContracts contracts)
    {
        Contracts = contracts;
    }

    public static bool HasProtoOutput(ParsedContracts contracts)
    {
        return contracts.Messages.Any(i => i.Options.Proto)
               || contracts.Enums.Any(i => i.Options.Proto);
    }

    public static string Generate(ParsedContracts contracts)
    {
        using var generator = new ProtoGenerator(contracts);
        return generator.Generate();
    }

    private string Generate()
    {
        Reset();

        WriteHeader();
        var messageTree = SymbolNode.Create(Contracts.Messages.Where(msg => msg.Options.Proto));

        foreach (var enumDef in Contracts.Enums.Where(msg => msg.Options.Proto))
            WriteEnum(enumDef);

        foreach (var message in messageTree.Children.Values)
            WriteMessage(message);

        return GeneratedOutput();
    }

    private void WriteHeader()
    {
        Writer.WriteLine();
        Writer.WriteLine("// Generated by {0} v{1}", GeneratorName, GeneratorVersion);
        Writer.WriteLine();
        Writer.WriteLine("syntax = \"proto2\";");
        Writer.WriteLine();

        var requiresBclPackage = Contracts.Messages
                                          .SelectMany(msg => msg.Parameters)
                                          .Any(param => param.Type.ProtoBufType.StartsWith("bcl."));

        if (requiresBclPackage)
            Writer.WriteLine("import \"bcl/bcl.proto\";");

        Writer.WriteLine("import \"servicebus.proto\";");

        if (!string.IsNullOrEmpty(Contracts.Namespace))
        {
            Writer.WriteLine();
            Writer.WriteLine("package {0};", Contracts.Namespace);
        }
    }

    private void WriteEnum(EnumDefinition enumDef)
    {
        Writer.WriteLine();
        Writer.Write("enum {0} ", enumDef.Name);

        using (Block())
        {
            if (enumDef.Members.Where(i => i.InferredValueAsNumber != null).GroupBy(i => i.InferredValueAsNumber).Any(g => g.Count() > 1))
                Writer.WriteLine("option allow_alias = true;");

            foreach (var member in enumDef.Members)
                Writer.WriteLine($"{enumDef.Name}_{member.Name} = {member.InferredValueAsNumber ?? "TODO"};");
        }
    }

    private void WriteMessage(SymbolNode node)
    {
        Writer.WriteLine();
        Writer.Write("message {0} ", node.Name);

        using (Block())
        {
            if (node.Definition is { } message)
            {
                WriteMessageOptions(message);
                WriteReservedFields(message);

                foreach (var param in message.Parameters)
                    WriteField(param);

                WriteIncludedMessages(message);
            }

            foreach (var child in node.Children.Values)
            {
                WriteMessage(child);
            }
        }
    }

    private void WriteMessageOptions(MessageDefinition message)
    {
        if (message.Type != MessageType.Custom)
        {
            Writer.WriteLine("option (servicebus.message).type = {0};", message.Type == MessageType.Command ? "Command" : "Event");

            if (message.IsTransient)
                Writer.WriteLine("option (servicebus.message).transient = true;");

            if (message.IsRoutable)
                Writer.WriteLine("option (servicebus.message).routable = true;");

            Writer.WriteLine();
        }
    }

    private void WriteReservedFields(MessageDefinition message)
    {
        if (message.ReservedRanges.Count == 0)
            return;

        Writer.Write("reserved ");
        var first = true;

        foreach (var reservation in ReservationRange.Compress(message.ReservedRanges))
        {
            if (first)
                first = false;
            else
                Writer.Write(", ");

            if (reservation.StartTag == reservation.EndTag)
                Writer.Write(reservation.StartTag);
            else
                Writer.Write("{0} to {1}", reservation.StartTag, reservation.EndTag);
        }

        Writer.WriteLine(";");
    }

    private void WriteField(ParameterDefinition param)
    {
        Writer.Write(
            "{0} {1} {2} = {3}",
            param.Rules.ToString().ToLowerInvariant(),
            param.Type.ProtoBufType,
            MemberCase(param.Name),
            param.Tag);

        WriteFieldOptions(param);

        Writer.WriteLine(";");
    }

    private void WriteFieldOptions(ParameterDefinition param)
    {
        var first = true;

        if (param.IsPacked)
            WriteFieldOption("packed", "true", ref first);

        if (param.Attributes.HasAttribute(KnownTypes.ObsoleteAttribute))
            WriteFieldOption("deprecated", "true", ref first);

        if (param.RoutingPosition != null)
            WriteFieldOption("(servicebus.routing_position)", param.RoutingPosition.ToString()!, ref first);

        if (!first)
            Writer.Write("]");
    }

    private void WriteFieldOption(string key, string value, ref bool first)
    {
        if (first)
        {
            Writer.Write(" [");
            first = false;
        }
        else
        {
            Writer.Write(", ");
        }

        Writer.Write("{0} = {1}", key, value);
    }

    private void WriteIncludedMessages(MessageDefinition message)
    {
        foreach (var attr in message.Attributes)
        {
            if (!Equals(attr.TypeName, KnownTypes.ProtoIncludeAttribute))
                continue;

            if (!AttributeInterpreter.TryParseProtoInclude(attr, out var tag, out var typeName))
            {
                Writer.WriteLine("// ERROR: bad sub type definition");
                continue;
            }

            Writer.Write("optional ");
            Writer.Write(typeName.ProtoBufType);
            Writer.Write(" _subType");
            Writer.Write(typeName.ProtoBufType);
            Writer.Write(" = ");
            Writer.Write(tag);
            Writer.WriteLine(";");
        }
    }

    private class SymbolNode(string name)
    {
        public string Name { get; } = name;
        public MessageDefinition? Definition { get; private set; }
        public Dictionary<string, SymbolNode> Children { get; } = [];

        public static SymbolNode Create(IEnumerable<MessageDefinition> messages)
        {
            var rootNode = new SymbolNode(string.Empty);

            foreach (var message in messages)
            {
                var parent = rootNode;

                foreach (var containingClass in message.ContainingClasses)
                    parent = parent.GetOrCreateChild(containingClass.ProtoBufType);

                parent.GetOrCreateChild(message.Name).Definition = message;
            }

            return rootNode;
        }

        private SymbolNode GetOrCreateChild(string name)
        {
            if (!Children.TryGetValue(name, out var childNode))
            {
                childNode = new SymbolNode(name);
                Children.Add(name, childNode);
            }

            return childNode;
        }
    }
}
