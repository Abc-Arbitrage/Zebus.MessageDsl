using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Analysis
{
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

                if (nextTag >= AstValidator.ProtoFirstReservedTag && nextTag <= AstValidator.ProtoLastReservedTag)
                    nextTag = AstValidator.ProtoLastReservedTag + 1;
            }
        }

        private static void ResolveEnumValues(EnumDefinition enumDef)
        {
            if (!enumDef.Options.Proto)
                return;

            if (enumDef.UnderlyingType.NetType != "int")
                return;

            var nextValue = (int?)0;

            foreach (var member in enumDef.Members)
            {
                member.ProtoValue = string.IsNullOrEmpty(member.Value)
                    ? nextValue
                    : enumDef.GetValidUnderlyingValue(member.Value) as int?;

                nextValue = member.ProtoValue + 1;
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
    }
}
