namespace Unai.VITC
{
	public struct FrameFormat
	{
		public int Width { get; set; }
		public int Height { get; set; }
		public PixelFormat PixelFormat { get; set; }

		public FrameFormat(int width, int height, PixelFormat pixelFormat)
		{
			Width = width;
			Height = height;
			PixelFormat = pixelFormat;
		}
	}
}