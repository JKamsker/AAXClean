﻿using System;
using System.IO;

namespace AAXClean
{
	/// <summary>
	/// A read-only stream that tracks the stream position based on the number of bytes read. 
	/// </summary>
	internal class TrackedReadStream : Stream
	{
		private long ReadPosition = 0;
		private readonly Stream BaseStream;
		private readonly long BaseStreamLength;

		public TrackedReadStream(Stream baseStream, long streamLength)
		{
			BaseStream = baseStream;
			BaseStreamLength = streamLength;
		}

		public override bool CanRead => BaseStream.CanRead;
		public override bool CanSeek => BaseStream.CanSeek;
		public override long Length => BaseStreamLength;
		public override bool CanWrite => BaseStream.CanWrite;
		public override long Position
		{
			get => CanSeek ? BaseStream.Position : ReadPosition;
			set
			{
				if (!CanSeek)
					throw new NotSupportedException();
				
				// Debug break when new position is before the current position
				if (value < ReadPosition)
					System.Diagnostics.Debugger.Break();
				
				BaseStream.Position = ReadPosition = value;
			}
		}

		public override void Flush()
			=> throw new NotSupportedException();

		public override int Read(byte[] buffer, int offset, int count)
		{
			int totalRead = 0, btsRead;

			do
			{
				btsRead = BaseStream.Read(buffer, offset + totalRead, count - totalRead);
				totalRead += btsRead;
			} while (totalRead < count && btsRead > 0);

			ReadPosition += totalRead;

			return totalRead;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			if (!CanSeek)
				throw new NotSupportedException();

			return ReadPosition = BaseStream.Seek(offset, origin);
		}

		public override void SetLength(long value)
			=> BaseStream.SetLength(value);

		public override void Write(byte[] buffer, int offset, int count)
			=> BaseStream.Write(buffer, offset, count);

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				BaseStream.Dispose();

			base.Dispose(disposing);
		}
	}
}
