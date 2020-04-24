using Nevermore.IntegrationTests.Contracts;

namespace Nevermore.IntegrationTests.Model
{
    public class Customer
    {
        public Customer()
        {
            Roles = new ReferenceCollection();
        }

        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ReferenceCollection Roles { get; }
        public string Nickname { get; set; }
        public int[] LuckyNumbers { get; set; }
        public string ApiKey { get; set; }
        public string[] Passphrases { get; set; }
        public byte[] RowVersion { get; set; }
    }
}