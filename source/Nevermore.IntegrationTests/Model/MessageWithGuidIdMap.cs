using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class MessageWithGuidIdMap: DocumentMap<MessageWithGuidId>
    {
        public MessageWithGuidIdMap()
        {
            Id(x => x.Id);
            Column(x => x.Sender);
        }
    }
}
