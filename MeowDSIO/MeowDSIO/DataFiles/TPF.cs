using MeowDSIO.DataTypes.TPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace MeowDSIO.DataFiles
{
    public class TPF : DataFile, IList<TPFEntry>
    {
        public List<TPFEntry> Entries { get; set; } = new List<TPFEntry>();
        public uint Flags { get; set; } = 0x00020300;
        public bool IsBigEndian { get; set; } = false;

        protected override void Read(DSBinaryReader bin, IProgress<(int, int)> prog)
        {
            bin.BigEndian = IsBigEndian = false;

            string signature = bin.ReadStringAscii(3);
            if (signature != "TPF")
                throw new Exception($"Invalid TPF file signature: '{signature}'");

            bin.ReadByte(); // signature terminator char

            int dataSize = bin.ReadInt32();
            int textureCount = bin.ReadInt32();

            if (textureCount >= 0x1000000)
            {
                bin.BigEndian = IsBigEndian = true;
                bin.Position = 4;
                dataSize = bin.ReadInt32();
                textureCount = bin.ReadInt32();
            }

            Flags = bin.ReadUInt32();

            if (Entries.Count > 0)
            {
                foreach (var e in Entries)
                    e.Dispose();
            }

            Entries = new List<TPFEntry>();

            var entryBuffer = new TPFEntryReadBuffer()
            {
                TpfFlags = Flags,
            };

            for (int i = 0; i < textureCount; i++)
            {
                Entries.Add(entryBuffer.ReadNext(bin));
            }
        }

        protected override void Write(DSBinaryWriter bin, IProgress<(int, int)> prog)
        {
            bin.WriteStringAscii("TPF", true);

            bin.Placeholder("DataSize");
            bin.Write(Entries.Count);
            bin.Write(Flags);

            for (int i = 0; i < Entries.Count; i++)
            {
                bin.Placeholder($"Entries[{i}].Offset");
                bin.Write(Entries[i].Size);
                bin.Write(Entries[i].FlagsA);

                if (Flags == 0x00020300) //Dark Souls
                {
                    bin.Placeholder($"Entries[{i}].NameOffset");
                    bin.Write(Entries[i].FlagsB);
                }
                else if (Flags == 0x02010200 || Flags == 0x02010000) //Demon's Souls
                {
                    bin.Write(Entries[i].FlagsB);
                    bin.Placeholder($"Entries[{i}].NameOffset");
                }
            }

            //No padding before the names?!

            for (int i = 0; i < Entries.Count; i++)
            {
                bin.Replace($"Entries[{i}].NameOffset", (int)bin.Position);
                bin.WriteStringShiftJIS(Entries[i].Name, terminate: true);
            }

            var LOC_DataStart = bin.Position;

            //An extremely specific amount of padding between the names and the data?!
            bin.Write(new byte[Entries.Count * 0x10]);

            for (int i = 0; i < Entries.Count; i++)
            {
                bin.Replace($"Entries[{i}].Offset", (int)bin.Position);
                bin.Write(Entries[i].DDSBytes);

                //No padding between entries?!
            }

            //No padding at the end of the file?!


            //Data Size = (Current Offset) - (Offset Where Data Started)
            bin.Replace("DataSize", (int)(bin.Position - LOC_DataStart));
        }

        #region IList
        public int IndexOf(TPFEntry item)
        {
            return ((IList<TPFEntry>)Entries).IndexOf(item);
        }

        public void Insert(int index, TPFEntry item)
        {
            ((IList<TPFEntry>)Entries).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<TPFEntry>)Entries).RemoveAt(index);
        }

        public TPFEntry this[int index] { get => ((IList<TPFEntry>)Entries)[index]; set => ((IList<TPFEntry>)Entries)[index] = value; }

        public void Add(TPFEntry item)
        {
            ((IList<TPFEntry>)Entries).Add(item);
        }

        public void Clear()
        {
            ((IList<TPFEntry>)Entries).Clear();
        }

        public bool Contains(TPFEntry item)
        {
            return ((IList<TPFEntry>)Entries).Contains(item);
        }

        public void CopyTo(TPFEntry[] array, int arrayIndex)
        {
            ((IList<TPFEntry>)Entries).CopyTo(array, arrayIndex);
        }

        public bool Remove(TPFEntry item)
        {
            return ((IList<TPFEntry>)Entries).Remove(item);
        }

        public int Count => ((IList<TPFEntry>)Entries).Count;

        public bool IsReadOnly => ((IList<TPFEntry>)Entries).IsReadOnly;

        public IEnumerator<TPFEntry> GetEnumerator()
        {
            return ((IList<TPFEntry>)Entries).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<TPFEntry>)Entries).GetEnumerator();
        }
        #endregion
    }
}
