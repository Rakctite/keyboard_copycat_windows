# Keyboard Copycat Windows Sender

Windows console sender for the Keyboard Copycat Arduino firmware.

The app captures local keyboard events with a low-level keyboard hook, converts the current pressed-key state into an 8-byte USB HID keyboard report, and writes that report to the Arduino Nano ESP32 over BLE GATT.

## Flow

```text
Windows keyboard input
  -> low-level keyboard hook
  -> 8-byte USB HID keyboard report
  -> BLE GATT write
  -> Arduino KeyboardBridge firmware
  -> USB HID output to target device
```

## BLE Contract

Device name:

```text
KeyboardBridge
```

Service UUID:

```text
7f2b4c00-7b64-4c0d-9b7a-1e0f3c9a0001
```

Report characteristic UUID:

```text
7f2b4c01-7b64-4c0d-9b7a-1e0f3c9a0001
```

Payload:

```text
byte 0: modifier bitmask
byte 1: reserved
byte 2-7: up to six USB HID keyboard usage IDs
```

## Build

```bash
dotnet build
```

## Test

```bash
dotnet test
```

## Run

Flash and power the Arduino firmware first, then run:

```bash
dotnet run --project KeyboardCopycat.Windows
```

Press `Ctrl+C` to stop. The app sends a release-all report during shutdown.

## Notes

- This is not standard Windows Bluetooth keyboard pairing. The app connects to a custom BLE GATT service.
- The first version maps common alphanumeric, modifier, symbol, navigation, and F1-F12 keys.
- The app ignores injected keyboard events to reduce feedback loops.
