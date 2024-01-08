using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Analysis;

internal static class KnownTypes
{
    public static TypeName EventInterface { get; } = new("IEvent");
    public static TypeName EventInterfaceFullName { get; } = new("Abc.Zebus.IEvent");

    public static TypeName CommandInterface { get; } = new("ICommand");
    public static TypeName CommandInterfaceFullName { get; } = new("Abc.Zebus.ICommand");

    public static TypeName MessageInterface { get; } = new("IMessage");

    public static TypeName RoutableAttribute { get; } = new("Routable");
    public static TypeName RoutingPositionAttribute { get; } = new("RoutingPosition");
    public static TypeName TransientAttribute { get; } = new("Transient");

    public static TypeName ProtoContractAttribute { get; } = new("ProtoContract");
    public static TypeName ProtoMemberAttribute { get; } = new("ProtoMember");
    public static TypeName ProtoMapAttribute { get; } = new("ProtoMap");
    public static TypeName ProtoIncludeAttribute { get; } = new("ProtoInclude");

    public static TypeName ObsoleteAttribute { get; } = new("Obsolete");
    public static TypeName DescriptionAttribute { get; } = new("Description");
}
