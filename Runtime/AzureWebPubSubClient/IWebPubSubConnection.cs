using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace Netcode.Transports.AzureWebPubSub
{
    internal interface IWebPubSubConnection
    {
        public void Init(ConnectionOptions options);

        public UniTask StartAsync(CancellationToken cancellationToken);

        public UniTask StopAsync();

        public UniTask PublishDataAsync(ulong targetClientId, ArraySegment<byte> data);

        public ConnectionState State { get; }

        public event Action<GroupData> NetworkDataEventReceived;
    }
}