namespace CommunicationApi.Contracts.v1
{
    public class ActivatedDevice
    {
        public string ActivationCode { get; set; }
        public string BoxId { get; set; }
        public BoxStatus Status { get; set; }
    }
}