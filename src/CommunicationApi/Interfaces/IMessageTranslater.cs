using System.Threading.Tasks;

namespace CommunicationApi.Interfaces
{
    public interface IMessageTranslater
    {
        Task<string> Translate(string language, string message, params object[] pars);
    }
}