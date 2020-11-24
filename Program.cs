using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace Unai.VITC
{
    public static class Program
    {
        public static Stream output;

        public static VITCLine vitc;

        public enum EventType { UserBits, Timecode, Flags, FPS, UserBitsClear }
        public static Dictionary<long, KeyValuePair<EventType, string>> events =
            new Dictionary<long, KeyValuePair<EventType, string>>();

        
        public static void PrintOutput()
        {
            for (int bi = 0; bi < vitc.ba.Count; bi++)
            {
                if (vitc.ba[bi])
                    output.WriteByte(0xff);
                else
                    output.WriteByte(0x00);
            }
        }

        [STAThread]
        static void Main(string[] args)
        {
            // initialisation
            output = Console.OpenStandardOutput();
            vitc = new VITCLine();

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
                            vitc.fps = Convert.ToInt32(args[i + 1]);
                            vitc.frameRateType = (VITCLine.FrameRateType)Convert.ToInt32(args[i + 1]);
                        }
                        catch { Console.Error.WriteLine("Unable to parse FPS argument!"); }
                        i++;
                        break;
                    case "tc":
                        try
                        {
                            var tc = args[i + 1].Split(':');
                            vitc.hour = Convert.ToInt32(tc[0]);
                            vitc.min = Convert.ToInt32(tc[1]);
                            vitc.sec = Convert.ToInt32(tc[2]);
                            vitc.frame = Convert.ToInt32(tc[3]);
                        }
                        catch { Console.Error.WriteLine("Unable to parse initial timecode (TC) argument!"); }
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
                            vitc.totalFrames = frame + (sec * vitc.fps) + (min * vitc.fps * 60) + (hour * vitc.fps * 3600);
                        }
                        catch { Console.Error.WriteLine("Unable to parse duration (T) argument!"); }
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
                            long tcf = frame + (sec * vitc.fps) + (min * vitc.fps * 60) + (hour * vitc.fps * 3600);
                            events[tcf] = new KeyValuePair<EventType, string>((EventType)Enum.Parse(typeof(EventType), args[i + 2], true), args[i + 3].Trim('"'));
                        }
                        catch { Console.Error.WriteLine("Unable to parse event (EV) argument!"); }
                        i += 3;
                        break;
                    case "inter":
                        vitc.interlaced = true;
                        i++;
                        break;
                }
            }

            //render vitc lines to output.
            while (vitc.currentFrame <= vitc.totalFrames)
            {
                if (events.ContainsKey(vitc.currentFrame))
                {
                    switch (events[vitc.currentFrame].Key)
                    {
                        case EventType.UserBits:
                            string str = events[vitc.currentFrame].Value.Substring(0, 4);
                            var strbyte = Encoding.ASCII.GetBytes(str);
                            vitc.ub = new BitArray(strbyte);
                            break;
                        case EventType.Timecode:
                            var tc = events[vitc.currentFrame].Value.Split(':');
                            vitc.hour = Convert.ToInt32(tc[0]);
                            vitc.min = Convert.ToInt32(tc[1]);
                            vitc.sec = Convert.ToInt32(tc[2]);
                            vitc.frame = Convert.ToInt32(tc[3]);
                            break;
                        case EventType.UserBitsClear:
                            vitc.ub.SetAll(false);
                            break;
                    }
                }

                vitc.Generate();
                PrintOutput();
                if (vitc.interlaced)
                {
                    vitc.ba.Set(vitc.fps == 25 ? 75 : 35, true);
                    PrintOutput();
                }

                vitc.StepOneFrame();
            }
        }
    }
}
