namespace Nevermore.IntegrationTests.Model
{
    public class FeedType : ICustomThing
    {
        public static readonly FeedType BuiltIn = new FeedType("BuiltIn");
        public static readonly FeedType NuGet = new FeedType("NuGet");

        public FeedType(string name)
        {
            Name = name;
        }
        
        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }

    public interface ICustomThing
    {
        string Name { get; }
    }
}