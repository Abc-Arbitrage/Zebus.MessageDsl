using Abc.Zebus.MessageDsl.Analysis;
using Abc.Zebus.MessageDsl.Tests.TestTools;
using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tests.MessageDsl;

[TestFixture]
public class ReservationRangeTests
{
    [Test]
    public void should_compress_ranges()
    {
        ReservationRange.Compress([new(2, 4), new(3, 5), new(6, 7), new(10, 12)])
                        .ShouldEqual([new(2, 7), new(10, 12)]);

        ReservationRange.Compress([]).ShouldEqual([]);
    }
}
