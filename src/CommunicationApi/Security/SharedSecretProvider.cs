using System.Threading.Tasks;
using Arcus.Security.Secrets.Core.Interfaces;

namespace CommunicationApi.Security
{
    public class SharedSecretProvider : ISecretProvider
    {
        public Task<string> Get(string secretName)
        {
            return Task.FromResult("H@ckCr1s!s");
        }
    }
}