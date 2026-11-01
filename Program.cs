using System.Diagnostics;
using System.Text;

using EAACDummyStub;

const string configFileName = "EAAntiCheat.cfg";
const string argsFileName = "EAACDummyStub.Args.txt";
const string stubArgPrefix = "--EAACDummyStub";

Console.WriteLine($"[EAAC Dummy Stub] Received {args.Length} argument(s):");
for (int i = 0; i < args.Length; i++)
{
    Console.WriteLine($"  [{i}] {args[i]}");
}

bool wait = false, pause = false;
int exitCode = 0;
try
{
    // The stub sits next to the game, resolve everything relative to itself.
    string gameDirectory = AppContext.BaseDirectory;

    // Extra arguments from the text file next to the stub, appended after the received ones.
    List<string> allArgs = [.. args];
    List<string> argsPaths = [.. Directory
        .EnumerateFiles(gameDirectory)
        .Where(static f => Path.GetFileName(f).Equals(argsFileName, StringComparison.OrdinalIgnoreCase))];
    foreach (string argsPath in argsPaths)
    {
        List<string> fileArgs = ReadArgsFile(argsPath);
        Console.WriteLine($"[EAAC Dummy Stub] Loaded {fileArgs.Count} argument(s) from {Path.GetFileName(argsPath)}:");
        for (int i = 0; i < fileArgs.Count; i++)
        {
            Console.WriteLine($"  [{i}] {fileArgs[i]}");
        }
        allArgs.AddRange(fileArgs);
    }

    // --EAACDummyStub-Wait    Stay alive until the game exits and return its exit code
    // --EAACDummyStub-Pause   Wait for a key press before the stub closes, to read the output
    // Stub-only switches are stripped so the game never sees them.
    List<string> gameArgs = [];
    foreach (string arg in allArgs)
    {
        if (!arg.StartsWith(stubArgPrefix, StringComparison.OrdinalIgnoreCase))
        {
            gameArgs.Add(arg);
            continue;
        }

        switch (arg[stubArgPrefix.Length..].ToLowerInvariant())
        {
            case "-wait":
                wait = true;
                Console.WriteLine($"[EAAC Dummy Stub] Stub option -Wait received and excluded");
                break;
            case "-pause": 
                pause = true; 
                Console.WriteLine($"[EAAC Dummy Stub] Stub option -Pause received and excluded");
                break;
            default:
                Console.WriteLine($"[EAAC Dummy Stub] Ignoring and excluding unknown stub option: {arg}"); 
                break;
        }
    }

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

// Each line split on whitespace, double quotes group text containing spaces.
// Blank lines and lines starting with '#' are ignored.
static List<string> ReadArgsFile(string path)
{
    List<string> result = [];
    foreach (string rawLine in File.ReadLines(path))
    {
        string line = rawLine.Trim();
        if (line.Length == 0 || line.StartsWith('#'))
        {
            continue;
        }

        StringBuilder current = new();
        bool inQuotes = false, hasToken = false;
        foreach (char c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                hasToken = true;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (hasToken)
                {
                    result.Add(current.ToString());
                    current.Clear();
                    hasToken = false;
                }
            }
            else
            {
                current.Append(c);
                hasToken = true;
            }
        }
        if (hasToken)
        {
            result.Add(current.ToString());
        }
    }
    return result;
}