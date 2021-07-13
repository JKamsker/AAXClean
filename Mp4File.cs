﻿using AAXClean.Boxes;
using AAXClean.Chunks;
using AAXClean.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AAXClean
{
    public enum DecryptionResult
    {
        Failed,
        NoErrorsDetected
    }
    public enum DecryptionStatus
    {
        NotStarted,
        Working,
        Completed
    }
    public enum FileType
    {
        Aax,
        Aaxc,
        Mpeg4
    }
    public class Mp4File : Box
    {
        public event EventHandler<DecryptionProgressEventArgs> DecryptionProgressUpdate;
        public event EventHandler<DecryptionResult> DecryptionComplete;
        public AppleTags AppleTags { get; }
        public Stream InputStream { get; }
        public ChapterInfo Chapters { get; private set; }
        public DecryptionStatus Status { get; private set; } = DecryptionStatus.NotStarted;
        public FileType FileType { get; }
        public TimeSpan Duration => TimeSpan.FromSeconds((double)Moov.AudioTrack.Mdia.Mdhd.Duration / TimeScale);
        public uint MaxBitrate => Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Esds.ES_Descriptor.DecoderConfig.MaxBitrate;
        public uint AverageBitrate => Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Esds.ES_Descriptor.DecoderConfig.AverageBitrate;
        public uint TimeScale => Moov.AudioTrack.Mdia.Mdhd.Timescale;
        public int AudioChannels => Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Esds.ES_Descriptor.DecoderConfig.AudioConfig.ChannelConfiguration;

        private FtypBox Ftyp { get; }
        private MoovBox Moov { get; }
        private MdatBox Mdat { get; }

        public Mp4File(Stream file, long fileSize) : base(new BoxHeader((uint)fileSize, "MPEG"), null)
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
        public Mp4File(Stream file) : this(file, file.Length) { }

        public Mp4File(string fileName, FileAccess access = FileAccess.Read) : this(File.Open(fileName, FileMode.Open, access)) { }

        private bool isCancelled = false;

        public Mp4File DecryptAax(FileStream outputStream, string activationBytes)
        {
            if (string.IsNullOrWhiteSpace(activationBytes) || activationBytes.Length != 8)
                throw new ArgumentException($"{nameof(activationBytes)} must be 4 bytes long.");

            byte[] actBytes = ByteUtil.BytesFromHexString(activationBytes);

            return DecryptAax(outputStream, actBytes);
        }
        public Mp4File DecryptAax(FileStream outputStream, byte[] activationBytes)
        {
            if (activationBytes is null || activationBytes.Length != 4)
                throw new ArgumentException($"{nameof(activationBytes)} must be 4 bytes long.");
            if (FileType != FileType.Aax)
                throw new Exception($"This instance of {nameof(Mp4File)} is not an {FileType.Aax} file.");

            var adrm = Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.GetChild<AdrmBox>();

            if (adrm is null)
                return null;

            //Adrm key derrivation from 
            //https://github.com/FFmpeg/FFmpeg/blob/master/libavformat/mov.c in mov_read_adrm

            var intermediate_key = Crypto.Sha1(
               (audible_fixed_key, 0, audible_fixed_key.Length),
               (activationBytes, 0, activationBytes.Length));

            var intermediate_iv = Crypto.Sha1(
                (audible_fixed_key, 0, audible_fixed_key.Length),
                (intermediate_key, 0, intermediate_key.Length),
                (activationBytes, 0, activationBytes.Length));

            var calculatedChecksum = Crypto.Sha1(
                (intermediate_key, 0, 16),
                (intermediate_iv, 0, 16));

            if (!ByteUtil.BytesEqual(calculatedChecksum, adrm.Checksum))
                return null;

            var drmBlob = ByteUtil.CloneBytes(adrm.DrmBlob);

            Crypto.DecryptInPlace(
                ByteUtil.CloneBytes(intermediate_key, 0, 16),
                ByteUtil.CloneBytes(intermediate_iv, 0, 16), 
                drmBlob);

            if (!ByteUtil.BytesEqual(drmBlob, 0, activationBytes, 0, 4, true))
                return null;

            byte[] file_key = ByteUtil.CloneBytes(drmBlob, 8, 16);

            var file_iv = Crypto.Sha1(
                (drmBlob, 26, 16),
                (file_key, 0, 16),
                (audible_fixed_key, 0, 16));

            Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Children.Remove(adrm);

            return DecryptAaxc(
                outputStream,
                file_key, 
                ByteUtil.CloneBytes(file_iv, 0, 16));
        }

        //Constant key
        //https://github.com/FFmpeg/FFmpeg/blob/master/libavformat/mov.c
        private static readonly byte[] audible_fixed_key = { 0x77, 0x21, 0x4d, 0x4b, 0x19, 0x6a, 0x87, 0xcd, 0x52, 0x00, 0x45, 0xfd, 0x20, 0xa5, 0x1d, 0x67 };

        public Mp4File DecryptAaxc(FileStream outputStream, string audible_key, string audible_iv, ChapterInfo userChapters = null)
        {
            if (string.IsNullOrWhiteSpace(audible_key) || audible_key.Length != 32)
                throw new ArgumentException($"{nameof(audible_key)} must be 16 bytes long.");
            if (string.IsNullOrWhiteSpace(audible_iv) || audible_iv.Length != 32)
                throw new ArgumentException($"{nameof(audible_iv)} must be 16 bytes long.");

            byte[] key = ByteUtil.BytesFromHexString(audible_key);

            byte[] iv = ByteUtil.BytesFromHexString(audible_iv);

            return DecryptAaxc(outputStream, key, iv, userChapters);
        }

        public Mp4File DecryptAaxc(FileStream outputStream, byte[] key, byte[] iv, ChapterInfo userChapters = null)
        {
            if (!outputStream.CanWrite)
                throw new IOException($"{nameof(outputStream)} must be writable.");
            if (FileType != FileType.Aax && FileType != FileType.Aaxc)
                throw new ArgumentException($"This instance of {nameof(Mp4File)} is not an Aax or Aaxc file.");
            if (key is null || key.Length != 16)
                throw new ArgumentException($"{nameof(key)} must be 16 bytes long.");
            if (iv is null || iv.Length != 16)
                throw new ArgumentException($"{nameof(iv)} must be 16 bytes long.");
            
            Status = DecryptionStatus.Working;

            PatchAaxc();
            uint audioSize = CalculateAndAddBitrate();
            uint chaptersSize;

            if (userChapters is not null)
            {
                chaptersSize = (uint)userChapters.RenderSize;
                //Aaxc files repeat the chapter titles in a metadata track, but they
                //aren't necessary for media players and they will contradict the new
                //chapter titles, so we remove them.
                var textUdta = Moov.TextTrack.GetChild<UdtaBox>();

                if (textUdta is not null)
                    Moov.TextTrack.Children.Remove(textUdta);
            }
            else
            {
                chaptersSize = (uint)Moov.TextTrack.Mdia.Minf.Stbl.Stsz.SampleSizes.Sum(s => s);
            }

            //Write ftyp to output file
            Ftyp.Save(outputStream);

            //Calculate mdat size and write mdat header.
            uint mdatSize = Mdat.Header.HeaderSize + audioSize + chaptersSize;
            outputStream.WriteUInt32BE(mdatSize);
            outputStream.WriteType("mdat");


            var audioHandler = new AavdChunkHandler(TimeScale, Moov.AudioTrack, key, iv, outputStream);
            var chapterHandler = new ChapterChunkHandler(TimeScale, Moov.TextTrack);

            var chunkReader = new TrakChunkReader(InputStream, audioHandler, chapterHandler);


            #region Decryption Loop

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
                    DecryptionProgressUpdate?.Invoke(this, new DecryptionProgressEventArgs(position, speed));

                    nextUpdate = DateTime.Now.AddMilliseconds(200);
                }
            }

            #endregion

            Chapters = userChapters ?? chapterHandler.Chapters;
            //Write chapters to end of mdat and update moov
            Chapters.WriteChapters(Moov.TextTrack, TimeScale, outputStream);

            //write moov to end of file
            Moov.Save(outputStream);

            outputStream.Close();
            InputStream.Close();

            //Update status and events
            Status = DecryptionStatus.Completed;
            var decryptionResult = audioHandler.Success && !isCancelled ? DecryptionResult.NoErrorsDetected : DecryptionResult.Failed;
            DecryptionComplete?.Invoke(this, decryptionResult);

            return decryptionResult switch
            {
                DecryptionResult.NoErrorsDetected => new Mp4File(outputStream.Name, FileAccess.ReadWrite),
                _ => null
            };
        }

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
        public void Close()
        {
            InputStream?.Close();
        }
        public void Cancel()
        {
            isCancelled = true;
        }

        /// <returns>Number of audio bytes</returns>
        private uint CalculateAndAddBitrate()
        {
            //Calculate the actual average bitrate because aaxc file is wrong.
            long audioBits = Moov.AudioTrack.Mdia.Minf.Stbl.Stsz.SampleSizes.Sum(s => (long)s) * 8;
            double duration = Moov.AudioTrack.Mdia.Mdhd.Duration;
            uint avgBitrate = (uint)(audioBits * TimeScale / duration);

            Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Esds.ES_Descriptor.DecoderConfig.AverageBitrate = avgBitrate;

            //Remove extra Free boxes
            List<Box> children = Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Children;
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i] is FreeBox)
                    children.RemoveAt(i);
            }
            //Add a btrt box to the audio sample description.
            BtrtBox.Create(0, MaxBitrate, avgBitrate, Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry);
            return (uint)(audioBits / 8);
        }

        private void PatchAaxc()
        {
            Ftyp.MajorBrand = "isom";
            Ftyp.MajorVersion = 0x200;
            Ftyp.CompatibleBrands.Clear();
            Ftyp.CompatibleBrands.Add("iso2");
            Ftyp.CompatibleBrands.Add("mp41");
            Ftyp.CompatibleBrands.Add("M4A ");
            Ftyp.CompatibleBrands.Add("M4B ");

            //This is the flag that, if set, prevents cover art from loading on android.
            Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Esds.ES_Descriptor.DecoderConfig.AudioConfig.DependsOnCoreCoder = 0;
            //Must change the audio type from aavd to mp4a
            Moov.AudioTrack.Mdia.Minf.Stbl.Stsd.AudioSampleEntry.Header.Type = "mp4a";
        }

        private bool _disposed = false;
        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;
            base.Dispose(disposing);
            GC.Collect();
        }

        protected override void Render(Stream file) => throw new NotImplementedException();
    }
}
