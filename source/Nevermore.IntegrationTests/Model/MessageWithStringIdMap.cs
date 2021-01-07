using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class MessageWithStringIdMap : DocumentMap<MessageWithStringId>
    {
        public MessageWithStringIdMap()
        {
            Id(x => x.Id);
            Column(x => x.Sender);
        }
    }
}