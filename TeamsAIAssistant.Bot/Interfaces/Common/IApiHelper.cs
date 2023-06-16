using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace TeamsAIAssistant.Bot.Interfaces.Common
{
    public interface IApiHelper<T> where T : class
    {
        Task<T> MakeApiCallAsync<T>(string endpoint, HttpMethod method, object data = null, Dictionary<string, string> headers = null);
    }
}
