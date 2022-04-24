﻿using System;
using System.Collections.Generic;

namespace AAXClean.AudioFilters
{
	public abstract class MultipartFilter : AudioFilter
	{
		protected abstract Action<NewSplitCallback> NewFileCallback { get; }
		private int SampleRate { get; }
		private IEnumerator<Chapter> SplitChapters { get; }

		private uint StartFrame { get; set; }
		private long EndFrame { get; set; }

		private const int AAC_TIME_DOMAIN_SAMPLES = 1024;

		private long lastChunk = -1;
		private static readonly int[] asc_samplerates = { 96000, 88200, 64000, 48000, 44100, 32000, 24000, 22050, 16000, 12000, 11025, 8000, 7350 };

		public MultipartFilter(Span<byte> audioSpecificConfig, ChapterInfo splitChapters)
		{
			if (splitChapters is null || splitChapters.Count == 0)
				throw new ArgumentException($"{nameof(splitChapters)} must contain at least one chapter.");

			SampleRate = asc_samplerates[(audioSpecificConfig[0] & 7) << 1 | audioSpecificConfig[1] >> 7];
			SplitChapters = splitChapters.GetEnumerator();

			EndFrame = -1;
		}

		protected abstract void CloseCurrentWriter();
		protected abstract void WriteFrameToFile(Span<byte> audioFrame, bool newChunk);
		protected abstract void CreateNewWriter(NewSplitCallback callback);

		public override void Close()
		{
			CloseCurrentWriter();
		}

		public override bool FilterFrame(uint chunkIndex, uint frameIndex, Span<byte> audioFrame)
		{
			if (frameIndex > EndFrame)
			{
				CloseCurrentWriter();

				if (!GetNextChapter())
					return false;

				var callback = new NewSplitCallback(SplitChapters.Current);
				CreateNewWriter(callback);
				WriteFrameToFile(audioFrame, true);
				lastChunk = chunkIndex;
			}
			else if (frameIndex >= StartFrame)
			{
				bool newChunk = false;
				if (chunkIndex > lastChunk)
				{
					newChunk = true;
					lastChunk = chunkIndex;
				}
				WriteFrameToFile(audioFrame, newChunk);
			}

			return true;
		}

		private bool GetNextChapter()
		{
			if (!SplitChapters.MoveNext())
				return false;

			StartFrame = (uint)(SplitChapters.Current.StartOffset.TotalSeconds * SampleRate / AAC_TIME_DOMAIN_SAMPLES);
			EndFrame = (uint)(SplitChapters.Current.EndOffset.TotalSeconds * SampleRate / AAC_TIME_DOMAIN_SAMPLES);
			return true;
		}

		protected override void Dispose(bool disposing)
		{
			SplitChapters?.Dispose();
			base.Dispose(disposing);
		}
	}
}
