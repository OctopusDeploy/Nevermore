using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class MessageWithIntIdMap : DocumentMap<MessageWithIntId>
    {
        public MessageWithIntIdMap()
        {
            Id(x => x.Id);
            Column(x => x.Sender);
        }
    }
}