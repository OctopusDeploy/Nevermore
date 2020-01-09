using Nevermore.Contracts;
using TinyTypes.TypedStrings;

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
        public string Nickname { get; set; }
        public int[] LuckyNumbers { get; set; }
        public string ApiKey { get; set; }
        public string[] Passphrases { get; set; }
        public byte[] RowVersion { get; set; }
        string IId.Id => Id?.Value;
    }

    public class CustomerId : TypedString, IIdWrapper
    {
        public CustomerId(string value) : base(value)
        {
        }
    }
}