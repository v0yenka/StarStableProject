using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
// using System.Windows.Forms;

namespace SSO_Library_Test
{
    public class Mem
    {
        // Working on 32-bit system
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, uint lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(IntPtr hProcess, uint lpBaseAddress, byte[] lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        public static extern int VirtualQueryEx(IntPtr hProcess, uint lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public uint BaseAddress;
            public uint AllocationBase;
            public uint AllocationProtect;
            public uint RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        private IntPtr _handle;
        public int _pid;

        // Opening the process by name and getting its handle
        public bool OpenProcess(string procName)
        {
            Process[] procs = Process.GetProcessesByName(procName);
            if (procs.Length > 0)
            {
                _pid = procs[0].Id;
                _handle = OpenProcess(0x1F0FFF, false, _pid); // All Access
                return _handle != IntPtr.Zero;
            }
            return false;
        }

        // Scanning for a byte pattern (AoB) in the process memory
        public async Task<IEnumerable<uint>> AoBScan(string hexSignature)
        {
            return await Task.Run(() =>
            {
                ConcurrentBag<uint> foundAddresses = new ConcurrentBag<uint>();

                // Taking every 2 characters as a byte (ignoring spaces)
                string[] hexSplit = hexSignature.Replace(" ", "").Split(new[] { "" }, StringSplitOptions.RemoveEmptyEntries);
                byte[] pattern = new byte[hexSignature.Replace(" ", "").Length / 2];
                for (int i = 0; i < pattern.Length; i++)
                {
                    string byteStr = hexSignature.Replace(" ", "").Substring(i * 2, 2);
                    pattern[i] = byte.Parse(byteStr, NumberStyles.HexNumber);
                }

                // Had to set limits for scanning due to 32-bit process
                uint minAddress = 0;
                uint maxAddress = 0x7FFFFFFF; // 2 GB is the standard limit

                MEMORY_BASIC_INFORMATION memInfo = new MEMORY_BASIC_INFORMATION();
                uint currentAddr = minAddress;

                while (currentAddr < maxAddress)
                {
                    if (VirtualQueryEx(_handle, currentAddr, out memInfo, (uint)Marshal.SizeOf(memInfo)) == 0)
                        break;

                    // Checking the access rights and state of the memory block
                    if (memInfo.State == 0x1000 && (memInfo.Protect & 0x01) == 0)
                    {
                        byte[] buffer = new byte[memInfo.RegionSize];
                        int bytesRead;

                        // Read the whole block into a buffer
                        if (ReadProcessMemory(_handle, memInfo.BaseAddress, buffer, (int)memInfo.RegionSize, out bytesRead))
                        {
                            // Checking for the pattern in the buffer
                            for (int i = 0; i < bytesRead - pattern.Length; i++)
                            {
                                bool match = true;
                                for (int j = 0; j < pattern.Length; j++)
                                {
                                    if (buffer[i + j] != pattern[j])
                                    {
                                        match = false;
                                        break;
                                    }
                                }

                                if (match)
                                {
                                    foundAddresses.Add(memInfo.BaseAddress + (uint)i);
                                }
                            }
                        }
                    }

                    // Go to the next memory block
                    currentAddr = memInfo.BaseAddress + memInfo.RegionSize;
                }

                return foundAddresses;
            });
        }

        // Saving
        public void WriteMemory(uint address, string text)
        {
            byte[] bytes = System.Text.Encoding.ASCII.GetBytes(text + "\0");
            int bytesWritten;
            WriteProcessMemory(_handle, address, bytes, bytes.Length, out bytesWritten);
        }
        // ADDITIONAL: Writing bytes to the process memory
        public void WriteBytes(uint address, byte[] bytes)
        {
            int bytesWritten;
            WriteProcessMemory(_handle, address, bytes, bytes.Length, out bytesWritten);
        }
    }
}