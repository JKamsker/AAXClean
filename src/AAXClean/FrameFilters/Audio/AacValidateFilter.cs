using System;
using System.Threading.Tasks;

namespace AAXClean.FrameFilters.Audio
{
	internal class AacValidateFilter : FrameTransformBase<FrameEntry, FrameEntry>
	{
		protected override int InputBufferSize => 1000;
		public override async ValueTask<FrameEntry> PerformFiltering(FrameEntry input)
		{
			if (!await ValidateFrame(input.FrameData))
				throw new Exception("Aac error!");

			return input;
		}
		protected virtual ValueTask<bool> ValidateFrame(Memory<byte> frame) 
			=> new((AV_RB16(frame.Span) & 0xfff0) != 0xfff0);

		//Defined at
		//http://man.hubwiz.com/docset/FFmpeg.docset/Contents/Resources/Documents/api/intreadwrite_8h_source.html
		private static ushort AV_RB16(Span<byte> frame)
		{
			return (ushort)(frame[0] << 8 | frame[1]);
		}
	}
}
