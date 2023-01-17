﻿using Mpeg4Lib.Boxes;
using System;

namespace AAXClean.FrameFilters.Audio
{
	internal sealed class LosslessMultipartFilter : MultipartFilterBase<FrameEntry, NewSplitCallback>
	{
		private Action<NewSplitCallback> NewFileCallback { get; }

		private readonly FtypBox Ftyp;
		private readonly MoovBox Moov;
		private Mp4aWriter Mp4writer;
		protected override int InputBufferSize => 200;
		public bool Closed { get; private set; }
		public LosslessMultipartFilter(ChapterInfo splitChapters, FtypBox ftyp, MoovBox moov, Action<NewSplitCallback> newFileCallback)
			: base(splitChapters, (SampleRate)moov.AudioTrack.Mdia.Mdhd.Timescale, moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.ChannelCount == 2)
		{
			NewFileCallback = newFileCallback;
			Ftyp = ftyp;
			Moov = moov;
		}
		protected override void CloseCurrentWriter()
		{
			if (Closed) return;
			Mp4writer?.Close();
			Mp4writer?.OutputFile.Close();
			Closed = true;
		}

		protected override void WriteFrameToFile(FrameEntry audioFrame, bool newChunk)
		{
			Mp4writer.AddFrame(audioFrame.FrameData.Span, newChunk);
		}

		protected override void CreateNewWriter(NewSplitCallback callback)
		{
			NewFileCallback(callback);

			Mp4writer = new Mp4aWriter(callback.OutputFile, Ftyp, Moov, false);
			Closed = false;
			Mp4writer.RemoveTextTrack();

			if (Mp4writer.Moov.ILst is not null)
			{
				var tags = new AppleTags(Mp4writer.Moov.ILst);
				tags.Tracks = (callback.TrackNumber, callback.TrackCount);
				tags.Title = callback.TrackTitle ?? tags.Title;
			}
		}
		protected override void Dispose(bool disposing)
		{
			if (disposing && !Disposed)
				CloseCurrentWriter();
			base.Dispose(disposing);
		}
	}
}
