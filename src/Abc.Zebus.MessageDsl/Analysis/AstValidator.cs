using System.Collections.Generic;
using System.Linq;
using Abc.Zebus.MessageDsl.Ast;
using Antlr4.Runtime;

namespace Abc.Zebus.MessageDsl.Analysis;

internal class AstValidator
{
    public const int ProtoMinTag = 1;
    public const int ProtoMaxTag = 536870911;
    public const int ProtoFirstReservedTag = 19000;
    public const int ProtoLastReservedTag = 19999;

    private static readonly AttributeTarget[] _validAttributeTargetsMessage = [AttributeTarget.Type];
    private static readonly AttributeTarget[] _validAttributeTargetsMessageMember = [AttributeTarget.Param, AttributeTarget.Property];
    private static readonly AttributeTarget[] _validAttributeTargetsEnum = [AttributeTarget.Type];
    private static readonly AttributeTarget[] _validAttributeTargetsEnumMember = [AttributeTarget.Field];

    private readonly ParsedContracts _contracts;

    public AstValidator(ParsedContracts contracts)
    {
        _contracts = contracts;
    }

    public void Validate()
    {
        foreach (var message in _contracts.Messages)
            ValidateMessage(message);

        foreach (var enumDef in _contracts.Enums)
            ValidateEnum(enumDef);

        DetectDuplicateTypes();
    }

    private void ValidateMessage(MessageDefinition message)
    {
        var paramNames = new HashSet<string>();
        var genericConstraints = new HashSet<string>();

        if (message.Options.Proto)
        {
            if (message.GenericParameters.Count > 0)
                _contracts.AddError(message.ParseContext, $"Cannot generate .proto for generic message {message.Name}");
        }

        ValidateAttributes(message.Attributes, _validAttributeTargetsMessage);
        ValidateTags(message);

        foreach (var param in message.Parameters)
        {
            if (param.IsDiscarded)
                continue;

            var errorContext = param.ParseContext ?? message.ParseContext;

            if (!paramNames.Add(param.Name))
                _contracts.AddError(errorContext, $"Duplicate parameter name: {param.Name}");

            ValidateType(param.Type, param.ParseContext);
            ValidateAttributes(param.Attributes, _validAttributeTargetsMessageMember);
        }

        var requiredParameterSeen = false;

        for (var i = message.Parameters.Count - 1; i >= 0; --i)
        {
            var param = message.Parameters[i];
            var errorContext = param.ParseContext ?? message.ParseContext;

            if (param.IsDiscarded)
                continue;

            if (string.IsNullOrEmpty(param.DefaultValue))
                requiredParameterSeen = true;
            else if (requiredParameterSeen)
                _contracts.AddError(errorContext, $"Optional parameter {param.Name} cannot appear before a required parameter");
        }

        foreach (var constraint in message.GenericConstraints)
        {
            var errorContext = constraint.ParseContext ?? message.ParseContext;

            if (!genericConstraints.Add(constraint.GenericParameterName))
                _contracts.AddError(errorContext, $"Duplicate generic constraint: '{constraint.GenericParameterName}'");

            if (!message.GenericParameters.Contains(constraint.GenericParameterName))
                _contracts.AddError(errorContext, $"Undefined generic parameter: '{constraint.GenericParameterName}'");

            if (constraint.IsClass && constraint.IsStruct)
                _contracts.AddError(errorContext, $"Constraint on '{constraint.GenericParameterName}' cannot require both class and struct");

            foreach (var constraintType in constraint.Types)
                ValidateType(constraintType, message.ParseContext);
        }

        foreach (var baseType in message.BaseTypes)
            ValidateType(baseType, message.ParseContext);

        ValidateInheritance(message);
    }

    private void ValidateTags(MessageDefinition message)
    {
        var tags = new HashSet<int>();

        foreach (var param in message.Parameters)
        {
            if (param.IsDiscarded)
                continue;

            var errorContext = param.ParseContext ?? message.ParseContext;

            if (!IsValidTag(param.Tag))
                _contracts.AddError(errorContext, $"Tag for parameter '{param.Name}' is not within the valid range ({param.Tag})");

            if (!tags.Add(param.Tag))
                _contracts.AddError(errorContext, $"Duplicate tag {param.Tag} on parameter {param.Name}");

            if (message.ReservedRanges.Any(range => range.Contains(param.Tag)))
                _contracts.AddError(errorContext, $"Tag {param.Tag} of parameter {param.Name} is reserved");
        }

        foreach (var attr in message.Attributes)
        {
            if (!Equals(attr.TypeName, KnownTypes.ProtoIncludeAttribute))
                continue;

            var errorContext = attr.ParseContext ?? message.ParseContext;

            if (!AttributeInterpreter.TryParseProtoInclude(attr, out var tag, out _))
            {
                _contracts.AddError(errorContext, $"Invalid [{KnownTypes.ProtoIncludeAttribute}] parameters");
                continue;
            }

            if (!IsValidTag(tag))
                _contracts.AddError(errorContext, $"Tag for [{KnownTypes.ProtoIncludeAttribute}] is not within the valid range ({tag})");

            if (!tags.Add(tag))
                _contracts.AddError(errorContext, $"Duplicate tag {tag} on [{KnownTypes.ProtoIncludeAttribute}]");
        }
    }

    private void ValidateEnum(EnumDefinition enumDef)
    {
        if (!enumDef.IsValidUnderlyingType())
            _contracts.AddError(enumDef.ParseContext, $"Invalid underlying type: {enumDef.UnderlyingType}");

        if (enumDef.Options.Proto && enumDef.UnderlyingType.NetType != "int")
            _contracts.AddError(enumDef.ParseContext, "An enum used in a proto file must have an underlying type of int");

        ValidateAttributes(enumDef.Attributes, _validAttributeTargetsEnum);

        var definedMembers = new HashSet<string>();

        foreach (var member in enumDef.Members)
        {
            if (!definedMembers.Add(member.Name))
                _contracts.AddError(member.ParseContext, $"Duplicate enum member: {member.Name}");

            ValidateAttributes(member.Attributes, _validAttributeTargetsEnumMember);
        }
    }

    private void ValidateAttributes(AttributeSet attributes, AttributeTarget[] validTargets)
    {
        foreach (var attribute in attributes)
        {
            ValidateType(attribute.TypeName, attribute.ParseContext);

            if (attribute.Target != AttributeTarget.Default && !validTargets.Contains(attribute.Target))
                _contracts.AddError(attribute.ParseContext, $"Invalid target for attribute: {attribute.Target.ToString().ToLowerInvariant()}, valid targets in this context: {string.Join(", ", validTargets.Select(i => i.ToString().ToLowerInvariant()))}");
        }
    }

    private void ValidateType(TypeName type, ParserRuleContext? context)
    {
        if (type.NetType.Contains("??"))
            _contracts.AddError(context, $"Invalid type: {type.NetType}");
    }

    private void ValidateInheritance(MessageDefinition message)
    {
        if (message.BaseTypes.Count == 0)
            return;

        var seenTypes = new HashSet<TypeName>
        {
            message.Name
        };

        var currentMessage = message;

        while (true)
        {
            if (currentMessage.BaseTypes.Count == 0)
                break;

            currentMessage = _contracts.Messages.FirstOrDefault(m => m.Name == currentMessage.BaseTypes[0].NetType);
            if (currentMessage is null)
                break;

            if (!seenTypes.Add(currentMessage.Name))
            {
                _contracts.AddError(message.ParseContext, "There is a loop in the inheritance chain");
                break;
            }
        }
    }

    private void DetectDuplicateTypes()
    {
        var seenTypes = new HashSet<string>();
        var duplicates = new HashSet<string>();

        var types = _contracts.Messages
                              .Cast<AstNode>()
                              .Concat(_contracts.Enums)
                              .ToList();

        foreach (var typeNode in types)
        {
            var nameWithGenericArity = GetNameWithGenericArity(typeNode);

            if (!seenTypes.Add(nameWithGenericArity))
                duplicates.Add(nameWithGenericArity);
        }

        foreach (var typeNode in types)
        {
            var nameWithGenericArity = GetNameWithGenericArity(typeNode);

            if (duplicates.Contains(nameWithGenericArity))
                _contracts.AddError(typeNode.ParseContext, $"Duplicate type name: {nameWithGenericArity}");
        }

        static string GetNameWithGenericArity(AstNode node)
        {
            var name = ((INamedNode)node).Name;
            if (node is MessageDefinition messageDef && messageDef.GenericParameters.Count > 0)
                name = $"{name}`{messageDef.GenericParameters.Count}";

            return name;
        }
    }

    public static bool IsValidTag(int tag)
        => tag is >= ProtoMinTag and <= ProtoMaxTag and (< ProtoFirstReservedTag or > ProtoLastReservedTag);
}
