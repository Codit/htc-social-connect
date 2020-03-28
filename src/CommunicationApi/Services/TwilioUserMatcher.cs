using System.Threading.Tasks;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Services
{
    public class TwilioUserMatcher : IUserMatcher
    {
        private ILogger<TwilioUserMatcher> _logger;
        private IUserStore _userStore;

        public TwilioUserMatcher(ILogger<TwilioUserMatcher> logger, IUserStore userStore)
        {
            Guard.NotNull(logger, nameof(logger));
            Guard.NotNull(userStore, nameof(userStore));
            _userStore = userStore;
            _logger = logger;
        }

        public async Task<UserInfo> Match(string phoneNumber)
        {
            var userInfo = await _userStore.GetUserInfo(phoneNumber);
            if (userInfo == null)
            {
                _logger.LogWarning($"A new user sent a message from phone number {phoneNumber}");
                return null;
            }

            return userInfo;
        }
    }
}