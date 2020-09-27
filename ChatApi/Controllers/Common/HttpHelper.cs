using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ChatApi.Controllers.Common
{
    public static class HttpHelper
    {

        public const string ROOMS_API = "/api/RoomsData";
        public const string MESSAGES_API = "/api/MessagesData";

        public static StringContent GetJSONContent(object o)
        {
            StringContent content = new StringContent(JsonConvert.SerializeObject(o, Formatting.Indented), Encoding.UTF8, "application/json");
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return content;
        }
    }
}
