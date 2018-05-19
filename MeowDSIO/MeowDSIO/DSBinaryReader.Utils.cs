using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO
{
    public partial class DSBinaryReader : BinaryReader
    {
        private static Encoding ShiftJISEncoding = Encoding.GetEncoding("shift_jis");

        // Now with 100% less 0DD0ADDE
        public static readonly byte[] PLACEHOLDER_32BIT = new byte[] { 0xDE, 0xAD, 0xD0, 0x0D };

        public string FileName { get; private set; }

        public long Position { get => BaseStream.Position; set => BaseStream.Position = value; }
        public long Length => BaseStream.Length;
        public void Goto(long absoluteOffset) => BaseStream.Seek(absoluteOffset, SeekOrigin.Begin);
        public void Jump(long relativeOffset) => BaseStream.Seek(relativeOffset, SeekOrigin.Current);
        private Stack<long> StepStack = new Stack<long>();
        private Stack<PaddedRegion> PaddedRegionStack = new Stack<PaddedRegion>();

        public bool BigEndian = false;

        public void StepIn(long offset)
        {
            StepStack.Push(Position);
            Goto(offset);
        }

        public void StepOut()
        {
            if (StepStack.Count == 0)
                throw new InvalidOperationException("You cannot step out unless StepIn() was previously called on an offset.");

            Goto(StepStack.Pop());
        }

        public void StepIntoPaddedRegion(long length, byte? padding)
        {
            PaddedRegionStack.Push(new PaddedRegion(Position, length, padding));
        }

        public void StepOutOfPaddedRegion()
        {
            if (PaddedRegionStack.Count == 0)
                throw new InvalidOperationException("You cannot step out of padded region unless inside of one.");

            var deepestPaddedRegion = PaddedRegionStack.Pop();
            deepestPaddedRegion.AdvanceReaderToEnd(this);
        }

        public void StepOutOfPaddedRegion(out byte foundPadding)
        {
            if (PaddedRegionStack.Count == 0)
                throw new InvalidOperationException("You cannot step out of padded region unless inside of one.");

            var deepestPaddedRegion = PaddedRegionStack.Pop();
            deepestPaddedRegion.AdvanceReaderToEnd(this, out foundPadding);
        }

        public void DoAt(long offset, Action doAction)
        {
            StepIn(offset);
            doAction();
            StepOut();
        }




    }
}
