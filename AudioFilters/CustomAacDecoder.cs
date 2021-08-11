using AAXClean.Descriptors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AAXClean.AudioFilters
{
    class CustomAacDecoder : AacDecoder
    {

        [DllImport("ffmpegdll.dll")]
        private static extern IntPtr aac_dec_init(int sampleRate, int channels, byte[] asc, int ascSize);

        [DllImport("ffmpegdll.dll")]
        private static extern int aac_dec_decode_frame(IntPtr avcontext, byte[] aacFrame, int frame_size, byte[] decodeBuff, int buffSize);

        IntPtr handle;
        FileStream fs = File.OpenWrite(@"C:\Users\mbuca\Libation\Books\The Martian [B082BHJMFF]\The Martian [B082BHJMFF] dll.wav");
        public CustomAacDecoder(AudioSpecificConfig asc) : base(asc.Blob)
        {
            handle = aac_dec_init(SampleRate, Channels, asc.Blob, asc.Blob.Length);
        }

        public override byte[] DecodeBytes(byte[] aacFrame)
        {
            byte[] decBuff = new byte[AAC_FRAME_SIZE * Channels];

            aac_dec_decode_frame(handle, aacFrame, aacFrame.Length, decBuff, decBuff.Length);

            fs.Write(decBuff);

            return decBuff;
        }

        public override short[] DecodeShort(byte[] aacFrame)
        {
            byte[] decBuff = new byte[AAC_FRAME_SIZE * Channels];

            aac_dec_decode_frame(handle, aacFrame, aacFrame.Length, decBuff, decBuff.Length / BITS_PER_SAMPLE * 8);

            return Array.Empty<short>();
        }

        protected override IntPtr DecodeUnmanaged(byte[] aacFrame)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
