using System;

namespace CommunicationApi.Models
{
    public class MediaItem
    {
        public MediaType MediaType { get; set; }
        public string MediaUrl { get; set; }
        public string Text { get; set; }
        public string UserName { get; set; }
        public DateTimeOffset Timestamp { get; set; }
    }
}