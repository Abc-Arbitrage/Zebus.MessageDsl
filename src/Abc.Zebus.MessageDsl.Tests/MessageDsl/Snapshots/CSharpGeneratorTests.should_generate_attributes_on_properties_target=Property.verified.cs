﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using Abc.Zebus;
using ProtoBuf;

[ProtoContract]
[System.Diagnostics.DebuggerNonUserCode]
[System.CodeDom.Compiler.GeneratedCode("Abc.Zebus.MessageDsl", "1.2.3.4")]
public sealed partial class FooExecuted : IEvent
{
    [ProtoMember(1, IsRequired = true)]
    [Target]
    public int Foo { get; private set; }
    
    private FooExecuted()
    {
    }
    
    public FooExecuted(int foo)
    {
        Foo = foo;
    }
}
