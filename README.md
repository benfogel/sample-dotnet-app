# sample-dotnet-app

A sample dotnet app that generates some DB load and file load per request and as a background service.

## Prerequisites

`dotnet` is needed

```powershell
Invoke-WebRequest -Uri https://dot.net/v1/dotnet-install.ps1 -OutFile $env:TEMP\dotnet-install.ps1; & $env:TEMP\dotnet-install.ps1 -Version latest -InstallDir $env:ProgramFiles\dotnet
```

## Run

```powershell
dotnet run --urls "http://*:8123"
```

