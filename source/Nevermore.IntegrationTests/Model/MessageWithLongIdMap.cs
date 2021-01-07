using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class MessageWithLongIdMap : DocumentMap<MessageWithLongId>
    {
        public MessageWithLongIdMap()
        {
            Id(x => x.Id);
            Column(x => x.Sender);
        }
    }
}
