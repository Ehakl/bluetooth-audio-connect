# Bluetooth Audio Connect

**Connect paired Bluetooth headphones and switch Windows audio output from one shortcut.**

Bluetooth Audio Connect is a small C# command-line utility for Windows headphones, headsets and speakers. Save a paired device once, then use the included launcher to request reconnection and select your audio defaults.

**Experimental, source-only:** hardware validation is incomplete. See [testing status and known limitations](TESTING.md) before building or running.

No installer, external packages, Settings automation, radio toggling, driver replacement or background service. Full C# source is included.

## Choose your platform

- **Windows:** build and use the utility below.
- **iPhone:** follow the [iPhone installation and shortcut guide](docs/IPHONE.md). It uses Apple Shortcuts to select headphones on the iPhone; no Windows app or server is needed.

## Build

On Windows x64 with .NET Framework 4.x installed, run:

```cmd
build.cmd
```

This uses the compiler included with Windows .NET Framework and creates `bin\BluetoothAudioConnect.exe`. The build then runs seven offline tests. No package download or administrator privileges are required.

See [TESTING.md](TESTING.md) for validation results and the unresolved local security detection.

## Quick start

```cmd
bin\BluetoothAudioConnect.exe --list
bin\BluetoothAudioConnect.exe --setup
```

Choose a device from the numbered list. Setup saves its stable Windows container ID locally and does not connect or change audio defaults.

Then double-click **Connect Bluetooth Audio.cmd** to connect your saved device. You can use Windows **Send to > Desktop (create shortcut)** on that launcher. Running without saved settings prompts for a device, saves it, then connects.

## Commands

```cmd
bin\BluetoothAudioConnect.exe --device "My Headphones"
bin\BluetoothAudioConnect.exe --container "00000000-0000-0000-0000-000000000001"
bin\BluetoothAudioConnect.exe --device "My Speaker" --defaults output
bin\BluetoothAudioConnect.exe --device "My Headset" --defaults both
bin\BluetoothAudioConnect.exe --device "My Headphones" --defaults none
bin\BluetoothAudioConnect.exe --status
bin\BluetoothAudioConnect.exe --help
```

Replace example names/IDs with values from `--list`. Names match exactly, ignoring case and normalizing curly apostrophes. Duplicate names require `--container` to avoid selecting the wrong device.

| Defaults mode | Behavior |
| --- | --- |
| `auto` (default) | Select output and an available microphone; otherwise retain the current microphone |
| `both` | Require active output and microphone before changing defaults |
| `output` | Select output; retain current microphone |
| `none` | Connect without changing defaults |

To save a different default behavior, use `--setup --defaults output` (or another mode). Explicit `--device`/`--container` commands do not overwrite the saved device. `--status` and `--list` do not connect or change audio defaults.

Default selections apply to console, multimedia and communications roles. The utility reads them back before reporting success. Apps that select their own audio device may ignore Windows defaults. Using a Bluetooth microphone can change the audio quality/profile according to the driver.

## Requirements and limits

- Windows x64; .NET Framework 4.x. Windows 11 is the intended target. ARM64/native x86 are not tested.
- Device already paired in Windows, awake and in range; Bluetooth already enabled.
- Bluetooth audio driver exposing a compatible audio topology and reconnect request. This is not a general Bluetooth device manager.
- Discovery checks Bluetooth filter paths (`bth` prefix). This is a driver-dependent heuristic; some drivers/LE Audio devices may not appear.
- A driver accepting a request does not prove connection. The app waits up to 20 seconds for an active playback endpoint, and up to 15 additional seconds for requested input/output endpoints.
- Default selection uses Windows' undocumented `IPolicyConfig` interface, which may change. Reconnect uses the documented `KSPROPERTY_ONESHOT_RECONNECT` property.
- If a default-setting call fails partway through, some defaults may have changed. Use `--status` to inspect them. There is no automatic rollback or disconnect feature.
- The program leaves other devices paired and never toggles the Bluetooth adapter or its services.
- Unsigned builds may trigger security warnings or detections. Do not disable protections. Maintainers should resolve detections and validate release artifacts before distribution.

## Local data and privacy

Settings and the latest diagnostic log live in `%LOCALAPPDATA%\BluetoothAudioConnect`. Settings contain a device container ID and defaults mode. Logs may contain local device names. Nothing is uploaded; the app has no networking code. Review logs before sharing them in issues. No personal device configuration is included in this repository.

## Exit codes

0 success; 1 failure/cancelled selection; 2 invalid arguments; 3 disconnected/timeout; 4 another instance running.

## Development

Source: `src/Program.cs` and `src/Native.cs`. Run `build.cmd` to compile and run offline tests. Tests cover exact matching, apostrophe normalization, duplicate names, container-based selection, missing devices and native structure sizes. They do not exercise hardware or change audio settings.

The Windows CI workflow builds and runs these tests on pushes and pull requests. Hardware validation must be performed separately; see [TESTING.md](TESTING.md).

## Help improve Bluetooth Audio Connect

Documentation contributions are welcome. Browse [good first issues](https://github.com/ozkozk/bluetooth-audio-connect/issues?q=is%3Aissue%20is%3Aopen%20label%3A%22good%20first%20issue%22) or read [CONTRIBUTING.md](CONTRIBUTING.md) to get started.

For bug reports, include your Windows version, device model, command and error code. Remove personal device names and IDs from logs before sharing them. Hardware testing remains subject to the [testing and security guidance](TESTING.md).

## Technical references and attribution

The audio-topology approach was informed by [ToothTray](https://github.com/m2jean/ToothTray). This is a separate implementation and includes none of its binaries. COM declarations follow Windows ABI definitions; default-audio interface layout was cross-checked against [Sunshine's PolicyConfig interface](https://github.com/LizardByte/Sunshine/blob/master/src/platform/windows/PolicyConfig.h).

- [Microsoft: reconnect request](https://learn.microsoft.com/en-us/windows-hardware/drivers/audio/ksproperty-oneshot-reconnect)
- [Microsoft: Bluetooth audio property set](https://learn.microsoft.com/en-us/windows-hardware/drivers/audio/kspropsetid-btaudio)

Licensed under [MIT](LICENSE). See [PUBLISHING.md](PUBLISHING.md) for publishing the source on GitHub.

## Launching from the Command Line

If you are running the scripts manually from the current directory, use the following syntax depending on your shell.

**Command Prompt (cmd.exe):**
```cmd
build.cmd
"C:\Path With Spaces\bluetooth-audio-connect\build.cmd"
```

**PowerShell:**
PowerShell requires an explicit relative path (`.\`) to execute scripts in the current directory. If the path contains spaces, you must use the call operator (`&`) and quotes:
```powershell
.\build.cmd
& "C:\Path With Spaces\bluetooth-audio-connect\build.cmd"
```
