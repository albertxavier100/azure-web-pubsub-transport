namespace Netcode.Transports.AzureWebPubSub
{
    public enum ConnectionState
    {
        Init,
        Connecting,
        Connected,
        ConnectFailed,
        Disconnected,
        Stopping,
        Stopped,
        StopFailed,
        RejoinGroupFailed,
    }
}