namespace Nevermore.IntegrationTests.Model
{
    class CustomPrefixIdKeyHandler : StringCustomIdTypeIdKeyHandler<CustomPrefixId>
    {
        public const string CustomPrefix = "CustomPrefix";

        public CustomPrefixIdKeyHandler():base(CustomPrefix)
        {
        }
    }
}