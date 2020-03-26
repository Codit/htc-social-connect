using System;

namespace CommunicationApi.Models
{
    public class TextMessage
    {
        public string From { get; set; }
        public DateTimeOffset ExpirationTime { get; set; }
        public string Message { get; set; }
        public string PhoneNumber { get; set; }
    }
}