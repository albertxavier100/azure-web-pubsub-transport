using System;
using Unity.Netcode;

namespace Netcode.Transports.AzureWebPubSub
{
    internal class AWPSNetworkEvent

    {
        public ulong FromClientId { get; set; }
        public NetworkEvent NetworkEvent { get; set; }
        public ArraySegment<byte> Payload { get; set; }
    }
}