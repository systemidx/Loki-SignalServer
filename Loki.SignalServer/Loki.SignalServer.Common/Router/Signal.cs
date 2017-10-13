using System;
using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Common.Router
{
    public class Signal : ISignal
    {
        #region Private Variables

        /// <summary>
        /// The extension
        /// </summary>
        private string _extension;

        /// <summary>
        /// The action
        /// </summary>
        private string _action;

        /// <summary>
        /// The route
        /// </summary>
        private string _route;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the extension.
        /// </summary>
        /// <value>
        /// The extension.
        /// </value>
        public string Extension => _extension;

        /// <summary>
        /// Gets the action.
        /// </summary>
        /// <value>
        /// The action.
        /// </value>
        public string Action => _action;

        /// <summary>
        /// Gets or sets the sender.
        /// </summary>
        /// <value>
        /// The sender.
        /// </value>
        public string Sender { get; set; }

        /// <summary>
        /// Gets or sets the sender unique identifier.
        /// </summary>
        /// <value>
        /// The sender unique identifier.
        /// </value>
        public Guid SenderIdentifier { get; set; }

        /// <summary>
        /// Gets or sets the recipient.
        /// </summary>
        /// <value>
        /// The recipient.
        /// </value>
        public string Recipient { get; set; }

        /// <summary>
        /// Gets or sets the route.
        /// </summary>
        /// <value>
        /// The route.
        /// </value>
        public string Route
        {
            get => _route;
            set
            {
                _route = value.ToLowerInvariant();

                if (!value.Contains("/"))
                    return;

                string[] values = value.Split('/');

                _extension = values[0];

                if (values.Length == 2)
                    _action = values[1];
            }
        }

        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        /// <value>
        /// The payload.
        /// </value>
        public byte[] Payload { get; set; }

        /// <summary>
        /// Returns true if ... is valid.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public bool IsValid => !(string.IsNullOrEmpty(Sender) || string.IsNullOrEmpty(Extension) || string.IsNullOrEmpty(Action));

        #endregion
    }
}
