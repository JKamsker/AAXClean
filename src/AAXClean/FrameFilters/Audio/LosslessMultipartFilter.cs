﻿using Mpeg4Lib.Boxes;
using System;

namespace AAXClean.FrameFilters.Audio
{
	internal sealed class LosslessMultipartFilter : MultipartFilterBase<FrameEntry>
	{
		protected override Action<NewSplitCallback> NewFileCallback { get; }

		private readonly FtypBox Ftyp;
		private readonly MoovBox Moov;
		private Mp4aWriter Writer;
		public LosslessMultipartFilter(ChapterInfo splitChapters, Action<NewSplitCallback> newFileCallback, FtypBox ftyp, MoovBox moov)
			: base(moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Esds.ES_Descriptor.DecoderConfig.AudioSpecificConfig.AscBlob, splitChapters)
		{
			NewFileCallback = newFileCallback;
			Ftyp = ftyp;
			Moov = moov;
		}
		protected override void CloseCurrentWriter()
		{
			Writer?.Close();
			Writer?.OutputFile.Close();
		}

		protected override void WriteFrameToFile(IFrameEntry audioFrame, bool newChunk)
		{
			Writer.AddFrame(audioFrame.FrameData.Span, newChunk);
		}

		protected override void CreateNewWriter(NewSplitCallback callback)
		{
			NewFileCallback(callback);

			Writer = new Mp4aWriter(callback.OutputFile, Ftyp, Moov, false);
			Writer.RemoveTextTrack();
		}
		protected override void Dispose(bool disposing)
		{
			CloseCurrentWriter();
			base.Dispose(disposing);
		}
	}
}
