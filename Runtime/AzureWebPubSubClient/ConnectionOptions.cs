namespace Netcode.Transports.AzureWebPubSub
{
    internal struct ConnectionOptions
    {
        public ServiceType ServiceType { get; set; }
        public string NegotiateEndpoint { get; set; }
        public string RoomId { get; set; }
        public NetworkEventQueue EventQueue { get; set; }

        public override string ToString()
        {
            return $"service type: {ServiceType};\n" +
                $"negotiate endpoint: {NegotiateEndpoint};\n" +
                $"room id: {RoomId};\n";
        }
    }
}