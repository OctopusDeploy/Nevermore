namespace Nevermore.IntegrationTests.Model
{
    public class DocumentWithCustomPrefix
    {
        public CustomPrefixId Id { get; set; }
        public string Name { get; set; }
    }

    public class CustomPrefixId : StringCustomIdType
    {
        internal CustomPrefixId(string value) : base(value)
        {
        }
    }
}