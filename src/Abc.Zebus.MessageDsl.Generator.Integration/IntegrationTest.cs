#if INTEGRATION_TEST

using System;

namespace Abc.Zebus.MessageDsl.Generator.Integration
{
    public class IntegrationTest
    {
        static IntegrationTest()
        {
#if RIDER_FALLBACK
            GC.KeepAlive(typeof(SomeMessage));
            GC.KeepAlive(typeof(InnerNamespace.InnerMessage));
            GC.KeepAlive(typeof(Abc.Zebus.CustomExplicitNamespace.HasCustomExplicitNamespace));
#else
            GC.KeepAlive(typeof(SomeMessage));
            GC.KeepAlive(typeof(InnerNamespace.InnerMessage));
            GC.KeepAlive(typeof(Abc.Zebus.CustomNamespace.HasCustomNamespace));
            GC.KeepAlive(typeof(Abc.Zebus.CustomExplicitNamespace.HasCustomExplicitNamespace));
            GC.KeepAlive(typeof(global::HasEmptyNamespace));
            GC.KeepAlive(typeof(ExplicitItems.A.ExplicitlyDefinedMessage));
            GC.KeepAlive(typeof(ExplicitItems.B.ExplicitlyDefinedMessage));
#endif
        }
    }
}

#endif
