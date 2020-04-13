using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Model
{
    public class Machine : IDocument
    {
        public string Id { get; protected set; }
        public string Name { get; set; }

        public string Description { get; set; }
        public Endpoint Endpoint { get; set; }
    }
}