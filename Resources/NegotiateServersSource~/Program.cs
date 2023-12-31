using Azure.Messaging.WebPubSub;
using Microsoft.AspNetCore.StaticFiles;

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

            var hub = GetValue("Hub", builder.Configuration);
            var connectionString = GetValue("ConnectionString", builder.Configuration);
            var serviceClient = new WebPubSubServiceClient(connectionString, hub);

            // Add services for transport
            builder.Services.AddSingleton<IRoomManager, RoomManager>();
            builder.Services.AddSingleton<IConnectionContextGenerator, ConnectionContextGenerator>();
            builder.Services.AddSingleton(s => serviceClient);

            var app = builder.Build();

            app.UseCors(allowAllOrigins);
            var provider = new FileExtensionContentTypeProvider();
            provider.Mappings[".data"] = "application/octet-stream";
            provider.Mappings[".wasm"] = "application/wasm";
            provider.Mappings[".br"] = "application/octet-stream";
            provider.Mappings[".js"] = "application/javascript";

            app.UseDefaultFiles();
            app.UseStaticFiles(new StaticFileOptions()
            {
                ContentTypeProvider = provider,
                OnPrepareResponse = context =>
                {
                    var path = context.Context.Request.Path.Value;
                    var extension = Path.GetExtension(path);

                    if (extension == ".gz" || extension == ".br")
                    {
                        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(path) ?? "";
                        if (provider.TryGetContentType(fileNameWithoutExtension, out string? contentType))
                        {
                            context.Context.Response.ContentType = contentType;
                            context.Context.Response.Headers.Add("Content-Encoding", extension == ".gz" ? "gzip" : "br");
                        }
                    }
                },
            });

            app.UseRouting();

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

        private static string? GetValue(string key, IConfiguration configuration)
        {
            var val = Environment.GetEnvironmentVariable(key);
            val ??= configuration.GetSection(key).Get<string>();
            return val;
        }
    }
}