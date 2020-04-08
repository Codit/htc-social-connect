using System;

namespace CommunicationApi.Models
{
    public class ActivatedDevice
    {
        public string ActivationCode { get; set; }
        public string BoxId { get; set; }
        public BoxStatus Status { get; set; }
        public string AdminUserName { get; set; }
        public string AdminUserPhone { get; set; }
        public string Language { get; set; } = "nl-BE";
        public DateTime LastConnectedDateTime { get; internal set; }
    }
}