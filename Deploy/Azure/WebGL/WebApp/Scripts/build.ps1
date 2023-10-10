param(
    # server
    [Parameter(Mandatory = $true)]
    [string]$AzureWebPubSubTransportPackagePath,

    [Parameter(Mandatory = $true)]
    [string]$ServerOutputPath,

    # unity
    [Parameter(Mandatory = $true)]
    [string]$UnityWebGLBuildPath
)
# Sample command from your Unity project root
# .\Packages\com.community.netcode.transport.azure-webpubsub\Deploy\Azure\WebGL\WebApp\Scripts\build.ps1 -AzureWebPubSubTransportPackagePath ".\Packages\com.community.netcode.transport.azure-webpubsub"  -ServerOutputPath "Output~" -UnityWebGLBuildPath "Build\WebGL"

# Source paths
$ServerPackagePath = "$AzureWebPubSubTransportPackagePath/Resources/NegotiateServersSource~.zip"
$SharedModelPath = "$AzureWebPubSubTransportPackagePath/Runtime/Models"

# Temp paths
$ServerTempPath = "$ServerOutputPath/.tmp/Resources/Server"
$SharedModelTempPath = "$ServerOutputPath/.tmp/Runtime/Models"

# Dest paths
$WWWRootDestPath = "$ServerOutputPath/wwwroot"
$DockerfileDestPath = "$ServerOutputPath/Dockerfile"

# Clean output folder
Remove-Item -Path $ServerOutputPath -Recurse
New-Item -ItemType "directory" -Path $ServerTempPath

# Extract server package
Expand-Archive -Path $ServerPackagePath -DestinationPath $ServerTempPath

# Copy shared model
Copy-Item -Path $SharedModelPath -Destination  $SharedModelTempPath -Recurse

# Generate docker file
$Dockerfile = @"
FROM mcr.microsoft.com/dotnet/aspnet:6.0 as runtime
COPY . .
ENTRYPOINT ["dotnet", "AWPSNegotiateServer.dll"]
"@
Set-Content -Path $DockerfileDestPath -Value $Dockerfile

# Build server
dotnet clean $ServerTempPath
dotnet build $ServerTempPath
dotnet publish $ServerTempPath -o $ServerOutputPath

# Copy Unity build
Copy-Item -Path $UnityWebGLBuildPath/* -Destination $WWWRootDestPath -Recurse -Force