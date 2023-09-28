using Microsoft.AspNetCore.Mvc.Controllers;
using Netcode.Transports.Azure.RealtimeMessaging.WebPubSub.NegotiateServer.Controllers;
using System.Reflection;

namespace Netcode.Transports.Azure.RealtimeMessaging.WebPubSub.NegotiateServer.Services
{
    public class NegotiateControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo)
        {
            var controller = !typeInfo.IsAbstract && typeof(NegotiateController).IsAssignableFrom(typeInfo);
            return controller || base.IsController(typeInfo);
        }
    }
}