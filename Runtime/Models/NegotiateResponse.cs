namespace Netcode.Transports.AzureWebPubSub
{
    internal class NegotiateResponse
    {
        public NegotiateResult Result { get; set; }
        public string Message { get; set; }

        public ConnectionContext ConnectionContext { get; set; }

        public string Url { get; set; }
    }
}