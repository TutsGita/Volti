# Changelog

## 0.3.3

- Rebuilt the panel around the GymOrganizer palette, header card and bottom navigation. Existing theme preferences carry over and newly created profiles use the GymOrganizer theme.
- Added local Razer Synapse 3 and 4 log readers, including charging and per-device tray indicators.
- Added experimental HyperX Pulsefire Haste 2 Wireless battery reading and a diagnostic card for the original PS4 Stinger.
- Kept discovered readings and Bluetooth cards synchronized between the tray monitor and panel. Added level icons for discovered devices.
- Recorded NGENUITY inspection results and protocol sources in the third-party notices and source package.
- Fixed Bluetooth card sizing, startup localization and empty Synapse snapshots in the rebuilt panel.

## 0.3.2

- Rounded and aligned the window, cards, navigation and theme tiles. Added Dracula.
- Detects the original HyperX Cloud Stinger Wireless PS4 receiver (0951:16DA). Battery reading is pending protocol verification.

- Compact device overview and tray icons driven by valid readings. Manual detection also discovers reconnected USB receivers.

## 0.3.1

- Added battery queries for Cloud Stinger 2 Wireless and three Cloud Stinger Core Wireless receivers. Physical validation in Volti is pending.
- Added a tray shortcut to Windows taskbar settings.

## 0.3.0

- Matched the device panel scrollbar to the active theme and added a Frutiger Aero palette.
- Added passive receiver readers for Attack Shark R1, X6, X11 and X11 SE, DELUX M600 Pro, Rapoo MT760 Mini and JBL Quantum 910 Wireless.
- Show receiver cards and tray entries after a valid battery percentage. The new readers send no HID requests and use no external runtime dependencies.
- Credited protocol sources in THIRD_PARTY_NOTICES.md. Physical validation of the new readers in Volti is pending.

## 0.2.0 · November 20, 2026

### ⚡ Lighter in the background

- Split Volti into a persistent tray monitor and a separate interface process.
- Open the panel directly from the tray. Closing it releases the interface process while battery monitoring and notifications continue.
- Approximately **60% lower private RAM use in tray-only mode** in a local measurement, dropping from 61.6 MiB with both processes open to 23.9 MiB after closing the panel. This compares open-panel and idle modes in 0.2.0. Actual savings vary by system.

### 🔵 Bluetooth and tray controls

- Added Bluetooth battery discovery for devices that expose their battery percentage through Windows device properties.
- Added a **Detect devices** button and automatic Bluetooth refresh every minute.
- Added a saved **Show in tray** checkbox beside each device.
- Keep a Volti tray icon available when all device icons are hidden.

### 🌐 Simplified Chinese

- Added **简体中文** alongside English and Brazilian Portuguese.
- Translate panel labels, device status messages, settings and notifications.

### ✨ Interface improvements

- Fixed the off-center settings gear with a larger, centered graphic.
- Improved rounded card borders and strengthened the frosted appearance.
- Replaced percentage spinners with themed, rounded selectors offering 10% steps and typed values.
- Improved theme interaction colors and status text sizing.
- Added a small, clickable **tutsgita** GitHub link.
- Preserve current readings when reopening the panel and bring the existing panel forward on repeated tray clicks.

### Compatibility notes

- Bluetooth availability depends on the device and the battery property exposed by Windows. Physical Bluetooth battery validation remains pending.
- Custom low and critical alerts currently apply to the DELUX and JBL USB receiver readers.
- Windows executables are unsigned.

## 0.1.0-preview.1

- Initial public preview with DELUX M800 Mini and JBL Quantum 650 receiver readers.
- Added per-device battery alerts, four themes, English and Portuguese, a Windows installer and a portable package.
