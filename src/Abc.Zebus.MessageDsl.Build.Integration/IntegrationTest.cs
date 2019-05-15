#if INTEGRATION_TEST

using System;

namespace Abc.Zebus.MessageDsl.Build.Integration
{
    public class IntegrationTest
    {
        static IntegrationTest()
        {
            GC.KeepAlive(typeof(SomeMessage));
            GC.KeepAlive(typeof(InnerNamespace.InnerMessage));
            GC.KeepAlive(typeof(Abc.Zebus.CustomNamespace.HasCustomNamespace));
            GC.KeepAlive(typeof(ExplicitItems.A.ExplicitlyDefinedMessage));
            GC.KeepAlive(typeof(ExplicitItems.B.ExplicitlyDefinedMessage));
        }
    }
}

#endif
