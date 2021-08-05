using AAXClean.Descriptors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AAXClean.AudioFilters
{
    class CustomAacDecoder : AacDecoder
    {
        const int MAX_ELEM_ID = 16;
        private int m4ac_object_type;
        private int m4ac_chan_config = 0;
        private ChannelElement[][] che = new ChannelElement[4][];
        private ChannelElement[][] tag_che_map = new ChannelElement[4][];

        private AudioSpecificConfig M4ac;
        public CustomAacDecoder(AudioSpecificConfig asc) : base(asc.Blob)
        {
            M4ac = asc;
               m4ac_chan_config = Channels;
            m4ac_object_type = asc.AudioObjectType;

            for (int i = 0; i < 4; i++)
            {
                che[i] = new ChannelElement[MAX_ELEM_ID];
                tag_che_map[i] = new ChannelElement[MAX_ELEM_ID];
            }
            //must have 1 or 2 channels
            //must be type 2 AAC_LC
        }

        public override byte[] DecodeBytes(byte[] aacFrame)
        {
            throw new NotImplementedException();
        }

        public override short[] DecodeShort(byte[] aacFrame)
        {
            BitStream bs = new(aacFrame);

            byte[][] che_presence = new byte[4][];
            for (int i = 0; i < 4; i++)
                che_presence[i] = new byte[MAX_ELEM_ID];

            RawDataBlockType elem_type;

            int samples = 1024;

            while ((elem_type = (RawDataBlockType)bs.get_bits(3)) != RawDataBlockType.TYPE_END)
            {
                int elem_id = bs.get_bits(4);

                ChannelElement che = default;

                if (elem_type < RawDataBlockType.TYPE_DSE)
                {
                    if (che_presence[(int)elem_type][elem_id] != 0)
                    {
                        bool error = che_presence[(int)elem_type][elem_id] > 1;
                    }

                    che_presence[(int)elem_type][elem_id]++;

                    che = get_che(elem_type, elem_id);
                    che.present = 1;
                }


                switch(elem_type)
                {
                    case RawDataBlockType.TYPE_CPE:
                        decode_cpe(bs, che);

                    break;
                }
            }


            return Array.Empty<short>();
        }
        private int tags_mapped = 0;
        private ChannelElement get_che(RawDataBlockType type, int elem_id)
        {
            if (m4ac_chan_config == 2 && type == RawDataBlockType.TYPE_CPE)
            {
                tags_mapped++;
                che[(int)type][0] ??= new ChannelElement();

                 return tag_che_map[(int)type][elem_id] = che[(int)type][0];
            }
            return default(ChannelElement);
        }

        private void decode_cpe(BitStream gb, ChannelElement cpe)
        {
            int i, ret, common_window, ms_present = 0;

            common_window = gb.get_bits(1);

            if (common_window != 0)
            {
                decode_ics_info(cpe.ch[0].ics, gb);

                i = cpe.ch[1].ics.use_kb_window[0];
                cpe.ch[1].ics = cpe.ch[0].ics;
                cpe.ch[1].ics.use_kb_window[1] = (byte)i;

                if (cpe.ch[1].ics.predictor_present != 0 && M4ac.AudioObjectType != 1)
                {
                    if (cpe.ch[1].ics.ltp.present == gb.get_bits(1))
                    {
                        //decode_ltp
                    }
                }

                ms_present = gb.get_bits(2);

                if (ms_present > 0 && ms_present < 3)
                    decode_mid_side_stereo(cpe, gb, ms_present);
            }
            decode_ics(cpe.ch[0], gb, common_window, 0);
            decode_ics(cpe.ch[1], gb, common_window, 0);
        }

        public void decode_ics(SingleChannelElement sce, BitStream gb, int common_window, int scale_flag)
        {
            Pulse pulse = new();
            TemporalNoiseShaping tns = sce.tns;
            IndividualChannelStream ics = sce.ics;
            float[] pout = sce.coeffs;

            int global_gain, eld_syntax, er_syntax, pulse_present = 0;

            global_gain = gb.get_bits(8);

            if (common_window == 0 && scale_flag == 0)
            {
                //fail;
            }
            decode_band_types(sce.band_type, sce.band_type_run_end, gb, ics);
            decode_scalefactors(sce.sf, gb, global_gain, ics, sce.band_type, sce.band_type_run_end);

            pulse_present = 0;

            if ((pulse_present = gb.get_bits(1)) != 0)
            {
               
            }
            tns.present = gb.get_bits(1);

            if (tns.present != 0)
            {
                decode_tns(tns, gb, ics);
            }
            if (gb.get_bits(1) != 0)
            {
                
            }

            decode_spectrum_and_dequant(pout, gb, sce.sf, pulse_present, pulse, ics, sce.band_type);


        }

        private void decode_spectrum_and_dequant(float[] coef, BitStream gb, float[] sf, int pulse_present, Pulse pulse, IndividualChannelStream ics, BandType[] band_type)
        {
            int i, k, g, idx = 0;
            int c = 1024 / ics.num_windows;
            short[] offsets = ics.swb_offset;
            float[] coef_base = coef;

            int size = sizeof(float) * (c - offsets[ics.max_sfb]);

            if (size > 0)
            {

            }

            for (g = 0; g < ics.num_window_groups; g++)
            {
                var g_len = ics.group_len[g];

                for (i = 0; i < ics.max_sfb; i++, idx++)
                {
                    var cbt_m1 = band_type[idx] - 1;
                    INTFLOAT* cfo = coef + offsets[i];
                    int off_len = offsets[i + 1] - offsets[i];
                    int group;
                }

            }
        }
        private void decode_band_types(BandType[] band_type, int[] band_type_run_end, BitStream gb, IndividualChannelStream ics)
        {
            int g, idx = 0;
            int bits = (ics.window_sequence[0] == WindowSequence.EIGHT_SHORT_SEQUENCE) ? 3 : 5;

            for (g = 0; g < ics.num_window_groups; g++)
            {
                int k = 0;

                while (k < ics.max_sfb)
                {
                    int sect_end = k;
                    int sect_len_incr;
                    int sect_band_type = gb.get_bits(4);
                    if (sect_band_type == 12)
                    {
                        //av_log(ac->avctx, AV_LOG_ERROR, "invalid band type\n");
                        //return AVERROR_INVALIDDATA;
                    }
                    do
                    {
                        sect_len_incr = gb.get_bits(bits);
                        sect_end += sect_len_incr;
                        if (gb.get_bits_left() < 0)
                        {
                           //av_log(ac->avctx, AV_LOG_ERROR, "decode_band_types: "overread_err);
                            //return AVERROR_INVALIDDATA;
                        }
                        if (sect_end > ics.max_sfb)
                        {
                            //av_log(ac->avctx, AV_LOG_ERROR,
                            //       "Number of bands (%d) exceeds limit (%d).\n",
                            //       sect_end, ics->max_sfb);
                            //return AVERROR_INVALIDDATA;
                        }
                    } while (sect_len_incr == (1 << bits) - 1);

                    for (; k < sect_end; k++)
                    {
                        band_type[idx] = (BandType)sect_band_type;
                        band_type_run_end[idx++] = sect_end;
                    }
                }
            }
        }
        const int NOISE_OFFSET = 90;
        public void decode_scalefactors(float[] sf, BitStream gb, int global_gain, IndividualChannelStream ics, BandType[] band_type, int[] band_type_run_end)
        {
            int g, i, idx = 0;
            int[] offset = { global_gain, global_gain - NOISE_OFFSET, 0 };
            int clipped_offset;
            int noise_flag = 1;
            for (g = 0; g < ics.num_window_groups; g++)
            {
                for (i = 0; i < ics.max_sfb;)
                {
                    int run_end = band_type_run_end[idx];

                    if (band_type[idx] == BandType.ZERO_BT)
                    {
                        for (; i < run_end; i++, idx++)
                            sf[idx] = 0f;
                    }
                    else if ((band_type[idx] == BandType.INTENSITY_BT) ||
                       (band_type[idx] == BandType.INTENSITY_BT2))
                    {

                    }
                    else if (band_type[idx] == BandType.NOISE_BT)
                    {

                    }
                    else
                    {

                    }
                }
            }
        }

        private void decode_tns(TemporalNoiseShaping tns, BitStream gb, IndividualChannelStream ics)
        {
            int w, filt, i, coef_len, coef_res, coef_compress;
            bool is8 = ics.window_sequence[0] == WindowSequence.EIGHT_SHORT_SEQUENCE;

            int tns_max_order = is8 ? 7 : M4ac.AudioObjectType == 1 ? 20 : 12;
            for (w = 0; w < ics.num_windows; w++)
            {
                if ((tns.n_filt[w] = gb.get_bits(2 - (is8 ? 1:0))) != 0)
                {
                    coef_res = gb.get_bits(1);

                    for (filt = 0; filt < tns.n_filt[w]; filt++)
                    {
                        int tmp2_idx;
                        tns.length[w][filt] = gb.get_bits(6 - 2 * (is8 ? 1 : 0));

                        if ((tns.order[w][filt] = gb.get_bits(5 - 2 * (is8 ? 1 : 0))) > tns_max_order)
                        {
                            //av_log(ac->avctx, AV_LOG_ERROR,
                            //       "TNS filter order %d is greater than maximum %d.\n",
                            //       tns->order[w][filt], tns_max_order);
                            //tns->order[w][filt] = 0;
                            //return AVERROR_INVALIDDATA;
                        }
                        if (tns.order[w][filt] != 0)
                        {
                            tns.direction[w][filt] = gb.get_bits(1);
                            coef_compress = gb.get_bits(1);
                            coef_len = coef_res + 3 - coef_compress;
                            tmp2_idx = 2 * coef_compress + coef_res;

                            for (i = 0; i < tns.order[w][filt]; i++)
                                tns.coef[w][filt][i] = tns_tmp2_map[tmp2_idx][gb.get_bits(coef_len)];
                        }
                    }
                }
            }
        }
        private void decode_mid_side_stereo(ChannelElement cpe, BitStream gb, int ms_present)
        {
            int max_idx = cpe.ch[0].ics.num_window_groups * cpe.ch[0].ics.max_sfb;

            if (ms_present == 1)
            {
                for (int idx = 0; idx < max_idx; idx++)
                    cpe.ms_mask[idx] = (byte)gb.get_bits(1);
            }
            else if (ms_present == 2)
            {
                //memset(cpe->ms_mask, 1, max_idx * sizeof(cpe->ms_mask[0]));
            }
        }

        int[] ms_mask = new int[128];

        private void decode_ics_info(IndividualChannelStream ics, BitStream gb)
        {

            if (gb.get_bits(1) != 0)
            {
                //Reserved bit set
            }

            ics.window_sequence[1] = ics.window_sequence[0];
            ics.window_sequence[0] = (WindowSequence)gb.get_bits(2);

            ics.use_kb_window[1] = ics.use_kb_window[0];
            ics.use_kb_window[0] = (byte)gb.get_bits(1);

            ics.num_window_groups = 1;
            ics.group_len[0] = 1;


            if (ics.window_sequence[0] == WindowSequence.EIGHT_SHORT_SEQUENCE)
            {

            }
            else
            {
               ics.max_sfb = (byte)gb.get_bits(6);
                ics.num_windows = 1;

                //if full aac frame length
                {
                    ics.num_swb = ff_aac_num_swb_1024[M4ac.SamplingFrequencyIndex];
                    ics.swb_offset = ff_swb_offset_1024[M4ac.SamplingFrequencyIndex];
                }
                ics.tns_max_bands = ff_tns_max_bands_1024[M4ac.SamplingFrequencyIndex];
            }

            if (M4ac.AudioObjectType != 39)
            {
                ics.predictor_present = gb.get_bits(1);
                ics.predictor_reset_group = 0;
            }

            if (ics.predictor_present != 0)
            {

            }

            if (ics.max_sfb > ics.num_swb)
                throw new Exception($"Number of scalefactor bands in group ({ics.max_sfb}) exceeds limit ({ics.num_swb}).");
        }
        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        protected override IntPtr DecodeUnmanaged(byte[] aacFrame)
        {
            throw new NotImplementedException();
        }

        private static byte[] ff_tns_max_bands_1024 = {
    31, 31, 34, 40, 42, 51, 46, 46, 42, 42, 42, 39, 39
};

        private static int[] ff_aac_num_swb_1024 = {
    41, 41, 47, 49, 49, 51, 47, 47, 43, 43, 43, 40, 40
};


        static short[] swb_offset_1024_96 = {
      0,   4,   8,  12,  16,  20,  24,  28,
     32,  36,  40,  44,  48,  52,  56,  64,
     72,  80,  88,  96, 108, 120, 132, 144,
    156, 172, 188, 212, 240, 276, 320, 384,
    448, 512, 576, 640, 704, 768, 832, 896,
    960, 1024
};

        static short[] swb_offset_1024_64 = {
      0,   4,   8,  12,  16,  20,  24,  28,
     32,  36,  40,  44,  48,  52,  56,  64,
     72,  80,  88, 100, 112, 124, 140, 156,
    172, 192, 216, 240, 268, 304, 344, 384,
    424, 464, 504, 544, 584, 624, 664, 704,
    744, 784, 824, 864, 904, 944, 984, 1024
};
        static short[] swb_offset_1024_48 = {
      0,   4,   8,  12,  16,  20,  24,  28,
     32,  36,  40,  48,  56,  64,  72,  80,
     88,  96, 108, 120, 132, 144, 160, 176,
    196, 216, 240, 264, 292, 320, 352, 384,
    416, 448, 480, 512, 544, 576, 608, 640,
    672, 704, 736, 768, 800, 832, 864, 896,
    928, 1024
};

        static short[] swb_offset_1024_32 = {
      0,   4,   8,  12,  16,  20,  24,  28,
     32,  36,  40,  48,  56,  64,  72,  80,
     88,  96, 108, 120, 132, 144, 160, 176,
    196, 216, 240, 264, 292, 320, 352, 384,
    416, 448, 480, 512, 544, 576, 608, 640,
    672, 704, 736, 768, 800, 832, 864, 896,
    928, 960, 992, 1024
};

        static short[] swb_offset_1024_24 = {
      0,   4,   8,  12,  16,  20,  24,  28,
     32,  36,  40,  44,  52,  60,  68,  76,
     84,  92, 100, 108, 116, 124, 136, 148,
    160, 172, 188, 204, 220, 240, 260, 284,
    308, 336, 364, 396, 432, 468, 508, 552,
    600, 652, 704, 768, 832, 896, 960, 1024
};

        static short[] swb_offset_1024_16 = {
      0,   8,  16,  24,  32,  40,  48,  56,
     64,  72,  80,  88, 100, 112, 124, 136,
    148, 160, 172, 184, 196, 212, 228, 244,
    260, 280, 300, 320, 344, 368, 396, 424,
    456, 492, 532, 572, 616, 664, 716, 772,
    832, 896, 960, 1024
};
        static short[] swb_offset_1024_8 = {
      0,  12,  24,  36,  48,  60,  72,  84,
     96, 108, 120, 132, 144, 156, 172, 188,
    204, 220, 236, 252, 268, 288, 308, 328,
    348, 372, 396, 420, 448, 476, 508, 544,
    580, 620, 664, 712, 764, 820, 880, 944,
    1024
};

        static short[][] ff_swb_offset_1024 = {
            swb_offset_1024_96, swb_offset_1024_96, swb_offset_1024_64,
            swb_offset_1024_48, swb_offset_1024_48, swb_offset_1024_32,
            swb_offset_1024_24, swb_offset_1024_24, swb_offset_1024_16,
            swb_offset_1024_16, swb_offset_1024_16, swb_offset_1024_8,
            swb_offset_1024_8
         };

        static float[] tns_tmp2_map_1_3 = {
    0.00000000f, -0.43388373f,  0.64278758f,  0.34202015f,
};

        static float[] tns_tmp2_map_0_3 = {
    0.00000000f, -0.43388373f, -0.78183150f, -0.97492790f,
    0.98480773f, 0.86602539f, 0.64278758f, 0.34202015f,
};

        static float[] tns_tmp2_map_1_4 = {
    0.00000000f, -0.20791170f, 0.40673664f, -0.58778524f,
    0.67369562f,  0.52643216f, 0.36124167f, 0.18374951f,
};
        static float[] tns_tmp2_map_0_4 = {
     0.00000000f, -0.20791170f, -0.40673664f, -0.58778524f,
    -0.74314481f, -0.86602539f, -0.95105654f, -0.99452192f,
    0.99573416f,  0.96182561f, 0.89516330f, 0.79801720f,
    0.67369562f, 0.52643216f, 0.36124167f, 0.18374951f,
};
        static float[][] tns_tmp2_map =
        {
                tns_tmp2_map_0_3,
    tns_tmp2_map_0_4,
    tns_tmp2_map_1_3,
    tns_tmp2_map_1_4

        };

    }

    class Pulse
    {
        public int num_pulse;
        public int start;
        public int[] pos = new int[4];
        public int[] amp = new int[4];
    }

    class ChannelElement
    {
        public int present;
        public int common_window;
        public int ms_mode;

        public byte is_mode;
        public byte[] ms_mask = new byte[128];
        public byte[] is_mask = new byte[128];

        public SingleChannelElement[] ch = new SingleChannelElement[2];
        public ChannelCoupling coup;
        public SpectralBandReplication sbr;

        public ChannelElement()
        {
            for (int i = 0; i < 2; i++)
                ch[i] = new SingleChannelElement();
        }

    }

    class SingleChannelElement
    {
        const int MAX_PREDICTORS = 672;
        public IndividualChannelStream ics = new();
        public TemporalNoiseShaping tns = new();
        public Pulse pulse = new();
        public BandType[] band_type = new BandType[128];                   ///< band types
        public int[] band_type_run_end = new int[120];                     ///< band type run end points
        public float[] sf = new float[120];                               ///< scalefactors
        public byte[] can_pns = new byte[128];                           ///< band is allowed to PNS (informative)
        public float[] pcoeffs = new float[1024];   ///< coefficients for IMDCT, pristine
        public float[] coeffs = new float[1024];    ///< coefficients for IMDCT, maybe processed
        public float[] saved = new float[1536];     ///< overlap
        public float[] ret_buf = new float[2048];   ///< PCM output buffer
        public float[] ltp_state = new float[3072]; ///< time signal for LTP

        public PredictorState[] predictor_state = new PredictorState[MAX_PREDICTORS];
        public float[] ret;                                  ///< PCM output
    }
    class ChannelCoupling
    {

    }
    class SpectralBandReplication
    {

    }
    class PredictorState
    {
        public float cor0;
        public float cor1;
        public float var0;
        public float var1;
        public float r0;
        public float r1;
        public float k1;
        public float x_est;
    }

    class TemporalNoiseShaping
    {
        const int TNS_MAX_ORDER = 20;
        public int present;
        public int[] n_filt = new int[8];
        public int[][] length = new int[8][];
        public int[][] direction = new int[8][];
        public int[][] order = new int[8][];
        public int[][][] coef_idx = new int[8][][];
        public float[][][] coef = new float[8][][];

        public TemporalNoiseShaping()
        {
            for (int i = 0; i < 8; i++)
            {
                length[i] = new int[4];
                direction[i] = new int[4];
                order[i] = new int[4];
                coef_idx[i] = new int[4][];
                coef[i] = new float[4][];

                for (int j = 0; j < 4; j++)
                {
                    coef_idx[i][j] = new int[TNS_MAX_ORDER];
                    coef[i][j] = new float[TNS_MAX_ORDER];
                }
            }
        }
    }

    class IndividualChannelStream
    {
        public byte max_sfb;            ///< number of scalefactor bands per group
        public WindowSequence[] window_sequence = new WindowSequence[2];
        public byte[] use_kb_window = new byte[2];   ///< If set, use Kaiser-Bessel window, otherwise use a sine window.
        public int num_window_groups;
        public byte[] group_len = new byte[8];
        public LongTermPrediction ltp;
        public short[] swb_offset; ///< table of offsets to the lowest spectral coefficient of a scalefactor band, sfb, for a particular window
        public byte[] swb_sizes;   ///< table of scalefactor band sizes for a particular window
        public int num_swb;                ///< number of scalefactor window bands
        public int num_windows;
        public int tns_max_bands;
        public int predictor_present;
        public int predictor_initialized;
        public int predictor_reset_group;
        public int[] predictor_reset_count = new int[31];  ///< used by encoder to count prediction resets
        public byte[] prediction_used = new byte[41];
        public byte[] window_clipping = new byte[8]; ///< set if a certain window is near clipping
        public float clip_avoidance_factor; ///< set if any window is near clipping to the necessary atennuation factor to avoid it
    }

    class LongTermPrediction
    {
        const int MAX_LTP_LONG_SFB = 40;
        public byte present;
        public short lag;
        public int coef_idx;
        public float coef;
        public byte[] used = new byte[MAX_LTP_LONG_SFB];
    }
    enum WindowSequence
    {
        ONLY_LONG_SEQUENCE,
        LONG_START_SEQUENCE,
        EIGHT_SHORT_SEQUENCE,
        LONG_STOP_SEQUENCE,
    }
    enum RawDataBlockType : int
    {
        TYPE_SCE = 0,
        TYPE_CPE = 1,
        TYPE_CCE = 2,
        TYPE_LFE = 3,
        TYPE_DSE = 4,
        TYPE_PCE = 5,
        TYPE_FIL = 6,
        TYPE_END = 7,
    }
    enum BandType
    {
        ZERO_BT = 0,     ///< Scalefactors and spectral data are all zero.
        FIRST_PAIR_BT = 5,     ///< This and later band types encode two values (rather than four) with one code word.
        ESC_BT = 11,    ///< Spectral data are coded with an escape sequence.
        RESERVED_BT = 12,    ///< Band types following are encoded differently from others.
        NOISE_BT = 13,    ///< Spectral data are scaled white noise not coded in the bitstream.
        INTENSITY_BT2 = 14,    ///< Scalefactor data are intensity stereo positions (out of phase).
        INTENSITY_BT = 15,    ///< Scalefactor data are intensity stereo positions (in phase).
    };

    class BitStream 
    {
        private int bit_position = 0;
        private byte[] buffer;

        private int get_bit()
        {
            int byte_position = bit_position / 8;

            int bit = bit_position % 8;
            bit_position++;

            byte currentByte = buffer[byte_position];

            var bitValue = (currentByte >> (7 - bit)) & 1;


            return bitValue;
        }
        public int get_bits_count() => bit_position;
        public int get_bits_left() => buffer.Length * 8 - get_bits_count();

        public int get_bits(int count)
        {
            int bitValue = 0;
            for (int i = 0; i < count;i++)
                bitValue = (bitValue << 1) | get_bit();

            return bitValue;
        }
        public BitStream(byte[] data)
        {
            buffer = data;
        }
    }
}
