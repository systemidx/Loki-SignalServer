using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Loki.Common.Events;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.Server.Attributes;
using Loki.Server.Data;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Router;
using Loki.SignalServer.Router;
using Newtonsoft.Json;

namespace Loki.SignalServer.Routes
{
    [ConnectionRoute("/")]
    public class Default : WebSocketDataHandler
    {
        private readonly IExtensionLoader _extensionLoader;
        private readonly ISignalRouter _signalRouter;

        public Default(IDependencyUtility dependencyUtility) : base(dependencyUtility)
        {
            _extensionLoader = dependencyUtility.Resolve<IExtensionLoader>();
            _signalRouter = dependencyUtility.Resolve<ISignalRouter>();
        }

        public override void OnOpen(IWebSocketConnection sender, ConnectionOpenedEventArgs args)
        {
            if (!Authenticate(sender, args))
                return;

            Logger.Info($"{sender.ClientIdentifier}/{sender.UniqueIdentifier} connected");

            RegisterInExtensions(sender);

            base.OnOpen(sender, args);
        }

        public override void OnClose(IWebSocketConnection sender, ConnectionClosedEventArgs args)
        {
            //Logger.Info($"{sender.ClientIdentifier}/{sender.UniqueIdentifier} disconnected");

            //UnregisterInExtensions(sender);

            base.OnClose(sender, args);
        }

        public override void OnText(IWebSocketConnection sender, TextFrameEventArgs args)
        {
            ISignal signal = JsonConvert.DeserializeObject<Signal>(args.Message);
            _signalRouter.Route(signal);

            base.OnText(sender, args);
        }

        
        
        public override void OnTextPart(IWebSocketConnection sender, TextMultiFrameEventArgs args)
        {
            

            base.OnTextPart(sender, args);
        }

        private void RegisterInExtensions(IWebSocketConnection sender)
        {
            foreach (IExtension extension in _extensionLoader.Extensions)
                extension.RegisterConnection(sender);
        }

        private void UnregisterInExtensions(IWebSocketConnection sender)
        {
            
        }

        /// <summary>
        /// Authenticates the specified connection.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ConnectionOpenedEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        private bool Authenticate(IWebSocketConnection sender, ConnectionOpenedEventArgs args)
        {
            //Make sure that we have an ID in the querystring
            //if (string.IsNullOrWhiteSpace(args.Querystrings["id"]))
            //{ 
            //    sender.Close();
            //    return false;
            //}

            //Assign it to the client identifier
            sender.ClientIdentifier = args.Querystrings["id"];
            
            return true;
        }
    }
}