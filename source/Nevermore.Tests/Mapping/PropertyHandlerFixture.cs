using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Nevermore.Advanced.PropertyHandlers;
using NUnit.Framework;

namespace Nevermore.Tests.Mapping
{
    public class PropertyHandlerFixture
    {
        MyClass instance;

        [SetUp]
        public void SetUp()
        {
            instance = new MyClass();
        }
        
        [Test]
        public void ShouldSetProperty1()
        {
            var handler = CreatePropertyHandler("Property1");
            handler.Read(instance).Should().BeNull();
            handler.Write(instance, "Hello");
            handler.Read(instance).Should().Be("Hello");
        }
        
        [Test]
        public void ShouldSetProperty2()
        {
            var handler = CreatePropertyHandler("Property2");
            handler.Read(instance).Should().BeNull();
            handler.Write(instance, "Hello");
            handler.Read(instance).Should().Be("Hello");
        }
        
        [Test]
        public void ShouldSetProperty3()
        {
            var handler = CreatePropertyHandler("Property3");
            handler.Read(instance).Should().BeNull();
            handler.Write(instance, "Hello");
            handler.Read(instance).Should().Be("Hello");
            handler.Write(instance, null);
            handler.Read(instance).Should().Be(null);
        }
        
        [Test]
        public void ShouldSetProperty4()
        {
            var handler = CreatePropertyHandler("Property4");
            handler.Read(instance).Should().BeNull();
            handler.Write(instance, 34);
            handler.Read(instance).Should().Be(34);
            handler.Write(instance, null);
            handler.Read(instance).Should().Be(null);
        }
        
        [Test]
        public void ShouldSetProperty5()
        {
            var handler = CreatePropertyHandler("Property5");
            handler.Read(instance).Should().Be(0);
            handler.Write(instance, 16);
            handler.Read(instance).Should().Be(16);
            Assert.Throws<InvalidCastException>(() => handler.Write(instance, "Hello"));
            Assert.Throws<InvalidCastException>(() => handler.Write(instance, null));
            Assert.Throws<InvalidCastException>(() => handler.Write(instance, 38.4));
        }
        
        [Test]
        public void ShouldSetProperty6()
        {
            var value = new HashSet<string> {"hello", "goodbye"};
            var handler = CreatePropertyHandler("Property6");
            handler.Read(instance).Should().BeNull();
            handler.Write(instance, value);
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Should().BeSameAs(value);
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Count.Should().Be(2);
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Contains("hello").Should().BeTrue();
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Contains("goodbye").Should().BeTrue();
        }
        
        [Test]
        public void ShouldSetProperty7()
        {
            var value = new HashSet<string> {"hello", "goodbye"};
            var handler = CreatePropertyHandler("Property7");
            handler.Read(instance).Should().BeNull();
            handler.Write(instance, value);
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Should().BeSameAs(value);
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Count.Should().Be(2);
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Contains("hello").Should().BeTrue();
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Contains("goodbye").Should().BeTrue();
        }
        
        [Test]
        public void ShouldSetProperty8()
        {
            var value = new HashSet<string> {"hello", "goodbye"};
            var handler = CreatePropertyHandler("Property8");
            handler.Read(instance).Should().NotBeNull();
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Count.Should().Be(0);
            handler.Write(instance, value);
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Should().NotBeSameAs(value);
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Count.Should().Be(2);
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Contains("hello").Should().BeTrue();
            handler.Read(instance).Should().BeOfType<HashSet<string>>().Which.Contains("goodbye").Should().BeTrue();
        }
        
        [Test]
        public void ShouldSetProperty9()
        {
            var value = new[] {"hello", "goodbye"};
            var handler = CreatePropertyHandler("Property9");
            handler.Read(instance).Should().NotBeNull();
            handler.Read(instance).Should().BeOfType<List<string>>().Which.Count.Should().Be(0);
            handler.Write(instance, value);
            handler.Read(instance).Should().BeOfType<List<string>>().Which.Should().NotBeSameAs(value);
            handler.Read(instance).Should().BeOfType<List<string>>().Which.Count.Should().Be(2);
            handler.Read(instance).Should().BeOfType<List<string>>().Which.Contains("hello").Should().BeTrue();
            handler.Read(instance).Should().BeOfType<List<string>>().Which.Contains("goodbye").Should().BeTrue();
        }
        
        [Test]
        public void ShouldSetProperty10()
        {
            var value = new string[] {"hello", "goodbye", "HELLO"};
            var handler = CreatePropertyHandler("Property10");
            handler.Read(instance).Should().NotBeNull();
            handler.Read(instance).Should().BeOfType<RefCollection>().Which.Count.Should().Be(0);
            handler.Write(instance, value);
            handler.Read(instance).Should().BeOfType<RefCollection>().Which.Should().NotBeSameAs(value);
            handler.Read(instance).Should().BeOfType<RefCollection>().Which.Count.Should().Be(2);
            handler.Read(instance).Should().BeOfType<RefCollection>().Which.Contains("hello").Should().BeTrue();
            handler.Read(instance).Should().BeOfType<RefCollection>().Which.Contains("goodbye").Should().BeTrue();
        }

        IPropertyHandler CreatePropertyHandler(string propertyName)
        {
            return new PropertyHandler(typeof(MyClass).GetProperty(propertyName));
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local")]
        class MyClass
        {
            public string Property1 { get; set; }
            public string Property2 { get; protected set; }
            public string Property3 { get; private set; }
            
            public int? Property4 { get; set; }
            public int Property5 { get; set; }
            
            public HashSet<string> Property6 { get; set; }
            public HashSet<string> Property7 { get; private set; }
            public HashSet<string> Property8 { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public List<string> Property9 { get; } = new List<string>();
            public RefCollection Property10 { get; } = new RefCollection();
        }
    }

    internal class RefCollection : HashSet<string>
    {
        public RefCollection() : base(StringComparer.OrdinalIgnoreCase)
        {
            
        }
    }
}