using System.Collections.Generic;
using Abc.Zebus.MessageDsl.Analysis;

namespace Abc.Zebus.MessageDsl.Ast
{
    public class MessageDefinition : AstNode, IClassNode
    {
        private MemberOptions? _options;

        public string Name { get; set; } = default!;
        public AccessModifier AccessModifier { get; set; }
        public InheritanceModifier InheritanceModifier { get; set; }
        public IList<string> GenericParameters { get; } = new List<string>();
        public IList<GenericConstraint> GenericConstraints { get; } = new List<GenericConstraint>();
        public IList<TypeName> ContainingClasses { get; } = new List<TypeName>();

        public IList<ParameterDefinition> Parameters { get; } = new List<ParameterDefinition>();
        public IList<TypeName> BaseTypes { get; } = new List<TypeName>();
        public AttributeSet Attributes { get; } = new();

        public MemberOptions Options
        {
            get => _options ??= new MemberOptions();
            set => _options = value;
        }

        public bool IsCustom { get; set; }
        public bool IsTransient { get; set; }
        public bool IsRoutable { get; set; }
        public bool IsInterface { get; set; }

        public MessageType Type
        {
            get
            {
                if (IsInterface)
                    return MessageType.None;

                if (IsCustom)
                    return MessageType.Custom;

                if (BaseTypes.Contains(KnownTypes.CommandInterface) || BaseTypes.Contains(KnownTypes.CommandInterfaceFullName))
                    return MessageType.Command;

                if (BaseTypes.Contains(KnownTypes.EventInterface) || BaseTypes.Contains(KnownTypes.EventInterfaceFullName))
                    return MessageType.Event;

                if (Name.EndsWith("Command"))
                    return MessageType.Command;

                return MessageType.Event;
            }
        }

        public override string ToString() => Name;
    }
}
