using Nevermore.Contracts;

namespace Nevermore.Benchmarks.Model
{
    public class Customer : IId
    {
        public Customer()
        {
            Roles = new ReferenceCollection();
        }

        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public ReferenceCollection Roles { get; private set; }
        public string Nickname { get; set; }
        public int[] LuckyNumbers { get; set; }
        public string ApiKey { get; set; }
        public string[] Passphrases { get; set; }
        public byte[] RowVersion { get; set; }
    }
}