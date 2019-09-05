using System;
using System.Linq;
using Abc.Zebus.MessageDsl.Analysis;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Tests.TestTools;
using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tests.MessageDsl
{
    [TestFixture]
    public class ParsedContractsTests
    {
        [Test]
        public void should_parse_simple_contracts()
        {
            var contracts = ParseValid(@"FooCommand(int id); FooExecuted(int id, bool success = true);");

            contracts.Errors.ShouldBeEmpty();
            contracts.Namespace.ShouldEqual("Some.Namespace");
            contracts.ImportedNamespaces.ShouldContain("System");
            contracts.ImportedNamespaces.ShouldContain("ProtoBuf");
            contracts.ImportedNamespaces.ShouldContain("Abc.Zebus");

            contracts.Messages.Count.ShouldEqual(2);

            var cmd = contracts.Messages[0];
            var evt = contracts.Messages[1];

            cmd.Name.ShouldEqual("FooCommand");
            cmd.IsCustom.ShouldBeFalse();
            cmd.Type.ShouldEqual(MessageType.Command);
            cmd.Parameters.Count.ShouldEqual(1);
            cmd.Parameters[0].Name.ShouldEqual("id");
            cmd.Parameters[0].Type.ShouldEqual(new TypeName("int"));

            evt.Name.ShouldEqual("FooExecuted");
            evt.IsCustom.ShouldBeFalse();
            evt.Type.ShouldEqual(MessageType.Event);
            evt.Parameters.Count.ShouldEqual(2);
            evt.Parameters[0].Name.ShouldEqual("id");
            evt.Parameters[0].Type.ShouldEqual(new TypeName("int"));
            evt.Parameters[1].Name.ShouldEqual("success");
            evt.Parameters[1].Type.ShouldEqual(new TypeName("bool"));
            evt.Parameters[1].DefaultValue.ShouldEqual("true");
        }

        [Test]
        public void should_detect_duplicate_names()
        {
            ParseInvalid("Foo(); Foo();");
            ParseInvalid("Foo(); enum Foo();");
            ParseInvalid("enum Foo(); enum Foo();");
        }

        [Test]
        public void should_handle_separators()
        {
            ParseValid("Foo()");
            ParseValid("Foo();");
            ParseValid(";;;;");
            ParseValid("Foo();Bar();");
            ParseValid("Foo()\r\nBar()");
            ParseValid("\r\nFoo()\r\n\r\nBar()\r\n");
            ParseInvalid("Foo() Bar()");
        }

        [Test]
        public void should_handle_usings()
        {
            var contracts = ParseValid("using Foo.Bar; Foo();");
            contracts.ImportedNamespaces.ShouldContain("Foo.Bar");
        }

        [Test]
        public void should_disallow_usings_after_messages()
        {
            var contracts = ParseInvalid(" Foo(); using Foo.Bar;");
            ShouldContainError(contracts, "top of the file");
        }

        [Test]
        public void should_handle_attributes()
        {
            var contracts = ParseValid(@"[Transient, System.ObsoleteAttribute(""No good"")] FooExecuted([LolAttribute] int id);");

            var msg = contracts.Messages.ExpectedSingle();
            msg.Attributes.Count.ShouldEqual(2);
            msg.Attributes[0].TypeName.ShouldEqual(new TypeName("Transient"));
            msg.Attributes[1].TypeName.ShouldEqual(new TypeName("Obsolete"));
            msg.Attributes[1].Parameters.ShouldEqual("\"No good\"");
            msg.Parameters[0].Attributes.Count.ShouldEqual(1);
            msg.Parameters[0].Attributes[0].TypeName.ShouldEqual(new TypeName("Lol"));
        }

        [Test]
        public void should_set_message_as_transient()
        {
            var contracts = ParseValid(@"[Transient] FooExecuted(int id);");

            var msg = contracts.Messages.ExpectedSingle();
            msg.Attributes.Count.ShouldEqual(1);
            msg.Attributes[0].TypeName.ShouldEqual(new TypeName("Transient"));
            msg.IsTransient.ShouldBeTrue();
        }

        [Test]
        public void should_set_message_as_routable()
        {
            var contracts = ParseValid(@"[Routable] FooExecuted([RoutingPosition(1)] int id, [RoutingPosition(2)] int id2);");

            var msg = contracts.Messages.ExpectedSingle();
            msg.IsRoutable.ShouldBeTrue();
            msg.Parameters[0].RoutingPosition.ShouldEqual(1);
            msg.Parameters[1].RoutingPosition.ShouldEqual(2);
        }

        [Test]
        public void should_detect_duplicated_routing_position()
        {
            ParseInvalid(@"[Routable] FooExecuted([RoutingPosition(1)] int id, [RoutingPosition(1)] int id2);");
        }

        [Test]
        public void should_detect_zero_based_routing_position()
        {
            ParseInvalid(@"[Routable] FooExecuted([RoutingPosition(0)] int id, [RoutingPosition(1)] int id2);");
        }

        [Test]
        public void should_detect_multiple_routing_position()
        {
            ParseInvalid(@"[Routable] FooExecuted([RoutingPosition(1), RoutingPosition(2)] int id, int id2);");
        }

        [Test]
        public void should_detect_bad_routing_position()
        {
            ParseInvalid(@"[Routable] FooExecuted([RoutingPosition(""pouet"")] int id, int id2);");
        }

        [Test]
        public void should_detect_non_consecutive_routing_position()
        {
            ParseInvalid(@"[Routable] FooExecuted([RoutingPosition(1)] int id, [RoutingPosition(3)] int id2);");
        }

        [Test]
        public void should_detect_routing_positions_on_non_routable_messages()
        {
            ParseInvalid(@"FooExecuted([RoutingPosition(1)] int id, int id2);");
        }

        [Test]
        public void should_handle_explicit_tags()
        {
            var contracts = ParseValid(@"FooExecuted([42] int id, int other);");

            var msg = contracts.Messages.ExpectedSingle();
            msg.Parameters[0].Attributes.ShouldBeEmpty();
            msg.Parameters[0].Tag.ShouldEqual(42);
            msg.Parameters[1].Tag.ShouldEqual(43);
        }

        [Test]
        public void should_detect_invalid_tags()
        {
            ParseInvalid(@"FooExecuted([42] int a, int b, [42] int c);");
            ParseInvalid(@"FooExecuted([42] int a, int b, [ProtoMember(42)] int c);");
            ParseInvalid(@"FooExecuted([42] int a, int b, int c, [43] int d);");
            ParseInvalid(@"FooExecuted([42, 43] int a);");
            ParseInvalid(@"FooExecuted([42.10] int a);");
            ParseInvalid(@"FooExecuted([-42] int a);");
            ParseInvalid(@"FooExecuted([0] int a);");
            ParseInvalid(@"FooExecuted([19500] int a);");
        }

        [Test]
        public void should_detect_invalid_parameters()
        {
            ParseInvalid(@"FooExecuted(int a, int a)");
            ParseInvalid(@"FooExecuted([0] int a)");
            ParseInvalid(@"FooExecuted([ProtoMember(0)] int a)");
            ParseInvalid(@"FooExecuted([19042] int a)");
            ParseInvalid(@"FooExecuted([ProtoMember(19042)] int a)");
            ParseInvalid(@"FooExecuted([42] int a, [42] int b)");
            ParseInvalid(@"FooExecuted([42] int a, [ProtoMember(42)] int b)");
        }

        [Test]
        public void should_parse_boolean_options()
        {
            var contracts = ParseValid("#pragma Proto    \r\nFoo()");
            contracts.Messages.First().Options.Proto.ShouldBeTrue();

            contracts = ParseValid("#pragma proto    \r\nFoo()");
            contracts.Messages.First().Options.Proto.ShouldBeTrue();

            contracts = ParseValid("#pragma Proto true    \r\nFoo()");
            contracts.Messages.First().Options.Proto.ShouldBeTrue();

            contracts = ParseValid("#pragma Proto false    \r\nFoo()");
            contracts.Messages.First().Options.Proto.ShouldBeFalse();

            contracts = ParseValid("#pragma !proto    \r\nFoo()");
            contracts.Messages.First().Options.Proto.ShouldBeFalse();

            contracts = ParseValid("#pragma Proto = true    \r\nFoo()");
            contracts.Messages.First().Options.Proto.ShouldBeTrue();

            contracts = ParseValid("   #  pragma Proto    \r\nFoo()");
            contracts.Messages.First().Options.Proto.ShouldBeTrue();
        }

        [Test]
        public void should_detect_invalid_options()
        {
            ParseInvalid("#pragma !proto true    \r\nFoo()");
            ParseInvalid("#pragma \r\n mutable");
            ParseInvalid("#pragma ! \r\n mutable");
            ParseInvalid("#pragma mutable \r\n false");
        }

        [Test]
        public void should_not_allow_anything_after_pragma_on_the_same_line()
        {
            ParseInvalid("#pragma proto; Foo()");
            ParseInvalid("#pragma proto Foo()");
            ParseInvalid("#pragma proto foo bar");
        }

        [Test]
        public void should_parse_pragma_internal_and_public()
        {
            var contracts = ParseValid("#pragma internal\r\nMsgA()\r\n#pragma public\r\nMsgB()");
            contracts.Messages[0].Options.Internal.ShouldBeTrue();
            contracts.Messages[0].Options.Public.ShouldBeFalse();
            contracts.Messages[1].Options.Internal.ShouldBeFalse();
            contracts.Messages[1].Options.Public.ShouldBeTrue();
        }

        [Test]
        public void should_parse_access_modifiers()
        {
            var contracts = ParseValid("public MsgA(); internal MsgB();");
            contracts.Messages[0].AccessModifier.ShouldEqual(AccessModifier.Public);
            contracts.Messages[1].AccessModifier.ShouldEqual(AccessModifier.Internal);
        }

        [Test]
        public void should_parse_access_modifiers_in_internal_scope()
        {
            var contracts = ParseValid("#pragma internal\r\npublic MsgA(); internal MsgB();");
            contracts.Messages[0].AccessModifier.ShouldEqual(AccessModifier.Public);
            contracts.Messages[1].AccessModifier.ShouldEqual(AccessModifier.Internal);
        }

        [Test]
        public void should_parse_custom_types()
        {
            var contracts = ParseValid(@"FooType!(int id);");

            var msg = contracts.Messages.ExpectedSingle();
            msg.IsCustom.ShouldBeTrue();
        }

        [Test]
        public void should_handle_generic_types()
        {
            var contracts = ParseValid(@"FooEvent(IDictionary<int,string> foo, IList<IDictionary  < int,  string >  >bar)");
            var msg = contracts.Messages.ExpectedSingle();
            msg.Parameters[0].Type.NetType.ShouldEqual("IDictionary<int, string>");
            msg.Parameters[1].Type.NetType.ShouldEqual("IList<IDictionary<int, string>>");

            contracts = ParseValid(@"FooEvent(SomeGenericStruct<int?, string>? foo)");
            msg = contracts.Messages.ExpectedSingle();
            msg.Parameters[0].Type.NetType.ShouldEqual("SomeGenericStruct<int?, string>?");
        }

        [Test]
        public void should_handle_array_types()
        {
            ParseValid(@"FooEvent(int[] foo)");
            ParseValid(@"FooEvent(int[,] foo)");
            ParseValid(@"FooEvent(int[,][,,] foo)");
            ParseValid(@"FooEvent(int[,][,,][,,,] foo)");
            ParseValid(@"FooEvent(int?[,][,,][,,,] foo)");
        }

        [Test]
        public void should_reject_invalid_types()
        {
            ParseInvalid("FooEvent(int?? id)");
            ParseInvalid("FooEvent(int[]? id)");
        }

        [Test]
        public void should_handle_namespaces()
        {
            var contracts = ParseValid("FooEvent(global::System.Collection.Generic.List<global::System.Int32> foo)");
            var msg = contracts.Messages.ExpectedSingle();
            msg.Parameters.ExpectedSingle().Type.NetType.ShouldEqual("global::System.Collection.Generic.List<int>");

            contracts = ParseValid("FooEvent(global::System.Int32 id)");
            msg = contracts.Messages.ExpectedSingle();
            msg.Parameters.ExpectedSingle().Type.NetType.ShouldEqual("int");
        }

        [Test]
        public void should_handle_generic_messages()
        {
            ParseValid("Foo<TFoo>()");
            ParseValid("Foo<TFoo, TBar>()");
            ParseValid("Foo<TFoo, TBar,TBaz>()");

            ParseInvalid("Foo<>()");
            ParseInvalid("Foo<42>()");
            ParseInvalid("Foo<TFoo?>()");
            ParseInvalid("Foo<TFoo[]>()");
            ParseInvalid("Foo<TFoo,>()");
            ParseInvalid("Foo<,>()");
            ParseInvalid("Foo<TFoo,>()");
            ParseInvalid("Foo<TFoo, TFoo>()");
        }

        [Test]
        public void should_handle_generic_constraints()
        {
            ParseValid("Foo<T>() where T : class");
            ParseValid("Foo<T>() where T : struct");
            ParseValid("Foo<T>() where T : new()");
            ParseValid("Foo<T>() where T : IDisposable");
            ParseValid("Foo<T>() where T : class, IDisposable");
            ParseValid("Foo<T>() where T : IDisposable, new()");
            ParseValid("Foo<T>() where T : class, IDisposable, new()");
            ParseValid("Foo<T>() where T : new(), IDisposable, class");
            ParseValid("Foo<T>() where T : struct, new()");
            ParseValid("Foo<T, U>() where U : class");
            ParseValid("Foo<T, U>() where T : class where U : class");
            ParseValid("Foo<T>() where T : ISomething<T, int?, char[]>");
            ParseValid("Foo<T>() where T : @class, struct");

            ParseInvalid("Foo<T>() where T : class, struct");
            ParseInvalid("Foo<T>() where T : class, class");
            ParseInvalid("Foo<T>() where T : struct, struct");
            ParseInvalid("Foo<T>() where T : new(), new()");
            ParseInvalid("Foo<T>() where T : @new()");
            ParseInvalid("Foo<T>() where T : IDisposable, IDisposable");
            ParseInvalid("Foo<T>() where U : class");
            ParseInvalid("Foo<T>() where T : class where T : IDisposable");
            ParseInvalid("Foo<T>() where T : ISomething<T, int?, new, char[]>");
            ParseInvalid("Foo<T>() where T : ISomething<T, int?, new(), char[]>");
        }

        [Test]
        public void should_disallow_unhandled_features_in_proto()
        {
            ParseInvalid("#pragma proto\r\nFoo<TFoo>()");
            ParseInvalid("#pragma proto\r\nFoo#v0()");
        }

        [Test]
        public void should_handle_keywords()
        {
            ParseValid("@Foo(@if @void)");
            ParseValid("@null(@true @false, @public @static)");

            ParseInvalid("Foo(@ if test)");
            ParseInvalid("Foo(if void)");
            ParseInvalid("Foo(null true)");
        }

        [Test]
        public void should_handle_additional_interfaces()
        {
            ParseValid("Foo(int id) : ITest").Messages.Single().Interfaces.ShouldContain((TypeName)"ITest");
            ParseValid("Foo(int id) : IGeneric<Foo ,Bar[ ]>").Messages.Single().Interfaces.ShouldContain((TypeName)"IGeneric<Foo, Bar[]>");
        }

        [Test]
        public void should_override_message_type()
        {
            var msg = ParseValid("Foo(int id)").Messages.Single();
            msg.Type.ShouldEqual(MessageType.Event);
            msg.Interfaces.ShouldContain((TypeName)"IEvent");
            msg.Interfaces.ShouldNotContain((TypeName)"ICommand");

            msg = ParseValid("Foo(int id) : ICommand").Messages.Single();
            msg.Type.ShouldEqual(MessageType.Command);
            msg.Interfaces.ShouldContain((TypeName)"ICommand");
            msg.Interfaces.ShouldNotContain((TypeName)"IEvent");
        }

        [Test]
        public void should_parse_enums()
        {
            var msg = ParseValid("[EnumAttr] enum Foo { Red, Green = 4, [MemberAttr] Blue = Red }; Test();");
            var fooEnum = msg.Enums.ExpectedSingle();

            fooEnum.Name.ShouldEqual("Foo");
            fooEnum.UnderlyingType.ShouldEqual((TypeName)"int");
            fooEnum.Attributes.ExpectedSingle().TypeName.ShouldEqual((TypeName)"EnumAttr");

            fooEnum.Members.Count.ShouldEqual(3);
            fooEnum.Members[0].Name.ShouldEqual("Red");
            fooEnum.Members[0].Value.ShouldBeNull();
            fooEnum.Members[0].Attributes.ShouldBeEmpty();

            fooEnum.Members[1].Name.ShouldEqual("Green");
            fooEnum.Members[1].Value.ShouldEqual("4");
            fooEnum.Members[1].Attributes.ShouldBeEmpty();

            fooEnum.Members[2].Name.ShouldEqual("Blue");
            fooEnum.Members[2].Value.ShouldEqual("Red");
            fooEnum.Members[2].Attributes.ExpectedSingle().TypeName.ShouldEqual((TypeName)"MemberAttr");
        }

        [Test]
        public void should_parse_complex_enums()
        {
            var msg = ParseValid(@"
    [Flags]
    public enum Metrics
    {
        None = 0,
        HitOrdersProportion = 1 << 0,
        HitRatio = 1 << 1,
        IocExecution = 1 << 2,
        Latency = 1 << 3,
        Lifetime = 1 << 4,
        MessageCounter = 1 << 5,
        CancelReject = 1 << 6,
        ExcessiveMessageRatioNasdaq = 1 << 7,
        All = ~0,
        AllExceptOrderContextMetrics = All & ~HitOrdersProportion & ~HitRatio & ~ExcessiveMessageRatioNasdaq,
        Test = Lifetime | (Latency)
    }
");

            var enumDef = msg.Enums.ExpectedSingle();
            enumDef.Members.Count.ShouldEqual(12);
            enumDef.Members.ExpectedSingle(i => i.Name == "Test").Value.ShouldEqual("Lifetime | (Latency)");
        }

        [Test]
        public void should_handle_double_angled_brackets()
        {
            ParseValid(@"enum Foo { Bar = 1 << 4 };");
            ParseValid(@"enum Foo { Bar = 1 >> 0 };");
            ParseInvalid(@"enum Foo { Bar = 1 < < 4 };");
            ParseInvalid(@"enum Foo { Bar = 1 > > 0 };");

            ParseValid(@"Foo(Dictionary<int, List<int>> bar)");
            ParseValid(@"Foo(Dictionary<int, List<int> > bar)");
        }

        [Test]
        public void should_allow_public_keyword_for_enums()
        {
            var msg = ParseValid("[EnumAttr] public enum Foo { Red, Green = 4, [MemberAttr] Blue = Red }; Test();");
            msg.Enums.ExpectedSingle().Name.ShouldEqual("Foo");
        }

        [Test]
        public void should_parse_enum_separators()
        {
            ParseValid("enum Foo { }; enum Bar { A }; enum Baz { A, }");
            ParseInvalid("enum Foo { , }");
        }

        [Test]
        public void should_detect_duplicate_enum_members()
        {
            ParseValid("enum Foo { Bar, bar }");
            ParseInvalid("enum Foo { Bar, Bar }");
        }

        [Test]
        public void should_detect_invalid_underlying_enum_types()
        {
            ParseValid("enum Foo { Bar }");
            ParseValid("enum Foo : byte { Bar }");
            ParseValid("enum Foo : sbyte { Bar }");
            ParseValid("enum Foo : short { Bar }");
            ParseValid("enum Foo : ushort { Bar }");
            ParseValid("enum Foo : int { Bar }");
            ParseValid("enum Foo : uint { Bar }");
            ParseValid("enum Foo : long { Bar }");
            ParseValid("enum Foo : ulong { Bar }");

            ParseInvalid("enum Foo : string { Bar }");
            ParseInvalid("enum Foo : Baz { Bar }");

            ParseInvalid("#pragma proto\nenum Foo : short { Bar }");
        }

        [Test]
        public void should_detect_invalid_custom_protomember_attributes()
        {
            ParseValid("Foo([ProtoMember(42)] int bar)");
            ParseValid("Foo([ProtoMember( 42 )] int bar)");
            ParseValid("Foo([ProtoMember( 42, IsRequired = false )] int bar)");

            ParseInvalid("Foo([10] [ProtoMember(42)] int bar)");
            ParseInvalid("Foo([ProtoMember()] int bar)");
            ParseInvalid("Foo([ProtoMember] int bar)");
            ParseInvalid("Foo([ProtoMember('a')] int bar)");
        }

        [Test]
        public void should_return_source_text()
        {
            var contracts = ParseValid(@"  [Attr(42)] Foo ( int bar );  ");

            var foo = contracts.Messages.ExpectedSingle();
            foo.GetSourceText().ShouldEqual("[Attr(42)] Foo ( int bar )");
            foo.GetSourceTextInterval().ShouldEqual(new TextInterval(2, 28));

            var bar = foo.Parameters.ExpectedSingle();
            bar.GetSourceText().ShouldEqual("int bar");
            bar.GetSourceTextInterval().ShouldEqual(new TextInterval(19, 26));

            var attr = foo.Attributes.ExpectedSingle();
            attr.GetSourceText().ShouldEqual("Attr(42)");
            attr.GetSourceTextInterval().ShouldEqual(new TextInterval(3, 11));
        }

        [Test]
        public void should_return_two_messages_with_same_name_but_different_arity()
        {
            var contracts = ParseValid("Foo(); Foo<T>();");

            contracts.Messages.Count.ShouldEqual(2);
            contracts.Messages[0].Name.ShouldEqual("Foo");
            contracts.Messages[1].Name.ShouldEqual("Foo");
            contracts.Messages[1].GenericParameters.ExpectedSingle().ShouldEqual("T");
        }

        private static ParsedContracts ParseValid(string definitionText)
        {
            var contracts = Parse(definitionText);

            if (contracts.Errors.Any())
            {
                SyntaxDebugHelper.DumpParseTree(contracts);
                Assert.Fail("There are unexpected errors");
            }

            return contracts;
        }

        private static ParsedContracts ParseInvalid(string definitionText)
        {
            var contracts = Parse(definitionText);

            if (!contracts.Errors.Any())
            {
                SyntaxDebugHelper.DumpParseTree(contracts);
                Assert.Fail("Errors were expected");
            }

            return contracts;
        }

        private static ParsedContracts Parse(string definitionText)
        {
            Console.WriteLine("PARSE: {0}", definitionText);
            var contracts = ParsedContracts.Parse(definitionText, "Some.Namespace");

            foreach (var error in contracts.Errors)
                Console.WriteLine("ERROR: {0}", error);

            return contracts;
        }

        private static void ShouldContainError(ParsedContracts contracts, string expectedMessage)
        {
            var containsError = contracts.Errors.Any(err => err.Message.IndexOf(expectedMessage, StringComparison.OrdinalIgnoreCase) >= 0);
            if (!containsError)
                Assert.Fail($"Expected error: {expectedMessage}");
        }
    }
}
