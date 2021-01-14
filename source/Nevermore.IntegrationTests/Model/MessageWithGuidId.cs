using System;

namespace Nevermore.IntegrationTests.Model
{
    public class MessageWithGuidId
    {
        public Guid Id { get; set; }

        public string Sender { get; set; }

        public string Body { get; set; }
    }
}
