using Loki.Common.Events;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.Server.Attributes;
using Loki.Server.Data;
using Loki.SignalServer.Interfaces.Router;
using Loki.SignalServer.Router;
using Newtonsoft.Json;

namespace Loki.SignalServer.Routes
{
    [ConnectionRoute("/")]
    public class Default : WebSocketDataHandler
    {
        public Default(IDependencyUtility dependencyUtility) : base(dependencyUtility)
        {
        }

        public override void OnOpen(IWebSocketConnection sender, ConnectionOpenedEventArgs args)
        {
            //Make sure that we have an ID in the querystring
            if (string.IsNullOrWhiteSpace(args.Querystrings["id"]))
                sender.Close();

            //Assign it to the client identifier
            sender.ClientIdentifier = args.Querystrings["id"];

            Logger.Info($"{sender.ClientIdentifier}/{sender.UniqueIdentifier} connected");

            base.OnOpen(sender, args);
        }

        public override void OnBinary(IWebSocketConnection sender, BinaryFrameEventArgs args)
        {
            base.OnBinary(sender, args);
        }

        public override void OnText(IWebSocketConnection sender, TextFrameEventArgs args)
        {
            ISignal signal = JsonConvert.DeserializeObject<Signal>(args.Message);

            base.OnText(sender, args);
        }
    }
}