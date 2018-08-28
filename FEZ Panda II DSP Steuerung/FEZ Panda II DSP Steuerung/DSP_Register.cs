using System;
using Microsoft.SPOT;

namespace SigmaDSP.Register
{
    static class DSP_Register
    {
        // Input Gain
        public const UInt16 ADDR_GAIN1 = 5;
        public const UInt16 ADDR_GAIN2 = 6;

        // Volume
        public const UInt16 ADDR_GAIN_VOL = 103;            // DB Val
        // nicht nötig wenn nicht als SaveLoad gesendet
        //public const UInt16 ADDR_GAIN_VOL_Step = 104;
        //public const byte[] VOL_Step = new byte[] { 0x00, 0x00, 0x08, 0x00 };    // = 0.000244140625

        // MUTE
        public const UInt16 ADDR_MUTE_Speaker = 107;        // VAL 0 ooder 1 => 4bytes: 0x00 0x80 0x00 0x00 => 0x80 = 128 für 1
        public const UInt16 ADDR_MUTE_Line = 108;
        
        // EQ 0 - 9
        public const UInt16 EQ1940DualS10B1 = 11;
        public const UInt16 EQ1940DualS10B2 = 16;
        public const UInt16 EQ1940DualS10B3 = 21;
        public const UInt16 EQ1940DualS10B4 = 26;
        public const UInt16 EQ1940DualS10B5 = 31;
        public const UInt16 EQ1940DualS10B6 = 36;
        public const UInt16 EQ1940DualS10B7 = 41;
        public const UInt16 EQ1940DualS10B8 = 46;
        public const UInt16 EQ1940DualS10B9 = 51;
        public const UInt16 EQ1940DualS10B10 = 56;

        // Safeload
        public const UInt16 SAFELOAD_ADDR = 2069;   // 0 bis 4
        public const UInt16 SAFELOAD_DATA = 2064;   // 0 bis 4

        // DSP core control 
        public const UInt16 DSPcoreControl = 2076; 
    }

    //static class DSP_Level
    //{
    //    // Level Meter Input L
    //    public const byte[] askLM_L1 = new byte[] { 0x08, 0x1a, 0x00, 0x7e};
    //    public const byte[] askLM_L2 = new byte[] { 0x08, 0x1a };

    //    // Level Meter Input R
    //    public const byte[] askLM_L1 = new byte[] { 0x08, 0x1a, 0x00, 0x96 };
    //    public const byte[] askLM_L2 = new byte[] { 0x08, 0x1a };

    //}

}
