using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;

namespace Netcode.Transports.AzureWebPubSub
{
    internal class NetworkEventQueue
    {
        private static readonly object _connectionLock = new object();
        private Queue<AWPSNetworkEvent> _eventQueue { get; } = new();

        public int Count => _eventQueue.Count;

        public AWPSNetworkEvent Poll()
        {
            lock (_connectionLock)
            {
                if (_eventQueue.Count > 0)
                {
                    return _eventQueue.Dequeue();
                }
                else
                {
                    return new AWPSNetworkEvent()
                    {
                        FromClientId = 0,
                        Payload = null,
                        NetworkEvent = NetworkEvent.Nothing,
                    };
                }
            }
        }

        public void Push(AWPSNetworkEvent networkEvent)
        {
            lock (_connectionLock)
            {
                _eventQueue.Enqueue(networkEvent);
            }
        }
    }
}