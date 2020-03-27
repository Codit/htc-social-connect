using System.Collections.Generic;
using System.Net;
using CommunicationApi.Extensions;

namespace CommunicationApi.Models
{
    public class WhatsappMessage
    {
        public WhatsappMessage()
        {
            
        }

        public WhatsappMessage(IDictionary<string, string> formParameters)
        {
            Sender = WebUtility.UrlDecode(formParameters.GetParameter("From", "")).Replace("whatsapp:", "");
            Destination = WebUtility.UrlDecode(formParameters.GetParameter("To", "")).Replace("whatsapp:", "");
            var mediaCount = int.Parse((string) formParameters.GetParameter("NumMedia", "0"));
            MessageContent= formParameters.GetParameter("Body");
            MessageId= formParameters.GetParameter("MessageSid");
            MediaItems=new List<WhatsappMediaItem>();
            RawValues = formParameters;
            if (mediaCount > 0)
            {
                var mediaFound = true;
                int currentMediaId = 0;
                while (mediaFound)
                {
                    var mediaUrl = formParameters.GetParameter($"MediaUrl{currentMediaId}");
                    mediaFound = !string.IsNullOrEmpty(mediaUrl);
                    if (mediaFound)
                    {
                        MediaItems.Add(new WhatsappMediaItem
                        {
                            Url = WebUtility.UrlDecode(formParameters.GetParameter($"MediaUrl{currentMediaId}")),
                            ContentType = formParameters.GetParameter($"MediaContentType{currentMediaId}"),
                        });
                    }
                    currentMediaId++;
                }
            }
        }
        
        public string MessageContent { get; set; }
        public List<WhatsappMediaItem> MediaItems { get; set; }
        public string Sender { get; set; }
        public string Destination { get; set; }
        public string MessageId { get; set; }
        public IDictionary<string, string> RawValues { get; set; }
    }

    public class WhatsappMediaItem
    {
        public string Url { get; set; }
        public string ContentType { get; set; }
    }
}