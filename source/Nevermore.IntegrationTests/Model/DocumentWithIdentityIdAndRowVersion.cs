namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithIdentityIdAndRowVersion
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] RowVersion { get; set; }
    }
}