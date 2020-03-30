using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Security.Core;
using CommunicationApi.Interfaces;
using CommunicationApi.Models;
using Microsoft.Extensions.Logging;

namespace CommunicationApi.Services.Tablestorage
{
    public class TableUserStorage : TableTransmitter<UserInfoEntity>, IUserStore
    {
        private string PartitionKey = "User";

        public TableUserStorage(ISecretProvider secretProvider, ILogger<TableStorageBoxStore> logger) :
            base( "users", secretProvider, logger)
        {
        }

        public async Task<UserInfo> GetUserInfo(string phoneNumber)
        {
            var tableEntity = await GetItem(PartitionKey, phoneNumber);
            return tableEntity == null ? new UserInfo{PhoneNumber = phoneNumber} : tableEntity.Parse();
        }

        public async Task<UserInfo> CreateUser(string phoneNumber, string name,
            ConversationState conversationState = ConversationState.New)
        {
            var userInfo = new UserInfo
            {
                ConversationState = conversationState,
                Name = name, PhoneNumber = phoneNumber
            };
            await Upsert(new UserInfoEntity(userInfo), PartitionKey, phoneNumber);
            return userInfo;
        }

        public async Task<UserInfo> LinkUserToTenant(string phoneNumber, string tenant)
        {
            var tableEntity = await GetItem(PartitionKey, phoneNumber);

            if (tableEntity != null)
            {
                tableEntity.TenantId = tenant;
                await Upsert(tableEntity, PartitionKey, phoneNumber);
                return (await GetItem(PartitionKey, phoneNumber)).Parse();
            }

            return null;
        }

        public async Task<IEnumerable<UserInfo>> GetUsersFromTenant(string tenant)
        {
            var users = new List<UserInfo> { };
            foreach (var userEntity in (await GetItems(PartitionKey)).Where(u =>
                u.TenantId.Equals(tenant, StringComparison.InvariantCultureIgnoreCase)))
            {
                users.Add(userEntity.Parse());
            }

            return users;
        }

        public async Task UpdateUser(UserInfo userInfo)
        {
            if (!string.IsNullOrEmpty(userInfo?.PhoneNumber))
            {
                await Upsert(new UserInfoEntity(userInfo), PartitionKey, userInfo.PhoneNumber);
            }
        }
    }
}