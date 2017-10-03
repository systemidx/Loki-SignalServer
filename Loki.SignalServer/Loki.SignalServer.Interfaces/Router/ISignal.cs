namespace Loki.SignalServer.Interfaces.Router
{
    public interface ISignal
    {
        /// <summary>
        /// Gets or sets the route.
        /// </summary>
        /// <value>
        /// The route.
        /// </value>
        string Route { get; set; }

        /// <summary>
        /// Gets the extension.
        /// </summary>
        /// <value>
        /// The extension.
        /// </value>
        string Extension { get; }

        /// <summary>
        /// Gets the action.
        /// </summary>
        /// <value>
        /// The action.
        /// </value>
        string Action { get; }

        /// <summary>
        /// Gets or sets the sender.
        /// </summary>
        /// <value>
        /// The sender.
        /// </value>
        string Sender { get; set; }

        /// <summary>
        /// Gets or sets the recipient.
        /// </summary>
        /// <value>
        /// The recipient.
        /// </value>
        string Recipient { get; set; }

        /// <summary>
        /// Gets or sets the payload.
        /// </summary>
        /// <value>
        /// The payload.
        /// </value>
        byte[] Payload { get; set; }

        /// <summary>
        /// Returns true if ... is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        bool IsValid { get; }
    }
}