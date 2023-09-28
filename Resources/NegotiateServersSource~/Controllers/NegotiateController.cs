using Azure.Messaging.WebPubSub;
using Microsoft.AspNetCore.Mvc;
using Netcode.Transports.Azure.RealtimeMessaging.WebPubSub.NegotiateServer.Services;
using Netcode.Transports.AzureWebPubSub;

namespace Netcode.Transports.Azure.RealtimeMessaging.WebPubSub.NegotiateServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    internal class NegotiateController : ControllerBase
    {
        private readonly WebPubSubServiceClient _serviceClient;
        private readonly ILogger<NegotiateController> _logger;
        private readonly IConnectionContextGenerator _connectionContextGenerator;
        private readonly List<string> _roles = new() { "webpubsub.joinLeaveGroup", "webpubsub.sendToGroup" };
        private readonly IRoomManager _roomManager;

        public NegotiateController(WebPubSubServiceClient serviceClient, IConnectionContextGenerator connectionContextGenerator, IRoomManager roomManager, ILogger<NegotiateController> logger)
        {
            _logger = logger;
            _connectionContextGenerator = connectionContextGenerator;
            _roomManager = roomManager;
            _serviceClient = serviceClient;
        }

        [HttpPost]
        public Task<ActionResult> PostAsync([FromBody] NegotiateParameters parameters)
        {
            switch (parameters.NegotiateType)
            {
                case NegotiateType.ServerConnect: return ServerConnectAsync(parameters);
                case NegotiateType.ClientConnect: return ClientConnectAsync(parameters);
                default: return Task.FromResult<ActionResult>(BadRequest(new NegotiateResponse() { Result = NegotiateResult.UnSupported }));
            }
        }

        private async Task<ActionResult> ServerConnectAsync(NegotiateParameters parameters)
        {
            var isExist = await _roomManager.ExistAsync(parameters.RoomId);
            // todo: remove:
            isExist = false;
            if (!isExist)
            {
                var connectionContext = await _connectionContextGenerator.NextAsync(parameters.RoomId, true);
                var uri = _serviceClient.GetClientAccessUri(userId: parameters.RoomId, roles: _roles, groups: new[] {
                    parameters.RoomId,
                });
                return Ok(new NegotiateResponse
                {
                    Result = NegotiateResult.Success,
                    Url = uri.AbsoluteUri,
                    ConnectionContext = connectionContext,
                });
            }
            return BadRequest(new NegotiateResponse { Result = NegotiateResult.ServerAlreadyExist });
        }

        private async Task<ActionResult> ClientConnectAsync(NegotiateParameters parameters)
        {
            var isExist = await _roomManager.ExistAsync(parameters.RoomId);
            if (isExist)
            {
                var connectionContext = await _connectionContextGenerator.NextAsync(parameters.RoomId, false);
                var uri = _serviceClient.GetClientAccessUri(roles: _roles, groups: new[] { connectionContext.SubChannel });
                return Ok(new NegotiateResponse
                {
                    Result = NegotiateResult.Success,
                    Url = uri.AbsoluteUri,
                    ConnectionContext = connectionContext,
                });
            }
            return BadRequest(new NegotiateResponse { Result = NegotiateResult.ServerNotFound });
        }
    }
}