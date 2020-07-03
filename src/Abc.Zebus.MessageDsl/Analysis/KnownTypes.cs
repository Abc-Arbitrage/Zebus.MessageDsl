using Abc.Zebus.MessageDsl.Ast;

namespace Abc.Zebus.MessageDsl.Analysis
{
    internal static class KnownTypes
    {
        public static TypeName EventInterface { get; } = new TypeName("IEvent");
        public static TypeName EventInterfaceFullName { get; } = new TypeName("Abc.Zebus.IEvent");

        public static TypeName CommandInterface { get; } = new TypeName("ICommand");
        public static TypeName CommandInterfaceFullName { get; } = new TypeName("Abc.Zebus.ICommand");

        public static TypeName MessageInterface { get; } = new TypeName("IMessage");

        public static TypeName RoutableAttribute { get; } = new TypeName("Routable");
        public static TypeName RoutingPositionAttribute { get; } = new TypeName("RoutingPosition");
        public static TypeName TransientAttribute { get; } = new TypeName("Transient");

        public static TypeName ProtoContractAttribute { get; } = new TypeName("ProtoContract");
        public static TypeName ProtoMemberAttribute { get; } = new TypeName("ProtoMember");
        public static TypeName ProtoMapAttribute { get; } = new TypeName("ProtoMap");

        public static TypeName ObsoleteAttribute { get; } = new TypeName("Obsolete");
        public static TypeName DescriptionAttribute { get; } = new TypeName("Description");
    }
}
