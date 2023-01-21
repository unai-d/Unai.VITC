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
}