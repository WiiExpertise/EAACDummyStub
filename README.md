> [!NOTE]
> Project on hold. Planned to rewrite to eliminate steps 1, 2, 3.

# EAAC Dummy Stub

A stub utility designed to assist Frosty Toolsuite in generating Type SDKs for Frostbite engine games protected by EA Javelin anticheat. It **DOES NOT** bypass anti-cheat mechanisms and **CANNOT** be used for cheating. It only provides a brief cold-start window to inspect memory for modding interoperability.

## How to Use

> [!WARNING]
> This method was last tested in 2024, not sure if it works now.

1. Change the target executable name to the game you are analyzing in `Program.cs`.
2. Build the project.
3. Rename the compiled binary to `EAAntiCheat.GameServiceLauncher.exe`.
4. Place it in the game directory next to the game main executable (back up the original launcher is suggested).
5. Launch the game.
- Because no anti-cheat background service is actually running, the game process will detect the missing handshake **after a while** and terminate itself.
6. Before the process notice and self-terminates, minimize or suspend the game process (e.g. Process Explorer since you have full access).
8. While the process is suspended in its initial state, the memory is accessible, you may generate the Type SDK using Frosty.
- Once you resume the process, it will still terminate. It is **impossible** to enter actual gameplay or connect to online services using this stub.

## Legal & Disclaimer

This project cannot, and does not intent to bypass anticheat. It is designed to read memory locally and cannot be used to facilitate any form of cheating. When using this tool, the game never reach EA server nor open normally. It is developed and distributed strictly in accordance with 17 U.S.C. § 1201(f) (Reverse Engineering for Interoperability) of the Digital Millennium Copyright Act:

1. Sole Purpose of Interoperability: This stub is designed exclusively to enable interoperability between lawfully acquired video game software and independently created third-party modding/analysis tools (specifically the open-source Frosty Toolsuite for static Type SDK generation).

2. No Anti-Cheat Circumvention: This software contains zero code to bypass, spoof, hook, or disable EA Javelin anticheat, digital rights management, or any other technical protection measure. The target application cannot complete initialization, cannot enter playable states, and cannot connect to EA servers or any online services.

3. Zero Tolerance for Cheating: This tool cannot be used to facilitate any form of online cheating, unauthorized automation, or terms-of-service violations.

4. No Proprietary Assets: This repository contains only a standard .NET process-launch wrapper and includes no proprietary code, binaries, or cryptographic keys belonging to Electronic Arts Inc.
