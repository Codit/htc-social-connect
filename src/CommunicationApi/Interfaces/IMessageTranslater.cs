using System.Threading.Tasks;
using Twilio.TwiML.Voice;

namespace CommunicationApi.Interfaces
{
    public interface IMessageTranslater
    {
        Task<string> Translate(string language, string message, params object[] pars);
    }
}