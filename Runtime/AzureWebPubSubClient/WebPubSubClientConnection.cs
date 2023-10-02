using Azure.Messaging.WebPubSub.Clients;
using Cysharp.Threading.Tasks;
using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Netcode.Transports.AzureWebPubSub
{
    internal class WebPubSubClientConnection : WebPubSubConnection, IWebPubSubClientConnection
    {
        public override event Action<GroupData> NetworkDataEventReceived;

        protected override ulong UClientId => Negotiation.ConnectionContext.UClientId;
        protected override NegotiateType NegotiateType => NegotiateType.ClientConnect;

        protected override Func<string, Task> Connected => async connectionId =>
        {
            State = ConnectionState.Connected;
            try
            {
                await NotifyServerConnectivityEvent(NetworkEvent.Connect);
                var data = new AWPSNetworkEvent()
                {
                    FromClientId = UClientId,
                    NetworkEvent = NetworkEvent.Connect,
                    Payload = new byte[] { },
                };
                Options.EventQueue.Push(data);
                Debug.Log($"Connection {connectionId} connected.");
            }
            catch (Exception ex)
            {
                State = ConnectionState.ConnectFailed;
                Debug.LogException(ex);
            }
        };

        protected override Func<GroupDataMessage, Task> GroupMessageReceived => message =>
        {
            try
            {
                var groupData = Deserialize<GroupData>(message.Data.ToString());
                NetworkDataEventReceived?.Invoke(groupData);
            }
            catch (Exception ex)
            {
                State = ConnectionState.Disconnected;
                Debug.LogException(ex);
            }

            return Task.CompletedTask;
        };

        protected override Func<string, string, Task> Disconnected => (connectionId, reason) =>
        {
            Debug.Log($"Connection {connectionId} disconnected.");
            State = ConnectionState.Disconnected;
            return Task.CompletedTask;
        };

        protected override Func<Task> Stopped => () =>
        {
            Debug.Log($"Connection stopped.");
            State = ConnectionState.Stopped;
            return Task.CompletedTask;
        };

        public async UniTask SendDisconnectEventToServer()
        {
            var groupData = new GroupData()
            {
                FromClient = Negotiation.ConnectionContext.UClientId,
                NetworkEvent = NetworkEvent.Disconnect,
            };
            await SendEventAsync(Negotiation.ConnectionContext.PubChannel, groupData);
        }

        public async UniTask PublishDataAsync(ulong _, ArraySegment<byte> data)
        {
            try
            {
                var groupData = new GroupData()
                {
                    FromClient = Negotiation.ConnectionContext.UClientId,
                    Payload = data.ToArray(),
                    NetworkEvent = NetworkEvent.Data,
                };
                await SendEventAsync(Negotiation.ConnectionContext.PubChannel, groupData);
            }
            catch (Exception ex)
            {
                State = ConnectionState.Disconnected;
                Debug.Log($"Connection failed to publish data.");
                Debug.LogException(ex);
                throw;
            }
        }

        private async UniTask NotifyServerConnectivityEvent(NetworkEvent networkEvent)
        {
            var groupData = new GroupData()
            {
                FromClient = UClientId,
                SubChannel = Negotiation.ConnectionContext.SubChannel,
                NetworkEvent = networkEvent
            };
            await SendEventAsync(Negotiation.ConnectionContext.PubChannel, groupData);
        }
    }
}