using System.Collections;

namespace Unai.VITC
{
	public class VITCLine
	{
		private BitArray ba = new BitArray(90);
		public BitArray userBits = new BitArray(32);
		private int frame = 0, second = 0, minute = 0, hour = 0;
		private int fps = 25;
		private long currentFrame = 0;
		private bool secondField = false;

		public bool b14 = false;
		public bool b15 = false;
		public bool b35 = false;
		public bool b55 = false;
		public bool b74 = false;
		public bool b75 = false;

		// Public properties //

		/// <summary>
		/// <see cref="BitArray"/> that contains the full VITC line.
		/// </summary>
		public BitArray Result => ba;

		public int Frame
		{
			get
			{
				return frame;
			}
			set
			{
				frame = value;
				UpdateFrameCount();
			}
		}
		public int Second
		{
			get
			{
				return second;
			}
			set
			{
				second = value;
				UpdateFrameCount();
			}
		}
		public int Minute
		{
			get
			{
				return minute;
			}
			set
			{
				minute = value;
				UpdateFrameCount();
			}
		}
		public int Hour
		{
			get
			{
				return hour;
			}
			set
			{
				hour = value;
				UpdateFrameCount();
			}
		}

		/// <summary>
		/// Gets or sets the frames per second.<br />
		/// Values higher than 30 breaks the VITC specification, and values higher than 39 cannot be represented correctly.
		/// </summary>
		public int FramesPerSecond
		{
			get
			{
				return fps;
			}
			set
			{
				currentFrame = (long)((currentFrame / (double)fps) * value);
				fps = value;
				UpdateTimeData();
			}
		}

		public long CurrentFrame
		{
			get
			{
				return currentFrame;
			}
			set
			{
				currentFrame = value;
				UpdateTimeData();
			}
		}

		/// <summary>
		/// Indicates if the VITC represents a second/even field.
		/// </summary>
		public bool IsSecondField => secondField;

		/// <summary>
		/// Bit 14: Frame drop bit. Used in NTSC/film video signals to indicate frame-drop timecode mode.
		/// </summary>
		public bool FrameDropBit => b14;
		/// <summary>
		/// Bit 15: Colour framing bit. Used to indicate if the timecode is synchronised to the color component of an external video signal.
		/// </summary>
		public bool ColourFramingBit => b15;
		/// <summary>
		/// Bit 35: NTSC field bit. Used to indicate a NTSC (29.97/30 fps) second field.
		/// </summary>
		public bool NtscSecondFieldBit => b35;
		/// <summary>
		/// Bit 55: User bits format bit.
		/// </summary>
		public bool UserBitsFormatBit => b55;
		/// <summary>
		/// Bit 74: BGF1 bit. Used to indicate if the timecode is synchronised to an external clock.
		/// </summary>
		public bool ExternalClockBit => b74;
		/// <summary>
		/// Bit 75: PAL field bit. Used to indicate PAL/SECAM (25 fps) second field.
		/// </summary>
		public bool PalSecondFieldBit => b75;

		/// <summary>
		/// Gets the framerate type.
		/// </summary>
		public FrameRateType FrameRateType => (FrameRateType)fps;
		/// <summary>
		/// Gets or sets the drop frame mode. This is used to compensate the timecode for non-integer framerates like NTSC (29.97).
		/// </summary>
		public bool DropFrameMode { get; set; } = false;
		/// <summary>
		/// Gets or sets interlaced video mode. This enables the second field bits when rendering the line on the second field.
		/// </summary>
		public bool Interlaced { get; set; } = false;

		#region Methods
		private void UpdateTimeData()
		{
			frame = (int)(currentFrame % FramesPerSecond);
			second = (int)(currentFrame / FramesPerSecond) % 60;
			minute = (int)(currentFrame / FramesPerSecond / 60) % 60;
			hour = (int)(currentFrame / FramesPerSecond / 3600) % 24;
		}

		private void UpdateFrameCount()
		{
			currentFrame = frame
				+ (second * fps)
				+ (minute * (fps * 60))
				+ (hour * (fps * 3600));
		}

		public override string ToString()
		{
			return $"{Hour:D2}:{Minute:D2}:{Second:D2}{(DropFrameMode ? ';' : ':')}{Frame:D2}";
		}

		/// <summary>
		/// Generates the VITC line based on the data contained (including the timestamp, the user data bits…).<br />
		/// The result is stored on the `Result` property as a <see cref="BitArray" />.
		/// </summary>
		public void Generate()
		{
			// Process flags.
			b75 = secondField && FrameRateType == FrameRateType.PAL;
			b35 = secondField && FrameRateType == FrameRateType.NTSC;
			b14 = DropFrameMode && FrameRateType == FrameRateType.NTSC;

			// Set unit and decimal values.
			int frameU = frame % 10;
			int frameD = (frame - frameU) / 10;
			int secU = second % 10;
			int secD = (second - secU) / 10;
			int minU = minute % 10;
			int minD = (minute - minU) / 10;
			int hourU = hour % 10;
			int hourD = (hour - hourU) / 10;

			// Clear all bits.
			ba.SetAll(false);

			// Set synchronisation bits.
			ba.Set(0, true); ba.Set(10, true); ba.Set(20, true); ba.Set(30, true); ba.Set(40, true);
			ba.Set(50, true); ba.Set(60, true); ba.Set(70, true); ba.Set(80, true);

			// Set frame number bits (2-5 and 12-13, including flags 14 and 15).
			ba.Set(2, (frameU & 1) != 0);
			ba.Set(3, (frameU & 2) != 0);
			ba.Set(4, (frameU & 4) != 0);
			ba.Set(5, (frameU & 8) != 0);

			ba.Set(12, (frameD & 1) != 0);
			ba.Set(13, (frameD & 2) != 0);
			ba.Set(14, b14);
			ba.Set(15, b15);

			// Set second number bits (22-25 and 32-34, including flag 35).
			ba.Set(22, (secU & 1) != 0);
			ba.Set(23, (secU & 2) != 0);
			ba.Set(24, (secU & 4) != 0);
			ba.Set(25, (secU & 8) != 0);

			ba.Set(32, (secD & 1) != 0);
			ba.Set(33, (secD & 2) != 0);
			ba.Set(34, (secD & 4) != 0);
			ba.Set(35, b35);

			// Set minute number bits (42-45 and 52-54, including flag 55).
			ba.Set(42, (minU & 1) != 0);
			ba.Set(43, (minU & 2) != 0);
			ba.Set(44, (minU & 4) != 0);
			ba.Set(45, (minU & 8) != 0);

			ba.Set(52, (minD & 1) != 0);
			ba.Set(53, (minD & 2) != 0);
			ba.Set(54, (minD & 4) != 0);
			ba.Set(55, b55);

			// Set hour number bits (62-65 and 72-73, including flags 74 and 75).
			ba.Set(62, (hourU & 1) != 0);
			ba.Set(63, (hourU & 2) != 0);
			ba.Set(64, (hourU & 4) != 0);
			ba.Set(65, (hourU & 8) != 0);

			ba.Set(72, (hourD & 1) != 0);
			ba.Set(73, (hourD & 2) != 0);
			ba.Set(74, b74);
			ba.Set(75, b75);

			// Set user bits (4 bytes).
			ba.Set(6, userBits.Get(0)); ba.Set(7, userBits.Get(1)); ba.Set(8, userBits.Get(2)); ba.Set(9, userBits.Get(3));
			ba.Set(16, userBits.Get(4)); ba.Set(17, userBits.Get(5)); ba.Set(18, userBits.Get(6)); ba.Set(19, userBits.Get(7));
			ba.Set(26, userBits.Get(8)); ba.Set(27, userBits.Get(9)); ba.Set(28, userBits.Get(10)); ba.Set(29, userBits.Get(11));
			ba.Set(36, userBits.Get(12)); ba.Set(37, userBits.Get(13)); ba.Set(38, userBits.Get(14)); ba.Set(39, userBits.Get(15));
			ba.Set(46, userBits.Get(16)); ba.Set(47, userBits.Get(17)); ba.Set(48, userBits.Get(18)); ba.Set(49, userBits.Get(19));
			ba.Set(56, userBits.Get(20)); ba.Set(57, userBits.Get(21)); ba.Set(58, userBits.Get(22)); ba.Set(59, userBits.Get(23));
			ba.Set(66, userBits.Get(24)); ba.Set(67, userBits.Get(25)); ba.Set(68, userBits.Get(26)); ba.Set(69, userBits.Get(27));
			ba.Set(76, userBits.Get(28)); ba.Set(77, userBits.Get(29)); ba.Set(78, userBits.Get(30)); ba.Set(79, userBits.Get(31));

			SetChecksum();
		}

		public void SetChecksum()
		{
			// Set checksum (bits 82-89).
			ba.Set(82, ba.Get(74) ^ ba.Get(66) ^ ba.Get(58) ^ ba.Get(50) ^ ba.Get(42) ^ ba.Get(34) ^ ba.Get(26) ^ ba.Get(18) ^ ba.Get(10) ^ ba.Get(2));
			ba.Set(83, ba.Get(75) ^ ba.Get(67) ^ ba.Get(59) ^ ba.Get(51) ^ ba.Get(43) ^ ba.Get(35) ^ ba.Get(27) ^ ba.Get(19) ^ ba.Get(11) ^ ba.Get(3));
			ba.Set(84, ba.Get(76) ^ ba.Get(68) ^ ba.Get(60) ^ ba.Get(52) ^ ba.Get(44) ^ ba.Get(36) ^ ba.Get(28) ^ ba.Get(20) ^ ba.Get(12) ^ ba.Get(4));
			ba.Set(85, ba.Get(77) ^ ba.Get(69) ^ ba.Get(61) ^ ba.Get(53) ^ ba.Get(45) ^ ba.Get(37) ^ ba.Get(29) ^ ba.Get(21) ^ ba.Get(13) ^ ba.Get(5));

			ba.Set(86, ba.Get(78) ^ ba.Get(70) ^ ba.Get(62) ^ ba.Get(54) ^ ba.Get(46) ^ ba.Get(38) ^ ba.Get(30) ^ ba.Get(22) ^ ba.Get(14) ^ ba.Get(6));
			ba.Set(87, ba.Get(79) ^ ba.Get(71) ^ ba.Get(63) ^ ba.Get(55) ^ ba.Get(47) ^ ba.Get(39) ^ ba.Get(31) ^ ba.Get(23) ^ ba.Get(15) ^ ba.Get(7));
			ba.Set(88, ba.Get(80) ^ ba.Get(72) ^ ba.Get(64) ^ ba.Get(56) ^ ba.Get(48) ^ ba.Get(40) ^ ba.Get(32) ^ ba.Get(24) ^ ba.Get(16) ^ ba.Get(8) ^ ba.Get(0));
			ba.Set(89, ba.Get(81) ^ ba.Get(73) ^ ba.Get(65) ^ ba.Get(57) ^ ba.Get(49) ^ ba.Get(41) ^ ba.Get(33) ^ ba.Get(25) ^ ba.Get(17) ^ ba.Get(9) ^ ba.Get(1));
		}

		/// <summary>
		/// Increments one frame on the timestamp.
		/// </summary>
		public void StepOneFrame()
		{
			frame++;
			if (frame >= fps) { frame = 0; second++; }
			if (second > 59) { second = 0; minute++; }
			if (minute > 59) { minute = 0; hour++; }
			if (hour > 23) { hour = 0; }
			UpdateFrameCount();

			// SMPTE drop-frame timecode.
			if (DropFrameMode && FrameRateType == FrameRateType.NTSC)
			{
				if (Minute % 10 != 0 && Second == 0 && Frame == 0)
				{
					Frame = 2;
				}
			}

			secondField = false;
		}

		/// <summary>
		/// Switches the field type: from first/odd field to second/even field and viceversa.
		/// </summary>
		public void SwitchFieldType()
		{
			if (Interlaced)
			{
				secondField = !secondField;
			}
		}
		#endregion
	}
}