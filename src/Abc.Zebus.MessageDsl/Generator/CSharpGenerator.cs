﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Abc.Zebus.MessageDsl.Analysis;
using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Generator;

public sealed class CSharpGenerator : GeneratorBase
{
    private static readonly AttributeDefinition _attrProtoContract = new(KnownTypes.ProtoContractAttribute);
    private static readonly AttributeDefinition _attrNonUserCode = new("System.Diagnostics.DebuggerNonUserCode");
    private static readonly AttributeDefinition _attrGeneratedCode = new("System.CodeDom.Compiler.GeneratedCode", $@"""{GeneratorName}"", ""{GeneratorVersion}""");

    private readonly Dictionary<string, MessageDefinition> _messagesByName = new();

    private ParsedContracts Contracts { get; }

    private CSharpGenerator(ParsedContracts contracts)
    {
        Contracts = contracts;

        foreach (var message in contracts.Messages)
            _messagesByName[message.Name] = message;
    }

    public static string Generate(ParsedContracts contracts)
    {
        using var generator = new CSharpGenerator(contracts);
        return generator.Generate();
    }

    private string Generate()
    {
        Reset();

        WriteHeader();
        WriteUsingDirectives();
        WritePragmas();

        var hasNamespace = !string.IsNullOrEmpty(Contracts.Namespace);
        if (hasNamespace)
            Writer.WriteLine("namespace {0}", Identifier(Contracts.Namespace!));

        using (hasNamespace ? Block() : null)
        {
            var firstMember = true;

            foreach (var enumDef in Contracts.Enums)
            {
                if (!firstMember)
                    Writer.WriteLine();

                WriteEnum(enumDef);
                firstMember = false;
            }

            var nullableRefTypes = false;

            foreach (var message in Contracts.Messages)
            {
                if (!firstMember)
                    Writer.WriteLine();

                if (message.Options.Nullable != nullableRefTypes)
                {
                    WriteNullableDirective(message.Options.Nullable);
                    nullableRefTypes = message.Options.Nullable;
                }

                WriteMessage(message);
                firstMember = false;
            }
        }

        return GeneratedOutput();
    }

    private void WriteHeader()
    {
        Writer.WriteLine("//------------------------------------------------------------------------------");
        Writer.WriteLine("// <auto-generated>");
        Writer.WriteLine("//     This code was generated by a tool.");
        Writer.WriteLine("// </auto-generated>");
        Writer.WriteLine("//------------------------------------------------------------------------------");
        Writer.WriteLine();
    }

    private void WriteUsingDirectives()
    {
        var orderedNamespaces = Contracts.ImportedNamespaces
                                         .OrderByDescending(ns => ns == "System" || ns.StartsWith("System."))
                                         .ThenBy(ns => ns, StringComparer.OrdinalIgnoreCase);

        foreach (var ns in orderedNamespaces)
            Writer.WriteLine("using {0};", Identifier(ns));

        Writer.WriteLine();
    }

    private void WritePragmas()
    {
        var hasObsolete = Contracts.Messages.Any(m => m.Attributes.HasAttribute(KnownTypes.ObsoleteAttribute))
                          || Contracts.Messages.SelectMany(m => m.Parameters).Any(p => p.Attributes.HasAttribute(KnownTypes.ObsoleteAttribute))
                          || Contracts.Enums.Any(m => m.Attributes.HasAttribute(KnownTypes.ObsoleteAttribute))
                          || Contracts.Enums.SelectMany(m => m.Members).Any(m => m.Attributes.HasAttribute(KnownTypes.ObsoleteAttribute));

        if (hasObsolete)
        {
            Writer.WriteLine("#pragma warning disable 612");
            Writer.WriteLine("");
        }
    }

    private void WriteEnum(EnumDefinition enumDef)
    {
        if (!enumDef.Attributes.GetAttributes(_attrProtoContract.TypeName).Any(attr => attr.Target is AttributeTarget.Default or AttributeTarget.Type))
            WriteAttributeLine(_attrProtoContract);

        WriteAttributeLine(_attrGeneratedCode);

        foreach (var attribute in enumDef.Attributes)
        {
            if (attribute.Target is AttributeTarget.Default or AttributeTarget.Type)
                WriteAttributeLine(attribute);
        }

        Writer.Write(
            "{0} enum {1}",
            AccessModifier(enumDef.AccessModifier),
            Identifier(enumDef.Name)
        );

        if (enumDef.UnderlyingType.NetType != "int")
            Writer.Write(" : {0}", enumDef.UnderlyingType.NetType);

        Writer.WriteLine();

        using (Block())
        {
            var hasAnyAttributeOnMembers = enumDef.Members.Any(m => m.Attributes.Count > 0);
            var lastMember = enumDef.Members.LastOrDefault();

            foreach (var member in enumDef.Members)
            {
                foreach (var attribute in member.Attributes)
                {
                    if (attribute.Target is AttributeTarget.Default or AttributeTarget.Field)
                        WriteAttributeLine(attribute);
                }

                Writer.Write(Identifier(member.Name));

                if (!string.IsNullOrEmpty(member.Value))
                    Writer.Write(" = {0}", member.Value);
                else if (enumDef.UseInferredValues && !string.IsNullOrEmpty(member.InferredValueAsString))
                    Writer.Write(" = {0}", member.InferredValueAsString);

                if (member != lastMember)
                {
                    Writer.Write(",");

                    if (hasAnyAttributeOnMembers)
                        Writer.WriteLine();
                }

                Writer.WriteLine();
            }
        }
    }

    private void WriteNullableDirective(bool enable)
    {
        Writer.WriteLine("#nullable {0}", enable ? "enable" : "disable");
        Writer.WriteLine();
    }

    private void WriteMessage(MessageDefinition message)
    {
        var containingClassesStack = new Stack<IDisposable>();
        foreach (var containingClass in message.ContainingClasses)
        {
            Writer.Write("partial class ");
            Writer.WriteLine(containingClass.NetType);

            containingClassesStack.Push(Block());
        }

        if (!message.Attributes.GetAttributes(_attrProtoContract.TypeName).Any(attr => attr.Target is AttributeTarget.Default or AttributeTarget.Type))
            WriteAttributeLine(_attrProtoContract);

        WriteAttributeLine(_attrNonUserCode);
        WriteAttributeLine(_attrGeneratedCode);

        foreach (var attribute in message.Attributes)
        {
            if (attribute.Target is AttributeTarget.Default or AttributeTarget.Type)
                WriteAttributeLine(attribute);
        }

        Writer.Write(AccessModifier(message.AccessModifier));
        Writer.Write(" ");

        var inheritanceModifier = InheritanceModifier(message.InheritanceModifier);
        if (!string.IsNullOrEmpty(inheritanceModifier))
        {
            Writer.Write(inheritanceModifier);
            Writer.Write(" ");
        }

        Writer.Write("partial class ");
        Writer.Write(Identifier(message.Name));

        if (message.GenericParameters.Count > 0)
        {
            Writer.Write("<");
            var templateParamList = List();

            foreach (var templateParameter in message.GenericParameters)
            {
                templateParamList.NextItem();
                Writer.Write(Identifier(templateParameter));
            }

            Writer.Write(">");
        }

        if (message.BaseTypes.Count > 0)
        {
            Writer.Write(" : ");
            var baseTypeList = List();

            foreach (var baseType in message.BaseTypes.Distinct())
            {
                baseTypeList.NextItem();
                Writer.Write(baseType.NetType);
            }
        }

        Writer.WriteLine();

        WriteGenericConstraints(message);

        using (Block())
        {
            foreach (var param in message.Parameters)
                WriteParameterMember(message, param);

            var parameters = GetConstructorParameters(message);
            if (parameters.Count != 0)
            {
                WriteDefaultConstructor(message);
                WriteForwardingConstructor(message, parameters);
                WriteMessageConstructor(message, parameters);
            }
        }

        while (containingClassesStack.Count != 0)
            containingClassesStack.Pop().Dispose();
    }

    private void WriteGenericConstraints(MessageDefinition message)
    {
        if (message.GenericConstraints.Count == 0)
            return;

        using (Indent())
        {
            foreach (var genericConstraint in message.GenericConstraints)
            {
                Writer.Write("where ");
                Writer.Write(Identifier(genericConstraint.GenericParameterName));
                Writer.Write(" : ");

                var constraintList = List();

                if (genericConstraint.IsClass)
                {
                    constraintList.NextItem();
                    Writer.Write("class");
                }
                else if (genericConstraint.IsStruct)
                {
                    constraintList.NextItem();
                    Writer.Write("struct");
                }

                foreach (var type in genericConstraint.Types)
                {
                    constraintList.NextItem();
                    Writer.Write(type.NetType);
                }

                if (genericConstraint.HasDefaultConstructor && !genericConstraint.IsStruct)
                {
                    constraintList.NextItem();
                    Writer.Write("new()");
                }

                Writer.WriteLine();
            }
        }
    }

    private void WriteParameterMember(MessageDefinition message, ParameterDefinition param)
    {
        if (!param.Attributes.GetAttributes(KnownTypes.ProtoMemberAttribute).Any(attr => attr.Target is AttributeTarget.Default or AttributeTarget.Property))
        {
            var protoMemberParams = new StringBuilder();

            protoMemberParams.Append(param.Tag);
            protoMemberParams.AppendFormat(", IsRequired = {0}", param.Rules == FieldRules.Required ? "true" : "false");

            if (param.IsPacked)
                protoMemberParams.Append(", IsPacked = true");

            WriteAttributeLine(new AttributeDefinition(KnownTypes.ProtoMemberAttribute, protoMemberParams.ToString()));
        }

        foreach (var attribute in param.Attributes)
        {
            if (attribute.Target is AttributeTarget.Default or AttributeTarget.Property)
                WriteAttributeLine(attribute);
        }

        var isWritable = param.IsWritableProperty || message.Options.Mutable;

        Writer.Write("public {0} {1}", param.Type.NetType, Identifier(MemberCase(param.Name)));
        Writer.WriteLine(isWritable ? " { get; set; }" : " { get; private set; }");
        Writer.WriteLine();
    }

    private void WriteDefaultConstructor(MessageDefinition message)
    {
        Writer.Write(
            message.InheritanceModifier == Ast.InheritanceModifier.Abstract
                ? "protected"
                : message.Options.Mutable
                    ? "public"
                    : "private"
        );

        Writer.Write(" ");
        Writer.Write(Identifier(message.Name));
        Writer.WriteLine("()");

        WriteDefaultConstructorBody(message);
    }

    private void WriteDefaultConstructorBody(MessageDefinition message)
    {
        using (Block())
        {
            foreach (var param in message.Parameters)
            {
                if (param.Type.IsNullable)
                    continue;

                if (param.Type.IsArray)
                    Writer.WriteLine("{0} = Array.Empty<{1}>();", Identifier(MemberCase(param.Name)), param.Type.GetRepeatedItemType()!.NetType);
                else if (param.Type.IsList || param.Type.IsDictionary || param.Type.IsHashSet)
                    Writer.WriteLine("{0} = new {1}();", Identifier(MemberCase(param.Name)), param.Type);
                else if (message.Options.Nullable && !param.Type.IsKnownValueType())
                    Writer.WriteLine("{0} = default!;", Identifier(MemberCase(param.Name)));
            }
        }
    }

    private void WriteForwardingConstructor(MessageDefinition message, List<ParameterData> parameters)
    {
        if (message.InheritanceModifier == Ast.InheritanceModifier.Sealed
            || !message.Options.Mutable
            || parameters.Count == 0
            || !parameters[0].IsFromBase
            || parameters.All(p => p.IsFromBase))
        {
            return;
        }

        Writer.WriteLine();

        Writer.Write("protected ");
        Writer.Write(Identifier(message.Name));
        Writer.Write("(");

        var paramList = List();
        foreach (var param in parameters)
        {
            if (!param.IsFromBase)
                break;

            paramList.NextItem();

            foreach (var attribute in param.Parameter.Attributes)
            {
                if (attribute.Target is AttributeTarget.Param)
                    WriteAttributeInline(attribute);
            }

            Writer.Write("{0} {1}", param.Parameter.Type.NetType, Identifier(ParameterCase(param.Parameter.Name)));
        }

        Writer.WriteLine(")");

        using (Indent())
        {
            Writer.Write(": base(");

            paramList.Reset();
            foreach (var param in parameters)
            {
                if (!param.IsFromBase)
                    break;

                paramList.NextItem();
                Writer.Write(Identifier(ParameterCase(param.Parameter.Name)));
            }

            Writer.WriteLine(")");
        }

        WriteDefaultConstructorBody(message);
    }

    private void WriteMessageConstructor(MessageDefinition message, List<ParameterData> parameters)
    {
        Writer.WriteLine();

        Writer.Write(
            message.InheritanceModifier == Ast.InheritanceModifier.Abstract
                ? "protected"
                : "public"
        );

        Writer.Write(" ");
        Writer.Write(Identifier(message.Name));
        Writer.Write("(");

        var paramList = List();
        foreach (var param in parameters)
        {
            paramList.NextItem();

            foreach (var attribute in param.Parameter.Attributes)
            {
                if (attribute.Target is AttributeTarget.Param)
                    WriteAttributeInline(attribute);
            }

            Writer.Write("{0} {1}", param.Parameter.Type.NetType, Identifier(ParameterCase(param.Parameter.Name)));

            if (!param.IsRequired && !string.IsNullOrEmpty(param.Parameter.DefaultValue))
                Writer.Write(" = {0}", param.Parameter.DefaultValue);
        }

        Writer.WriteLine(")");

        if (parameters.Count != 0 && parameters[0].IsFromBase)
        {
            using (Indent())
            {
                Writer.Write(": base(");

                paramList.Reset();
                foreach (var param in parameters)
                {
                    if (!param.IsFromBase)
                        break;

                    paramList.NextItem();
                    Writer.Write(Identifier(ParameterCase(param.Parameter.Name)));
                }

                Writer.WriteLine(")");
            }
        }

        using (Block())
        {
            foreach (var param in parameters)
            {
                if (param.IsFromBase)
                    continue;

                Writer.Write("{0} = {1}", Identifier(MemberCase(param.Parameter.Name)), Identifier(ParameterCase(param.Parameter.Name)));

                if (param.Parameter.Type.IsArray && !param.Parameter.Type.IsNullable)
                    Writer.Write(" ?? Array.Empty<{0}>()", param.Parameter.Type.GetRepeatedItemType()!.NetType);

                Writer.WriteLine(";");
            }
        }
    }

    private List<ParameterData> GetConstructorParameters(MessageDefinition message)
    {
        var result = new List<ParameterData>();

        var baseTypeName = message.BaseTypes.FirstOrDefault();

        while (baseTypeName != null)
        {
            if (!_messagesByName.TryGetValue(baseTypeName.NetType, out var baseType))
                break;

            if (!baseType.Options.Mutable)
            {
                var index = 0;

                foreach (var param in baseType.Parameters)
                {
                    if (IsConstructorParameter(param))
                        result.Insert(index++, new ParameterData(param, true));
                }
            }

            baseTypeName = baseType.BaseTypes.FirstOrDefault();
        }

        foreach (var param in message.Parameters)
        {
            if (IsConstructorParameter(param))
                result.Add(new ParameterData(param, false));
        }

        var requiredParameterSeen = false;

        for (var i = result.Count - 1; i >= 0; --i)
        {
            var param = result[i];

            if (string.IsNullOrEmpty(param.Parameter.DefaultValue))
                requiredParameterSeen = true;

            if (requiredParameterSeen)
                param.IsRequired = true;
        }

        return result;

        static bool IsConstructorParameter(ParameterDefinition parameter)
            => !parameter.IsWritableProperty;
    }

    private void WriteAttributeLine(AttributeDefinition attribute)
    {
        Writer.Write("[");
        WriteAttributeBody(attribute);
        Writer.WriteLine("]");
    }

    private void WriteAttributeInline(AttributeDefinition attribute)
    {
        Writer.Write("[");
        WriteAttributeBody(attribute);
        Writer.Write("] ");
    }

    private void WriteAttributeBody(AttributeDefinition attribute)
    {
        Writer.Write(Identifier(attribute.TypeName.NetType));

        if (!string.IsNullOrEmpty(attribute.Parameters))
            Writer.Write("({0})", attribute.Parameters);
    }

    private static string AccessModifier(AccessModifier accessModifier)
    {
        return accessModifier switch
        {
            Ast.AccessModifier.Public   => "public",
            Ast.AccessModifier.Internal => "internal",
            _                           => throw new ArgumentOutOfRangeException(nameof(accessModifier), accessModifier, null)
        };
    }

    private static string InheritanceModifier(InheritanceModifier inheritanceModifier)
    {
        return inheritanceModifier switch
        {
            Ast.InheritanceModifier.Default  => string.Empty,
            Ast.InheritanceModifier.None     => string.Empty,
            Ast.InheritanceModifier.Sealed   => "sealed",
            Ast.InheritanceModifier.Abstract => "abstract",
            _                                => throw new ArgumentOutOfRangeException(nameof(inheritanceModifier), inheritanceModifier, null)
        };
    }

    private static string Identifier(string id)
        => CSharpSyntax.Identifier(id);

    private class ParameterData(ParameterDefinition parameter, bool isFromBase)
    {
        public ParameterDefinition Parameter { get; } = parameter;
        public bool IsFromBase { get; } = isFromBase;
        public bool IsRequired { get; set; }
    }
}
