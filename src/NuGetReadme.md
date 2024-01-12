# Zebus Message DSL

This is a DSL which simplifies the writing of ProtoBuf contracts for [Zebus](https://github.com/Abc-Arbitrage/Zebus).

See the [GitHub repository](https://github.com/Abc-Arbitrage/Zebus.MessageDsl) for more information.

## Example

Input file:

```C#
SomeMessage(int foo, string[] bar)
```

Generated code:

```C#
[ProtoContract]
public sealed partial class SomeMessage : IEvent
{
    [ProtoMember(1, IsRequired = true)]
    public int Foo { get; private set; }
        
    [ProtoMember(2, IsRequired = false)]
    public string[] Bar { get; private set; }
        
    private SomeMessage()
    {
        Bar = Array.Empty<string>();
    }
        
    public SomeMessage(int foo, string[] bar)
    {
        Foo = foo;
        Bar = bar;
    }
}
```
