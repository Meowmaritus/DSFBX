using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.BND
{
    public struct BNDEntryHeaderBuffer
    {
        public int FileSize;
        public int FileOffset;
        public int FileID;
        public int FileNameOffset;
        public int? Unknown1;

        public void Reset()
        {
            FileSize = -1;
            FileOffset = -1;
            FileID = -1;
            FileNameOffset = -1;
            Unknown1 = null;
        }

        public BNDEntry GetEntry(DSBinaryReader bin)
        {
            if (FileOffset < 0 || FileOffset > bin.Length)
            {
                throw new Exception("Invalid BND3 Entry File Offset.");
            }

            bin.StepIn(FileOffset);
            var bytes = bin.ReadBytes(FileSize);
            bin.StepOut();

            string fileName = null;

            if (FileNameOffset > -1)
            {
                bin.StepIn(FileNameOffset);
                {
                    fileName = bin.ReadStringShiftJIS();
                }
                bin.StepOut();
            }

            return new BNDEntry(FileID, fileName, Unknown1, bytes);
        }
    }
}
