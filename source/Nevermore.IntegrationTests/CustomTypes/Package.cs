using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.CustomTypes
{
    public class Package : IId
    {
        public string Id { get; set; }
        public Version Version { get; set; }
    }
}