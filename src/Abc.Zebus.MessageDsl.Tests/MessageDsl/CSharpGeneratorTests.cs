using System;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Generator;
using Abc.Zebus.MessageDsl.Tests.TestTools;
using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tests.MessageDsl
{
    [TestFixture]
    public class CSharpGeneratorTests : GeneratorTests
    {
        [Test]
        public void should_generate_code()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("System.Int32", "foo"),
                    new ParameterDefinition("string", "bar")
                }
            });

            code.ShouldContain("public sealed partial class FooExecuted : IEvent");
            code.ShouldContain("public FooExecuted(int foo, string bar)");
        }

        [Test]
        public void should_generate_default_values()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("int", "foo") { DefaultValue = "42" }
                }
            });

            code.ShouldContain("public FooExecuted(int foo = 42)");
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
                    new ParameterDefinition("List<int>", "fooList"),
                    new ParameterDefinition("List<LolType>", "barList"),
                }
            });

            code.ShouldContainIgnoreIndent("[ProtoMember(1, IsRequired = false, IsPacked = true)]\npublic int[] Foo { get; private set; }");
            code.ShouldContainIgnoreIndent("[ProtoMember(2, IsRequired = false)]\npublic LolType[] Bar { get; private set; }");
            code.ShouldContainIgnoreIndent("[ProtoMember(3, IsRequired = false, IsPacked = true)]\npublic List<int> FooList { get; private set; }");
            code.ShouldContainIgnoreIndent("[ProtoMember(4, IsRequired = false)]\npublic List<LolType> BarList { get; private set; }");
            code.ShouldContain("Foo = Array.Empty<int>();");
            code.ShouldContain("Bar = Array.Empty<LolType>();");
            code.ShouldContain("FooList = new List<int>();");
            code.ShouldContain("BarList = new List<LolType>();");
            code.ShouldContain("using System.Collections.Generic;");
        }

        [Test]
        public void should_call_constructor_for_Dictionary()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("Dictionary<string,int>", "fooDico"),
                }
            });

            code.ShouldContainIgnoreIndent("[ProtoMember(1, IsRequired = true)]\n[ProtoMap(DisableMap = true)]\npublic Dictionary<string, int> FooDico { get; private set; }");
            code.ShouldContain("using System.Collections.Generic;");
            code.ShouldContain("FooDico = new Dictionary<string, int>();");
        }

        [Test]
        public void should_call_constructor_for_HashSet()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("HashSet<string>", "fooHashSet"),
                }
            });

            code.ShouldContainIgnoreIndent("[ProtoMember(1, IsRequired = false)]\npublic HashSet<string> FooHashSet { get; private set; }");
            code.ShouldContain("using System.Collections.Generic;");
            code.ShouldContain("FooHashSet = new HashSet<string>();");
        }

        [Test]
        public void should_generate_attributes()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Attributes =
                {
                    new AttributeDefinition("Transient")
                },
                Parameters =
                {
                    new ParameterDefinition("int", "foo")
                    {
                        Attributes =
                        {
                            new AttributeDefinition("LolAttribute", "LolParam = 42")
                        }
                    }
                }
            });

            code.ShouldContain("[Transient]");
            code.ShouldContain("Lol(LolParam = 42)");
        }

        [Test]
        public void should_generate_mutable_properties()
        {
            var contract = new ParsedContracts();

            contract.Messages.Add(new MessageDefinition
            {
                Name = "FooExecuted",
                Options = { Mutable = true },
                Parameters =
                {
                    new ParameterDefinition("System.Int32", "foo"),
                    new ParameterDefinition("string", "bar")
                }
            });
            var code = Generate(contract);

            code.ShouldContain("public int Foo { get; set; }");
        }

        [Test]
        public void should_generate_properties()
        {
            var contract = new ParsedContracts();

            contract.Messages.Add(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("System.Int32", "foo"),
                    new ParameterDefinition("string", "bar")
                }
            });
            var code = Generate(contract);

            code.ShouldContain("public int Foo { get; private set; }");
        }

        [Test]
        public void should_generate_generic_messages()
        {
            var contract = new ParsedContracts();

            contract.Messages.Add(new MessageDefinition
            {
                Name = "FooExecuted",
                GenericParameters = { "TFoo", "TBar" }
            });
            var code = Generate(contract);

            code.ShouldContain("class FooExecuted<TFoo, TBar> : IEvent");
        }

        [Test]
        public void should_generate_generic_constraints()
        {
            var contract = new ParsedContracts();

            contract.Messages.Add(new MessageDefinition
            {
                Name = "FooExecuted",
                GenericParameters = { "TFoo", "TBar" },
                GenericConstraints =
                {
                    new GenericConstraint
                    {
                        GenericParameterName = "TFoo",
                        IsClass = true,
                        HasDefaultConstructor = true,
                        Types = { "IDisposable" }
                    },
                    new GenericConstraint
                    {
                        GenericParameterName = "TBar",
                        IsStruct = true,
                        HasDefaultConstructor = true,
                        Types = { "IDisposable" }
                    },
                }
            });
            var code = Generate(contract);

            code.ShouldContain("where TFoo : class, IDisposable, new()");
            code.ShouldContain("where TBar : struct, IDisposable");
        }

        [Test]
        public void should_not_generate_constructor_when_there_are_no_parameters()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted"
            });

            code.ShouldNotContain("FooExecuted(");
        }

        [Test]
        public void should_handle_escaped_keywords()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "void",
                Parameters =
                {
                    new ParameterDefinition("int", "double"),
                    new ParameterDefinition("if", "else")
                },
                GenericParameters = { "float" },
                GenericConstraints =
                {
                    new GenericConstraint
                    {
                        GenericParameterName = "float",
                        Types = { "volatile" }
                    }
                }
            });

            code.ShouldContain("class @void<@float>");
            code.ShouldContain("where @float : @volatile");
            code.ShouldContain("public @void(int @double, @if @else)");
            code.ShouldContain("Double = @double;");
            code.ShouldContain("Else = @else;");
            code.ShouldContain("where @float : @volatile");
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
                            }
                        }
                    }
                }
            });

            code.ShouldContain("public enum Foo");
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
                        UnderlyingType = "short",
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
                                Value = "-2",
                                Attributes =
                                {
                                    new AttributeDefinition("Description(\"Beer!\")")
                                },
                            },
                            new EnumMemberDefinition
                            {
                                Name = "Baz",
                                Value = "Bar",
                                Attributes =
                                {
                                    new AttributeDefinition("EnumValueAttr")
                                },
                            }
                        }
                    }
                },
                Messages =
                {
                    new MessageDefinition
                    {
                        Name = "Test"
                    }
                }
            });

            code.ShouldContain("public enum Foo : short");
            code.ShouldContain("Default,");
            code.ShouldContain("Bar = -2,");
            code.ShouldContain("Baz = Bar");
            code.ShouldContain("[EnumAttr]");
            code.ShouldContain("[EnumValueAttr]");
        }

        [Test]
        public void should_handle_obsolete_attribute()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("int", "foo")
                }
            });

            code.ShouldNotContain("#pragma warning disable 612");

            code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("int", "foo")
                    {
                        Attributes = { new AttributeDefinition("Obsolete") }
                    }
                }
            });

            code.ShouldContain("#pragma warning disable 612");

            code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Attributes = { new AttributeDefinition("Obsolete") }
            });

            code.ShouldContain("#pragma warning disable 612");
        }

        [Test]
        public void should_handle_custom_contract_attribute()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Attributes = { new AttributeDefinition("ProtoContract", "EnumPassthru = true") }
            });

            code.ShouldContain("[ProtoContract(EnumPassthru = true)]");
            code.ShouldNotContain("[ProtoContract]");
        }

        [Test]
        public void should_handle_custom_member_attribute()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("string", "foo")
                    {
                        Attributes = { new AttributeDefinition("ProtoMember", "42, AsReference = true") }
                    }
                }
            });

            code.ShouldContain("ProtoMember(42, AsReference = true)");
            code.ShouldNotContain("ProtoMember(1");
        }

        [Test]
        public void should_handle_custom_contract_attribute_on_enums()
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
                            new AttributeDefinition("ProtoContract", "EnumPassthru = true")
                        },
                        Members =
                        {
                            new EnumMemberDefinition
                            {
                                Name = "Default"
                            }
                        }
                    }
                }
            });

            code.ShouldContain("[ProtoContract(EnumPassthru = true)]");
            code.ShouldNotContain("[ProtoContract]");
        }

        [Test]
        public void should_add_protomap_attribute_to_dictionaries()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("Dictionary<int, string>", "foo")
                }
            });

            code.ShouldContain("ProtoMap(DisableMap = true)");
        }

        [Test]
        public void should_leave_supplied_protomap()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("Dictionary<int, string>", "foo")
                    {
                        Attributes =
                        {
                            new AttributeDefinition("ProtoMap", "Foo = lol")
                        }
                    }
                }
            });

            code.ShouldContain("ProtoMap(Foo = lol)");
            code.ShouldNotContain("ProtoMap(DisableMap = true)");
        }

        [Test]
        public void should_generate_two_classes_with_same_name_and_different_arity()
        {
            var code = GenerateRaw(new ParsedContracts
            {
                Messages =
                {
                    new MessageDefinition { Name = "GenericCommand" },
                    new MessageDefinition { Name = "GenericCommand", GenericParameters = { "T" } }
                }
            });

            code.ShouldContain("public sealed partial class GenericCommand");
            code.ShouldContain("public sealed partial class GenericCommand<T>");
        }

        [Test]
        public void should_generate_internal_messages()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                AccessModifier = AccessModifier.Internal
            });

            code.ShouldContain("internal sealed partial class FooExecuted : IEvent");
        }

        [Test]
        public void should_generate_internal_enums()
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
                            }
                        },
                        AccessModifier = AccessModifier.Internal
                    }
                }
            });

            code.ShouldContain("internal enum Foo");
        }

        [Test]
        public void should_handle_nullable_reference_types()
        {
            var code = Generate(new ParsedContracts
            {
                Messages =
                {
                    new MessageDefinition { Name = "FooMessage" },
                    new MessageDefinition { Name = "BarMessage", Options = { Nullable = true } },
                    new MessageDefinition { Name = "BazMessage" }
                }
            });

            var fooIndex = code.IndexOf("FooMessage", StringComparison.Ordinal);
            var barIndex = code.IndexOf("BarMessage", StringComparison.Ordinal);
            var bazIndex = code.IndexOf("BazMessage", StringComparison.Ordinal);

            var nullableEnableIndex = code.IndexOf("#nullable enable", StringComparison.Ordinal);
            var nullableDisableIndex = code.IndexOf("#nullable disable", StringComparison.Ordinal);

            foreach (var index in new[] { fooIndex, barIndex, bazIndex, nullableEnableIndex, nullableDisableIndex })
                index.ShouldBeGreaterThan(0);

            nullableEnableIndex.ShouldBeBetween(fooIndex, barIndex);
            nullableDisableIndex.ShouldBeBetween(barIndex, bazIndex);
        }

        [Test]
        public void should_generate_initializers_for_nullable_reference_types()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("string", "strNotNull"),
                    new ParameterDefinition("string?", "strNull"),
                    new ParameterDefinition("int[]", "arrayNotNull"),
                    new ParameterDefinition("int[]?", "arrayNull")
                },
                Options = { Nullable = true }
            });

            code.ShouldContain("StrNotNull = default!;");
            code.ShouldNotContain("StrNull = default!;");
            code.ShouldContain("ArrayNotNull = Array.Empty<int>();");
            code.ShouldNotContain("ArrayNull = Array.Empty<int>();");
        }

        [Test]
        public void should_not_generate_initializers_for_known_nullable_value_types()
        {
            var code = Generate(new MessageDefinition
            {
                Name = "FooExecuted",
                Parameters =
                {
                    new ParameterDefinition("int", "intNotNull"),
                    new ParameterDefinition("int?", "intNull"),
                    new ParameterDefinition("int[]", "arrayNotNull"),
                    new ParameterDefinition("int[]?", "arrayNull")
                },
                Options = { Nullable = true }
            });

            code.ShouldNotContain("IntNotNull = default!;");
            code.ShouldNotContain("IntNull = default!;");
            code.ShouldContain("ArrayNotNull = Array.Empty<int>();");
            code.ShouldNotContain("ArrayNull = Array.Empty<int>();");
        }

        protected override string GenerateRaw(ParsedContracts contracts) => CSharpGenerator.Generate(contracts);
    }
}
