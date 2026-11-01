# EAAC Dummy Stub

A stub utility designed to assist Frosty Toolsuite in generating Type SDKs for Frostbite engine games protected by EA Javelin anticheat. It **DOES NOT** bypass anti-cheat mechanisms and **CANNOT** be used for cheating. It only provides a brief cold-start window to inspect memory for modding interoperability.

## How It Works

When an EAAC-protected game starts:

1. EA app / Steam / etc. done with DRM and launches the game executable.
2. The game executable launches `EAAntiCheat.GameServiceLauncher.exe` and exit.
3. The launcher OTA updates and starts the background service (`C:\Program Files\EA\AC\EAAntiCheat.GameService.exe`) that communicates with the game.
4. The launcher launches the game executable and exits. What a round trip.
* This is a simplified description. The actual thing is more complex, but it has nothing to do with us, no need to dive deeper.

This stub replaces that launcher, reads `EAAntiCheat.cfg` to find the game executable, launches it with the arguments it received, and exits, without starting any real anti-cheat service so **the game launch is promised to fail**.

`EAAntiCheat.cfg` is a resource-only PE image signed by EA using Authenticode. Its RCDATA resource holds a protobuf (Google Protocol Buffers) listing launch profiles (e.g. `default` -> `GW2.Main_Win64_Retail.exe`, `trial` -> `GW2.Main_Win64_Retail_Trial.exe`). The stub prefers `default` and falls back to whichever listed executable exists. Received arguments and detected profiles are printed to the console.

## How to Use

> [!NOTE]
> Last test: 2026/11/01 using PvZGW2, PvZBFN, and Battlefield 6.

1. Download from releases and make sure it's named `EAAntiCheat.GameServiceLauncher.exe`, PDB file is there in case you need it. You do not need .NET runtime because the stub is published as a self-contained NativeAOT executable.
2. Place it in the game directory, replacing the original launcher (suggested to back up the original first).
3. Launch the game.
- Because no anti-cheat background service is actually running, the game process detect the missing handshake **after a while** and terminate itself. For some titles you can get into bootflow (keeps saying connection error) and even run in local hosted mode (real gameplay but with DRM protected), but still, no online access.
4. Before the game realizes EAAC is fake and quits in protest, minimize or suspend the game process (e.g. Process Explorer since you have full access).
5. While the process is suspended in its initial state, the memory is accessible, you may generate the Type SDK using Frosty.
- Once you resume the process, it will still terminate. It is **impossible** to enter actual gameplay or connect to online services using this stub.

### Stub Options

Arguments starting with `--EAACDummyStub` (case-insensitive) are handled by the stub and excluded from being passed to the game.

| Option                  | Effect                                                                          |
| ----------------------- | ------------------------------------------------------------------------------- |
| `--EAACDummyStub-Wait`  | Stay alive until the game exits, then exit with the game's exit code.           |
| `--EAACDummyStub-Pause` | Wait for a key press before the stub closes, so the console output can be read. |

The stub always waits for a key press on errors.

### ARM64, Linux, macOS and other OS (Wine / Proton)

> [!WARNING]
> This method was not tested. Please tell me does it works, and report any issues.

Use the same x64 Windows build so the game/stub detects `.exe` extension, let Wine do the rest, the stub was designed to be case-insensitive. To suspend the game, suggest using `kill -STOP <pid>` on its Wine process.

## Building

Install .NET 10 SDK and run `dotnet publish`.

## Contributions

This project does not accept any form of cheating or anti-cheat circumvention. Issues and pull requests will be rejected if they aim to:

- Emulate, spoof, or replace the anti-cheat background services, the anti-cheat driver, or any part of the game <-> anti-cheat handshake.
- Keep the game running past the anti-cheat check, or let it reach gameplay or online services.
- Hide, inject into, or tamper with the game process, the anti-cheat, or their files (including modifying or re-signing `EAAntiCheat.cfg`).
- Decode or document anti-cheat data beyond the launch profile needed to start the game executable.

## Legal & Disclaimer

This project cannot, and does not intent to bypass anticheat. It is designed to read memory locally and cannot be used to facilitate any form of cheating. When using this tool, the game never reach EA server nor open normally. It is developed and distributed strictly in accordance with 17 U.S.C. § 1201(f) (Reverse Engineering for Interoperability) of the Digital Millennium Copyright Act:

1. Sole Purpose of Interoperability: This stub is designed exclusively to enable interoperability between lawfully acquired video game software and independently created third-party modding/analysis tools (specifically the open-source Frosty Toolsuite for static Type SDK generation).

2. No Anti-Cheat Circumvention: This software contains zero code to bypass, spoof, hook, or disable EA Javelin anticheat, digital rights management, or any other technical protection measure. The target application cannot complete initialization, cannot enter playable states, and cannot connect to EA servers or any online services.

3. Zero Tolerance for Cheating: This tool cannot be used to facilitate any form of online cheating, unauthorized automation, or terms-of-service violations.

4. No Proprietary Assets: This repository contains only a standard .NET process-launch wrapper and includes no proprietary code, binaries, or cryptographic keys belonging to Electronic Arts Inc.
