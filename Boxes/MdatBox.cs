﻿using AAXClean.Chunks;
using System;
using System.IO;

namespace AAXClean.Boxes
{
    internal class MdatBox : Box
    {
        public MdatChunk FirstEntry { get; }
        internal MdatBox(Stream file, BoxHeader header, Box parent) : base(header, parent)
        {
            FirstEntry = MdatFactory.CreateEntry(file);

            //Normally we don't want to seek, but if moov is after mdat then we have to.
            if (Parent.GetChild<MoovBox>() is null)
                file.Position = header.FilePosition + header.TotalBoxSize;
        }

        protected override void Render(Stream file)
        {
            throw new NotImplementedException();
        }
    }
}
