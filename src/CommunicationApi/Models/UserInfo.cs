namespace CommunicationApi.Models
{
    public class UserInfo
    {
        public TenantInfo TenantInfo { get; set; }
        public string Name { get; set; } 
        public string PhoneNumber { get; set; }
        public ConversationState ConversationState { get; set; }

        public string GetLanguage()
        {
            return TenantInfo != null ? TenantInfo.Language : "nl-BE";
        }
    }

    public enum ConversationState
    {
        New,
        AwaitingName,
        AwaitingActivation,
        Completed
    }
}