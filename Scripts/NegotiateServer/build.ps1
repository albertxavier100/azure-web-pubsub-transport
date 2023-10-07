param(
    [Parameter(Mandatory=$true)]
    [string]$ServerRoot,
    
    [Parameter(Mandatory=$true)]
    [string]$Tag
)
# e.g. build.ps1 -ServerRoot "<root>/Resources/NegotiateServersSource~/" -Tag "awps-negotiate-server:v0.1.2"

dotnet clean $ServerRoot

# dotnet build $serverDir
# docker build -t TAG . 