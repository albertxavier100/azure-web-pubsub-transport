# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build

# copy csproj and restore as distinct layers
WORKDIR /root/Resources/NegotiateServersSource~
COPY ./Resources/NegotiateServersSource~/AWPSNegotiateServer.csproj .
RUN dotnet restore

# copy everything else
COPY ./Resources/NegotiateServersSource~/. .
WORKDIR /root/Runtime/Models
COPY ./Runtime/Models/. .

# build app
WORKDIR /root/Resources/NegotiateServersSource~
RUN dotnet publish --no-restore -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["./AWPSNegotiateServer"]