namespace Abc.Zebus.MessageDsl.Ast;

public enum AttributeTarget
{
    Default,

    // The lowercased names of these values are compared to the ones used in the message definition
    Type,
    Param,
    Property,
    Field
}
