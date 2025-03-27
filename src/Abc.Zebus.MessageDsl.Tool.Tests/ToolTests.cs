using System;
using System.IO;
using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tool.Tests;

[TestFixture]
public class ToolTests
{
    private TextWriter _originalOut = null!;
    private TextReader _originalIn = null!;
    private StringWriter _output = null!;

    [SetUp]
    public void Setup()
    {
        _originalOut = Console.Out;
        _originalIn = Console.In;
        _output = new StringWriter();
        Console.SetOut(_output);
    }

    [TearDown]
    public void TearDown()
    {
        Console.SetOut(_originalOut);
        Console.SetIn(_originalIn);
    }

    [Test]
    public void should_generate_csharp_with_namespace()
    {
        Console.SetIn(new StringReader("TestMessage()"));

        var exitCode = Program.Main(["--namespace", "TestNS", "--format", "CSharp"]);
        Assert.That(exitCode, Is.EqualTo(0));
        var outputString = _output.ToString();
        Assert.That(outputString, Does.Contain("namespace TestNS"));
        Assert.That(outputString, Does.Contain("class TestMessage"));
    }

    [Test]
    public void should_generate_proto_output_with_empty_namespace_when_not_provided()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "TestMessage()");

            var exitCode = Program.Main([tempFile, "--format", "Proto"]);
            Assert.That(exitCode, Is.EqualTo(0));
            var outputString = _output.ToString();
            Assert.That(outputString.Contains("message TestMessage"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Test]
    public void should_error_on_file_not_found()
    {
        var exitCode = Program.Main(["invalid_file.msg"]);
        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public void should_error_on_directory()
    {
        var aDirectory = Environment.CurrentDirectory;
        var exitCode = Program.Main([aDirectory]);
        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public void should_error_on_invalid_msg()
    {
        Console.SetIn(new StringReader("InvalidSyntax"));
        var exitCode = Program.Main([]);
        Assert.That(exitCode, Is.EqualTo(1));
    }
}
