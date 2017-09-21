using System.Text;
using Loki.SignalServer.Interfaces.Router;
using Newtonsoft.Json;

namespace Loki.SignalServer.Common.Router
{
    public static class SignalExtensions
    {
        public static T ResolvePayload<T>(this ISignal signal)
        {
            if (signal == null)
                return default(T);

            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(signal.Payload));
        }
    }
}