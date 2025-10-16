# sample-dotnet-app

A sample dotnet app that generates some DB load and file load per request and as a background service.

## Installation

```powershell
Invoke-WebRequest -Uri "https://github.com/benfogel/sample-dotnet-app/archive/refs/heads/main.zip" -OutFile main.zip

Expand-Archive -Path main.zip -DestinationPath . -Force

cd sample-dotnet-app-main

install.ps1
```

## Run

```powershell
dotnet run --urls "http://*:8123"
```

