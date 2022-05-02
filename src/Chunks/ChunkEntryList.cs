﻿using AAXClean.Boxes;
using System;
using System.Collections;
using System.Collections.Generic;

namespace AAXClean.Chunks
{
	/// <summary>
	/// A readonly list of <see cref="ChunkEntry"/> from a <see cref="TrakBox"/> in order of the chunk offset.
	/// </summary>
	internal class ChunkEntryList : IReadOnlyList<ChunkEntry>
	{
		private readonly IReadOnlyList<ChunkOffsetEntry> ChunkTable;
		private readonly StszBox Stsz;
		private readonly int EntryCount;
		private readonly (uint firstFrameIndex, uint numFrames)[] ChunkFrameTable;
		private delegate ChunkEntry ChunkEntryDelegate(int chunkIndex);

		public TrakBox Track { get; }
		public int Count => ChunkFrameTable.Length;
		public ChunkEntry this[int chunkIndex] => GetChunkEntry(chunkIndex);

		public ChunkEntryList(TrakBox track)
		{
			Track = track;
			Stsz = Track.Mdia.Minf.Stbl.Stsz;

			if (Track.Mdia.Minf.Stbl.Stco is not null)
			{
				ChunkTable = Track.Mdia.Minf.Stbl.Stco.ChunkOffsets;
				EntryCount = (int)Track.Mdia.Minf.Stbl.Stco.EntryCount;
			}
			else
			{
				ChunkTable = Track.Mdia.Minf.Stbl.Co64.ChunkOffsets;
				EntryCount = (int)Track.Mdia.Minf.Stbl.Co64.EntryCount;
			}

			ChunkFrameTable = Track.Mdia.Minf.Stbl.Stsc.CalculateChunkFrameTable();
		}

		public IEnumerator<ChunkEntry> GetEnumerator()
			=> new TrachChunkEnumerator(EntryCount, i => GetChunkEntry(i));

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		private ChunkEntry GetChunkEntry(int chunkIndex)
		{
			ChunkOffsetEntry cEntry = ChunkTable[chunkIndex];

			(uint firstFrameIndex, uint numFrames) = ChunkFrameTable[cEntry.EntryIndex];

			(int[] frameSizes, int totalChunkSize) = Stsz.GetChunkFrameSizes(firstFrameIndex, numFrames);

			return new ChunkEntry
			{
				FirstFrameIndex = firstFrameIndex,
				FrameSizes = frameSizes,
				ChunkIndex = cEntry.EntryIndex,
				ChunkSize = totalChunkSize,
				ChunkOffset = cEntry.ChunkOffset
			};
		}

		/// <summary>
		/// Enumerate over all track chunks in a track, and retrieve all information about that chunk.
		/// </summary>
		private class TrachChunkEnumerator : IEnumerator<ChunkEntry>
		{
			private readonly int EntryCount;
			private readonly ChunkEntryDelegate GetChunkEntryDelegate;
			private int CurrentChunkIndex = 0;

			public TrachChunkEnumerator(int entryCount, ChunkEntryDelegate chunkEntryDelegate)
			{
				EntryCount = entryCount;
				GetChunkEntryDelegate = chunkEntryDelegate;
			}

			public ChunkEntry Current { get; private set; }

			object IEnumerator.Current => Current;

			public void Dispose() { }

			public bool MoveNext()
			{
				if (CurrentChunkIndex >= EntryCount) return false;

				Current = GetChunkEntryDelegate(CurrentChunkIndex++);

				return true;
			}

			public void Reset()
			{
				CurrentChunkIndex = 0;
			}
		}
	}
}
