using System;

namespace Unai.VITC
{
	public class Grayscale8Framebuffer : IFramebuffer
	{
		byte[] buffer;
		int width = -1, height = -1;

		byte[] IFramebuffer.Buffer => buffer;
		int IFramebuffer.Width => width;
		int IFramebuffer.Height => height;
		PixelFormat IFramebuffer.PixelFormat => PixelFormat.Grayscale8;

		public void New(int width = -1, int height = -1)
		{
			if (width != -1 && height != -1)
			{
				this.width = width;
				this.height = height;
			}
			buffer = new byte[width * height * PixelFormat.Grayscale8.GetPixelSize()];
		}

		public void SetPixelValue(int x, int y, float v)
		{
			buffer[x + y * width] = (byte)Math.Clamp(v * 256, 0, 255);
		}
	}
}