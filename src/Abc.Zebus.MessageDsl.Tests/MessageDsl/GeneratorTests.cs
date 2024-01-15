using System;
using System.Threading.Tasks;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Generator;
using Abc.Zebus.MessageDsl.Tests.TestTools;
using JetBrains.Annotations;
using VerifyNUnit;

namespace Abc.Zebus.MessageDsl.Tests.MessageDsl;

public abstract class GeneratorTests
{
    static GeneratorTests()
    {
        // Make sure the version is stable across releases
        GeneratorBase.GeneratorVersion = new Version(1, 2, 3, 4);
    }

    protected abstract string SnapshotExtension { get; }
    protected abstract string GenerateRaw(ParsedContracts contracts);

    [MustUseReturnValue]
    protected string Generate(MessageDefinition message)
    {
        var contracts = new ParsedContracts();
        contracts.Messages.Add(message);
        return Generate(contracts);
    }

    [MustUseReturnValue]
    protected string Generate(ParsedContracts contracts)
    {
        PreProcess(contracts);
        var result = GenerateRaw(contracts);

        Console.WriteLine("----- START -----");
        Console.WriteLine(result);
        Console.WriteLine("-----  END  -----");

        contracts.Errors.ShouldBeEmpty();

        return result;
    }

    protected Task<string> Verify(MessageDefinition message)
    {
        var contracts = new ParsedContracts();
        contracts.Messages.Add(message);
        return Verify(contracts);
    }

    protected async Task<string> Verify(ParsedContracts contracts)
    {
        PreProcess(contracts);
        var result = GenerateRaw(contracts);

        await Verifier.Verify(result, SnapshotExtension)
                      .UseDirectory("Snapshots");

        return result;
    }

    private static void PreProcess(ParsedContracts contracts)
    {
        if (!contracts.ImportedNamespaces.Contains("System"))
            contracts.Process();
    }
}
