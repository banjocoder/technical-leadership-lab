$runtimePath = "C:\Users\stith_b\source\repos\technical-leadership-lab\src\PolicyService"

if ([string]::IsNullOrWhiteSpace($manifest.version) -or
    [string]::IsNullOrWhiteSpace($manifest.sourceRevision)) {
    throw "Artifact manifest is missing version or source revision."
}

$env:Deployment__Version = "1.0.0"
$env:Deployment__SourceRevision = "1"
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://localhost:5006"
$env:ConnectionStrings__PolicyDatabase = "Server=(localdb)\MSSQLLocalDB;Database=PolicyDb;Integrated Security=true;Connect Timeout=3"
$env:EnvironmentSettings__DisplayName = 'DEV'
$env:Readiness__RequireDatabase = "true"

Set-Location $runtimePath
dotnet run