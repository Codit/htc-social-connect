using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationHandlerApi.Models;

namespace CommunicationHandlerApi.Interfaces
{
    public interface IUserMatcher
    {
        Task<UserInfo> Match(IDictionary<string, string> parameters);
    }
}