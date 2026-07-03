# Windows BLE Keyboard Sender Design

## Goal

Capture keyboard input on Windows, convert the current key state into an 8-byte USB HID keyboard report, and send that report to the Arduino `KeyboardBridge` firmware over BLE GATT.

## Protocol

- BLE device name: `KeyboardBridge`
- Service UUID: `7f2b4c00-7b64-4c0d-9b7a-1e0f3c9a0001`
- Report characteristic UUID: `7f2b4c01-7b64-4c0d-9b7a-1e0f3c9a0001`
- Payload size: 8 bytes
- Payload layout: USB HID boot keyboard report

```text
byte 0: modifier bitmask
byte 1: reserved
byte 2-7: up to six USB HID keyboard usage IDs
```

## Components

- `HidReportBuilder`: tracks pressed keys and produces the current 8-byte report.
- `VirtualKeyMapper`: maps Windows virtual key codes to USB HID usage IDs and modifier bits.
- `LowLevelKeyboardHook`: captures physical key up/down events using `WH_KEYBOARD_LL`.
- `BleKeyboardBridgeClient`: finds `KeyboardBridge`, opens the configured GATT characteristic, and writes reports.
- `KeyboardReportSendQueue`: serializes BLE writes from hook callbacks.

## Safety

- Injected keyboard events are ignored to reduce feedback loops.
- On shutdown, the app sends an all-zero release report.
- Unsupported keys leave the current report unchanged.

## Non-Goals

- No GUI in the first version.
- No text or IME character protocol.
- No Bluetooth HID pairing emulation.
