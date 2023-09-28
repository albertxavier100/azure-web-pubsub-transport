using Azure.Messaging.WebPubSub;

namespace Netcode.Transports.Azure.RealtimeMessaging.WebPubSub.NegotiateServer.Services
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var allowAllOrigins = "AllowAllOrigins";
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services
                .AddControllers()
                .ConfigureApplicationPartManager(
                manager => manager.FeatureProviders.Add(new NegotiateControllerFeatureProvider()));
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddCors(o =>
            {
                o.AddPolicy(name: allowAllOrigins, policy =>
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });
            var hub = builder.Configuration.GetSection("Hub").Get<string>();
            var connectionString = builder.Configuration.GetSection("ConnectionString").Get<string>();
            var serviceClient = new WebPubSubServiceClient(connectionString, hub);

            // Add services for transport
            builder.Services.AddSingleton<IRoomManager, RoomManager>();
            builder.Services.AddSingleton<IConnectionContextGenerator, ConnectionContextGenerator>();
            builder.Services.AddSingleton(s => serviceClient);

            var app = builder.Build();

            app.UseCors(allowAllOrigins);

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}