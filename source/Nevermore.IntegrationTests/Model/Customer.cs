using System.Collections.Generic;

namespace Nevermore.IntegrationTests.Model
{
    public class Customer : IId
    {
        public Customer()
        {
            Roles = new HashSet<string>();
        }

        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public HashSet<string> Roles { get; private set; }
        public string Nickname { get; set; }
        public int[] LuckyNumbers { get; set; }
        public string ApiKey { get; set; }
        public string[] Passphrases { get; set; }
    }
}