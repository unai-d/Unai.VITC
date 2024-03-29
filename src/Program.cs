﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace Unai.VITC
{
	public static class Program
	{
		static VITCLine vitc = new VITCLine();

		// Mode of operation.
		static OperationMode operationMode = OperationMode.Generator;
		static PixelFormat pixelFormat = PixelFormat.Grayscale8;
		static int frameWidth = 90, frameHeight = 2;
		static int? totalFrames = null;

		// Basic I/O.
		static IFramebuffer framebuffer = null;
		static string inputFilePath = null, outputFilePath = null;
		static Stream inputStream = null, outputStream = null;

		public static Dictionary<long, KeyValuePair<EventType, string>> events =
			new Dictionary<long, KeyValuePair<EventType, string>>();

		[STAThread]
		static void Main(string[] args)
		{
			// Read command line arguments.
			ParseArguments(args);

			// Get I/O streams based on the provided file paths.
			if (!string.IsNullOrEmpty(inputFilePath)) // Input file path specified.
			{
				operationMode = OperationMode.Embedder;
				if (inputFilePath == "-") // Standard input.
				{
					inputStream = Console.OpenStandardInput();
				}
				else // Regular file.
				{
					inputStream = File.OpenRead(inputFilePath);
				}
			}
			if (!string.IsNullOrEmpty(outputFilePath)) // Output file path specified.
			{
				if (outputFilePath != "-") // Regular file.
				{
					outputStream = File.OpenWrite(outputFilePath);
				}
			}
			else // Use standard output as default output stream.
			{
				outputStream = Console.OpenStandardOutput();
			}

			// Create the frame format descriptor structure.
			FrameFormat frameFormat = new FrameFormat(frameWidth, frameHeight, pixelFormat);

			// Set the framebuffer type accordingly.
			framebuffer = pixelFormat switch
			{
				PixelFormat.Grayscale8 => new Grayscale8Framebuffer(),
				PixelFormat.R8G8B8 => new R8G8B8Framebuffer(),
				_ => throw new NotImplementedException("Pixel format not implemented yet.")
			};

			// Initialise the framebuffer.
			framebuffer.New(frameFormat.Width, frameFormat.Height);

			// Debug output.
			Console.Error.WriteLine($"[Unai.VITC] Mode is `{operationMode}` {frameWidth}×{frameHeight} `{pixelFormat}`.");
			Console.Error.WriteLine($"[Unai.VITC] Input is '{inputFilePath ?? "<null>"}'.");
			Console.Error.WriteLine($"[Unai.VITC] Output is '{outputFilePath ?? "-"}'.");
			Console.Error.WriteLine($"[Unai.VITC] {vitc.FramesPerSecond}FPS ({vitc.FrameRateType}) Drop={vitc.DropFrameMode} Inter={vitc.Interlaced}");
			Console.Error.WriteLine($"[Unai.VITC] Starting from {vitc}");

			// Main loop.
			while (totalFrames.HasValue ? vitc.CurrentFrame < totalFrames.Value : true)
			{
				// Check for events meant to be executed at the current frame.
				if (events.ContainsKey(vitc.CurrentFrame))
				{
					switch (events[vitc.CurrentFrame].Key)
					{
						default:
							Console.Error.WriteLine($"Unrecognised event type: `{events[vitc.CurrentFrame].Key}`.");
							break;

						case EventType.UserBits:
							string str = events[vitc.CurrentFrame].Value[..4];
							var strbyte = Encoding.ASCII.GetBytes(str);
							vitc.userBits = new BitArray(strbyte);
							break;

						case EventType.Timecode:
							var tc = events[vitc.CurrentFrame].Value.Split(':');
							vitc.Hour = Convert.ToInt32(tc[0]);
							vitc.Minute = Convert.ToInt32(tc[1]);
							vitc.Second = Convert.ToInt32(tc[2]);
							vitc.Frame = Convert.ToInt32(tc[3]);
							break;

						case EventType.UserBitsClear:
							vitc.userBits.SetAll(false);
							break;
					}
				}

				// Set VITC line render properties.
				int vitcBitWidth = frameFormat.Width / 90;
				int vitcBitHeight = 1;

				int leftMargin = (frameFormat.Width - (90 * vitcBitWidth)) / 2;
				int rightMargin = leftMargin;

				int topMargin = operationMode == OperationMode.Embedder ? 1 : Math.Max(0, (frameFormat.Height / 2) - vitcBitHeight);
				int bottomMargin = topMargin;

				// Copy the incoming frame data to the output framebuffer (if applicable).
				if (inputStream != null)
				{
					int bytesRead = 0;
					while (bytesRead < framebuffer.Buffer.Length)
					{
						bytesRead += inputStream.Read(framebuffer.Buffer, bytesRead, framebuffer.Buffer.Length - bytesRead);
					}

					framebuffer.DrawRectangle(0, 0, frameFormat.Width, topMargin + bottomMargin + (vitcBitHeight * 2), 0f);
				}

				// Render the VITC line into the framebuffer.
				vitc.Generate();
				for (int bi = 0; bi < vitc.Result.Count; bi++)
				{
					framebuffer.DrawRectangle(
						leftMargin + (bi * vitcBitWidth),
						topMargin,
						vitcBitWidth,
						vitcBitHeight,
						vitc.Result[bi] ? 1f : 0f
						);
				}

				vitc.SwitchFieldType();
				
				vitc.Generate();
				for (int bi = 0; bi < vitc.Result.Count; bi++)
				{
					framebuffer.DrawRectangle(
						leftMargin + (bi * vitcBitWidth),
						topMargin + vitcBitHeight,
						vitcBitWidth,
						vitcBitHeight,
						vitc.Result[bi] ? 1f : 0f
						);
				}
				
				// Output the framebuffer contents to the output stream.
				outputStream.Write(framebuffer.Buffer, 0, framebuffer.Buffer.Length);

				vitc.StepOneFrame();
			}
		}

		static void ParseArguments(string[] args)
		{
			for (int i = 0; i < args.Length; i++)
			{
				string arg = args[i];
				switch (arg)
				{
					default:
						Console.Error.WriteLine("Unrecognized argument: " + arg);
						break;

					case "-o":
					case "--output":
						outputFilePath = args[i + 1];
						i++;
						break;

					case "-i":
					case "--input":
						inputFilePath = args[i + 1];
						i++;
						break;

					case "-f":
					case "--pixel-format":
						try
						{
							pixelFormat = (PixelFormat)Enum.Parse(typeof(PixelFormat), args[i + 1], true);
						}
						catch { Console.Error.WriteLine($"Unrecognised pixel format: `{args[i + 1]}`."); }
						i++;
						break;

					case "-s":
					case "--size":
						try
						{
							string[] sizeStr = args[i + 1].Split('x');
							frameWidth = Convert.ToInt32(sizeStr[0]);
							frameHeight = Convert.ToInt32(sizeStr[1]);
						}
						catch { Console.Error.WriteLine("Cannot parse frame size argument."); }
						i++;
						break;

					case "-fps":
					case "--framerate":
					case "--frames-per-second":
						try
						{
							vitc.FramesPerSecond = Convert.ToInt32(args[i + 1]);
						}
						catch { Console.Error.WriteLine("Unable to parse FPS argument!"); }
						i++;
						break;

					case "-tc":
					case "--timecode":
						try
						{
							var tc = args[i + 1].Split(':');
							vitc.Hour = Convert.ToInt32(tc[0]);
							vitc.Minute = Convert.ToInt32(tc[1]);
							vitc.Second = Convert.ToInt32(tc[2]);
							vitc.Frame = Convert.ToInt32(tc[3]);
						}
						catch { Console.Error.WriteLine("Unable to parse initial timecode (TC) argument!"); }
						i++;
						break;

					case "-t":
					case "--length":
						try
						{
							var tc = args[i + 1].Split(':');
							int hour = Convert.ToInt32(tc[0]);
							int min = Convert.ToInt32(tc[1]);
							int sec = Convert.ToInt32(tc[2]);
							int frame = Convert.ToInt32(tc[3]);
							totalFrames = frame + (sec * vitc.FramesPerSecond) + (min * vitc.FramesPerSecond * 60) + (hour * vitc.FramesPerSecond * 3600);
						}
						catch { Console.Error.WriteLine("Unable to parse duration (T) argument!"); }
						i++;
						break;

					case "-ev": // -ev HH:MM:SS:FF [UserBits|Timecode|UserBitsClear]="Test"
					case "--event":
						try
						{
							// Parse event time.
							var tc = args[i + 1].Split(':');
							int hour = Convert.ToInt32(tc[0]);
							int min = Convert.ToInt32(tc[1]);
							int sec = Convert.ToInt32(tc[2]);
							int frame = Convert.ToInt32(tc[3]);
							long timecodeAbsoluteFrame = frame + (sec * vitc.FramesPerSecond) + (min * vitc.FramesPerSecond * 60) + (hour * vitc.FramesPerSecond * 3600);

							// Parse event type and its arguments (if any).
							var evparams = args[i + 2].Split('=');
							var eventType = (EventType)Enum.Parse(typeof(EventType), evparams[0], true);
							string data = evparams.Length > 1 ? evparams[1] : null;
							events[timecodeAbsoluteFrame] = new KeyValuePair<EventType, string>(eventType, data);
						}
						catch { Console.Error.WriteLine("Unable to parse event (EV) argument!"); }
						i += 3;
						break;

					case "-I":
					case "--interlaced":
						vitc.Interlaced = true;
						break;

					case "-d":
					case "--drop-frames":
						vitc.DropFrameMode = true;
						break;
				}
			}
		}
	}
}
