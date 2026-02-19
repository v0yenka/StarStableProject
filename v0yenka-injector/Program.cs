using SSO_Library_Test;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Program
{
    // Starting config
    static string processName = "PXStudioEngine";
    static string mapWindowHex = "67 6C 6F 62 61 6C 2F 4D 61 70 57 69 6E 64 6F 77 2E 53 74 61 72 74 28 29 3B";
    static string replacementScript = "global/Horse.AddRelativeForce(0,1,2.5f);"; // examplary script, modify if needed

    static void Main(string[] args)
    {
        try
        {
            RunHackAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Log($"[FATAL ERROR]: {ex.Message}", ConsoleColor.Red);
        }

        // Keeping the window open
        Console.WriteLine("\n[Press any key to start...]");
        Console.ReadKey();
    }

    static async Task RunHackAsync()
    {
        Console.Title = "★ v0yenka Memory Injector ★";
        PrintLogo();

        Log("[*] Initializing in process...", ConsoleColor.Yellow);

        Mem memory = new Mem();

        // 1. Attaching to the game
        if (!memory.OpenProcess(processName))
        {
            Log($"[!]Process not found: {processName}.exe", ConsoleColor.Red);
            Log("[!] Keep the game open!", ConsoleColor.DarkGray);
            return;
        }

        Log($"[+] Successfully attached (PID: {memory._pid})!", ConsoleColor.Green);
        Log("[*] Starting AoB scanning...", ConsoleColor.Cyan);

        // 2. Looking for HEX
        var foundAddresses = await memory.AoBScan(mapWindowHex);

        if (foundAddresses == null || !foundAddresses.Any())
        {
            Log("[-] HEX not found in game memory", ConsoleColor.Red);
            return;
        }

        uint targetAddress = foundAddresses.First();
        Log($"[+] Address found: 0x{targetAddress:X}", ConsoleColor.Magenta);

        // 3. Injecting the script
        Log("[*] Injection in process...", ConsoleColor.Yellow);
        InjectScriptSafely(memory, targetAddress, replacementScript, mapWindowHex);

        Log("[+] YOU'RE ALL SET! Have fun playing ;3", ConsoleColor.Green);
    }

    // Encoding the given script to HEX format
    static void InjectScriptSafely(Mem mem, uint address, string newScript, string hexSignature)
    {
        int originalByteLength = hexSignature.Replace(" ", "").Length / 2;
        byte[] scriptBytes = Encoding.ASCII.GetBytes(newScript);
        byte[] payload = new byte[originalByteLength];

        for (int i = 0; i < payload.Length; i++) payload[i] = 0x00;
        Array.Copy(scriptBytes, 0, payload, 0, Math.Min(scriptBytes.Length, payload.Length));

        mem.WriteBytes(address, payload);
    }

    static void PrintLogo()
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(@"
   _  _ ___  _   _ ____ _  _ _  _ ____ 
   |  | |  \  \_/  |___ |\ | |_/  |__| 
    \/  |__/   |   |___ | \| | \_ |  | 
          LIVE MEMORY INJECTOR
        ");
        Console.ResetColor();
    }

    static void Log(string message, ConsoleColor color)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}