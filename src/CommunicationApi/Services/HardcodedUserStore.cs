using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;

namespace CommunicationApi.Services
{
    public class HardcodedUserStore : IUserStore
    {
        private static List<UserInfo> _inMemoryUsers = new List<UserInfo>();
        private static List<TenantInfo> _inMemoryTenants = new List<TenantInfo>();
        public Task<UserInfo> GetUserInfo(string phoneNumber)
        {
            var user = _inMemoryUsers.FirstOrDefault(u => u.PhoneNumber.Equals(phoneNumber));
            return Task.FromResult(user ?? new UserInfo
            {
                ConversationState = ConversationState.New,
                Name = null, PhoneNumber = phoneNumber, TenantInfo = null
            });
        }

        public Task<UserInfo> CreateUser(string phoneNumber, string name)
        {
            var userInfo = new UserInfo
            {
                ConversationState = ConversationState.AwaitingName,
                Name = name,
                PhoneNumber = phoneNumber,
                TenantInfo = null
            };
            _inMemoryUsers.Add(userInfo);
            return Task.FromResult(userInfo);
        }

        public async Task<UserInfo> LinkUserToTenant(string phoneNumber, string tenantId)
        {
            var tenant =
                _inMemoryTenants.FirstOrDefault(t =>
                    t.Name.Equals(tenantId, StringComparison.InvariantCultureIgnoreCase));
            if (tenant != null)
            {
                var user = await GetUserInfo(phoneNumber);
                user.TenantInfo = tenant;
                return user;
            }

            return null;
        }

        public Task<IEnumerable<UserInfo>> GetUsersFromTenant(string tenant)
        {
            throw new System.NotImplementedException();
        }
    }
}