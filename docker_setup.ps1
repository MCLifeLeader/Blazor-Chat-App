# Setup Docker Services

if (Get-Command docker -ErrorAction SilentlyContinue) {
    Write-Host "Docker images and container setup started."

    ## Pull the Docker images
    docker pull datalust/seq
    docker pull docker.io/library/redis
    docker pull mailhog/mailhog
    docker pull mcr.microsoft.com/azure-messaging/servicebus-emulator
    docker pull mcr.microsoft.com/azure-sql-edge
    docker pull mcr.microsoft.com/azure-storage/azurite
    docker pull mcr.microsoft.com/dotnet/sdk
    docker pull mcr.microsoft.com/dotnet/aspnet
    docker pull mcr.microsoft.com/mssql/server
    docker pull mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator
    docker pull wiremock/wiremock

    ## Start the vs multi-container
    docker-compose -f "./containers/docker-compose-common.yml" -p common_shared up -d
}

Write-Host "Docker images and container setup completed."
Write-Host "Head back to README.md for deployment of the database and other services..."
