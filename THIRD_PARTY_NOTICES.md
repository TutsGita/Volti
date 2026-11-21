# Third-party notices

## MingCute device icons

Mouse and headphone icons are from MingCute, supplied through SVG Repo.

- Upstream project https://github.com/mingcute-design/mingcute-icons
- License Apache License 2.0, included in `assets/LICENSE-MingCute.txt`
- Original SVG files are included alongside their PNG derivatives.
- Modifications consist of rasterization and runtime tinting to match battery levels.

The upstream icon license applies to those assets. Volti application source and build scripts are licensed under AGPL-3.0-only.

## Volti branding

The supplied Volti symbol and banner were created with AI image generation and selected for this project. These branding assets are included in the project distribution.

## Passive USB receiver readers

- **Joule** (Dillan Cidral and contributors), AGPL v3. The Rapoo MT760 Mini report decoder and shared Attack Shark/DELUX report handling were adapted on 2026-11-20 for the Volti Win32 HID reader. See [RapooProvider.cs](https://github.com/DillanCidral/Joule/blob/6d19c30d440c0b344c71480ed5152e56ba087d80/src/BatteryTray/Providers/RapooProvider.cs) and [AttackSharkProvider.cs](https://github.com/DillanCidral/Joule/blob/6d19c30d440c0b344c71480ed5152e56ba087d80/src/BatteryTray/Providers/AttackSharkProvider.cs). Volti uses receiver percentages only and omits Joule’s cable detection and incomplete V6 support.
- **OpenMouse-Project**, copyright (C) 2026 OpenMouse-Project, AGPL-3.0-or-later. The shared receiver model bytes and scales come from [OpenMouse’s Attack Shark HID driver](https://github.com/OpenMouse-Project/mouse-protocol/blob/main/src/drivers/attackshark/hid.ts), via Joule. The AGPL v3 text is included in `LICENSE`.
- **Halo Battery contributors**, copyright (c) 2026 Halo Battery contributors, MIT. JBL Quantum 910 Wireless collection and report details were adapted on 2026-11-20 from [protocols.md](https://github.com/HeyOkay/HaloBattery/blob/73e229cbee2e97fad659fcd96c78afdfa78488fb/docs/protocols.md). Halo Battery credits [plugato/JBL_Baterry_Monitor](https://github.com/plugato/JBL_Baterry_Monitor) for confirming the report pattern. Volti reads only the percentage report.

### Halo Battery MIT License

```text
MIT License

Copyright (c) 2026 Halo Battery contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## HyperX Cloud Stinger wireless battery readers

Protocol framing and Core voltage calibration adapted from David Santana's
[HyperBatteryMonitor](https://github.com/davidsantanaalves/HyperBatteryMonitor),
specifically [CloudStinger2WirelessDevice.cs](https://github.com/davidsantanaalves/HyperBatteryMonitor/blob/999d40bc1502d6e932c5c3df4e6a32d5f00c7158/Devices/CloudStinger2WirelessDevice.cs)
and [CloudStingerCoreWirelessDevice.cs](https://github.com/davidsantanaalves/HyperBatteryMonitor/blob/999d40bc1502d6e932c5c3df4e6a32d5f00c7158/Devices/CloudStingerCoreWirelessDevice.cs).
Thanks to [pvpiv](https://github.com/davidsantanaalves/HyperBatteryMonitor/pull/6)
for the PlayStation receiver validation. See the upstream provenance notes for
historical credit to auto94.

MIT License

Copyright (c) 2026 David Santana

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## Dracula palette and original Stinger research

- **Dracula Theme**, by Zeno Rocha and contributors. Volti uses the published color values from the [Dracula specification](https://spec.draculatheme.com/). The theme controls are implemented in Volti.
- **Kingston / HyperX** published the [HX-HSCSW-BK manual](https://media.kingston.com/support/downloads/hx_cloud_stinger_wireless_usermanual.pdf). **Bluetooth Battery Monitor** documents the [0951:16DA receiver](https://www.bluetoothgoodies.com/battery-monitor/version-history/). Volti uses this ID for presence detection only.
- **[chase-irql / OpenHyperX](https://github.com/chase-irql/OpenHyperX)** and **[shirk33y / hyperx-battery](https://github.com/shirk33y/hyperx-battery)** were consulted for protocol research. Volti incorporates no code from those two projects.

## GymOrganizer

The author's GymOrganizer project is the design reference for the purple palette, rounded cards, header and bottom navigation. The Volti controls were implemented in WinForms.

## XInput battery category labels

Volti uses the Windows XInput API to read controller battery categories and maps the documented DISCONNECTED, WIRED, ALKALINE, NIMH and UNKNOWN values to their corresponding display states. No third-party code is redistributed for this reader. See Microsoft's [XINPUT_BATTERY_INFORMATION reference](https://learn.microsoft.com/en-us/windows/win32/api/xinput/ns-xinput-xinput_battery_information).

## Razer Synapse battery data

The C# reader uses log formats documented by **LeonardoVidolin** in [Razer Battery Widget](https://github.com/LeonardoVidolin/razer-battery-widget), with credit to [sanraith/razer-taskbar](https://github.com/sanraith/razer-taskbar) for the local-log approach. The [upstream MIT license and copyright notice](https://github.com/LeonardoVidolin/razer-battery-widget/blob/main/LICENSE) apply to the upstream project. Volti implements its own C# parsing and UI.

- [Synapse 3 reference](https://github.com/LeonardoVidolin/razer-battery-widget/blob/main/watcher/watcherV3.js) reads battery, load and removal events from `%LOCALAPPDATA%\Razer\Synapse3\Log\Razer Synapse 3.log`.
- [Synapse 4 reference](https://github.com/LeonardoVidolin/razer-battery-widget/blob/main/watcher/watcherV4.js) reads device snapshots from matching systray logs under `%LOCALAPPDATA%\Razer\RazerAppEngine\User Data\Logs`.
- Volti reads the tail of these local files. No Synapse logs, device serial numbers or samples from a real user account are included in the build.

## HyperX NGENUITY 2.38.0.0 research

Installer examined on 2026-11-20. The Authenticode signature was valid with HP Inc. as signer. SHA-256 `19d5744a6781130f9354cf455c16959eef8756ff683ff56cbd48ce9e50cff804`.

Static inspection used the embedded app ZIP64 archive beginning at offset 3067834 and ending before offset 435017047, with 1058 entries. The installer was not run. Relevant metadata and strings were examined in `NGenuity2.dll` and `Assets/Native/NGenuity2Helper.exe`.

| Device or family | Evidence found | Volti status |
| --- | --- | --- |
| Cloud Stinger 2 Wireless | `03F0:0D93`, interface 03/collection 03 and a battery-level command | Existing percentage reader |
| Cloud Stinger Core Wireless | `0951:170B`, interface 03/collection 03 and a battery query | Existing voltage-based estimate; compatibility varies by receiver |
| Cloud Stinger Wireless PS4 original | No exact `0951:16DA` or named battery routine found in the inspected strings | Detection only; see [Stinger Toolkit](tools/StingerToolkit/README.md) |
| Cloud Flight Wireless/Flight S, Cloud II Core Wireless, Cloud III/III S | Battery-query symbols in the helper | Candidates for future readers |
| Pulsefire Haste Wireless (first generation) | `03F0:028E`, interface 02, and battery-query symbols | Candidate for a future reader |
| Pulsefire Haste 2 Wireless | Haste 2 Wireless support symbols in this installer. The packet format comes from separate NGENUITY 3.1 research | Experimental reader for `03F0:0D97` / `03F0:0F98` |

String matches identify candidates and do not establish a working protocol. The Haste 2 reader uses the connection and battery queries documented by [PMarinov1994/ngenuity-linux-cli](https://github.com/PMarinov1994/ngenuity-linux-cli) and [Halo Battery contributors](https://github.com/HeyOkay/HaloBattery/blob/main/docs/protocols.md#hyperx-pulsefire-haste-2-wireless-ngenuity-protocol). It was implemented in Volti with an interface and report-length check. No vendor binaries, resources or decompiled code are redistributed.
