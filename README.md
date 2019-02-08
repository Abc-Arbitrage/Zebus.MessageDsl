# Zebus Message DSL

This is a DSL which simplifies writing of ProtoBuf contracts for [Zebus](https://github.com/Abc-Arbitrage/Zebus).

It is currently being ported from a single-file generator to a MSBuild task.

## Example

Input file:

```C#
SomeMessage(int foo, string bar)
```

Generated code (simplified):

```C#
[ProtoContract]
public sealed partial class SomeMessage : IEvent
{
    [ProtoMember(1, IsRequired = true)] public readonly int Foo;
    [ProtoMember(2, IsRequired = true)] public readonly string Bar;
    
    private SomeMessage() { }
    
    public SomeMessage(int foo, string bar)
    {
        Foo = foo;
        Bar = bar;
    }
}
```
