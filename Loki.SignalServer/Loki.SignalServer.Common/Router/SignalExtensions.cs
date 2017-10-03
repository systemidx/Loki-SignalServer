using System.Text;
using Loki.SignalServer.Interfaces.Router;
using Newtonsoft.Json;

namespace Loki.SignalServer.Common.Router
{
    public static class SignalExtensions
    {
        /// <summary>
        /// Resolves the payload.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="signal">The signal.</param>
        /// <returns></returns>
        public static T ResolvePayload<T>(this ISignal signal)
        {
            if (signal == null)
                return default(T);

            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(signal.Payload));
        }

        /// <summary>
        /// Formats the specified signal type.
        /// </summary>
        /// <param name="signal">The signal.</param>
        /// <param name="signalType">Type of the signal.</param>
        /// <returns></returns>
        public static string Format(this ISignal signal, string signalType)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("Type: {0}\r\n", signalType);
            sb.AppendFormat("Route: {0}\r\n", signal.Route);
            sb.AppendFormat("Sender: {0}\r\n", signal.Sender);
            sb.AppendFormat("Recipient: {0}\r\n", signal.Recipient ?? "null");
            sb.AppendFormat("Payload Length: {0}\r\n", signal.Payload?.Length);

            return sb.ToString();
        }
    }
}