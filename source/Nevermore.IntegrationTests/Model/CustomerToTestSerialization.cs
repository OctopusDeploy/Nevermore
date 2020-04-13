using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Model
{
    public class CustomerToTestSerialization : IId
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public string Nickname { get; set; }
        public ReferenceCollection Roles { get; private set; }
        public string JSON { get; set; }
    }
}