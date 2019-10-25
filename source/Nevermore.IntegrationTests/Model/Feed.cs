using Nevermore.Contracts;

namespace Nevermore.IntegrationTests.Model
{
    public abstract class Feed : IDocument
    {
        public string Id { get; protected set; }
        public string Name { get; set; }
        
        public FeedType FeedType { get; protected set; }        
    }

    public class NuGetFeed : Feed
    {
        public NuGetFeed()
        {
            FeedType = FeedType.NuGet;
        }
    }

    public class BuiltInFeed : Feed
    {
        public BuiltInFeed()
        {
            FeedType = FeedType.BuiltIn;
        }
    }
}