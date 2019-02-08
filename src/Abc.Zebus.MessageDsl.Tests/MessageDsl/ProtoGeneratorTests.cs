using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Generator;
using Abc.Zebus.MessageDsl.Tests.TestTools;
using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tests.MessageDsl
{
    [TestFixture]
    public class ProtoGeneratorTests : GeneratorTests
    {
        [Test]
        public void should_generate_code()
        {
            var code = Generate(new MessageDefinition
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
        public void should_handle_deprecated_fields()
        {
            var code = Generate(new MessageDefinition
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
        public void should_generate_packed_members()
        {
            var code = Generate(new MessageDefinition
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
        public void should_generate_simple_enums()
        {
            var code = Generate(new ParsedContracts
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
        public void should_generate_enums()
        {
            var code = Generate(new ParsedContracts
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

        protected override string GenerateRaw(ParsedContracts contracts)
        {
            return ProtoGenerator.Generate(contracts);
        }
    }
}
