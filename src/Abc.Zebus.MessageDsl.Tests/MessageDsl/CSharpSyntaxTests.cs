using Abc.Zebus.MessageDsl.Generator;
using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tests.MessageDsl;

[TestFixture]
public class CSharpSyntaxTests
{
    [Test]
    [TestCase(null, ExpectedResult = false)]
    [TestCase("", ExpectedResult = false)]
    [TestCase("foo", ExpectedResult = true)]
    [TestCase("int", ExpectedResult = false)]
    [TestCase("_int", ExpectedResult = true)]
    [TestCase("@int", ExpectedResult = true)]
    [TestCase("42lol", ExpectedResult = false)]
    [TestCase("lol_42", ExpectedResult = true)]
    [TestCase("lol.foo", ExpectedResult = false)]
    [TestCase("l\\u006f", ExpectedResult = true)]
    [TestCase("l\\U0000006f", ExpectedResult = true)]
    [TestCase("l\\U42", ExpectedResult = false)]
    [TestCase("in\\u0074", ExpectedResult = false)]
    public bool should_validate_identifiers(string value)
        => CSharpSyntax.IsValidIdentifier(value);

    [Test]
    [TestCase(null, ExpectedResult = false)]
    [TestCase("", ExpectedResult = false)]
    [TestCase("foo", ExpectedResult = true)]
    [TestCase("foo.foo", ExpectedResult = true)]
    [TestCase("foo.int", ExpectedResult = false)]
    [TestCase("foo.@int", ExpectedResult = true)]
    [TestCase("foo..foo", ExpectedResult = false)]
    [TestCase(".foo.foo", ExpectedResult = false)]
    [TestCase("foo.foo.", ExpectedResult = false)]
    public bool should_validate_namespaces(string value)
        => CSharpSyntax.IsValidNamespace(value);
}
