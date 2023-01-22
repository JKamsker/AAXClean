﻿using System;
using System.IO;

namespace Mpeg4Lib.Boxes
{
	public class MdatBox : Box
	{
		public MdatBox(BoxHeader header, Box parent) : base(header, parent) { }

		/// <summary>
		/// Shifts the position of the mdat atom in the stream. When Completed, <see cref="Stream.Position"/> is at the end of the mdat atom.
		/// </summary>
		/// <param name="file">A <see cref="Stream"/> that <see cref="Stream.CanRead"/>, <see cref="Stream.CanWrite"/>, <see cref="Stream.CanSeek"/> </param>
		/// <param name="shiftVector">The size and direction of the shift</param>
		public void ShiftMdat(Stream file, long shiftVector)
		{
			const int MIN_SHIFT_SIZE = 1024 * 1024;

			if (!file.CanRead || !file.CanWrite || !file.CanSeek)
				throw new InvalidOperationException($"{nameof(file)} must be readable, writable and seekable to {nameof(ShiftMdat)}");

			file.Position = Header.FilePosition;

			Header.FilePosition += shiftVector;

			var bufferSz = Math.Max(MIN_SHIFT_SIZE, Math.Abs(shiftVector));
			var buffer1 = new byte[bufferSz];
			var buffer2 = new byte[bufferSz];
			int read1 = file.Read(buffer1);
			int read2 = file.Read(buffer2);

			while (read1 == read2 && read2 == bufferSz)
			{
				file.Position -= read1 + read2 - shiftVector;
				file.Write(buffer1, 0, read1);

				file.Position += read2 - shiftVector;
				read1 = file.Read(buffer1);

				(read1, read2) = (read2, read1);
				(buffer1, buffer2) = (buffer2, buffer1);
			}

			file.Position -= read1 + read2 - shiftVector;
			file.Write(buffer1, 0, read1);
			file.Write(buffer2, 0, read2);
		}

		protected override void Render(Stream file)
		{
			throw new NotSupportedException();
		}
	}
}
