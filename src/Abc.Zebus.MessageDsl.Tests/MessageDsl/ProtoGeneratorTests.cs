using System.Threading.Tasks;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Generator;
using Abc.Zebus.MessageDsl.Tests.TestTools;
using EmptyFiles;
using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tests.MessageDsl;

[TestFixture]
public class ProtoGeneratorTests : GeneratorTests
{
    static ProtoGeneratorTests()
        => FileExtensions.AddTextExtension("proto");

    [Test]
    public async Task should_generate_code()
    {
        var code = await Verify(new MessageDefinition
        {
            Name = "FooExecuted",
            Parameters =
            {
                new ParameterDefinition("int?", "foo"),
                new ParameterDefinition("string", "bar"),
                new ParameterDefinition("BarBaz", "baz")
            },
            Options =
            {
                Proto = true
            }
        });

        code.ShouldContain("optional int32 Foo = 1;");
        code.ShouldContain("required string Bar = 2;");
        code.ShouldContain("required BarBaz Baz = 3;");
    }

    [Test]
    public async Task should_handle_deprecated_fields()
    {
        var code = await Verify(new MessageDefinition
        {
            Name = "FooExecuted",
            Parameters =
            {
                new ParameterDefinition("int", "foo")
                {
                    Attributes = { new AttributeDefinition("Obsolete") }
                }
            },
            Options =
            {
                Proto = true
            }
        });

        code.ShouldContain("required int32 Foo = 1 [deprecated = true];");
    }

    [Test]
    public async Task should_generate_packed_members()
    {
        var code = await Verify(new MessageDefinition
        {
            Name = "FooExecuted",
            Parameters =
            {
                new ParameterDefinition("System.Int32[]", "foo"),
                new ParameterDefinition("LolType[]", "bar"),
                new ParameterDefinition("List<System.Int32>", "fooList"),
                new ParameterDefinition("System.Collections.Generic.List<LolType>", "barList"),
            },
            Options =
            {
                Proto = true
            }
        });

        code.ShouldContain("repeated int32 Foo = 1 [packed = true];");
        code.ShouldContain("repeated LolType Bar = 2;");
        code.ShouldContain("repeated int32 FooList = 3 [packed = true];");
        code.ShouldContain("repeated LolType BarList = 4;");
    }

    [Test]
    public async Task should_generate_simple_enums()
    {
        var code = await Verify(new ParsedContracts
        {
            Enums =
            {
                new EnumDefinition
                {
                    Name = "Foo",
                    Members =
                    {
                        new EnumMemberDefinition
                        {
                            Name = "Default"
                        },
                        new EnumMemberDefinition
                        {
                            Name = "Bar",
                            Value = "-2"
                        }
                    },
                    Options =
                    {
                        Proto = true
                    }
                }
            }
        });

        code.ShouldContain("enum Foo {");
        code.ShouldNotContain("option allow_alias = true;");
        code.ShouldContain("Default = 0;");
        code.ShouldContain("Bar = -2;");
    }

    [Test]
    public async Task should_generate_enums()
    {
        var code = await Verify(new ParsedContracts
        {
            Enums =
            {
                new EnumDefinition
                {
                    Name = "Foo",
                    Attributes =
                    {
                        new AttributeDefinition("EnumAttr")
                    },
                    Members =
                    {
                        new EnumMemberDefinition
                        {
                            Name = "Default"
                        },
                        new EnumMemberDefinition
                        {
                            Name = "Bar",
                            Value = "-2"
                        },
                        new EnumMemberDefinition
                        {
                            Name = "Baz"
                        },
                        new EnumMemberDefinition
                        {
                            Name = "Alias"
                        }
                    },
                    Options =
                    {
                        Proto = true
                    }
                }
            }
        });

        code.ShouldContain("enum Foo {");
        code.ShouldContain("option allow_alias = true;");
        code.ShouldContain("Default = 0;");
        code.ShouldContain("Bar = -2;");
        code.ShouldContain("Baz = -1;");
        code.ShouldContain("Alias = 0;");
    }

    [Test]
    public async Task should_handle_message_inheritance()
    {
        var code = await Verify(new MessageDefinition
        {
            Name = "MsgA",
            Attributes =
            {
                new AttributeDefinition("ProtoInclude", "10, typeof(MsgB)"),
                new AttributeDefinition("ProtoInclude", "11, typeof(MsgC)")
            },
            Options =
            {
                Proto = true
            }
        });

        code.ShouldContain("optional MsgB _subTypeMsgB = 10;");
        code.ShouldContain("optional MsgC _subTypeMsgC = 11;");
    }

    protected override string SnapshotExtension => "proto";

    protected override string GenerateRaw(ParsedContracts contracts)
        => ProtoGenerator.Generate(contracts);
}
