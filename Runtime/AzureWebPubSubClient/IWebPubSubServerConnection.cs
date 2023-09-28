using Cysharp.Threading.Tasks;

namespace Netcode.Transports.AzureWebPubSub
{
    internal interface IWebPubSubServerConnection : IWebPubSubConnection
    {
        public void AbortUClient(ulong uClientId);
    }
}