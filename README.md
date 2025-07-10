# WatchdogR
![Made with C#](https://img.shields.io/badge/Made%20with-C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white) ![Platform](https://img.shields.io/badge/Platform-Windows-blue?style=for-the-badge&logo=windows&logoColor=white)
![Version](https://img.shields.io/badge/Version-1.0-blue?style=for-the-badge) ![License: AGPL v3](https://img.shields.io/badge/License-AGPL%20v3-blue?style=for-the-badge)


This is a aggressive USB-Whitelist written in C#.

## 📦 Requirements

- [.NET Framework](https://dotnet.microsoft.com/)
- [Visual Studio](https://visualstudio.microsoft.com/)

## 🛠️ Building

1. Open the project in Visual Studio.
2. Click **"Build"** to compile the project and generate the `.exe`.

## 🚀 Installation

1. Open a Command Prompt **as Administrator**.
2. Use `installutil` with the path to the compiled `.exe`:

   ```bash
   cd %WINDIR%\Microsoft.NET\Framework[64]\<framework_version>_
   installutil Path\To\YourService.exe
