using System.Collections.Generic;
using System.Threading.Tasks;
using CommunicationHandlerApi.Models;

namespace CommunicationHandlerApi.Interfaces
{
    public interface IWhatsappHandlerService
    {
        Task<WhatsappResponse> ProcessRequest(Dictionary<string,string> pars);
    }
}