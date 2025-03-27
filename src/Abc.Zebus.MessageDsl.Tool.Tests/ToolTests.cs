using System;
using System.IO;
using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tool.Tests;

[TestFixture]
public class ToolTests
{
#pragma warning disable NUnit1032
    private StringReader _input = null!;
    private StringWriter _output = null!;
    private StringWriter _errorOutput = null!;
#pragma warning restore NUnit1032

    [SetUp]
    public void SetUp()
    {
        _input = new StringReader("");
        _output = new StringWriter();
        _errorOutput = new StringWriter();
    }

    [Test]
    public void should_generate_csharp_with_namespace()
    {
        _input = new StringReader("TestMessage()");

        var exitCode = Run(["--namespace", "TestNS", "--format", "CSharp"]);
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

            var exitCode = Run([tempFile, "--format", "Proto"]);
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
        var exitCode = Run(["invalid_file.msg"]);
        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public void should_error_on_directory()
    {
        var aDirectory = Environment.CurrentDirectory;
        var exitCode = Run([aDirectory]);
        Assert.That(exitCode, Is.EqualTo(1));
    }

    [Test]
    public void should_error_on_invalid_msg()
    {
        _input = new StringReader("InvalidSyntax");
        var exitCode = Run([]);
        Assert.That(exitCode, Is.EqualTo(1));
    }

    private int Run(string[] args)
        => Program.Run(args, _input, _output, _errorOutput);
}
