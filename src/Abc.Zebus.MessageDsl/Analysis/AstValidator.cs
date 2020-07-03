using System.Collections.Generic;
using System.Linq;
using Abc.Zebus.MessageDsl.Ast;
using Antlr4.Runtime;

namespace Abc.Zebus.MessageDsl.Analysis
{
    internal class AstValidator
    {
        public const int ProtoMinTag = 1;
        public const int ProtoMaxTag = 536870911;
        public const int ProtoFirstReservedTag = 19000;
        public const int ProtoLastReservedTag = 19999;

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
                    _contracts.AddError(message.ParseContext, "Cannot generate .proto for generic message {0}", message.Name);
            }

            ValidateAttributes(message.Attributes);
            ValidateTags(message);

            foreach (var param in message.Parameters)
            {
                var errorContext = param.ParseContext ?? message.ParseContext;

                if (!paramNames.Add(param.Name))
                    _contracts.AddError(errorContext, "Duplicate parameter name: {0}", param.Name);

                ValidateType(param.Type, param.ParseContext);
                ValidateAttributes(param.Attributes);
            }

            foreach (var constraint in message.GenericConstraints)
            {
                var errorContext = constraint.ParseContext ?? message.ParseContext;

                if (!genericConstraints.Add(constraint.GenericParameterName))
                    _contracts.AddError(errorContext, "Duplicate generic constraint: '{0}'", constraint.GenericParameterName);

                if (!message.GenericParameters.Contains(constraint.GenericParameterName))
                    _contracts.AddError(errorContext, "Undefined generic parameter: '{0}'", constraint.GenericParameterName);

                if (constraint.IsClass && constraint.IsStruct)
                    _contracts.AddError(errorContext, "Constraint on '{0}' cannot require both class and struct", constraint.GenericParameterName);

                foreach (var constraintType in constraint.Types)
                    ValidateType(constraintType, message.ParseContext);
            }

            foreach (var baseType in message.BaseTypes)
                ValidateType(baseType, message.ParseContext);
        }

        private void ValidateTags(MessageDefinition message)
        {
            var tags = new HashSet<int>();

            foreach (var param in message.Parameters)
            {
                var errorContext = param.ParseContext ?? message.ParseContext;

                if (!IsValidTag(param.Tag))
                    _contracts.AddError(errorContext, "Tag for parameter '{0}' is not within the valid range ({1})", param.Name, param.Tag);

                if (!tags.Add(param.Tag))
                    _contracts.AddError(errorContext, "Duplicate tag {0} on parameter {1}", param.Tag, param.Name);
            }

            foreach (var attr in message.Attributes)
            {
                if (!Equals(attr.TypeName, KnownTypes.ProtoIncludeAttribute))
                    continue;

                var errorContext = attr.ParseContext ?? message.ParseContext;

                if (!AttributeInterpretor.TryParseProtoInclude(attr, out var tag, out _))
                {
                    _contracts.AddError(errorContext, "Invalid [{0}] parameters", KnownTypes.ProtoIncludeAttribute);
                    continue;
                }

                if (!IsValidTag(tag))
                    _contracts.AddError(errorContext, "Tag for [{0}] is not within the valid range ({1})", KnownTypes.ProtoIncludeAttribute, tag);

                if (!tags.Add(tag))
                    _contracts.AddError(errorContext, "Duplicate tag {0} on [{1}]", tag, KnownTypes.ProtoIncludeAttribute);
            }
        }

        private void ValidateEnum(EnumDefinition enumDef)
        {
            if (!enumDef.IsValidUnderlyingType())
                _contracts.AddError(enumDef.ParseContext, "Invalid underlying type: {0}", enumDef.UnderlyingType);

            if (enumDef.Options.Proto && enumDef.UnderlyingType.NetType != "int")
                _contracts.AddError(enumDef.ParseContext, "An enum used in a proto file must have an underlying type of int");

            ValidateAttributes(enumDef.Attributes);

            var definedMembers = new HashSet<string>();

            foreach (var member in enumDef.Members)
            {
                if (!definedMembers.Add(member.Name))
                    _contracts.AddError(member.ParseContext, "Duplicate enum member: {0}", member.Name);

                ValidateAttributes(member.Attributes);
            }
        }

        private void ValidateAttributes(AttributeSet attributes)
        {
            foreach (var attribute in attributes)
                ValidateType(attribute.TypeName, attribute.ParseContext);
        }

        private void ValidateType(TypeName type, ParserRuleContext? context)
        {
            if (type.NetType.Contains("??"))
                _contracts.AddError(context, "Invalid type: {0}", type.NetType);
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
                    _contracts.AddError(typeNode.ParseContext, "Duplicate type name: {0}", nameWithGenericArity);
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
        {
            return tag >= ProtoMinTag
                   && tag <= ProtoMaxTag
                   && (tag < ProtoFirstReservedTag || tag > ProtoLastReservedTag);
        }
    }
}
