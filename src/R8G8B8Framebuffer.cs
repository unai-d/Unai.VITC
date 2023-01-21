using System;

namespace Unai.VITC
{
	public class R8G8B8Framebuffer : IFramebuffer
	{
		byte[] buffer;
		int width = -1, height = -1;

		byte[] IFramebuffer.Buffer => buffer;
		int IFramebuffer.Width => width;
		int IFramebuffer.Height => height;
		PixelFormat IFramebuffer.PixelFormat => PixelFormat.R8G8B8;

		public void New(int width = -1, int height = -1)
		{
			if (width != -1 && height != -1)
			{
				this.width = width;
				this.height = height;
			}
			buffer = new byte[width * height * PixelFormat.R8G8B8.GetPixelSize()];
		}

		public void SetPixelValue(int x, int y, float v)
		{
			int pos = PixelFormat.R8G8B8.GetPixelSize() * (x + y * width);
			byte value = (byte)Math.Clamp(v * 256, 0, 255);
			buffer[pos] = value;
			buffer[pos + 1] = value;
			buffer[pos + 2] = value;
		}
	}
}