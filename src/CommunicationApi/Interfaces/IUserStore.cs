using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationApi.Models;

namespace CommunicationApi.Interfaces
{
    public interface IUserStore
    {
        Task<UserInfo> GetUserInfo(string phoneNumber);
        Task<UserInfo> CreateUser(string phoneNumber, string name, ConversationState conversationState = ConversationState.New);
        Task<UserInfo> LinkUserToTenant(string phoneNumber, string tenant);
        Task<IEnumerable<UserInfo>> GetUsersFromTenant(string tenant);
        Task UpdateUser(UserInfo userInfo);
    }
}