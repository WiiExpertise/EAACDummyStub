using System.Diagnostics;

using EAACDummyStub;

const string configFileName = "EAAntiCheat.cfg";
const string stubArgPrefix = "--EAACDummyStub";

Console.WriteLine($"[EAAC Dummy Stub] Received {args.Length} argument(s):");
for (int i = 0; i < args.Length; i++)
{
    Console.WriteLine($"  [{i}] {args[i]}");
}

// --EAACDummyStub-Wait    Stay alive until the game exits and return its exit code
// --EAACDummyStub-Pause   Wait for a key press before the stub closes, to read the output
// Stub-only switches are stripped so the game never sees them.
bool wait = false, pause = false;
List<string> gameArgs = [];
foreach (string arg in args)
{
    if (!arg.StartsWith(stubArgPrefix, StringComparison.OrdinalIgnoreCase))
    {
        gameArgs.Add(arg);
        continue;
    }

    switch (arg[stubArgPrefix.Length..].ToLowerInvariant())
    {
        case "-wait": wait = true; break;
        case "-pause": pause = true; break;
        default: Console.WriteLine($"[EAAC Dummy Stub] Ignoring unknown stub option: {arg}"); break;
    }
}

int exitCode = 0;
try
{
    // The stub sits next to the game, resolve everything relative to itself.
    string gameDirectory = AppContext.BaseDirectory;
    string configPath = Path.Combine(gameDirectory, configFileName);

    List<GameProfile> profiles = EaacConfig.Read(configPath);
    foreach (GameProfile p in profiles)
    {
        Console.WriteLine($"[EAAC Dummy Stub] Profile '{p.Name}': {p.Executable} (ID: {p.GameId})");
    }

    // Prefer the "default" profile, but fall back to whichever executable is actually installed (e.g. trial)
    GameProfile profile = profiles
        .OrderByDescending(static p => p.Name == "default")
        .FirstOrDefault(p => File.Exists(Path.Combine(gameDirectory, p.Executable)))
        ?? throw new FileNotFoundException($"None of the executables listed in {configFileName} exist in {gameDirectory}");

    string gamePath = Path.Combine(gameDirectory, profile.Executable);
    Console.WriteLine($"[EAAC Dummy Stub] Launching '{profile.Name}' profile: {gamePath}");

    using Process game = Process.Start(new ProcessStartInfo(gamePath, gameArgs) { WorkingDirectory = gameDirectory })
        ?? throw new InvalidOperationException($"Failed to start {gamePath}");
    Console.WriteLine($"[EAAC Dummy Stub] Game started, PID: {game.Id}");

    // Exit right after launching unless asked to wait.
    if (wait)
    {
        Console.WriteLine("[EAAC Dummy Stub] Waiting for the game to exit...");
        game.WaitForExit();
        exitCode = game.ExitCode;
        Console.WriteLine($"[EAAC Dummy Stub] Game exited with code: {exitCode}");
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"[EAAC Dummy Stub] Error occurred: {ex}");
    exitCode = 1;
    pause = true;
}

if (pause && !Console.IsInputRedirected)
{
    Console.WriteLine("Press any key to exit.");
    Console.ReadKey(true);
}
return exitCode;