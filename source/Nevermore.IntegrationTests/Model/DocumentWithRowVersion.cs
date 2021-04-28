namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithRowVersion
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public byte[] RowVersion { get; set; }
    }
}