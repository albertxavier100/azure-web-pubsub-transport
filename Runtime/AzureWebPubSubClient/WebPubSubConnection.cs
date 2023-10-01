using Azure.Messaging.WebPubSub.Clients;
using Cysharp.Threading.Tasks;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_WEBGL && !UNITY_EDITOR

using AOT;
using System.Runtime.InteropServices;

#endif

namespace Netcode.Transports.AzureWebPubSub
{
    internal abstract class WebPubSubConnection

    {
        public ConnectionState State { get; protected set; }

        protected NegotiateResponse Negotiation { get; private set; }

        protected JsonSerializerOptions JsonSerializerOptions => new()
        {
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = true,
        };

        protected ConnectionOptions Options { get; set; }

        private WebPubSubClient _client;

        private CancellationTokenSource _cts = new();

        private long _ackId = 0;

        public abstract event Action<GroupData> NetworkDataEventReceived;

        protected abstract ulong UClientId { get; }

        protected abstract NegotiateType NegotiateType { get; }
        protected abstract Func<GroupDataMessage, Task> GroupMessageReceived { get; }
        protected abstract Func<string, Task> Connected { get; }
        protected abstract Func<string, string, Task> Disconnected { get; }
        protected abstract Func<Task> Stopped { get; }
        protected Func<Task> RejoinGroupFailed { get; }

        public WebPubSubConnection()
        {
        }

#if UNITY_EDITOR || PLATFORM_SUPPORTS_MONO

        public void Init(ConnectionOptions options)
        {
            Options = options;
            State = ConnectionState.Init;

            var cred = new WebPubSubClientCredential(async _ =>
            {
                var uri = await NegotiateAsync();
                return uri;
            });
            _client = new WebPubSubClient(cred);
            _client.Connected += e => Connected(e.ConnectionId);
            _client.Disconnected += e => Disconnected(e.ConnectionId, e.DisconnectedMessage.Reason);
            // manually call StopAsync() or disable AutoReconnect
            _client.Stopped += e => Stopped();
            _client.GroupMessageReceived += e => GroupMessageReceived(e.Message);

            // join group permission maybe revoke before reconnect
            _client.RejoinGroupFailed += e => RejoinGroupFailed();
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                State = ConnectionState.Connecting;
                await _client.StartAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                State = ConnectionState.ConnectFailed;
                Debug.LogException(ex);
                throw ex;
            }
        }

        public async UniTask StopAsync()
        {
            try
            {
                State = ConnectionState.Stopping;
                _cts.Cancel();
                await _client.StopAsync().ConfigureAwait(false);
                State = ConnectionState.Stopped;
            }
            catch (Exception ex)
            {
                State = ConnectionState.StopFailed;
                Debug.LogException(ex);
                throw ex;
            }
        }

        protected async UniTask SendEventAsync(string channel, GroupData data)
        {
            try
            {
                var dataStr = JsonSerializer.Serialize(data);
                var content = new BinaryData(dataStr);

                if (State == ConnectionState.Connected)
                {
                    await _client.SendToGroupAsync(channel, content, WebPubSubDataType.Text, (ulong)Interlocked.Increment(ref _ackId), cancellationToken: _cts.Token);
                }
                else
                {
                    Debug.LogWarning($"Failed to send to channel {channel}, due to client state ({State}) is not connected");
                }
            }
            catch (SendMessageFailedException ex)
            {
                Debug.LogWarning($"Cancelled sending events. {ex}");
            }
            catch (Exception ex)
            {
                State = ConnectionState.Disconnected;
                Debug.LogException(ex);
                throw ex;
            }
        }

        private async UniTask<Uri> NegotiateAsync()
        {
            var parameters = new NegotiateParameters() { NegotiateType = NegotiateType, RoomId = Options.RoomId };
            var reqBody = JsonSerializer.Serialize(parameters);
            var jsonToSend = new System.Text.UTF8Encoding().GetBytes(reqBody);
            using var request = UnityWebRequest.Post(Options.NegotiateEndpoint, reqBody);
            using var uploadHandler = new UploadHandlerRaw(jsonToSend);
            using var downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = uploadHandler;
            request.downloadHandler = downloadHandler;
            request.SetRequestHeader("Content-Type", "application/json");
            await request.SendWebRequest();
            var content = request.downloadHandler.text;
            if (request.responseCode != 200)
            {
                try
                {
                    Negotiation = JsonSerializer.Deserialize<NegotiateResponse>(content, JsonSerializerOptions);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
                throw new NegotiateException($"Failed to negotiate with endpoint: {Options.NegotiateEndpoint}. {Negotiation.Result}. {Negotiation.Message}. Response status code: [{request.responseCode}]");
            }
            Negotiation = JsonSerializer.Deserialize<NegotiateResponse>(content, JsonSerializerOptions);
            return new Uri(Negotiation.Url);
        }

#elif UNITY_WEBGL

        private static WebPubSubConnection _instance = null;

        private static JsonSerializerOptions _jsonSerializerOptions = new()
        {
            IgnoreNullValues = true,
            PropertyNameCaseInsensitive = true,
        };

        [DllImport("__Internal")]
        private static extern void InitJs(string optionsStr, int negotiateType,
            Action<string> GroupMessageReceivedCallback,
            Action<string> ConnectedCallback,
            Action<string> DisconnectedCallback,
            Action StoppedCallback,
            Action<string> RejoinGroupFailedCallback,
            Action<string> UpdateNegotiation);

        [DllImport("__Internal")]
        private static extern void StartJs();

        [DllImport("__Internal")]
        private static extern void StopJs();

        [DllImport("__Internal")]
        private static extern void SendEventJs(string channel, string data);

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void UpdateNegotiation(string negotiationStr)
        {
            _instance.Negotiation = JsonSerializer.Deserialize<NegotiateResponse>(negotiationStr, _jsonSerializerOptions);
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void GroupMessageReceivedCallback(string argsStr)
        {
            try
            {
                using var doc = JsonDocument.Parse(argsStr);
                var messageProp = doc.RootElement.GetProperty("message");

                var channel = messageProp.GetProperty("group").GetString();
                var sequenceId = messageProp.GetProperty("sequenceId").GetUInt64();
                var fromUserId = messageProp.GetProperty("fromUserId").GetString();
                var data = new BinaryData(messageProp.GetProperty("data").GetString());
                var message = new GroupDataMessage(channel, WebPubSubDataType.Text, data, sequenceId, fromUserId);

                _instance.GroupMessageReceived.Invoke(message);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void ConnectedCallback(string argsStr)
        {
            try
            {
                using var doc = JsonDocument.Parse(argsStr);
                var connectionId = doc.RootElement.GetProperty("connectionId").GetString();
                _instance.Connected.Invoke(connectionId);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void DisconnectedCallback(string argsStr)
        {
            try
            {
                using var doc = JsonDocument.Parse(argsStr);
                var connectionId = doc.RootElement.GetProperty("connectionId").GetString();
                var reason = doc.RootElement.GetProperty("disconnectedMessage ").GetProperty("reason").GetString();
                _instance.Disconnected(connectionId, reason);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [MonoPInvokeCallback(typeof(Action))]
        private static void StoppedCallback()
        {
            try
            {
                _instance.Stopped();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        [MonoPInvokeCallback(typeof(Action<string>))]
        private static void RejoinGroupFailedCallback(string argsStr)
        {
            try
            {
                _instance.RejoinGroupFailed();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void Init(ConnectionOptions options)
        {
            _instance = this;
            Options = options;
            State = ConnectionState.Init;
            var optionsStr = JsonSerializer.Serialize(options);
            InitJs(optionsStr, (int)NegotiateType, GroupMessageReceivedCallback, ConnectedCallback, DisconnectedCallback, StoppedCallback, RejoinGroupFailedCallback, UpdateNegotiation);
        }

        public UniTask StartAsync(CancellationToken cancellationToken)
        {
            StartJs();
            return UniTask.CompletedTask;
        }

        public UniTask StopAsync()
        {
            StopJs();
            return UniTask.CompletedTask;
        }

        protected UniTask SendEventAsync(string channel, GroupData data)
        {
            var dataStr = JsonSerializer.Serialize(data);
            SendEventJs(channel, dataStr);
            return UniTask.CompletedTask;
        }

#else

#error PLATFORM NOT SUPPORTED

#endif
    }
}