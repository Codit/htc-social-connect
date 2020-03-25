using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationHandlerApi.Extensions;
using CommunicationHandlerApi.Interfaces;
using CommunicationHandlerApi.Models;

namespace CommunicationHandlerApi.Services
{
    public class TwilioUserMatcher : IUserMatcher
    {
        public async Task<UserInfo> Match(IDictionary<string, string> parameters)
        {
            var phoneNumber = parameters.GetParameter("From", "").Replace("whatsapp:", "");
            
            return new UserInfo
            {
                Name = "Sam",
                PhoneNumber = phoneNumber,
                TenantInfo = new TenantInfo
                {
                    Name = "Test",
                    Paid = false
                }
            };
        }
    }
}