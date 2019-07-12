using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModelReloaderDS1R
{
    class Program
    {
        static void ReloadPARTS()
        {
            if (!DS3Hook.Hook.DarkSouls3Handle.Attached)
            {
                if (!DS3Hook.Hook.DarkSouls3Handle.TryAttachToDarkSouls(out string errorMsg))
                {
                    Console.Error.WriteLine(errorMsg);
                    return;
                }
            }

            var thingAddr = DS3Hook.Hook.RInt32(0x141D151B0);
            DS3Hook.Hook.WInt32(thingAddr + 0x1F24, 0x3F800000);
            DS3Hook.Hook.WInt32(thingAddr + 0x1F28, 0x41200000);
            
        }



        static void ReloadCHR(string chrName)
        {
            if (!DS3Hook.Hook.DarkSouls3Handle.Attached)
            {
                if (!DS3Hook.Hook.DarkSouls3Handle.TryAttachToDarkSouls(out string errorMsg))
                {
                    Console.Error.WriteLine(errorMsg);
                    return;
                }
            }

            DS3Hook.Hook.WByte(0x141D151DB, 1);

            var stringAlloc = new DS3Hook.Injection.Structures.SafeRemoteHandle(chrName.Length * 2);

            DS3Hook.Hook.WBytes(stringAlloc.GetHandle(), Encoding.Unicode.GetBytes(chrName));

            var stringAllocAddrBytes = BitConverter.GetBytes(stringAlloc.GetHandle().ToInt64());

            DS3Hook.Hook.CallArrayOfBytes(new byte[]
            {
                0x48, 0xBA,
                stringAllocAddrBytes[0],
                stringAllocAddrBytes[1],
                stringAllocAddrBytes[2],
                stringAllocAddrBytes[3],
                stringAllocAddrBytes[4],
                stringAllocAddrBytes[5],
                stringAllocAddrBytes[6],
                stringAllocAddrBytes[7], 
                //mov rdx,YourAllocForString
                0x48, 0xA1, 0xB0, 0x51, 0xD1, 0x41, 0x01, 0x00, 0x00, 0x00,  //mov rax,[141D151B0]
                0x48, 0x8B, 0xC8, //mov rcx,rax
                0x49, 0xBE, 0xA0, 0x12, 0x37, 0x40, 0x01, 0x00, 0x00, 0x00, //mov r14,00000001403712A0
                0x48, 0x83, 0xEC, 0x28, //sub rsp,28
                0x41, 0xFF, 0xD6, //call r14
                0x48, 0x83, 0xC4, 0x28,  //add rsp,28
                0xC3 //Ret
            });
        }

        static void Main(string[] args)
        {
            //ReloadPARTS();

            if (args[0].StartsWith("c"))
            {
                ReloadCHR(args[0]);
            }
            else if (args[0].Contains("PARTS"))
            {
                ReloadPARTS();
            }
        }
    }
}
