# Testing

## Automated

Run build.cmd. Seven offline assertions exercise normalized/exact device matching, rejection of empty names, ambiguous/missing devices, GUID selection and x64 native structure layout. The current generic build compiled locally, but Windows security blocked execution, so those assertions have not yet run for this version. The configured CI has not been run yet.

## Hardware acceptance checklist

Use a paired device and keep other Bluetooth accessories connected.

- --list returns only supported Bluetooth audio devices, not USB/HDMI speakers.
- --setup saves the chosen device without changing audio defaults.
- --status is read-only and reports current Windows defaults.
- Exact name and container-ID selection resolve the same device.
- With the device asleep, timeout is reported rather than success.
- With the device awake and disconnected, reconnect succeeds without affecting a mouse.
- With it already connected, repeat invocation does not disconnect it.
- --defaults both selects and verifies output and microphone for all three roles.
- --defaults output changes no microphone default.
- --defaults none changes no audio default.
- A speaker without a microphone works with auto/output and reports failure with both.
- Duplicate names require an explicit container ID.
- A removed/re-paired device requires setup again when its container ID changes.
- A second concurrent instance exits without issuing another request.

The original device-specific prototype was tested on one Windows 11 PC. The new generic build needs its own hardware validation. Do not claim universal headset or Windows-driver support.
