using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Model
{
    public class MachineToTestSerialization : IDocument
    {
        public string Id { get; protected set; }
        public string Name { get; set; }

        public string JSON { get; set; }
    }
}