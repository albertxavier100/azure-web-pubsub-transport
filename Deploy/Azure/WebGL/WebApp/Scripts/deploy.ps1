param(
    # Web App
    [Parameter(Mandatory = $true)]
    [string]$AppServicePlanName,

    [Parameter(Mandatory = $true)]
    [string]$AppServiceSku,

    [Parameter(Mandatory = $true)]
    [string]$WebAppName,

    # Web PubSub
    [Parameter(Mandatory = $true)]
    [string]$WebPubSubName,

    [Parameter(Mandatory = $true)]
    [string]$WebPubSubSku,

    # Resource group
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupLocation,
    
    # Docker
    [Parameter(Mandatory = $true)]
    [string]$DockerImage
)

# Sample command from your Unity project root
# .\Packages\com.community.netcode.transport.azure-webpubsub\deploy.ps1 -AppServicePlanName awpst-plan -ResourceGroupName test-wpst-rg -WebAppName awpst-app -DockerImage "albertxavier100/azure-web-pubsub-transport-sample-unity-netcode-bootstrap:0.2.0" -AppServiceSku Free -WebPubSubSku Free_F1 -ResourceGroupLocation southeastasia -WebPubSubName awpst  

$Hub = "unity_hub_bootstrap"

Write-Host "Create resource group..." -ForegroundColor Green
az group create --name $ResourceGroupName --location $ResourceGroupLocation

Write-Host "Create Web PubSub..." -ForegroundColor Green
az webpubsub create --sku $WebPubSubSku --name $WebPubSubName --resource-group $ResourceGroupName
$ConnectionString = az webpubsub key show --name $WebPubSubName --resource-group $ResourceGroupName --query primaryConnectionString --output tsv | Out-String

Write-Host "Create Web App..." -ForegroundColor Green
az appservice plan create --name $AppServicePlanName --resource-group $ResourceGroupName --sku $AppServiceSku --is-linux
az webapp create --resource-group $ResourceGroupName --plan $AppServicePlanName --name $WebAppName --deployment-container-image-name albertxavier100/azure-web-pubsub-transport-samples:$DockerImage

Write-Host "Configure Web App..." -ForegroundColor Green
az webapp config appsettings set --name $WebAppName --resource-group $ResourceGroupName --settings ConnectionString=$ConnectionString
az webapp config appsettings set --name $WebAppName --resource-group $ResourceGroupName --settings Hub=$Hub