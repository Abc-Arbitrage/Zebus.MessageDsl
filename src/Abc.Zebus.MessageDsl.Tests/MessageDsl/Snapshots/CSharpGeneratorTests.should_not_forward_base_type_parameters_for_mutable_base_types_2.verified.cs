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
public sealed partial class FooMessage : BarMessage, IEvent
{
    [ProtoMember(1, IsRequired = true)]
    public int FooA { get; private set; }
    
    private FooMessage()
    {
    }
    
    public FooMessage(int fooC, int fooA)
        : base(fooC)
    {
        FooA = fooA;
    }
}

[ProtoContract]
[System.Diagnostics.DebuggerNonUserCode]
[System.CodeDom.Compiler.GeneratedCode("Abc.Zebus.MessageDsl", "1.2.3.4")]
public abstract partial class BarMessage : BazMessage, IEvent
{
    [ProtoMember(1, IsRequired = true)]
    public int FooB { get; set; }
    
    protected BarMessage()
    {
    }
    
    protected BarMessage(int fooC)
        : base(fooC)
    {
    }
    
    protected BarMessage(int fooC, int fooB)
        : base(fooC)
    {
        FooB = fooB;
    }
}

[ProtoContract]
[System.Diagnostics.DebuggerNonUserCode]
[System.CodeDom.Compiler.GeneratedCode("Abc.Zebus.MessageDsl", "1.2.3.4")]
public abstract partial class BazMessage : IEvent
{
    [ProtoMember(1, IsRequired = true)]
    public int FooC { get; private set; }
    
    protected BazMessage()
    {
    }
    
    protected BazMessage(int fooC)
    {
        FooC = fooC;
    }
}
