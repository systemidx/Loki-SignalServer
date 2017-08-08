namespace Loki.SignalServer.Interfaces.Router
{
    public interface ISignal
    {
        string Route { get; set; }
        string Module { get; }
        string Action { get; }

        string Sender { get; set; }
        string Recipient { get; set; }

        byte[] Payload { get; set; }
    }
}