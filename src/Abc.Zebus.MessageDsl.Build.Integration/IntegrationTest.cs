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
        }
    }
}

#endif
