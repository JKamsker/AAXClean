﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AAXClean.AudioFilters
{
    internal class FaadHandle : SafeHandle
    {
        public FaadHandle() : base(IntPtr.Zero, true) { }
        public override bool IsInvalid => !IsClosed && handle != IntPtr.Zero;
        protected override bool ReleaseHandle()
        {
            libFaad2.NeAACDecClose(handle);
            return true;
        }
    }
   
    internal static class libFaad2
    {
        const string libPath = "libfaad2_dll.dll";

        #region Enums
        internal enum OutputFormat : byte
        {
            FAAD_FMT_16BIT = 1,
            FAAD_FMT_24BIT = 2,
            FAAD_FMT_32BIT = 3,
            FAAD_FMT_FLOAT = 4,
            FAAD_FMT_DOUBLE = 5
        }

        internal enum ObjectType : byte
        {
            MAIN = 1,
            LC = 2,
            SSR = 3,
            LTP = 4,
            HE_AAC = 5,
            ER_LC = 17,
            ER_LTP = 5,
            LD = 5,
        }

        #endregion

        #region Structures

        [StructLayout(LayoutKind.Sequential)]
        public struct mp4AudioSpecificConfig
        {
            /* Audio Specific Info */
            byte objectTypeIndex;
            byte samplingFrequencyIndex;
            uint samplingFrequency;
            byte channelsConfiguration;

            /* GA Specific Info */
            byte frameLengthFlag;
            byte dependsOnCoreCoder;
            ushort coreCoderDelay;
            byte extensionFlag;
            byte aacSectionDataResilienceFlag;
            byte aacScalefactorDataResilienceFlag;
            byte aacSpectralDataResilienceFlag;
            byte epConfig;

            sbyte sbr_present_flag;
            sbyte forceUpSampling;
            sbyte downSampledSBR;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NeAACDecFrameInfo
        {
            public int bytesconsumed;
            public int samples;
            public byte channels;
            public byte error;
            public int samplerate;
            public byte sbr;
            public ObjectType object_type;
            public byte header_type;
            public byte num_front_channels;
            public byte num_side_channels;
            public byte num_back_channels;
            public byte num_lfe_channels;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] channel_position;
            public byte ps;
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct NeAACDecConfiguration
        {
            public ObjectType defObjectType;
            public uint defSampleRate;
            public OutputFormat outputFormat;
            public bool downMatrix;
            public bool useOldADTSFormat;
            public bool dontUpsample;
        }

        #endregion

        #region Methods

        [DllImport(libPath)]
        public static extern FaadHandle NeAACDecOpen();

        [DllImport(libPath)]
        public static extern void NeAACDecClose(IntPtr hpDecoder);

        [DllImport(libPath)]
        public static extern byte NeAACDecInit2(FaadHandle hpDecoder, byte[] pBuffer, int SizeOfDecoderSpecificInfo, out int samplerate, out int channels);

        [DllImport(libPath)] 
        public static extern IntPtr NeAACDecGetCurrentConfiguration(FaadHandle hpDecoder);

        [DllImport(libPath)]
        public static extern byte NeAACDecSetConfiguration(FaadHandle hpDecoder, IntPtr config);

        [DllImport(libPath)]
        public static extern byte NeAACDecAudioSpecificConfig(byte[] pBuffer, int buffer_size, out mp4AudioSpecificConfig mp4ASC);

        [DllImport(libPath)]
        public static extern IntPtr NeAACDecDecode(FaadHandle hpDecoder, out NeAACDecFrameInfo hInfo, byte[] buffer, int buffer_size);


        [DllImport(libPath, CharSet = CharSet.Auto)]
        public static extern IntPtr NeAACDecGetErrorMessage(byte errcode);

        #endregion
    }
}
