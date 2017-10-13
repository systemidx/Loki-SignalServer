using System.Data;
using System.Data.SqlClient;
using Dapper;
using Loki.Common.Events;
using Loki.Interfaces.Connections;
using Loki.Interfaces.Dependency;
using Loki.Server.Attributes;
using Loki.Server.Data;
using Loki.SignalServer.Common.Router;
using Loki.SignalServer.Extensions.Interfaces;
using Loki.SignalServer.Interfaces.Configuration;
using Loki.SignalServer.Interfaces.Exceptions;
using Loki.SignalServer.Interfaces.Router;
using Newtonsoft.Json;

namespace Loki.SignalServer.Routes
{
    [ConnectionRoute("/")]
    public class Default : WebSocketDataHandler
    {
        #region Readonly Variables

        /// <summary>
        /// The extension loader
        /// </summary>
        private readonly IExtensionLoader _extensionLoader;

        /// <summary>
        /// The signal router
        /// </summary>
        private readonly ISignalRouter _signalRouter;
        
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="Default"/> class.
        /// </summary>
        /// <param name="dependencyUtility">The dependency utility.</param>
        public Default(IDependencyUtility dependencyUtility) : base(dependencyUtility)
        {
            _extensionLoader = dependencyUtility.Resolve<IExtensionLoader>();
            _signalRouter = dependencyUtility.Resolve<ISignalRouter>();
        }

        #endregion

        #region Overrides

        /// <summary>
        /// Called when [open].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ConnectionOpenedEventArgs"/> instance containing the event data.</param>
        public override void OnOpen(IWebSocketConnection sender, ConnectionOpenedEventArgs args)
        {
            if (!Authenticate(sender, args))
                return;

            Logger.Info($"{sender.ClientIdentifier}/{sender.UniqueIdentifier} connected");
            
            RegisterInExtensions(sender);

            base.OnOpen(sender, args);
        }

        /// <summary>
        /// Raises the Close event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ConnectionClosedEventArgs"/> instance containing the event data.</param>
        public override void OnClose(IWebSocketConnection sender, ConnectionClosedEventArgs args)
        {
            Logger.Info($"{sender.ClientIdentifier}/{sender.UniqueIdentifier} disconnected");
            
            UnregisterInExtensions(sender);

            base.OnClose(sender, args);
        }

        /// <summary>
        /// Called when [text].
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="TextFrameEventArgs"/> instance containing the event data.</param>
        public override void OnText(IWebSocketConnection sender, TextFrameEventArgs args)
        {
            ISignal signal = JsonConvert.DeserializeObject<Signal>(args.Message);

            if (signal.Sender != sender.ClientIdentifier)
            { 
                Logger.Warn($"Signal from {sender.ClientIdentifier} sent with spoofed sender ({signal.Sender}). Forcibly setting to connection's client identifier.");

                signal.Sender = sender.ClientIdentifier;
            }

            signal.SenderIdentifier = sender.UniqueIdentifier;

            try
            {
                _signalRouter.Route(signal);
            }
            catch (InvalidExtensionException ex)
            {
                Logger.Error(ex);
            }

            base.OnText(sender, args);
        }
        
        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Registers the in extensions.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void RegisterInExtensions(IWebSocketConnection sender)
        {
            foreach (IExtension extension in _extensionLoader.Extensions)
                extension.RegisterConnection(sender);
        }

        /// <summary>
        /// Unregisters the in extensions.
        /// </summary>
        /// <param name="sender">The sender.</param>
        private void UnregisterInExtensions(IWebSocketConnection sender)
        {
            foreach (IExtension extension in _extensionLoader.Extensions)
                extension.UnregisterConnection(sender);
        }

        /// <summary>
        /// Authenticates the specified connection.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The <see cref="ConnectionOpenedEventArgs"/> instance containing the event data.</param>
        /// <returns></returns>
        private bool Authenticate(IWebSocketConnection sender, ConnectionOpenedEventArgs args)
        {
            string userDomainId = args.Querystrings["userdomainid"];
            string userId = args.Querystrings["userid"];
            string token = args.Querystrings["token"];

            //Make sure that we have an ID in the querystring
            if (string.IsNullOrWhiteSpace(userDomainId) || string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
            {
                sender.Close();
                return false;
            }

            //Assign it to the client identifier
            sender.ClientIdentifier = $"{userId}";
            sender.Metadata["token"] = token;
            sender.Metadata["domainId"] = userDomainId;

            return true;
        }

        #endregion
    }
}