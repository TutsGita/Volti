# Volti 0.3.3

Volti brings supported wireless device battery readings into the Windows system tray. Version 0.3.3 refreshes the device panel and adds Razer Synapse support.

## Highlights

- A new GymOrganizer-inspired panel with the purple palette, header, device cards and bottom navigation.
- Razer Synapse 3 and 4 log support for device names, battery percentages and charging status. Synapse must be running and updating its logs.
- Experimental USB receiver reading for HyperX Pulsefire Haste 2 Wireless.
- An explicit diagnostic card for the original HyperX Cloud Stinger Wireless PS4 receiver when its battery protocol is unavailable.
- Battery level tray icons for detected wireless devices.
- Retained existing saved theme preferences. New installations start with the GymOrganizer theme.

## Compatibility

| Family | Connection | Status in 0.3.3 |
| --- | --- | --- |
| DELUX M800 Mini, JBL Quantum 650 | USB 2.4 GHz receiver | Existing readers |
| Attack Shark R1, X6, X11, X11 SE, DELUX M600 Pro | USB 2.4 GHz receiver | Passive percentage reports |
| Rapoo MT760 Mini | USB 2.4 GHz receiver | Passive percentage reports |
| JBL Quantum 910 Wireless | USB 2.4 GHz receiver | Passive percentage reports |
| HyperX Cloud Stinger 2 Wireless | USB 2.4 GHz receiver | Battery percentage reader |
| HyperX Cloud Stinger Core Wireless | USB 2.4 GHz receiver | Estimated percentage from voltage |
| HyperX Pulsefire Haste 2 Wireless | USB 2.4 GHz receiver | Experimental, needs physical validation |
| HyperX Cloud Stinger Wireless PS4 original | USB 2.4 GHz receiver, 0951:16DA | Receiver detected. Battery reading unavailable |
| Razer wireless devices exposed by Synapse | Synapse 3 or 4 on this PC | Device logs read locally |
| Compatible Bluetooth devices | Bluetooth | Only when Windows exposes a battery property |
| XInput controllers | Xbox Wireless Adapter / compatible connection | Windows-reported battery category |

Exact model, firmware, dongle and connection mode can affect support. Synapse integration is based on the devices and data Synapse logs on the host. It does not guarantee that every Razer product reports a percentage. HID readers and the Haste 2 experiment still need validation across physical devices.

## Original Cloud Stinger PS4

The headset uses its Kingston `0951:16DA` 2.4 GHz receiver. NGENUITY 2.38.0.0 was inspected statically without running the installer. The inspected application assets contained Stinger Core and Stinger 2 identifiers and battery command names, but no matching battery routine or `0951:16DA` entry for the original PS4 receiver. Volti detects the receiver and displays its status without presenting a guessed percentage.

To investigate this receiver, compile `tools/StingerToolkit/Build.ps1`, run `StingerToolkit.exe --list`, then make annotated captures with the headset on, charging and at known battery levels. If the receiver only replies to a request, capture USB traffic while compatible software is actively displaying battery. The toolkit README explains how to interpret and share captures.

## Install

Download `Volti-0.3.3-Setup.exe` and run it for a per-user installation. To run portably, extract `Volti-0.3.3-portable.zip` and keep the included `assets` folder beside `Volti.exe`. Volti does not require administrator rights by default.

The release provides `Volti-0.3.3-source.zip`, containing the corresponding source, build scripts, license and third-party notices. `SHA256SUMS.txt` contains hashes for the distributable files.

## Licensing and acknowledgements

Volti is released under GNU AGPL v3 only. This release adapts report information from Joule and OpenMouse, HyperX HID research from HyperBatteryMonitor, and Synapse log formats documented by LeonardoVidolin's Razer Battery Widget. The Synapse approach also credits sanraith's razer-taskbar. UI layout and colors were inspired by the author's GymOrganizer. Credits, license notices and protocol provenance are in `THIRD_PARTY_NOTICES.md`.

The HyperX NGENUITY installer was inspected locally for product and protocol indicators. No installer, vendor executable, decompiled source, user log or personal device identifier is included in this release.

## Contributors

GitHub credits contributions through commit authors, linked issues and pull requests, and optionally a release's contributors panel. This release does not add the upstream protocol researchers to Volti's GitHub contributor graph unless they make a contribution to this repository. See `THIRD_PARTY_NOTICES.md` for their required project and license credits.
