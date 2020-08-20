using Nevermore.Mapping;

namespace Nevermore.IntegrationTests.Model
{
    public class MessageMap : DocumentMap<Message>
    {
        public MessageMap()
        {
            Id(x => x.Id);
            Column(x => x.Sender);
        }
    }
}