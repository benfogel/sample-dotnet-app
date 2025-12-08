Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile $env:TEMP\dotnet-install.ps1; & $env:TEMP\dotnet-install.ps1 -Version latest -InstallDir $env:ProgramFiles\dotnet

# Download and extract RavenDB
$ravenUrl = "https://daily-builds.s3.amazonaws.com/RavenDB-7.1.3-windows-x64.zip"
$ravenZipPath = "C:\Users\Administrator\RavenDB-7.1.3-windows-x64.zip"
$ravenExtractPath = "C:\Users\Administrator\Raven"

Write-Host "Downloading RavenDB from $ravenUrl"
try {
    Invoke-WebRequest -Uri $ravenUrl -OutFile $ravenZipPath
    Write-Host "RavenDB downloaded successfully."
} catch {
    Write-Host "Failed to download RavenDB: $($_.Exception.Message)"
    exit 1
}

Write-Host "Extracting RavenDB to $ravenExtractPath"
try {
    Expand-Archive -Path $ravenZipPath -DestinationPath $ravenExtractPath -Force
    Write-Host "RavenDB extracted successfully."
} catch {
    Write-Host "Failed to extract RavenDB: $($_.Exception.Message)"
    exit 1
}

# Create settings.json
$settingsJsonPath = "C:\Users\Administrator\Raven\Server\settings.json"
Write-Host "Creating settings.json at $settingsJsonPath"
$settingsContent = @'
{
    "ServerUrl": "http://127.0.0.1:8080",
    "Setup.Mode": "None",
    "DataDir": "RavenData"
}
'@
try {
    New-Item -Path "C:\Users\Administrator\Raven\Server" -Type Directory -Force
    Set-Content -Path $settingsJsonPath -Value $settingsContent
    Write-Host "settings.json created successfully."
} catch {
    Write-Host "Failed to create settings.json: $($_.Exception.Message)"
    exit 1
}


# Run setup
Write-Host "Running RavenDB setup as a service"
try {
    & "$ravenExtractPath\setup-as-service.ps1"
    Write-Host "RavenDB setup script executed."
} catch {
    Write-Host "Failed to run RavenDB setup script: $($_.Exception.Message)"
    exit 1
}

# Start dotnet app
# Define the action: Run 'dotnet' with specific arguments and working directory
$Action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-Command `"& { & 'C:/Program Files/dotnet/dotnet.exe' run --urls 'http://*:8123' }`"" -WorkingDirectory "C:\Users\Administrator\sample-dotnet-app-main\"

# Define the trigger: Run at system startup
$Trigger = New-ScheduledTaskTrigger -AtStartup

# Define the settings: Allow running on battery, etc.
$Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -DontStopOnIdleEnd -ExecutionTimeLimit 0

# Register the task to run as the SYSTEM user with highest privileges
Register-ScheduledTask -TaskName "StartDotNetApp" -Action $Action -Trigger $Trigger -Settings $Settings -User "NT AUTHORITY\SYSTEM" -RunLevel Highest

Start-ScheduledTask -TaskName "StartDotNetApp"

Write-Host "Task 'StartDotNetApp' created successfully."

