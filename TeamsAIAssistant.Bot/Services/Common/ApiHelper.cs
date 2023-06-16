using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using TeamsAIAssistant.Bot.Interfaces.Common;
using System.Text;

namespace TeamsAIAssistant.Bot.Services.Common
{
    public class ApiHelper<T> : IApiHelper<T> where T : class
    {
        public async Task<T> MakeApiCallAsync<T>(string endpoint, HttpMethod method, object data = null, Dictionary<string, string> headers = null)
        {
            T result = default(T);

            using (HttpClient client = new HttpClient())
            {
                if (headers != null && headers.Count > 0)
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    foreach (var item in headers)
                        client.DefaultRequestHeaders.Add(item.Key, item.Value);
                }

                HttpRequestMessage request = new HttpRequestMessage(method, endpoint);

                if (data != null)
                {
                    string jsonData = JsonConvert.SerializeObject(data);
                    request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                }

                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    string content = await response.Content.ReadAsStringAsync();
                    result = JsonConvert.DeserializeObject<T>(content);
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    return default(T);
                }
            }

            return result;
        }

    }
}
