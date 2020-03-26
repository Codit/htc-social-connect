using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CommunicationApi.Extensions;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;

namespace CommunicationApi.Services
{
    public class TwilioUserMatcher : IUserMatcher
    {
        public async Task<UserInfo> Match(IDictionary<string, string> parameters)
        {
            var phoneNumber = WebUtility.UrlDecode(parameters.GetParameter("From", "")).Replace("whatsapp:", "");
            
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