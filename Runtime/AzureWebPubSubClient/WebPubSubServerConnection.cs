using Azure.Messaging.WebPubSub.Clients;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace Netcode.Transports.AzureWebPubSub
{
    internal class WebPubSubServerConnection : WebPubSubConnection, IWebPubSubServerConnection
    {
        private Dictionary<ulong, string> _uClientIdToSubChannelMap { get; } = new();
        private HashSet<NetworkEvent> _connectivityEvents = new() { NetworkEvent.Connect, NetworkEvent.Disconnect };

        public override event Action<GroupData> NetworkDataEventReceived;

        protected override ulong UClientId => 0;

        protected override NegotiateType NegotiateType => NegotiateType.ServerConnect;

        protected override Func<string, Task> Connected => connectionId =>
        {
            State = ConnectionState.Connected;
            Debug.Log($"Connection {connectionId} connected.");
            return Task.CompletedTask;
        };

        protected override Func<GroupDataMessage, Task> GroupMessageReceived => message =>
        {
            try
            {
                if (message.Group != Negotiation.ConnectionContext.SubChannel)
                {
                    Debug.LogWarning($"Unsupported channel: {message.Group}");
                }
                var data = Deserialize<GroupData>(message.Data.ToString());
                if (data.NetworkEvent == NetworkEvent.Connect || data.NetworkEvent == NetworkEvent.Disconnect)
                {
                    return HandleClientConnectivityMessage(data);
                }
                else
                {
                    return HandleClientDataMessage(message);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return Task.CompletedTask;
            }
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

        public void AbortUClient(ulong uClientId)
        {
            var groupData = new GroupData()
            {
                NetworkEvent = NetworkEvent.Disconnect,
            };
            PublishDataToClientAsync(uClientId, groupData).Forget();
        }

        public UniTask PublishDataAsync(ulong targetClientId, ArraySegment<byte> data)
        {
            try
            {
                var groupData = new GroupData()
                {
                    Payload = data.ToArray(),
                    FromClient = UClientId,
                    NetworkEvent = NetworkEvent.Data
                };
                return PublishDataToClientAsync(targetClientId, groupData);
            }
            catch (Exception ex)
            {
                State = ConnectionState.Disconnected;
                Debug.LogException(ex);
                return UniTask.CompletedTask;
            }
        }

        private UniTask PublishDataToClientAsync(ulong targetClientId, GroupData groupData)
        {
            if (_uClientIdToSubChannelMap.TryGetValue(targetClientId, out var clientChannel))
            {
                return SendEventAsync(clientChannel, groupData);
            }

            Debug.LogWarning($"Failed to find subscribe channel for client {targetClientId}");
            return UniTask.CompletedTask;
        }

        private Task HandleClientDataMessage(GroupDataMessage message)
        {
            var groupData = Deserialize<GroupData>(message.Data.ToString());
            NetworkDataEventReceived?.Invoke(groupData);
            return Task.CompletedTask;
        }

        private Task HandleClientConnectivityMessage(GroupData data)
        {
            ManageClientConnectivity(data);
            return Task.CompletedTask;
        }

        private bool ManageClientConnectivity(GroupData groupData)
        {
            if (!_connectivityEvents.Contains(groupData.NetworkEvent))
            {
                Debug.LogWarning($"Not supported network event: {groupData.NetworkEvent}");
                return false;
            }
            var success = false;
            switch (groupData.NetworkEvent)
            {
                case NetworkEvent.Connect:
                    {
                        success = _uClientIdToSubChannelMap.TryAdd(groupData.FromClient, groupData.SubChannel);
                        break;
                    }
                case NetworkEvent.Disconnect:
                    {
                        success = _uClientIdToSubChannelMap.Remove(groupData.FromClient);
                        break;
                    }
                default:
                    Debug.LogWarning($"Unsupported event type: {groupData.NetworkEvent}");
                    return false;
            }

            var networkEvent = new AWPSNetworkEvent()
            {
                FromClientId = groupData.FromClient,
                NetworkEvent = groupData.NetworkEvent,
                Payload = new byte[] { },
            };

            Options.EventQueue.Push(networkEvent);

            var operation = groupData.NetworkEvent == NetworkEvent.Connect ? "add" : "remove";
            if (!success)
            {
                Debug.LogWarning($"Failed to {operation} client ID: {groupData.FromClient} with channel: {groupData.SubChannel}");
            }

            operation = groupData.NetworkEvent == NetworkEvent.Connect ? "Added" : "Removed";
            Debug.Log($"{operation} client ID {groupData.FromClient} with channel: {groupData.SubChannel}");

            return success;
        }
    }
}