﻿namespace Abc.Zebus.MessageDsl.Ast;

public class ParameterDefinition : AstNode, INamedNode
{
    public int Tag { get; set; }
    public string Name { get; set; } = string.Empty;
    public TypeName Type { get; set; } = TypeName.Empty;
    public bool IsMarkedOptional { get; set; }
    public string? DefaultValue { get; set; }
    public bool IsWritableProperty { get; set; }
    public AttributeSet Attributes { get; private set; } = new();

    internal bool IsDiscarded { get; set; }

    public FieldRules Rules
    {
        get
        {
            if (Type.IsRepeated)
                return FieldRules.Repeated;

            if (IsMarkedOptional || Type.IsNullable)
                return FieldRules.Optional;

            return FieldRules.Required;
        }
    }

    public bool IsPacked => Rules == FieldRules.Repeated && Type.IsPackable;

    public int? RoutingPosition { get; set; }

    public ParameterDefinition()
    {
    }

    internal ParameterDefinition(TypeName type, string name)
        : this()
    {
        Type = type;
        Name = name;
    }

    public ParameterDefinition Clone()
        => new()
        {
            Tag = Tag,
            Name = Name,
            Type = Type,
            IsMarkedOptional = IsMarkedOptional,
            DefaultValue = DefaultValue,
            Attributes = Attributes.Clone(),
            IsDiscarded = IsDiscarded,
            ParseContext = ParseContext
        };

    public override string ToString()
        => IsDiscarded ? "_" : $"{Type} {Name}";
}
