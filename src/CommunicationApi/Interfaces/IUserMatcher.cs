using System.Threading.Tasks;
using CommunicationApi.Models;

namespace CommunicationApi.Interfaces
{
    public interface IUserMatcher
    {
        Task<UserInfo> Match(string phoneNumber);
    }
}