namespace Unai.VITC
{
	public enum OperationMode
	{
		Generator = 0,
		Embedder = 1
	}

	public enum PixelFormat
	{
		Null,
		Binary,
		BinaryInverted,
		Grayscale8,
		Grayscale16,
		R8G8B8,
		R16G16B16,
		YUV444P8,
		YUV444P16
	}

	public static class Utils
	{
		public static int GetPixelBitDepth(this PixelFormat pixelFormat)
		{
			return pixelFormat switch
			{
				PixelFormat.Binary => 1,
				PixelFormat.BinaryInverted => 1,
				PixelFormat.Grayscale8 => 8,
				PixelFormat.Grayscale16 => 16,
				PixelFormat.R8G8B8 => 24,
				PixelFormat.R16G16B16 => 48,
				PixelFormat.YUV444P8 => 24,
				PixelFormat.YUV444P16 => 16,
				_ => -1,
			};
		}

		public static int GetPixelSize(this PixelFormat pixelFormat)
		{
			return pixelFormat.GetPixelBitDepth() / 8;
		}
	}
}