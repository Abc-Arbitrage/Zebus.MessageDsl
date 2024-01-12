using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Threading;

namespace Abc.Zebus.MessageDsl.Generator.Tests;

[TestFixture]
public class MessageDslGeneratorTests
{
    [Test]
    public void Should_generate_message_class_for_simple_dsl_file()
    {
        // Arrange
        var additionalTextMock = CreateAdditionalTextMock(@"Dsl\Messages.msg", "DoSomethingCommand(int foo);");
        var optionsProviderMock = CreateOptionProviderMock([additionalTextMock], ("build_metadata.AdditionalFiles.ZebusMessageDslNamespace", "Abc.Zebus.TestNamespace"));

        // Act
        var runResults = CSharpGeneratorDriver.Create(new MessageDslGenerator())
                                              .AddAdditionalTexts(ImmutableArray.Create(additionalTextMock.Object))
                                              .WithUpdatedAnalyzerConfigOptions(optionsProviderMock.Object)
                                              .RunGenerators(CSharpCompilation.Create("Tests"))
                                              .GetRunResult();

        // Assert
        var generatedSource = runResults.Results[0].GeneratedSources[0];
        Assert.That(generatedSource.HintName, Is.EqualTo("Dsl_Messages.msg.g.cs"));
        var sourceText = generatedSource.SourceText.ToString();
        Assert.That(sourceText, Does.Contain("public sealed partial class DoSomethingCommand : ICommand"));
    }

    [Test]
    public void Should_generate_message_class_for_multiple_additional_files_with_conflicting_names()
    {
        // Arrange
        var additionalTextMock1 = CreateAdditionalTextMock(@"Dsl\Messages.msg", "DoSomethingCommand(int foo);");
        var additionalTextMock2 = CreateAdditionalTextMock(@"Dsl\Messages.msg", "DoSomethingCommand(int foo);");

        var optionsProviderMock1 = CreateOptionProviderMock([additionalTextMock1], ("build_metadata.AdditionalFiles.ZebusMessageDslNamespace", "Abc.Zebus.TestNamespace"), ("build_metadata.AdditionalFiles.ZebusMessageDslRelativePath", "Dsl/Messages1.msg"));
        var optionsProviderMock2 = CreateOptionProviderMock([additionalTextMock2], ("build_metadata.AdditionalFiles.ZebusMessageDslNamespace", "Abc.Zebus.TestNamespace"), ("build_metadata.AdditionalFiles.ZebusMessageDslRelativePath", "Dsl/Messages2.msg"));

        // Act
        var runResults = CSharpGeneratorDriver.Create(new MessageDslGenerator())
                                              .AddAdditionalTexts(ImmutableArray.Create(additionalTextMock1.Object, additionalTextMock2.Object))
                                              .WithUpdatedAnalyzerConfigOptions(CombineOptionProviderMocks(optionsProviderMock1.Object, optionsProviderMock2.Object).Object)
                                              .RunGenerators(CSharpCompilation.Create("Tests"))
                                              .GetRunResult();

        // Assert
        var generatedSource1 = runResults.Results[0].GeneratedSources.Single(x => x.HintName == "Dsl_Messages1.msg.g.cs");
        AssertMessageSourceIsCorrect(generatedSource1, "Dsl_Messages1.msg.g.cs", "public sealed partial class DoSomethingCommand : ICommand");
        var generatedSource2 = runResults.Results[0].GeneratedSources.Single(x => x.HintName == "Dsl_Messages2.msg.g.cs");
        AssertMessageSourceIsCorrect(generatedSource2, "Dsl_Messages2.msg.g.cs", "public sealed partial class DoSomethingCommand : ICommand");
    }

    [Test]
    public void Should_not_generate_message_class_for_non_message_additional_files()
    {
        // Arrange
        var additionalTextMock = CreateAdditionalTextMock(@"Dsl\Messages.notamessage", "DoSomethingCommand(int foo);");
        var optionsProviderMock = CreateOptionProviderMock([additionalTextMock], ("build_metadata.AdditionalFiles.ZebusMessageDslNamespace", "Abc.Zebus.TestNamespace"));

        // Act
        var runResults = CSharpGeneratorDriver.Create(new MessageDslGenerator())
                                              .AddAdditionalTexts(ImmutableArray.Create(additionalTextMock.Object))
                                              .WithUpdatedAnalyzerConfigOptions(optionsProviderMock.Object)
                                              .RunGenerators(CSharpCompilation.Create("Tests"))
                                              .GetRunResult();

        // Assert
        Assert.That(runResults.Results[0].GeneratedSources, Is.Empty);
    }

    [Test]
    public void Should_generate_message_class_if_ZebusMessageDslNamespace_option_is_not_set()
    {
        // Arrange
        var additionalTextMock = CreateAdditionalTextMock(@"Dsl\Messages.msg", "DoSomethingCommand(int foo);");

        // Act
        var runResults = CSharpGeneratorDriver.Create(new MessageDslGenerator())
                                              .AddAdditionalTexts(ImmutableArray.Create(additionalTextMock.Object))
                                              .RunGenerators(CSharpCompilation.Create("Tests"))
                                              .GetRunResult();

        // Assert
        Assert.That(runResults.Results[0].GeneratedSources, Does.Not.Contain("namespace"));
    }

    private static Mock<AnalyzerConfigOptionsProvider> CreateOptionProviderMock(Mock<AdditionalText>[] additionalTextMocks, params (string key, string value)[] options)
    {
        var optionsProviderMock = new Mock<AnalyzerConfigOptionsProvider>();
        var optionsMock = new Mock<AnalyzerConfigOptions>();

        foreach (var option in options)
        {
            var fileNamespace = option.value;
            optionsMock.Setup(x => x.TryGetValue(option.key, out fileNamespace)).Returns(true);
        }

        foreach (var additionalTextMock in additionalTextMocks)
        {
            optionsProviderMock.Setup(x => x.GetOptions(additionalTextMock.Object)).Returns(optionsMock.Object);
        }

        return optionsProviderMock;
    }

    private static Mock<AnalyzerConfigOptionsProvider> CombineOptionProviderMocks(params AnalyzerConfigOptionsProvider[] providers)
    {
        var optionsProviderMock = new Mock<AnalyzerConfigOptionsProvider>();

        optionsProviderMock.Setup(x => x.GetOptions(It.IsAny<AdditionalText>()))
                           .Returns((AdditionalText additionalText) => providers.Select(p => p.GetOptions(additionalText)).FirstOrDefault(i => !ReferenceEquals(i, null))!);

        return optionsProviderMock;
    }

    private static Mock<AdditionalText> CreateAdditionalTextMock(string path, string source)
    {
        var additionalTextMock = new Mock<AdditionalText>();
        additionalTextMock.SetupGet(x => x.Path).Returns(path);
        additionalTextMock.Setup(x => x.GetText(It.IsAny<CancellationToken>()))
                          .Returns(SourceText.From(source));

        return additionalTextMock;
    }

    private static void AssertMessageSourceIsCorrect(GeneratedSourceResult generatedSource, string expectedHintName, string expectedSourceFragment)
    {
        Assert.That(generatedSource.HintName, Is.EqualTo(expectedHintName));
        var sourceText = generatedSource.SourceText.ToString();
        Assert.That(sourceText, Does.Contain(expectedSourceFragment));
    }
}
