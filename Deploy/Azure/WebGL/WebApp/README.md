# Deploy WebGL Demo to Azure

## Deploy to Azure using an ARM Template

### Use Prebuilt Docker Image

> Try prebuilt docker image is [here](https://hub.docker.com/repository/docker/albertxavier100/azure-web-pubsub-transport-sample-unity-netcode-bootstrap/general).

```powershell
# go to root of unity project
cd <unity-project-root>

# deploy to azure
.\Packages\com.community.netcode.transport.azure-webpubsub\deploy.ps1 -AppServicePlanName <AppServicePlanName> -ResourceGroupName <ResourceGroupName> -WebAppName <WebAppName> -DockerImage "albertxavier100/azure-web-pubsub-transport-sample-unity-netcode-bootstrap:latest" -AppServiceSku Free -WebPubSubSku Free_F1 -ResourceGroupLocation <Location> -WebPubSubName <WebPubSubName>

```

### Build Manually

```powershell
# go to root of unity project
cd <unity-project-root>

# build source to /Output~
.\Packages\com.community.netcode.transport.azure-webpubsub\Deploy\Azure\WebGL\WebApp\Scripts\build.ps1 -AzureWebPubSubTransportPackagePath ".\Packages\com.community.netcode.transport.azure-webpubsub"  -ServerOutputPath "Output~" -UnityWebGLBuildPath "Build\WebGL"

# build docker image from /Output~
docker build -t <name>/<repo>:<tag> .\Output~\  

# push to docker hub
docker push <name>/<repo>:<tag>

# deploy to azure
.\Packages\com.community.netcode.transport.azure-webpubsub\deploy.ps1 -AppServicePlanName <AppServicePlanName> -ResourceGroupName <ResourceGroupName> -WebAppName <WebAppName> -DockerImage "<name>/<repo>:<tag>" -AppServiceSku Free -WebPubSubSku Free_F1 -ResourceGroupLocation <Location> -WebPubSubName <WebPubSubName>
```
