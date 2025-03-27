using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tool.Tests;

[TestFixture]
public class ToolTests
{
    private TextWriter _originalOut = null!;
    private TextReader _originalIn = null!;

    [SetUp]
    public void Setup()
    {
        _originalOut = Console.Out;
        _originalIn = Console.In;
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
        string inputContent = "TestMessage()";
        string[] args = new[] { "--namespace", "TestNS", "--format", "CSharp" };
        var output = new StringWriter();
        Console.SetOut(output);
        Console.SetIn(new StringReader(inputContent));

        Program.Main(args);
        var outputString = output.ToString();
        Assert.That(outputString.Contains("namespace TestNS"));
        Assert.That(outputString.Contains("class TestMessage"));
    }

    [Test]
    public void should_generate_proto_output_with_empty_namespace_when_not_provided()
    {
        string fileContent = "TestMessage()";
        string tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, fileContent);
            string[] args = new[] { tempFile, "--format", "Proto" };
            var output = new StringWriter();
            Console.SetOut(output);

            Program.Main(args);
            var outputString = output.ToString();
            Assert.That(outputString.Contains("message TestMessage"));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
