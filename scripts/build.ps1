$ErrorActionPreference = "Stop"

Write-Host "Restoring packages..."
dotnet restore

Write-Host "Building solution..."
dotnet build --no-restore --configuration Release

Write-Host "Running tests..."
dotnet test --no-build --configuration Release

Write-Host "Build completed successfully."