using System.Collections.Generic;

namespace Abc.Zebus.MessageDsl.Ast
{
    public class MessageDefinition : AstNode, IMemberNode
    {
        private MemberOptions? _options;

        public string Name { get; set; } = default!;
        public AccessModifier AccessModifier { get; set; }
        public IList<string> GenericParameters { get; } = new List<string>();
        public IList<GenericConstraint> GenericConstraints { get; } = new List<GenericConstraint>();

        public IList<ParameterDefinition> Parameters { get; } = new List<ParameterDefinition>();
        public ICollection<TypeName> BaseTypes { get; } = new HashSet<TypeName>();
        public AttributeSet Attributes { get; } = new AttributeSet();

        public MemberOptions Options
        {
            get => _options ??= new MemberOptions();
            set => _options = value;
        }

        public bool IsCustom { get; set; }
        public bool IsTransient { get; set; }
        public bool IsRoutable { get; set; }

        public MessageType Type
        {
            get
            {
                if (IsCustom)
                    return MessageType.Custom;

                if (BaseTypes.Contains("ICommand") || BaseTypes.Contains("Abc.Zebus.ICommand"))
                    return MessageType.Command;

                if (BaseTypes.Contains("IEvent") || BaseTypes.Contains("IDomainEvent") || BaseTypes.Contains("Abc.Zebus.IEvent"))
                    return MessageType.Event;

                if (Name.EndsWith("Command"))
                    return MessageType.Command;

                return MessageType.Event;
            }
        }

        public override string ToString() => Name;
    }
}
