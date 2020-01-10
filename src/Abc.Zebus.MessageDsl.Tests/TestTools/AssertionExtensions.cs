using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Abc.Zebus.MessageDsl.Tests.TestTools
{
    public static class AssertionExtensions
    {
        public static void ShouldBeTrue(this bool actual)
            => Assert.That(actual, Is.True);

        public static void ShouldBeFalse(this bool actual)
            => Assert.That(actual, Is.False);

        public static void ShouldBeNull(this object actual)
            => Assert.That(actual, Is.Null);

        public static T ShouldNotBeNull<T>(this T actual)
            where T : class
        {
            Assert.That(actual, Is.Not.Null);
            return actual;
        }

        public static void ShouldContain(this string actual, string expected)
            => Assert.That(actual, Contains.Substring(expected));

        public static void ShouldContain<T>(this IEnumerable<T> actual, T expected)
            => Assert.That(actual, Contains.Item(expected));

        public static void ShouldContainIgnoreIndent(this string actual, string expected)
            => Assert.That(Regex.Replace(actual, @"^[ ]+|\r", string.Empty, RegexOptions.CultureInvariant | RegexOptions.Multiline), Contains.Substring(expected));

        public static void ShouldNotContain(this string actual, string unexpected)
            => Assert.That(actual, Does.Not.Contain(unexpected));

        public static void ShouldNotContain<T>(this IEnumerable<T> actual, T unexpected)
            => Assert.That(actual, Does.Not.Contain(unexpected));

        public static void ShouldBeEmpty(this object actual)
            => Assert.That(actual, Is.Empty);

        public static void ShouldEqual<T>(this T actual, T expected)
            => Assert.That(actual, Is.EqualTo(expected));

        public static T ExpectedSingle<T>(this IEnumerable<T> actual)
        {
            var list = actual.ToList();

            if (list.Count != 1)
                Assert.Fail($"Sequence should contain a single element, but had {list.Count}");

            return list[0];
        }

        public static T ExpectedSingle<T>(this IEnumerable<T> actual, Func<T, bool> predicate)
        {
            var list = actual.Where(predicate).ToList();

            if (list.Count != 1)
                Assert.Fail($"Sequence should contain a single element matching the predicate, but had {list.Count}");

            return list[0];
        }
    }
}
