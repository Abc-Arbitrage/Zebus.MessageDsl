using System;
using Abc.Zebus.MessageDsl.Ast;
using Abc.Zebus.MessageDsl.Tests.TestTools;

namespace Abc.Zebus.MessageDsl.Tests.MessageDsl;

public abstract class GeneratorTests
{
    protected abstract string GenerateRaw(ParsedContracts contracts);

    protected string Generate(MessageDefinition message)
    {
        var contracts = new ParsedContracts();
        contracts.Messages.Add(message);
        return Generate(contracts);
    }

    protected string Generate(ParsedContracts contracts)
    {
        if (!contracts.ImportedNamespaces.Contains("System"))
            contracts.Process();

        var result = GenerateRaw(contracts);

        Console.WriteLine("----- START -----");
        Console.WriteLine(result);
        Console.WriteLine("-----  END  -----");

        contracts.Errors.ShouldBeEmpty();

        return result;
    }
}
