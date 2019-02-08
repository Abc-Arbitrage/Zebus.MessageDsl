using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Tests.TestTools;
using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tests.MessageDsl
{
    [TestFixture]
    public class TypeNameTests
    {
        [Test]
        public void should_normalize_type()
        {
            var type = new TypeName("System.Int64");
            type.NetType.ShouldEqual("long");
        }

        [Test]
        public void should_remove_system_namespace()
        {
            var type = new TypeName("System.DateTime?");
            type.NetType.ShouldEqual("DateTime?");
        }

        [Test]
        public void should_not_remove_system_namespace()
        {
            var type = new TypeName("System.Diagnostics.DebuggerNonUserCode");
            type.NetType.ShouldEqual("System.Diagnostics.DebuggerNonUserCode");
        }

        [Test]
        public void should_detect_repeated_types()
        {
            var type = new TypeName("int");
            type.IsArray.ShouldBeFalse();
            type.IsList.ShouldBeFalse();
            type.IsRepeated.ShouldBeFalse();

            type = new TypeName("int[]");
            type.IsArray.ShouldBeTrue();
            type.IsList.ShouldBeFalse();
            type.IsRepeated.ShouldBeTrue();

            type = new TypeName("List<int>");
            type.IsArray.ShouldBeFalse();
            type.IsList.ShouldBeTrue();
            type.IsRepeated.ShouldBeTrue();

            type = new TypeName("System.Collections.Generic.List<int>");
            type.IsArray.ShouldBeFalse();
            type.IsList.ShouldBeTrue();
            type.IsRepeated.ShouldBeTrue();
        }

        [Test]
        public void should_detect_nullables()
        {
            var type = new TypeName("int");
            type.IsNullable.ShouldBeFalse();

            type = new TypeName("int?");
            type.IsNullable.ShouldBeTrue();
        }

        [Test]
        public void should_map_to_protobuf_type()
        {
            var type = new TypeName("int");
            type.ProtoBufType.ShouldEqual("int32");
            type.IsPackable.ShouldBeFalse();

            type = new TypeName("int[]");
            type.ProtoBufType.ShouldEqual("int32");
            type.IsPackable.ShouldBeTrue();

            type = new TypeName("System.String");
            type.ProtoBufType.ShouldEqual("string");
            type.IsPackable.ShouldBeFalse();

            type = new TypeName("System.DateTime");
            type.ProtoBufType.ShouldEqual("bcl.DateTime");

            type = new TypeName("DateTime");
            type.ProtoBufType.ShouldEqual("bcl.DateTime");

            type = new TypeName("global::System.DateTime");
            type.ProtoBufType.ShouldEqual("bcl.DateTime");

            type = new TypeName("System.Decimal");
            type.ProtoBufType.ShouldEqual("bcl.Decimal");

            type = new TypeName("Decimal");
            type.ProtoBufType.ShouldEqual("bcl.Decimal");

            type = new TypeName("decimal");
            type.ProtoBufType.ShouldEqual("bcl.Decimal");

            type = new TypeName("Decimal[]");
            type.ProtoBufType.ShouldEqual("bcl.Decimal");

            type = new TypeName("decimal[]");
            type.ProtoBufType.ShouldEqual("bcl.Decimal");

            type = new TypeName("decimal?");
            type.ProtoBufType.ShouldEqual("bcl.Decimal");

            type = new TypeName("Decimal?");
            type.ProtoBufType.ShouldEqual("bcl.Decimal");

            type = new TypeName("System.Decimal?");
            type.ProtoBufType.ShouldEqual("bcl.Decimal");

            type = new TypeName("global::System.Decimal?");
            type.ProtoBufType.ShouldEqual("bcl.Decimal");

            type = new TypeName("foo::System.Decimal");
            type.ProtoBufType.ShouldEqual("foo.System.Decimal");
        }

        [Test]
        public void should_escape_csharp_identifiers()
        {
            TypeName type = "new";
            type.NetType.ShouldEqual("@new");

            type = "int";
            type.NetType.ShouldEqual("int");

            type = "void";
            type.NetType.ShouldEqual("@void");

            type = "@void";
            type.NetType.ShouldEqual("@void");

            type = "@int";
            type.NetType.ShouldEqual("int");

            type = "@foo";
            type.NetType.ShouldEqual("foo");
        }

        [Test]
        public void should_normalize_spaces()
        {
            TypeName type = "  IFoo < Bar ? ,@Baz,Hello [, , ], @World < Tanks>, Int32>";
            type.NetType.ShouldEqual("IFoo<Bar?, Baz, Hello[,,], World<Tanks>, int>");
        }
    }
}
