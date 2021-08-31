using System.Collections.Generic;

namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithRowVersion
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string SomeOtherProperty { get; set; }
        public List<string> Items { get; set; }
        public byte[] RowVersion { get; set; }
    }

    public class DocumentWithoutRowVersion
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> Items { get; set; }
    }
}