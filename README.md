# FFXIVExpiryCallouts

Dalamud plugin that tracks selected self-applied buffs and enemy debuffs, then sends status names to IINACT TTS near expiry. A game sound can play if the IINACT connection fails.

## Project layout

- `FFXIVExpiryCallouts.csproj` — Dalamud API 15 project and plugin metadata.
- `src/` — plugin entry point, configuration window, tracker, and status catalog.
- `bin/` and `obj/` — generated build output; ignored by Git.

## Features

- Open settings with `/statuscallouts` or Dalamud's plugin config button.
- Select status groups matching the requested tracker list.
- Set expiry threshold from 1 to 600,000 milliseconds; default is 5,000 ms.
- Send status names to IINACT's OverlayPlugin WebSocket `say` call for TTS.
- Optionally play one of FFXIV's 16 chat sound effects when IINACT TTS cannot be reached.
- Track statuses directly through Dalamud. ACT is not required only IINACT for TTS if wanted.

## Dependencies

- Dalamud API 15 is required to load the plugin.
- IINACT is required for TTS. Enable its OverlayPlugin WebSocket server; the default URL is `ws://127.0.0.1:10501/ws`.
- The sound fallback works when IINACT cannot be reached.
- .NET 10 is required only to build the plugin.

## Copied and adapted material

- `src/StatusCatalog.cs` adapts the status IDs and group labels from [Triggevent's `DotBuff.java`](https://github.com/xpdota/event-trigger/blob/master/xivdata/src/main/java/gg/xp/xivdata/data/DotBuff.java). Triggevent is GPL-3.0; this project is GPL-3.0-or-later.
- The IINACT callout uses its documented OverlayPlugin WebSocket `say` request shape. No IINACT source code is adapted in this plugin.

## Build

Requires .NET 10 and Dalamud API 15. With `DALAMUD_HOME` set to your Dalamud Hooks directory, run from repository root:

```bash
dotnet build -c Release
```

The Dalamud SDK creates the plugin DLL under `bin/Release/` and package ZIP under `bin/Release/FFXIVExpiryCallouts/latest.zip`.

## IINACT TTS setup

IINACT must be running with its OverlayPlugin WebSocket server enabled. Set or verify the URL in plugin settings if it differs from `ws://127.0.0.1:10501/ws`.

On Linux under Wine, IINACT uses its Google TTS path by default, which needs internet access. The sound fallback activates when the WebSocket connection or send fails. IINACT does not acknowledge whether speech playback succeeded, so silent synthesis failures cannot trigger fallback.

## License

Licensed under the GNU General Public License, version 3 or later. See [LICENSE](LICENSE).
