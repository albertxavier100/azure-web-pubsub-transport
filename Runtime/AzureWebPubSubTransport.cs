using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using Unity.Netcode;
using UnityEngine;

#if UNITY_WEBGL

using System.Runtime.InteropServices;

#endif

namespace Netcode.Transports.AzureWebPubSub
{
    public class AzureWebPubSubTransport : NetworkTransport
    {
        #region Inspector settings

        public string NegotiateEndpoint = "http://localhost:5289/negotiate";
        public string RoomId = "DefaultRoom";

        private ConnectionOptions _options;

        #endregion Inspector settings

        #region Messaging

        public override NetworkEvent PollEvent(out ulong clientId, out ArraySegment<byte> payload, out float receiveTime)
        {
            if (_options.EventQueue.Count > 0)
            {
                var currentEvent = _options.EventQueue.Poll();
                clientId = currentEvent.FromClientId;
                receiveTime = Time.realtimeSinceStartup;
                payload = currentEvent.Payload;
                return currentEvent.NetworkEvent;
            }
            else
            {
                clientId = ServerClientId;
                receiveTime = Time.realtimeSinceStartup;
                payload = default;
                return NetworkEvent.Nothing;
            }
        }

        public override void Send(ulong uClientId, ArraySegment<byte> data, NetworkDelivery delivery)
        {
            _connection.PublishDataAsync(uClientId, data.ToArray()).Forget();
        }

        #endregion Messaging

        #region connection lifetime

        // Current connection state. e.g. Used to wait until webSocket connection is truly ready
        public ConnectionState? State => _connection?.State ?? ConnectionState.Init;

        public override ulong ServerClientId => 0;
        private IWebPubSubConnection _connection;
        private CancellationTokenSource _connectCts = new();
        private ConnectionData _connectionData = new();

        public override void Initialize(NetworkManager networkManager = null)
        {
        }

        public override bool StartClient()
        {
            SetConnectionOptions();
            _connection = new WebPubSubClientConnection();
            _connection.Init(_options);
            _connection.NetworkDataEventReceived += data =>
            {
                var networkEvent = new AWPSNetworkEvent()
                {
                    FromClientId = data.FromClient,
                    NetworkEvent = data.NetworkEvent,
                    Payload = data.Payload,
                };
                _options.EventQueue.Push(networkEvent);
            };
            _connection.StartAsync(_connectCts.Token).Forget();
            return true;
        }

        public override bool StartServer()
        {
            SetConnectionOptions();
            _connection = new WebPubSubServerConnection();
            _connection.Init(_options);
            _connection.NetworkDataEventReceived += data =>
            {
                var networkEvent = new AWPSNetworkEvent()
                {
                    FromClientId = data.FromClient,
                    NetworkEvent = data.NetworkEvent,
                    Payload = data.Payload,
                };
                _options.EventQueue.Push(networkEvent);
            };
            _connection.StartAsync(_connectCts.Token).Forget();
            return true;
        }

        // Disconnects a client from the server
        public override void DisconnectRemoteClient(ulong clientId)
        {
            (_connection as IWebPubSubServerConnection)?.AbortUClient(clientId);
        }

        // Client only, send disconnect to server
        public override void DisconnectLocalClient()
        {
            (_connection as IWebPubSubClientConnection)?.SendDisconnectEventToServer().Forget();
        }

        public override ulong GetCurrentRtt(ulong clientId)
        {
            return 0;
        }

        public override void Shutdown()
        {
            _connection?.StopAsync().Forget();
        }

        public void SetConnectionData(string roomId)
        {
            if (State != ConnectionState.Init)
            {
                throw new AzureRealtimeTransportException($"Current transport must be {ConnectionState.Init}, but: {State}");
            }
            _connectionData.RoomId = roomId;
        }

        private void SetConnectionOptions()
        {
            _options = new ConnectionOptions()
            {
                EventQueue = new(),
                NegotiateEndpoint = NegotiateEndpoint,
                ServiceType = ServiceType.WebPubSub,
                RoomId = _connectionData.RoomId,
            };
        }

        // unity methods

#if UNITY_EDITOR || PLATFORM_SUPPORTS_MONO

        private void Awake()
        {
            _connectionData.RoomId = RoomId;
        }

#elif UNITY_WEBGL

        [DllImport("__Internal")]
        private static extern void AddWebPubSubScript();

        private void Awake()
        {
            AddWebPubSubScript();
            _connectionData.RoomId = RoomId;
        }

#endif

        #endregion connection lifetime
    }
}