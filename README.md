# AggrWin

A lightweight Windows x64 desktop wrapper for [Aggr.trade](https://aggr.trade), ported from the Android **ETHBTC** WebView app.

AggrWin is an independent browser shell. It does not fork, proxy, or self-host Aggr.trade.

## Features

- Native Windows desktop window powered by Microsoft Edge WebView2, with a custom AggrWin app icon.
- Dark Windows title-bar border and caption styling.
- Opens directly to `https://aggr.trade`.
- Persistent browser profile/cache under `%LOCALAPPDATA%\AggrWin\WebView2`.
- JavaScript, DOM storage, cookies, and normal website storage supported by WebView2.
- Website popups and `target="_blank"` links stay in the app window.
- File upload flows are handled by WebView2.
- Home and reload controls.
- F5 / Ctrl+R to reload, Alt+Left to go back, F11 for fullscreen, and Esc to exit fullscreen.
- Optional **Start with Windows** toggle, stored in the current user's Run registry key.
- Closing the window minimizes AggrWin to the notification area; use the tray menu to reopen or exit.
- Single-instance guard to avoid duplicate windows.

## Download

Run the **Build AggrWin (Windows x64)** workflow manually from the GitHub Actions tab. Download the `AggrWin-windows-x64` artifact and extract `AggrWin.exe`.

The executable is self-contained for .NET; no .NET installation is required on the target PC.

### WebView2 runtime requirement

AggrWin uses the Microsoft Edge **Evergreen WebView2 Runtime**, which is a Windows browser runtime and is not bundled into the executable. Most Windows 10/11 systems already have it. If it is missing, install the [Evergreen WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) and launch AggrWin again.

## Automatic startup

1. Run `AggrWin.exe`.
2. Click **Start with Windows** in the top toolbar.
3. AggrWin adds/removes its own entry under the current Windows user's `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` key.
4. It will launch when that user signs in. No administrator permission or Windows service is required.

Closing the window hides it in the notification area instead of terminating the app. Choose **Exit** from the tray icon menu to stop it completely.

## Build locally

Requirements: Windows, Python 3.12 with Pillow, .NET 10 SDK, and internet access for package restore.

```powershell
python -m pip install Pillow
python tools/generate_icon.py
dotnet restore AggrWin.csproj -r win-x64
dotnet publish AggrWin.csproj -c Release -r win-x64 --self-contained true
```

Output:

```
bin/Release/net10.0-windows/win-x64/publish/AggrWin.exe
```

## GitHub Actions

The workflow is intentionally **manual-only** (`workflow_dispatch`). It builds a Windows x64 self-contained single-file executable and uploads only `AggrWin.exe` as a 30-day artifact. No installer, MSI, service, or automatic release is produced.

## Data and privacy

The WebView2 profile is stored locally in the current Windows user's Local AppData. Cookies, site storage, and cached website resources are managed by WebView2. Removing the app executable does not automatically delete this profile; remove `%LOCALAPPDATA%\AggrWin` manually if you want to clear it.

## Disclaimer

Aggr.trade is an external service. This independent wrapper is not affiliated with or endorsed by Aggr.trade. Availability and functionality depend on the upstream website and its services.
