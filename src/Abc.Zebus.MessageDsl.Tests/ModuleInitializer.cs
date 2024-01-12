using System.Runtime.CompilerServices;
using VerifyTests;

namespace Abc.Zebus.MessageDsl.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
        => VerifyDiffPlex.Initialize();
}
