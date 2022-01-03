﻿using AAXClean.AudioFilters;
using AAXClean.Boxes;
using AAXClean.Chunks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AAXClean
{
	public enum ConversionResult
	{
		Failed,
		NoErrorsDetected
	}
	public enum FileType
	{
		Aax,
		Aaxc,
		Mpeg4
	}
	public class Mp4File : Box
	{
		public ChapterInfo Chapters { get; internal set; }

		public event EventHandler<ConversionProgressEventArgs> ConversionProgressUpdate;
		public AppleTags AppleTags { get; }
		public Stream InputStream { get; }
		public FileType FileType { get; }
		public TimeSpan Duration => TimeSpan.FromSeconds((double)Moov.AudioTrack.Mdia.Mdhd.Duration / TimeScale);
		public uint MaxBitrate => Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Esds.ES_Descriptor.DecoderConfig.MaxBitrate;
		public uint AverageBitrate => Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Esds.ES_Descriptor.DecoderConfig.AverageBitrate;
		public uint TimeScale => Moov.AudioTrack.Mdia.Mdhd.Timescale;
		public int AudioChannels => Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Esds.ES_Descriptor.DecoderConfig.AudioConfig.ChannelConfiguration;

		public bool InputStreamCanSeek { get; protected set; }

		internal FtypBox Ftyp { get; set; }
		internal MoovBox Moov { get; }
		internal MdatBox Mdat { get; }

		private bool isCancelled = false;

		public Mp4File(Stream file, long fileSize) : base(new BoxHeader(fileSize, "MPEG"), null)
		{
			LoadChildren(file);
			InputStream = file;
			Ftyp = GetChild<FtypBox>();
			Moov = GetChild<MoovBox>();
			Mdat = GetChild<MdatBox>();

			FileType = Ftyp.MajorBrand switch
			{
				"aax " => FileType.Aax,
				"aaxc" => FileType.Aaxc,
				_ => FileType.Mpeg4
			};

			if (Moov.iLst is not null)
				AppleTags = new AppleTags(Moov.iLst);
		}
		public Mp4File(Stream file) : this(file, file.Length)
		{
			InputStreamCanSeek = file.CanSeek;
		}

		public Mp4File(string fileName, FileAccess access = FileAccess.Read) : this(File.Open(fileName, FileMode.Open, access))
		{
			InputStreamCanSeek = true;
		}

		internal virtual Mp4AudioChunkHandler GetAudioChunkHandler()
			=> new Mp4AudioChunkHandler(TimeScale, Moov.AudioTrack, InputStreamCanSeek);

		public void Save()
		{
			if (Moov.Header.FilePosition < Mdat.Header.FilePosition)
				throw new Exception("Does not support editing moov before mdat");

			InputStream.Position = Moov.Header.FilePosition;
			Moov.Save(InputStream);

			if (InputStream.Position < InputStream.Length)
			{
				int freeSize = (int)Math.Max(8, InputStream.Length - InputStream.Position);

				FreeBox.Create(freeSize, this).Save(InputStream);
			}
		}

		public ConversionResult ConvertToMp4a(Stream outputStream, ChapterInfo userChapters = null)
		{
			using var audioHandler = GetAudioChunkHandler();

			Mp4aWriter mp4AWriter = new Mp4aWriter(outputStream, Ftyp, Moov, InputStream.Length > uint.MaxValue);

			var losslessFilter = new LosslessFilter(mp4AWriter);
			audioHandler.FrameFilter = losslessFilter;

			var chapterHandler = new ChapterChunkHandler(TimeScale, Moov.TextTrack);

			ProcessAudio(audioHandler, chapterHandler);

			var chapters = userChapters ?? chapterHandler.Chapters;

			Chapters = chapters;
			mp4AWriter.WriteChapters(chapters);
			losslessFilter.Close();

			return audioHandler.Success && !isCancelled ? ConversionResult.NoErrorsDetected : ConversionResult.Failed;
		}

		public void ConvertToMultiMp4a(ChapterInfo userChapters, Action<NewSplitCallback> newFileCallback)
		{
			using var audioHandler = GetAudioChunkHandler();

			using var audioFilter = new LosslessMultipartFilter(
				userChapters,
				newFileCallback,
				Ftyp,
				Moov);

			audioHandler.FrameFilter = audioFilter;

			ProcessAudio(audioHandler);
			audioFilter.Close();
		}

		public ChapterInfo GetChapterInfo()
		{
			var chapterHandler = new ChapterChunkHandler(TimeScale, Moov.TextTrack, seekable: true);
			var chunkReader = new TrakChunkReader(InputStream, chapterHandler);

			isCancelled = false;

			while (!isCancelled && chunkReader.NextChunk()) ;
			return chapterHandler.Chapters;
		}

		public IEnumerable<(TimeSpan start, TimeSpan end)> DetectSilence(double decibels, TimeSpan minDuration)
		{
			if (decibels >= 0 || decibels < -90)
				throw new ArgumentException($"{nameof(decibels)} must fall in [-90,0)");
			if (minDuration.TotalSeconds * TimeScale < 2)
				throw new ArgumentException($"{nameof(minDuration)} must be no shorter than 2 audio samples.");

			using var audioHandler = GetAudioChunkHandler();

			SilenceDetect sil = new SilenceDetect(
				decibels,
				minDuration,
				audioHandler.Track.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Esds.ES_Descriptor.DecoderConfig.AudioConfig.Blob,
				audioHandler.Track.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.SampleSize);
			audioHandler.FrameFilter = sil;

			ProcessAudio(audioHandler);

			sil.Close();

			return sil.Silences;
		}

		private void ProcessAudio(Mp4AudioChunkHandler audioHandler, params IChunkHandler[] chunkHandlers)
		{
			var handlers = new List<IChunkHandler>();
			handlers.Add(audioHandler);
			handlers.AddRange(chunkHandlers);

			var chunkReader = new TrakChunkReader(InputStream, handlers.ToArray());

			var beginProcess = DateTime.Now;
			var nextUpdate = beginProcess;

			isCancelled = false;

			while (!isCancelled && chunkReader.NextChunk())
			{
				//Throttle update so it doesn't bog down UI
				if (DateTime.Now > nextUpdate)
				{
					TimeSpan position = audioHandler.ProcessPosition;
					var speed = position / (DateTime.Now - beginProcess);
					ConversionProgressUpdate?.Invoke(this, new ConversionProgressEventArgs(position, speed));

					nextUpdate = DateTime.Now.AddMilliseconds(200);
				}
			}
		}

		//private NAudio.Lame.LameConfig GetDefaultLameConfig()
		//{
		//	double channelDown = Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.ChannelCount == 1 ? 1 : 0.5;

		//	var lameConfig = new NAudio.Lame.LameConfig
		//	{
		//		ABRRateKbps = (int)(CalculateAudioSizeAndBitrate().avgBitrate * channelDown / 1024),
		//		Mode = NAudio.Lame.MPEGMode.Mono,
		//		VBR = NAudio.Lame.VBRMode.ABR,
		//	};
		//	return lameConfig;
		//}


		protected (long audioSize, uint avgBitrate) CalculateAudioSizeAndBitrate()
		{
			//Calculate the actual average bitrate because aaxc file is wrong.
			long audioBits = Moov.AudioTrack.Mdia.Minf.Stbl.Stsz.SampleSizes.Sum(s => (long)s) * 8;
			double duration = Moov.AudioTrack.Mdia.Mdhd.Duration;
			uint avgBitrate = (uint)(audioBits * TimeScale / duration);

			return (audioBits / 8, avgBitrate);
		}

		public void Cancel()
		{
			isCancelled = true;
		}

		public void Close()
		{
			InputStream?.Close();
		}

		private bool _disposed = false;
		protected override void Dispose(bool disposing)
		{
			if (_disposed)
				return;

			if (disposing)
				Close();

			_disposed = true;
			base.Dispose(disposing);
			GC.Collect();
		}

		protected override void Render(Stream file) => throw new NotImplementedException();
	}
}
