namespace Nevermore.IntegrationTests.CustomTypes
{
    public class Version
    {
        public Version(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public int Major { get; }
        public int Minor { get; }
        public int Patch { get; }
    }
}