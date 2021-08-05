using AAXClean.Descriptors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AAXClean.AudioFilters
{
    internal class WaveFilter : IFrameFilter
    {
        private AacDecoder decoder;
        private Stream OutputStream;
        public WaveFilter(Stream waveOutput, AudioSpecificConfig audioSpecificConfig, ushort sampleSize)
        {
            decoder = new CustomAacDecoder(audioSpecificConfig);

            OutputStream = waveOutput;

        }
        public void Close()
        {
        }

        public void Dispose()
        {
        }

        public bool FilterFrame(uint chunkIndex, uint frameIndex, byte[] audioFrame)
        {
            decoder.DecodeShort(audioFrame);
            return true;
        }
    }
}
