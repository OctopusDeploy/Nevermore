using System;
using System.Collections.Generic;
using Assent;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore.Serialization;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nevermore.Tests.Mapping
{
    [TestFixture]
    public class ExtensibleEnumFixture
    {
        abstract class Test
        {
            public string Name { get; set; }
            public abstract TestType TestType { get; }
        }

        class Test1 : Test
        {
            public override TestType TestType => TestType.TestType1;
        }
        
        class Test2 : Test
        {
            public override TestType TestType => TestType.TestType2;
        }

        class TestType : ExtensibleEnum
        {
            public static readonly TestType TestType1 = new TestType("TestType1"); 
            public static readonly TestType TestType2 = new TestType("TestType2");

            TestType(string name, string description = null) : base(name, description)
            {
            }
        }

        class TestConverter : InheritedClassByExtensibleEnumConverter<Test, TestType>
        {
            protected override IDictionary<string, Type> DerivedTypeMappings { get; } = new Dictionary<string, Type>
            {
                {TestType.TestType1.Name, typeof(Test1)},
                {TestType.TestType2.Name, typeof(Test2)},
            };
            protected override string TypeDesignatingPropertyName => nameof(Test.TestType);
        }

        class TestTypeConverter : ExtensibleEnumConverter<TestType>
        {
            protected override IDictionary<string, TestType> Mappings { get; } = new Dictionary<string, TestType>
            {
                {TestType.TestType1.Name, TestType.TestType1},
                {TestType.TestType2.Name, TestType.TestType2},
            };
        }
        
        [Test]
        public void SerializesCorrectly()
        {
            var test = new Test1
            {
                Name = "Test 1"
            };

            var json = JsonConvert.SerializeObject(test, Formatting.Indented, new TestTypeConverter(), new TestConverter());
            this.Assent(json);
        }

        [Test]
        public void DeserializesCorrectly()
        {
            var json = "{TestType: \"TestType2\", Name: \"Test 2\"}";

            var test = JsonConvert.DeserializeObject<Test>(json, new TestTypeConverter(), new TestConverter());
            test.Should().BeOfType<Test2>();
            test.Name.Should().Be("Test 2");
        }
    }
}