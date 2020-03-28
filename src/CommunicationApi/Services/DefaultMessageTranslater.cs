using System.Threading.Tasks;
using CommunicationApi.Interfaces;

namespace CommunicationApi.Services
{
    public class DefaultMessageTranslater : IMessageTranslater
    {
        public Task<string> Translate(string language, string message, params object[] pars)
        {
            return Task.FromResult(string.Format(message, pars));
        }
    }
}