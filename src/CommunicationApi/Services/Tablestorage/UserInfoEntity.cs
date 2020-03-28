using CommunicationApi.Models;

namespace CommunicationApi.Services.Tablestorage
{
    public class UserInfoEntity
    {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public ConversationState ConversationState { get; set; }
        public string TenantId { get; set; }

        public UserInfoEntity()
        {
        }

        public UserInfoEntity(UserInfo userInfo)
        {
            Name = userInfo.Name;
            PhoneNumber = userInfo.PhoneNumber;
            ConversationState = userInfo.ConversationState;
            TenantId = userInfo.TenantInfo?.Name;
        }


        public UserInfo Parse()
        {
            return new UserInfo
            {
                Name = Name,
                PhoneNumber = PhoneNumber,
                ConversationState = ConversationState,
                TenantInfo = new TenantInfo
                {
                    Name = TenantId,
                    Language = "nl-BE"
                }
            };
        }
    }
}