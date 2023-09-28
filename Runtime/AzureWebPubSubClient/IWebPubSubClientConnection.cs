using Cysharp.Threading.Tasks;

namespace Netcode.Transports.AzureWebPubSub
{
    internal interface IWebPubSubClientConnection : IWebPubSubConnection
    {
        public UniTask SendDisconnectEventToServer();
    }
}