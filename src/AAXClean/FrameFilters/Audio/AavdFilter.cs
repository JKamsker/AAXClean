using Mpeg4Lib.Util;
using System;
using System.Threading.Tasks;

namespace AAXClean.FrameFilters.Audio
{
	internal class AavdFilter : AacValidateFilter
	{
		private readonly IAesCryptoTransform aesCryptoTransform;

		public AavdFilter(byte[] key, byte[] iv)
		{
			if (key is null || key.Length != 16)
				throw new ArgumentException($"{nameof(key)} must be 16 bytes long.");
			if (iv is null || iv.Length != 16)
				throw new ArgumentException($"{nameof(iv)} must be 16 bytes long.");

			// aesCryptoTransform = new AesCryptoTransform(key, iv);
			aesCryptoTransform = AesCryptoTransformFactory.Create(key, iv);
		}
		protected override async ValueTask<bool> ValidateFrame(Memory<byte> frame)
		{
			if (frame.Length >= 0x10)
			{
				await aesCryptoTransform.TransformFinal(frame.Slice(0, frame.Length & 0x7ffffff0), frame);
			}

			return await base.ValidateFrame(frame);
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing && !Disposed)
				aesCryptoTransform.Dispose();
			base.Dispose(disposing);
		}
	}
}
