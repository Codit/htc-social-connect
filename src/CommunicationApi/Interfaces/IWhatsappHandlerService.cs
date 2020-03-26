using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationApi.Models;

namespace CommunicationApi.Interfaces
{
    public interface IWhatsappHandlerService
    {
        Task<WhatsappResponse> ProcessRequest(Dictionary<string,string> pars);
    }
}