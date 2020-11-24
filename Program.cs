using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Unai.VITC
{
    public static class Program
    {
        /// <summary>
        /// <see cref="BitArray"/> that contains the full VITC line.
        /// </summary>
        public static BitArray ba = new BitArray(90);
        /// <summary>
        /// <see cref="BitArray"/> that contains only the user bits.
        /// </summary>
        public static BitArray ub = new BitArray(32);
        public static int frame = 0;
        public static int sec = 0;
        public static int min = 0;
        public static int hour = 0;

        public static int fps = 25;
        public static long currentFrame = 0;
        public static long totalFrames = 2500;

        /// <summary>
        /// Bit 14: Frame drop bit.
        /// </summary>
        public static bool b14 = false;
        /// <summary>
        /// Bit 15: Chroma bit.
        /// </summary>
        public static bool b15 = false;
        /// <summary>
        /// Bit 35: NTSC bit.
        /// </summary>
        public static bool b35 = false;
        /// <summary>
        /// Bit 55: BF bit.
        /// </summary>
        public static bool b55 = false;
        /// <summary>
        /// Bit 74: BGF1 bit.
        /// </summary>
        public static bool b74 = false;
        /// <summary>
        /// Bit 75: PAL bit.
        /// </summary>
        public static bool b75 = false;

        public enum EventType { UserBits, Timecode, Flags, FPS, UserBitsClear }
        public static Dictionary<long, KeyValuePair<EventType, string>> events = new Dictionary<long, KeyValuePair<EventType, string>>();

        public static void GenerateBitArray()
        {
            int framesu = frame % 10;
            int secu = sec % 10;
            int secd = (sec - secu) / 10;
            int minu = min % 10;
            int mind = (min - minu) / 10;
            int houru = hour % 10;
            int hourd = (hour - houru) / 10;

            // new bitarray
            ba.SetAll(false);

            // init sync bits
            ba.Set(0, true); ba.Set(10, true); ba.Set(20, true); ba.Set(30, true); ba.Set(40, true);
            ba.Set(50, true); ba.Set(60, true); ba.Set(70, true); ba.Set(80, true);

            // set frame number bits (2-13 including flags 14 and 15)
            ba.Set(2, framesu % 2 == 1);
            ba.Set(3, framesu == 2 || framesu == 3 || framesu == 6 || framesu == 7);
            ba.Set(4, framesu >= 4 && framesu < 8);
            ba.Set(5, framesu >= 8);
            ba.Set(12, frame - framesu == 10);
            ba.Set(13, frame - framesu == 20);
            ba.Set(14, b14);
            ba.Set(15, b15);

            // set second number bits (22-34 including flag 35)
            ba.Set(22, secu % 2 == 1);
            ba.Set(23, secu == 2 || secu == 3 || secu == 6 || secu == 7);
            ba.Set(24, secu >= 4 && secu < 8);
            ba.Set(25, secu >= 8);
            ba.Set(32, secd % 2 == 1);
            ba.Set(33, secd == 2 || secd == 3 || secd == 6 || secd == 7);
            ba.Set(34, secd >= 4);
            ba.Set(35, b35);

            // set minute number bits (42-54 including flag 55)
            ba.Set(42, minu % 2 == 1);
            ba.Set(43, minu == 2 || minu == 3 || minu == 6 || minu == 7);
            ba.Set(44, minu >= 4 && minu < 8);
            ba.Set(45, minu >= 8);
            ba.Set(52, mind % 2 == 1);
            ba.Set(53, mind == 2 || mind == 3 || mind == 6 || mind == 7);
            ba.Set(54, mind >= 4);
            ba.Set(55, b55);

            // set hour number bits (62-73 including flags 74 and 75)
            ba.Set(62, houru % 2 == 1);
            ba.Set(63, houru == 2 || houru == 3 || houru == 6 || houru == 7);
            ba.Set(64, houru >= 4 && houru < 8);
            ba.Set(65, houru >= 8);
            ba.Set(72, hourd == 1);
            ba.Set(73, hourd == 2);
            ba.Set(74, b74);
            ba.Set(75, b75);

            // set user bits
            ba.Set(6, ub.Get(0)); ba.Set(7, ub.Get(1)); ba.Set(8, ub.Get(2)); ba.Set(9, ub.Get(3));
            ba.Set(16, ub.Get(4)); ba.Set(17, ub.Get(5)); ba.Set(18, ub.Get(6)); ba.Set(19, ub.Get(7));
            ba.Set(26, ub.Get(8)); ba.Set(27, ub.Get(9)); ba.Set(28, ub.Get(10)); ba.Set(29, ub.Get(11));
            ba.Set(36, ub.Get(12)); ba.Set(37, ub.Get(13)); ba.Set(38, ub.Get(14)); ba.Set(39, ub.Get(15));
            ba.Set(46, ub.Get(16)); ba.Set(47, ub.Get(17)); ba.Set(48, ub.Get(18)); ba.Set(49, ub.Get(19));
            ba.Set(56, ub.Get(20)); ba.Set(57, ub.Get(21)); ba.Set(58, ub.Get(22)); ba.Set(59, ub.Get(23));
            ba.Set(66, ub.Get(24)); ba.Set(67, ub.Get(25)); ba.Set(68, ub.Get(26)); ba.Set(69, ub.Get(27));
            ba.Set(76, ub.Get(28)); ba.Set(77, ub.Get(29)); ba.Set(78, ub.Get(30)); ba.Set(79, ub.Get(31));

            // set checksum (bits 82-89)
            ba.Set(82, ba.Get(74) ^ ba.Get(66) ^ ba.Get(58) ^ ba.Get(50) ^ ba.Get(42) ^ ba.Get(34) ^ ba.Get(26) ^ ba.Get(18) ^ ba.Get(10) ^ ba.Get(2));
            ba.Set(83, ba.Get(75) ^ ba.Get(67) ^ ba.Get(59) ^ ba.Get(51) ^ ba.Get(43) ^ ba.Get(35) ^ ba.Get(27) ^ ba.Get(19) ^ ba.Get(11) ^ ba.Get(3));
            ba.Set(84, ba.Get(76) ^ ba.Get(68) ^ ba.Get(60) ^ ba.Get(52) ^ ba.Get(44) ^ ba.Get(36) ^ ba.Get(28) ^ ba.Get(20) ^ ba.Get(12) ^ ba.Get(4));
            ba.Set(85, ba.Get(77) ^ ba.Get(69) ^ ba.Get(61) ^ ba.Get(53) ^ ba.Get(45) ^ ba.Get(37) ^ ba.Get(29) ^ ba.Get(21) ^ ba.Get(13) ^ ba.Get(5));

            ba.Set(86, ba.Get(78) ^ ba.Get(70) ^ ba.Get(62) ^ ba.Get(54) ^ ba.Get(46) ^ ba.Get(38) ^ ba.Get(30) ^ ba.Get(22) ^ ba.Get(14) ^ ba.Get(6));
            ba.Set(87, ba.Get(79) ^ ba.Get(71) ^ ba.Get(63) ^ ba.Get(55) ^ ba.Get(47) ^ ba.Get(39) ^ ba.Get(31) ^ ba.Get(23) ^ ba.Get(15) ^ ba.Get(7));
            ba.Set(88, ba.Get(80) ^ ba.Get(72) ^ ba.Get(64) ^ ba.Get(56) ^ ba.Get(48) ^ ba.Get(40) ^ ba.Get(32) ^ ba.Get(24) ^ ba.Get(16) ^ ba.Get(8) ^ ba.Get(0));
            ba.Set(89, ba.Get(81) ^ ba.Get(73) ^ ba.Get(65) ^ ba.Get(57) ^ ba.Get(49) ^ ba.Get(41) ^ ba.Get(33) ^ ba.Get(25) ^ ba.Get(17) ^ ba.Get(9) ^ ba.Get(1));
        }

        [STAThread]
        static void Main(string[] args)
        {
            // read arguments
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i].Replace("/", "").Replace("-", "");
                switch (arg)
                {
                    default:
                        Console.Error.WriteLine("Unrecognized argument: " + arg);
                        break;
                    case "fps":
                        try
                        {
                            fps = Convert.ToInt32(args[i + 1]);
                        }
                        catch { Console.Error.WriteLine("Unable to parse FPS argument!"); }
                        i++;
                        break;
                    case "tc":
                        try
                        {
                            var tc = args[i + 1].Split(':');
                            hour = Convert.ToInt32(tc[0]);
                            min = Convert.ToInt32(tc[1]);
                            sec = Convert.ToInt32(tc[2]);
                            frame = Convert.ToInt32(tc[3]);
                        }
                        catch { Console.Error.WriteLine("Unable to parse TC argument!"); }
                        i++;
                        break;
                    case "t":
                        try
                        {
                            var tc = args[i + 1].Split(':');
                            int hour = Convert.ToInt32(tc[0]);
                            int min = Convert.ToInt32(tc[1]);
                            int sec = Convert.ToInt32(tc[2]);
                            int frame = Convert.ToInt32(tc[3]);
                            totalFrames = frame + (sec * fps) + (min * fps * 60) + (hour * fps * 3600);
                        }
                        catch { Console.Error.WriteLine("Unable to parse T argument!"); }
                        i++;
                        break;
                    case "ev":
                        try
                        {
                            var tc = args[i + 1].Split(':');
                            int hour = Convert.ToInt32(tc[0]);
                            int min = Convert.ToInt32(tc[1]);
                            int sec = Convert.ToInt32(tc[2]);
                            int frame = Convert.ToInt32(tc[3]);
                            long tcf = frame + (sec * fps) + (min * fps * 60) + (hour * fps * 3600);
                            events[tcf] = new KeyValuePair<EventType, string>((EventType)Enum.Parse(typeof(EventType), args[i + 2], true), args[i + 3].Trim('"'));
                        }
                        catch { throw; }
                        i += 3;
                        break;
                }
            }

            //render vitc lines to stdout.
            while (currentFrame <= totalFrames)
            {
                if (events.ContainsKey(currentFrame))
                {
                    switch (events[currentFrame].Key)
                    {
                        case EventType.UserBits:
                            string str = events[currentFrame].Value.Substring(0, 4);
                            var strbyte = Encoding.ASCII.GetBytes(str);
                            ub = new BitArray(strbyte);
                            break;
                        case EventType.Timecode:
                            var tc = events[currentFrame].Value.Split(':');
                            hour = Convert.ToInt32(tc[0]);
                            min = Convert.ToInt32(tc[1]);
                            sec = Convert.ToInt32(tc[2]);
                            frame = Convert.ToInt32(tc[3]);
                            break;
                        case EventType.UserBitsClear:
                            ub.SetAll(false);
                            break;
                    }
                }

                GenerateBitArray();

                for (int bi = 0; bi < ba.Count; bi++)
                {
                    if (ba[bi]) { Console.Write(Convert.ToChar(0xFF)); } else { Console.Write(Convert.ToChar(0)); }
                }

                frame++;
                if (frame >= fps) { frame = 0; sec++; }
                if (sec > 59) { sec = 0; min++; }
                if (min > 59) { min = 0; hour++; }
                if (hour > 23) { hour = 0; }
                currentFrame++;
            }

            //Thread.Sleep(100);
        }
    }
}
