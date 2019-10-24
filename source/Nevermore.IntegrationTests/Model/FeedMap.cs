using System.Data;
using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class FeedMap : DocumentMap<Feed>
    {
        public FeedMap()
        {
            IdColumn.MaxLength = 210;

            Column(m => m.Name);
            VirtualColumn(nameof(Feed.FeedType), DbType.String, m => m.FeedType.Name);
        }
    }
}