using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Router
{
    public class Signal : ISignal
    {
        private string _module;
        private string _action;
        private string _route;

        public string Module => _module;
        public string Action => _action;

        public string Sender { get; set; }
        public string Recipient { get; set; }

        public string Route
        {
            get => _route;
            set
            {
                _route = value;

                if (value == null || !value.Contains("/"))
                    return;

                string[] values = value.Split('/');

                _module = values[0];

                if (values.Length == 2)
                    _action = values[1];
            }
        }

        public byte[] Payload { get; set; }

        
    }
}
