using System;
using Nevermore.IntegrationTests.Contracts;

namespace Nevermore.IntegrationTests.Model
{
    public class Customer
    {
        public Customer()
        {
            Roles = new ReferenceCollection();
        }

        public CustomerId Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ReferenceCollection Roles { get; }
        public string Nickname { get; set; }
        public int[] LuckyNumbers { get; set; }
        public string ApiKey { get; set; }
        public string[] Passphrases { get; set; }
    }

    public class CustomerId
    {
        public CustomerId(string value)
        {
            Value = value;
        }

        public string Value { get; }

        public override string ToString()
        {
            return Value;
        }
    }

    public static class CustomerIdExtensionMethods
    {
        public static CustomerId? ToCustomerId(this string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : new CustomerId(value);
        }
    }
}