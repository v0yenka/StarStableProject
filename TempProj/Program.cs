using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Threading;

class Program
{
   
    static string defaultGameExe = @"";  // Type your game exe path here
    static string defaultTarget = @"";    // Type your target file path here
    static string defaultBackupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "StarstableBackups");
    // Horse flying code swap
    static string originalText = "global/MapWindow.Start();";
    static string replacementText = "global/Horse.AddRelativeForce(0,1,2.5f);";
    static bool truncateIfLonger = true;

    static int Main(string[] args)
    {
        var cfg = CommandLine.Parse(args);
        string target = cfg.Target ?? defaultTarget;
        string gameExe = cfg.GameExe ?? defaultGameExe;
        string backupFolder = cfg.BackupFolder ?? defaultBackupFolder;
        bool restoreAfterExit = !cfg.NoRestore;

        Console.WriteLine($"Target: {target}");
        Console.WriteLine($"Game exe: {gameExe}");
        Console.WriteLine();

        if (!File.Exists(target))
        {
            Console.Error.WriteLine("Target file not found. Check path.");
            return 2;
        }

        Directory.CreateDirectory(backupFolder);
        string backupPath = MakeBackup(target, backupFolder);
        Console.WriteLine($"Backup created: {backupPath}");

        bool changed = false;

        try
        {
            if (TryTextReplace(target, originalText, replacementText, out string usedEncoding))
            {
                Console.WriteLine($"Text replacement succeeded (encoding: {usedEncoding}).");
                changed = true;
            }
            else
            {
                Console.WriteLine("Text replacement did not find the pattern or file is not plain-text. Falling back to binary attempt.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Text replacement attempt failed: {ex.Message}");
        }

        // If text replacement failed, try binary replacement for UTF-8 encoded bytes
        if (!changed)
        {
            try
            {
                byte[] origBytes = Encoding.UTF8.GetBytes(originalText);
                byte[] replBytes = Encoding.UTF8.GetBytes(replacementText);

                int result = TryBinaryReplace(target, origBytes, replBytes, truncateIfLonger, out string msg);
                if (result == 1)
                {
                    Console.WriteLine("Binary replacement succeeded.");
                    changed = true;
                }
                else if (result == 2)
                {
                    Console.WriteLine($"Binary replacement: pattern not found. {msg}");
                }
                else
                {
                    Console.WriteLine($"Binary replacement partial: {msg}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Binary replacement attempt failed: {ex.Message}");
            }
        }

        if (!changed)
        {
            Console.WriteLine("No changes were made. Restore will be skipped.");
        }

        // Launch game if requested
        if (cfg.Launch)
        {
            if (!File.Exists(gameExe))
            {
                Console.WriteLine("Game exe not found; skipping launch.");
            }
            else
            {
                var proc = StartGame(gameExe, Path.GetDirectoryName(gameExe));
                if (proc != null)
                {
                    Console.WriteLine($"Launched game (PID {proc.Id}). Waiting for exit...");
                    // If caller requested a specific process name to wait for
                    if (!string.IsNullOrEmpty(cfg.WaitProcessName))
                    {
                        WaitForProcessByName(cfg.WaitProcessName);
                        WaitForProcessExitByName(cfg.WaitProcessName);
                    }
                    else
                    {
                        proc.WaitForExit();
                    }
                    Console.WriteLine("Game appears to have exited.");
                }
            }
        }

        // Restore original if requested
        if (restoreAfterExit && File.Exists(backupPath))
        {
            Console.WriteLine("Restoring original from backup...");
            File.Copy(backupPath, target, overwrite: true);
            Console.WriteLine("Restore complete.");
        }
        else
        {
            Console.WriteLine("Restore skipped (no backup or --no-restore used).");
        }

        return 0;
    }

    static string MakeBackup(string target, string backupFolder)
    {
        string stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        string baseName = Path.GetFileName(target);
        string bk = Path.Combine(backupFolder, $"{baseName}.{stamp}.bak");
        File.Copy(target, bk);
        return bk;
    }

    static bool TryTextReplace(string path, string oldText, string newText, out string encodingUsed)
    {
        encodingUsed = null;
        // Heuristic: check for null bytes in first chunk -> treat as binary
        using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            byte[] peek = new byte[4096];
            int read = fs.Read(peek, 0, peek.Length);
            if (peek.Take(read).Contains((byte)0))
            {
                return false; // binary-looking file
            }
        }

        // Try UTF-8, then Windows-1252
        string[] encs = new[] { "utf-8", "windows-1252", "iso-8859-1" };
        foreach (var encName in encs)
        {
            try
            {
                var enc = Encoding.GetEncoding(encName);
                string data = File.ReadAllText(path, enc);
                if (data.Contains(oldText))
                {
                    string newData = data.Replace(oldText, newText);
                    // atomic write using temp then replace
                    string tmp = Path.Combine(Path.GetDirectoryName(path), Path.GetFileName(path) + ".tmp");
                    File.WriteAllText(tmp, newData, enc);
                    File.Replace(tmp, path, null);
                    encodingUsed = encName;
                    return true;
                }
            }
            catch { /* ignore and try next encoding */ }
        }
        return false;
    }

    // returns:
    // 1 == success
    // 2 == pattern not found
    // 3 == replaced but replacement truncated/padded (info in msg)
    static int TryBinaryReplace(string path, byte[] match, byte[] replace, bool truncateIfLonger, out string msg)
    {
        msg = "";
        byte[] data = File.ReadAllBytes(path);
        int idx = IndexOfSequence(data, match);
        if (idx < 0)
        {
            msg = "pattern not found";
            return 2;
        }

        if (replace.Length == match.Length)
        {
            Array.Copy(replace, 0, data, idx, replace.Length);
        }
        else if (replace.Length < match.Length)
        {
            // pad with 0x00
            byte[] padded = new byte[match.Length];
            Array.Copy(replace, 0, padded, 0, replace.Length);
            // remaining bytes are 0 by default
            Array.Copy(padded, 0, data, idx, padded.Length);
            msg = $"replacement shorter than match: padded with {match.Length - replace.Length} NUL bytes";
        }
        else // replace.Length > match.Length
        {
            if (truncateIfLonger)
            {
                byte[] truncated = new byte[match.Length];
                Array.Copy(replace, 0, truncated, 0, truncated.Length);
                Array.Copy(truncated, 0, data, idx, truncated.Length);
                msg = $"replacement longer than match: truncated to {match.Length} bytes";
            }
            else
            {
                // build new array (may shift file size — risky). We'll allow it but warn.
                byte[] newData = new byte[data.Length - match.Length + replace.Length];
                Buffer.BlockCopy(data, 0, newData, 0, idx);
                Buffer.BlockCopy(replace, 0, newData, idx, replace.Length);
                Buffer.BlockCopy(data, idx + match.Length, newData, idx + replace.Length, data.Length - (idx + match.Length));
                data = newData;
                msg = $"replacement longer than match: file size changed by {replace.Length - match.Length} bytes (risky)";
            }
        }

        // write temp + atomic replace
        string tmp = Path.Combine(Path.GetDirectoryName(path), Path.GetFileName(path) + ".tmp");
        File.WriteAllBytes(tmp, data);
        File.Replace(tmp, path, null);
        return msg == "" ? 1 : 3;
    }

    static int IndexOfSequence(byte[] array, byte[] pattern)
    {
        if (pattern.Length == 0) return -1;
        for (int i = 0; i <= array.Length - pattern.Length; i++)
        {
            bool ok = true;
            for (int j = 0; j < pattern.Length; j++)
            {
                if (array[i + j] != pattern[j]) { ok = false; break; }
            }
            if (ok) return i;
        }
        return -1;
    }

    static Process StartGame(string exePath, string workingDir)
    {
        try
        {
            var psi = new ProcessStartInfo { FileName = exePath, WorkingDirectory = workingDir, UseShellExecute = true };
            return Process.Start(psi);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to start game: " + ex.Message);
            return null;
        }
    }

    static void WaitForProcessByName(string name)
    {
        string shortName = Path.GetFileNameWithoutExtension(name);
        while (true)
        {
            var any = Process.GetProcessesByName(shortName);
            if (any.Length > 0) return;
            Thread.Sleep(500);
        }
    }

    static void WaitForProcessExitByName(string name)
    {
        string shortName = Path.GetFileNameWithoutExtension(name);
        while (true)
        {
            var procs = Process.GetProcessesByName(shortName);
            if (procs.Length == 0) return;
            Thread.Sleep(1000);
        }
    }

    class CommandLine
    {
        public string Target;
        public string GameExe;
        public string BackupFolder;
        public bool NoRestore = false;
        public bool Launch = true;
        public string WaitProcessName;

        public static CommandLine Parse(string[] args)
        {
            var c = new CommandLine();
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                switch (a.ToLowerInvariant())
                {
                    case "--target": c.Target = args[++i]; break;
                    case "--game-exe": c.GameExe = args[++i]; break;
                    case "--backup-folder": c.BackupFolder = args[++i]; break;
                    case "--no-restore": c.NoRestore = true; break;
                    case "--no-launch": c.Launch = false; break;
                    case "--wait-process-name": c.WaitProcessName = args[++i]; break;
                    case "--help":
                    case "-h":
                        Console.WriteLine("Options: --target <path> --game-exe <path> --backup-folder <path> --no-restore --no-launch --wait-process-name <name>");
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine($"Unknown arg {a}");
                        break;
                }
            }
            return c;
        }
    }
}