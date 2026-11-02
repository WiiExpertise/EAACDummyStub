# <img src="Resources/Logo_FA_RGB_1em.svg" alt="EA anticheat launcher icon with color changed to magenta and 'EA' changed to 'FA'. Means both the 'FAAA!!' meme and shorten for 'Fake'." align="absmiddle"> EA anticheat Dummy Stub

A stub utility for assist modding Frostbite engine games protected by EA Javelin anticheat with Frosty Toolsuite. Including generating Type SDKs, and launching modded games directly in local hosted mode for testing, without external tools (e.g. Frosty Fix). It **DOES NOT** bypass anti-cheat or DRM and **CANNOT** be used for online cheating or pirating, the game never reaches EA servers or any online service.

## How It Works

When an EAAC-protected game starts:

1. EA app / Steam / etc. done with DRM and launches the game executable.
2. The game executable launches `EAAntiCheat.GameServiceLauncher.exe` and exit.
3. The launcher OTA updates and starts the background service (`C:\Program Files\EA\AC\EAAntiCheat.GameService.exe`) that communicates with the game.
4. The launcher launches the game executable and exits. What a round trip.
* This is a simplified description. The actual thing is more complex, but it has nothing to do with us, no need to dive deeper.

This stub replaces that launcher, reads `EAAntiCheat.cfg` to find the game executable, launches it with the arguments it received, and exits, without starting any real anti-cheat service. The game never gets its anti-cheat handshake, so **it can never connect to EA servers** unless EA messed up. Depending on the title, it either quits after a while or stays offline, and some titles can still run in local hosted mode.

`EAAntiCheat.cfg` is a resource-only PE image signed by EA using Authenticode. Its RCDATA resource holds a protobuf (Google Protocol Buffers) listing launch profiles (e.g. `default` -> `GW2.Main_Win64_Retail.exe`, `trial` -> `GW2.Main_Win64_Retail_Trial.exe`). The stub prefers `default` and falls back to whichever listed executable exists. Received arguments and detected profiles are printed to the console.

The core code of this stub is 1 line. Everything else is about auto-locate game executable and args parsing so it’s a no-brain replace.

```csharp
Process.Start("Game.exe");
```

## How to Use

> [!NOTE]
> Last tested: 2026/11/01. See: [Test Result](#test-result)

1. Download from releases and make sure it's named `EAAntiCheat.GameServiceLauncher.exe`. PDB file is only needed for debugging when you got error from Stub itself. You do not need .NET runtime because the stub use NativeAOT.
2. Place it in the game directory, replacing the original launcher (suggested to back up the original first).
3. Launch the game as usual. DRM still applies.

### Generating Type SDK

1. Before the game realizes EAAC is fake and quits in protest, minimize or suspend the game process (e.g. Process Explorer since you have full access).
2. While the process is suspended, the memory is accessible, you may generate the Type SDK using Frosty.

### Local Hosted Mode

For titles that support it, pass the level and mode arguments (usually can be found with Frosty) through your platform or alternatively the [args file](#args-file) to load directly into a local hosted match. Useful for testing Frosty mods. Online services are still unreachable, DRM still applies.

### Stub Options

Arguments starting with `--EAACDummyStub` (case-insensitive) are handled by the stub and excluded from being passed to the game.

| Option                  | Effect                                                                          |
| ----------------------- | ------------------------------------------------------------------------------- |
| `--EAACDummyStub-Wait`  | Stay alive until the game exits, then exit with the game's exit code.           |
| `--EAACDummyStub-Pause` | Wait for a key press before the stub closes, so the console output can be read. |

The stub always waits for a key press on errors.

### Args File

Some launchers (e.g. EA app) reject special characters in launch arguments, especially like `;`. We got control during the launch process, so instead, put arguments in `EAACDummyStub.Args.txt` (case-insensitive) next to the stub, and they will be appended. Stub options work there too.

- Arguments are separated by whitespace, wrap an argument containing spaces in double quotes.
- Multiple lines are allowed; blank lines and lines starting with `#` are ignored.

Example (PvZGW2, level and mode names can be found with Frosty / Private Server tools):

```
# PvZGW2 local hosted (this comment starts with # so it's ignored)
-Level Level_FE_Hub -Game.DefaultLayerInclusion GameMode=FreeRoam;HostedMode=LocalHosted

# EAAC Dummy Stub options in another line for eazy editing
--EAACDummyStub-Pause
```

### ARM64, Linux, macOS and other OS (Wine / Proton)

> [!WARNING]
> This method was not tested. Please tell me does it works, and report any issues.

Use the same x64 Windows build so the game/stub detects `.exe` extension, let Wine do the rest, the stub was designed to be case-insensitive. To suspend the game, suggest using `kill -STOP <pid>` on its Wine process.

## Test Result

Feel free to test more games in your own environment and contributes! EAAC have OTA updates so keep this table up to date.

| Date       | OS      | Platform | Game          | Result                                                                                                         |
| ---------- | ------- | -------- | ------------- | -------------------------------------------------------------------------------------------------------------- |
| 2026-11-01 | Win-x64 | EA app   | PvZ: GW2      | 🟢 Reaches bootflow (keeps spinning on connecting). Local hosted mode works with [launch arguments](#args-file).  |
| 2026-11-01 | Win-x64 | EA app   | PvZ: BFN      | 🟡 Reaches bootflow (keeps showing connection errors). Local hosted mode not working (maybe wrong arguments).     |
| 2026-11-01 | Win-x64 | EA app   | Battlefield 6 | 🔴 Black screen, then a Win32 dialog saying the environment may be incompatible.                                  |
| 2024       | Win-x64 | EA app   | PvZ: GW2      | 😂 Reaches bootflow for few seconds and terminate itself. Seems more like a bug nowdays.                          |

*Bootflow is the Frostbite screen every game shows first to connect to EA servers.*

## Building

Install .NET 10 SDK and run `dotnet publish`.

## Contributions

This project does not accept any form of cheating, anti-cheat circumvention, or DRM circumvention. Offline play without anti-cheat is only acceptable because it never touches other players and still applies DRM. Issues and pull requests will be rejected if they aim to:

- Emulate, spoof, or replace the anti-cheat background services, the anti-cheat driver, or any part of the game <-> anti-cheat handshake.
- Let the game connect to EA servers or any official online service.
- Bypass or weaken DRM (EA app / Steam / Epic Games / Xbox / etc. ownership checks).
- Hide, inject into, or tamper with the game process, the anti-cheat, or their files (including modifying or re-signing `EAAntiCheat.cfg`).
- Decode or document anti-cheat data beyond the launch profile needed to start the game executable.

## Legal & Disclaimer

This project cannot, and does not intent to bypass anticheat. It is designed for local modding and cannot be used to facilitate online cheating. When using this tool, the game never reaches EA servers or any official online service. It is developed and distributed strictly in accordance with 17 U.S.C. § 1201(f) (Reverse Engineering for Interoperability) of the Digital Millennium Copyright Act:

1. Sole Purpose of Interoperability: This stub is designed exclusively to enable interoperability between lawfully acquired video game software and independently created third-party modding/analysis tools (specifically the open-source Frosty Toolsuite, for Type SDK generation and testing mods offline).

2. No Anti-Cheat Circumvention: This software contains zero code to bypass, spoof, hook, or disable EA Javelin anticheat, digital rights management, or any other technical protection measure. It never starts, emulates, or answers the anti-cheat service. Without it, the target application cannot connect to EA servers or any online service. Some titles still run in local hosted mode, which never involves other players. DRM remains fully enforced and untouched.

3. Zero Tolerance for Cheating: This tool cannot be used to facilitate any form of online cheating, as the game can never reach online services.

4. Zero Tolerance for Pirating: This tool cannot be used to facilitate any form of pirating. User still needs to purchased a legal license/copy to launch the game, the launch still goes through EA app or corresponding platform where they purchase at.

5. No Proprietary Assets: This repository contains only a standard .NET process-launch wrapper and includes no proprietary code, binaries, or cryptographic keys belonging to Electronic Arts Inc.
