using System;

namespace Unai.VITC
{
	public interface IFramebuffer
	{
		public abstract byte[] Buffer { get; }
		public abstract int Width { get; }
		public abstract int Height { get; }
		public abstract PixelFormat PixelFormat { get; }

		public abstract void New(int width = -1, int height = -1);
		public abstract void SetPixelValue(int x, int y, float v);
		public virtual void DrawRectangle(int x, int y, int w, int h, float v)
		{
			x = Math.Clamp(x, 0, Width);
			y = Math.Clamp(y, 0, Height);
			w = Math.Clamp(w, 0, Width - x);
			h = Math.Clamp(h, 0, Height - y);

			//Console.Error.WriteLine($"{x},{y} + {w},{h} → {x+w},{y+h}");

			for (int iy = y; iy < y + h; iy++)
			{
				for (int ix = x; ix < x + w; ix++)
				{
					SetPixelValue(ix, iy, v);
				}
			}
		}
	}

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