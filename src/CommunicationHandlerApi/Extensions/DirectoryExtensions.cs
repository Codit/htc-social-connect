using System.Collections.Generic;

namespace CommunicationHandlerApi.Extensions
{
    public static class DirectoryExtensions
    {
        public static string GetParameter(this IDictionary<string, string> pars, string parameterName, string defaultValue = null)
        {
            var response =  pars.ContainsKey(parameterName) ? pars[parameterName] : defaultValue;
            return response;
        }
    }
}