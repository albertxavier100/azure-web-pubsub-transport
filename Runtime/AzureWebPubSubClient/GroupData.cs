using Unity.Netcode;

namespace Netcode.Transports.AzureWebPubSub
{
    public class GroupData
    {
        // required
        public ulong FromClient { get; set; }

        public NetworkEvent NetworkEvent { get; set; }

        // optional
        public string SubChannel { get; set; }

        public byte[] Payload { get; set; }

        public override string ToString()
        {
            return
                $"from client: {FromClient};\n" +
                $"payload len: {Payload?.Length};\n" +
                $"subscribe channel: {SubChannel};\n" +
                $"network event: {NetworkEvent};\n";
        }
    }
}