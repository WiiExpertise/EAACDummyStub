internal class Program
{
    private static void Main(string[] args)
    {
        string gamePath = Environment.CurrentDirectory + "\\PVZBattleforNeighborville.exe";
        System.Diagnostics.Process.Start(gamePath, args);
    }
}