using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace NULL_EXTERNAL
{
    internal static class Aimbot
    {
        private static AimbotMem AimbotMem = new AimbotMem();

        private static string AobScan = "00 00 00 00 00 FF FF FF FF FF FF FF FF FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? ?? 00 00 00 00 00 00 00 00 00 00 00 00 00 00 A5 43";
        private static string headoffset = "0x95";
        private static string chestoffset = "0x91";

        private static Dictionary<long, int> OriginalValues1 = new Dictionary<long, int>();
        private static Dictionary<long, int> OriginalValues2 = new Dictionary<long, int>();
        private static Dictionary<long, int> OriginalValues3 = new Dictionary<long, int>();
        private static Dictionary<long, int> OriginalValues4 = new Dictionary<long, int>();


        public static async Task AimbotEnable()
        {
            long readOffset = Convert.ToInt64(headoffset, 16);
            long writeOffset = Convert.ToInt64(chestoffset, 16);

            var processes = Process.GetProcessesByName("HD-Player");
            if (processes.Length == 0)
                throw new Exception("HD-Player process not found");

            int proc = processes[0].Id;
            AimbotMem.OpenProcess(proc);

            var result = await AimbotMem.AoBScan2(AobScan, true, true);
            if (!result.Any())
            {
                Console.Beep(400, 500);
                return;
            }

            foreach (var CurrentAddress in result)
            {
                long addressToSave = CurrentAddress + writeOffset;
                var CurrentBytes = AimbotMem.ReadByGaurav(addressToSave.ToString("X"), sizeof(int));
                int CurrentValue = BitConverter.ToInt32(CurrentBytes, 0);
                OriginalValues1[addressToSave] = CurrentValue;

                long addressToSave9 = CurrentAddress + readOffset;
                var CurrentBytes9 = AimbotMem.ReadByGaurav(addressToSave9.ToString("X"), sizeof(int));
                int CurrentValue9 = BitConverter.ToInt32(CurrentBytes9, 0);
                OriginalValues2[addressToSave9] = CurrentValue9;

                long headbytes = CurrentAddress + readOffset;
                long chestbytes = CurrentAddress + writeOffset;
                var bytes = AimbotMem.ReadByGaurav(headbytes.ToString("X"), sizeof(int));
                int headValue = BitConverter.ToInt32(bytes, 0);
                var bytes2 = AimbotMem.ReadByGaurav(chestbytes.ToString("X"), sizeof(int));
                int chestValue = BitConverter.ToInt32(bytes2, 0);

                AimbotMem.WriteMemory(chestbytes.ToString("X"), "int", headValue.ToString());
                AimbotMem.WriteMemory(headbytes.ToString("X"), "int", chestValue.ToString());

                long addressToSave1 = CurrentAddress + writeOffset;
                var CurrentBytes1 = AimbotMem.ReadByGaurav(addressToSave1.ToString("X"), sizeof(int));
                int CurrentValue1 = BitConverter.ToInt32(CurrentBytes1, 0);
                OriginalValues3[addressToSave1] = CurrentValue1;

                long addressToSave19 = CurrentAddress + readOffset;
                var CurrentBytes19 = AimbotMem.ReadByGaurav(addressToSave19.ToString("X"), sizeof(int));
                int CurrentValue19 = BitConverter.ToInt32(CurrentBytes19, 0);
                OriginalValues2[addressToSave19] = CurrentValue19;
            }
        }

        public static void Aimbotoff()
        {
            foreach (var entry in OriginalValues1)
                AimbotMem.WriteMemory(entry.Key.ToString("x"), "int", entry.Value.ToString());
            foreach (var entry in OriginalValues2)
                AimbotMem.WriteMemory(entry.Key.ToString("x"), "int", entry.Value.ToString());
        }

        public static void Aimboton()
        {
            foreach (var entry in OriginalValues3)
                AimbotMem.WriteMemory(entry.Key.ToString("x"), "int", entry.Value.ToString());
            foreach (var entry in OriginalValues4)
                AimbotMem.WriteMemory(entry.Key.ToString("x"), "int", entry.Value.ToString());
        }

        public static void Disable()
        {
            Aimbotoff();
        }
    }
}