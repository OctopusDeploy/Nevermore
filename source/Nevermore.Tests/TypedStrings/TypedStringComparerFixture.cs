using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nevermore.TypedStrings;
using NUnit.Framework;

namespace Nevermore.Tests.TypedStrings
{
    [TestFixture]
    public class TypedStringComparerFixture
    {
        [Test]
        public void ToDictionary_KeysWithDifferentCaseOrdinal_DoesNotThrow()
        {
            var typedStrings = new List<SomeTypedString>
            {
                new SomeTypedString("foo"),
                new SomeTypedString("Foo")
            };

            typedStrings
                .ToDictionary(t => t, TypedStringComparer<SomeTypedString>.Ordinal)
                .Keys.Should().BeEquivalentTo(typedStrings);
        }

        [Test]
        public void ToDictionary_KeysWithDifferentCaseOrdinalIgnoreCase_Throws()
        {
            var typedStrings = new List<SomeTypedString>
            {
                new SomeTypedString("foo"),
                new SomeTypedString("Foo")
            };

            typedStrings
                .Invoking(ts => ts.ToDictionary(t => t, TypedStringComparer<SomeTypedString>.OrdinalIgnoreCase))
                .ShouldThrow<ArgumentException>();
        }

        class SomeTypedString : TypedString
        {
            public SomeTypedString(string value) : base(value)
            {
            }
        }
    }
}