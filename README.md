> [!NOTE]
> Project on hold, planned to rewrite and eliminate step 1, 2, 3.

# Fake EAAC

A tool to allow memory reading for EA Javelin anticheat protected games. Mostly used for Frosty to generate type SDK. It CANNOT be used for actual cheating, just a small trick to read memory.

## How to use

> [!WARNING]
> This method is only tested at 2024, not sure if it works now.

1. Change the executable name to the game you are working on at `Program.cs`
2. Build the program
3. Rename it to `EAAntiCheat.GameServiceLauncher.exe`
4. Replace it in game directory, next to your game. (Suggest to backup the original EAAC launcher)
5. Open the game.
- After a while, the game will discover the EAAC is fake, and terminate itself.
7. Before the game notice that, minimize or suspend (e.g. Process Explorer since you got full access) the game process.
8. The memory is accessable now, create SDK using Frosty.
- If you resume the process, it will still terminate, there is no chance to get in real game.

## Disclaimer

This project cannot, and does not intent to bypass anticheat. It is designed to read memory locally and cannot be used to facilitate any form of cheating. When using this tool, the game never reach EA server nor open normally.
