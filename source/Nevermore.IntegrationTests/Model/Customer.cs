using Nevermore.Contracts;
using Octopus.TinyTypes;

namespace Nevermore.IntegrationTests.Model
{
    public class Customer : IId<CustomerId>, IId
    {
        public Customer()
        {
            Roles = new ReferenceCollection();
        }

        public CustomerId Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ReferenceCollection Roles { get; private set; }
        public Nickname Nickname { get; set; }
        public int[] LuckyNumbers { get; set; }
        public string ApiKey { get; set; }
        public string[] Passphrases { get; set; }
        public byte[] RowVersion { get; set; }
        string IId.Id => Id?.Value;
    }

    public class Nickname : CaseSensitiveTypedString
    {
        public Nickname(string value) : base(value)
        {
        }
    }

    public class CustomerId : CaseSensitiveTypedString, IIdWrapper
    {
        public CustomerId(string value) : base(value)
        {
        }
    }
}