namespace CommunicationApi.Models
{
    public class UserInfo
    {
        public ActivatedDevice BoxInfo { get; set; }
        public string Name { get; set; } 
        public string PhoneNumber { get; set; }
        public ConversationState ConversationState { get; set; }

        public string GetLanguage()
        {
            return BoxInfo != null ? BoxInfo.Language : "nl-BE";
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