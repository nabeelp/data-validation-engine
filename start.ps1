$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$backendProject = Join-Path $repoRoot 'src\backend\DataValidationEngine.Api\DataValidationEngine.Api.csproj'
$frontendPath = Join-Path $repoRoot 'src\frontend'
$sqlContainerName = 'data-validation-engine-sql'
$sqlImage = 'mcr.microsoft.com/mssql/server:2022-latest'
$sqlPassword = 'Dev@12345'
$sqlDockerCommand = "docker run --name $sqlContainerName -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=$sqlPassword -p 1433:1433 -d $sqlImage"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK is required but dotnet was not found on PATH.'
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw 'Node.js with npm is required but npm was not found on PATH.'
}

if (-not (Test-Path $backendProject)) {
    throw "Backend project not found at $backendProject"
}

if (-not (Test-Path $frontendPath)) {
    throw "Frontend project not found at $frontendPath"
}

$frontendNodeModules = Join-Path $frontendPath 'node_modules'
if (-not (Test-Path $frontendNodeModules)) {
    throw 'Frontend dependencies are not installed. Run npm install in src/frontend first.'
}

if (Get-Command docker -ErrorAction SilentlyContinue) {
    try {
        $containerExists = docker ps -a --filter "name=^/${sqlContainerName}$" --format '{{.Names}}'
        if (-not $containerExists) {
            docker run --name $sqlContainerName -e ACCEPT_EULA=Y -e MSSQL_SA_PASSWORD=$sqlPassword -p 1433:1433 -d $sqlImage | Out-Null
            Write-Host 'Started local SQL Server container on localhost:1433.'
        }
        else {
            $containerRunning = docker ps --filter "name=^/${sqlContainerName}$" --format '{{.Names}}'
            if (-not $containerRunning) {
                docker start $sqlContainerName | Out-Null
                Write-Host 'Started existing local SQL Server container on localhost:1433.'
            }
            else {
                Write-Host 'Local SQL Server container is already running on localhost:1433.'
            }
        }
    }
    catch {
        Write-Warning "Unable to start local SQL Server via Docker automatically. Run this manually: $sqlDockerCommand"
    }
}
else {
    Write-Warning "Docker was not found. Start SQL Server locally with: $sqlDockerCommand"
}

$shellExecutable = (Get-Process -Id $PID).Path
$backendCommand = "Set-Location '$repoRoot'; dotnet run --project '$backendProject'"
$frontendCommand = "Set-Location '$frontendPath'; npm run dev"

Start-Process -FilePath $shellExecutable -ArgumentList '-NoExit', '-Command', $backendCommand -WorkingDirectory $repoRoot | Out-Null
Start-Process -FilePath $shellExecutable -ArgumentList '-NoExit', '-Command', $frontendCommand -WorkingDirectory $frontendPath | Out-Null

Write-Host 'Started backend at http://localhost:5225 and frontend at http://localhost:5173.'