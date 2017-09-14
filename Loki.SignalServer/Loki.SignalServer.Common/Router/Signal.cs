using Loki.SignalServer.Interfaces.Router;

namespace Loki.SignalServer.Common.Router
{
    public class Signal : ISignal
    {
        private string _extension;
        private string _action;
        private string _route;

        public string Extension => _extension;
        public string Action => _action;

        public string Sender { get; set; }
        public string Recipient { get; set; }

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

        public byte[] Payload { get; set; }
        public bool IsValid => !(string.IsNullOrEmpty(Sender) || string.IsNullOrEmpty(Extension) || string.IsNullOrEmpty(Action));
    }
}
