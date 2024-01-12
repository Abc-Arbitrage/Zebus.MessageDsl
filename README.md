# Zebus Message DSL

[![Build](https://github.com/Abc-Arbitrage/Zebus.MessageDsl/workflows/Build/badge.svg)](https://github.com/Abc-Arbitrage/Zebus.MessageDsl/actions?query=workflow%3ABuild)
[![Zebus.MessageDsl NuGet](https://img.shields.io/nuget/v/Zebus.MessageDsl.svg?label=Zebus.MessageDsl&logo=NuGet)](https://www.nuget.org/packages/Zebus.MessageDsl)
[![Zebus.MessageDsl.Build NuGet](https://img.shields.io/nuget/v/Zebus.MessageDsl.Build.svg?label=Zebus.MessageDsl.Build&logo=NuGet)](https://www.nuget.org/packages/Zebus.MessageDsl.Build)
[![Zebus.MessageDsl.Generator NuGet](https://img.shields.io/nuget/v/Zebus.MessageDsl.Generator.svg?label=Zebus.MessageDsl.Generator&logo=NuGet)](https://www.nuget.org/packages/Zebus.MessageDsl.Generator)

This is a DSL which simplifies the writing of ProtoBuf contracts for [Zebus](https://github.com/Abc-Arbitrage/Zebus).

## NuGet packages

 - [`Zebus.MessageDsl`](https://www.nuget.org/packages/Zebus.MessageDsl) provides the DSL parser, C# and proto generators
 - [`Zebus.MessageDsl.Build`](https://www.nuget.org/packages/Zebus.MessageDsl.Build) provides a MSBuild code generator which will translate `.msg` files in your project
 - [`Zebus.MessageDsl.Generator`](https://www.nuget.org/packages/Zebus.MessageDsl.Generator) provides a Roslyn source generator which will translate `.msg` files in your project

## Documentation

 - [DSL Syntax](docs/Syntax.md)
 - [Build-Time Code Generator](docs/BuildTimeCodeGen.md) (`.msg` files)

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
