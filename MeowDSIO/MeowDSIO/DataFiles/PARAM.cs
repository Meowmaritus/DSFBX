using MeowDSIO.DataTypes.PARAM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace MeowDSIO.DataFiles
{
    public class PARAM : DataFile, IList<ParamRow>
    {
        public string ID { get; set; } = null;
        public ObservableCollection<ParamRow> Entries { get; set; }
        public ushort Unknown1 { get; set; }
        public ushort Unknown2 { get; set; }
        public int Unknown3 { get; set; }

        public int EntrySize { get; set; } = -1;

        public PARAMDEF AppliedPARAMDEF { get; set; } = null;

        public void ApplyPARAMDEFTemplate(PARAMDEF def)
        {
            AppliedPARAMDEF = def;

            foreach (var e in Entries)
            {
                e.LoadValuesFromRawData(this);
                e.ClearRawData();
            }
        }

        protected override void Read(DSBinaryReader bin, IProgress<(int, int)> prog)
        {
            int stringsOffset = bin.ReadInt32();

            ushort firstRowDataOffset = bin.ReadUInt16();
            Unknown1 = bin.ReadUInt16();

            Unknown2 = bin.ReadUInt16();
            ushort rowCount = bin.ReadUInt16();

            byte namePad = 0;
            ID = bin.ReadPaddedStringShiftJIS(0x20, padding: null);

            Unknown3 = bin.ReadInt32();

            var nameOffsetList = new List<uint>();
            var dataOffsetList = new List<uint>();

            EntrySize = 0;

            Entries = new ObservableCollection<ParamRow>();

            for (int i = 0; i < rowCount; i++)
            {
                prog?.Report((i, rowCount * 3));

                var newEntry = new ParamRow();
                newEntry.ID = bin.ReadInt32();
                dataOffsetList.Add(bin.ReadUInt32());
                nameOffsetList.Add(bin.ReadUInt32());

                Entries.Add(newEntry);

                if (i > 0 && EntrySize == 0)
                {
                    EntrySize = (int)(dataOffsetList[i] - dataOffsetList[i - 1]);
                }
            }

            for (int i = 0; i < rowCount; i++)
            {
                prog?.Report((i + rowCount, rowCount * 3));

                bin.Position = dataOffsetList[i];
                Entries[i].RawData = bin.ReadBytes(EntrySize);
            }

            for (int i = 0; i < rowCount; i++)
            {
                prog?.Report((i + (rowCount * 2), rowCount * 3));

                bin.Position = nameOffsetList[i];
                Entries[i].Name = bin.ReadStringShiftJIS();
            }

            bin.Position = bin.Length;
        }

        protected override void Write(DSBinaryWriter bin, IProgress<(int, int)> prog)
        {
            if (AppliedPARAMDEF != null)
            {
                foreach (var e in Entries)
                {
                    e.ReInitRawData(this);
                    e.SaveValuesToRawData(this);
                }
            }

            //Placeholder - Strings start offset
            bin.Placeholder();

            //Placeholder - Data start offset
            bin.Write((ushort)0xDEAD);

            bin.Write(Unknown1);
            bin.Write(Unknown2);
            bin.Write((ushort)Entries.Count);
            bin.WritePaddedStringShiftJIS(ID, 0x20, null);
            bin.Write(Unknown3);

            var OFF_RowHeaders = bin.Position;

            //SKIP row headers and go right to data.
            //Row headers are filled in last because offsets.
            bin.Position += (Entries.Count * 0xC);

            var dataOffsets = new List<uint>();

            //var entrySize = AppliedPARAMDEF.CalculateEntrySize();

            for (int i = 0; i < Entries.Count; i++)
            {
                prog?.Report((i, Entries.Count * 3));

                dataOffsets.Add((uint)bin.Position);
                bin.Write(Entries[i].RawData, EntrySize);
            }

            var nameOffsets = new List<uint>();

            for (int i = 0; i < Entries.Count; i++)
            {
                prog?.Report((Entries.Count + i, Entries.Count * 3));

                nameOffsets.Add((uint)bin.Position);
                bin.WriteStringShiftJIS(Entries[i].Name, terminate: true);
            }

            // Fill in the first 2 offsets in the file real quick:

            bin.Position = 0;

            bin.Write(nameOffsets[0]);
            bin.Write((ushort)dataOffsets[0]);

            // Finally fill in the row headers:

            bin.Position = OFF_RowHeaders;

            for (int i = 0; i < Entries.Count; i++)
            {
                prog?.Report((i + (Entries.Count * 2), Entries.Count * 3));

                bin.Write(Entries[i].ID);
                bin.Write(dataOffsets[i]);
                bin.Write(nameOffsets[i]);
            }

            bin.Position = bin.Length;

            //bin.Pad(0x14);
        }

        public int IndexOf(ParamRow item)
        {
            return ((IList<ParamRow>)Entries).IndexOf(item);
        }

        public void Insert(int index, ParamRow item)
        {
            ((IList<ParamRow>)Entries).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<ParamRow>)Entries).RemoveAt(index);
        }

        public ParamRow this[int index] { get => ((IList<ParamRow>)Entries)[index]; set => ((IList<ParamRow>)Entries)[index] = value; }

        public void Add(ParamRow item)
        {
            ((IList<ParamRow>)Entries).Add(item);
        }

        public void Clear()
        {
            ((IList<ParamRow>)Entries).Clear();
        }

        public bool Contains(ParamRow item)
        {
            return ((IList<ParamRow>)Entries).Contains(item);
        }

        public void CopyTo(ParamRow[] array, int arrayIndex)
        {
            ((IList<ParamRow>)Entries).CopyTo(array, arrayIndex);
        }

        public bool Remove(ParamRow item)
        {
            return ((IList<ParamRow>)Entries).Remove(item);
        }

        public int Count => ((IList<ParamRow>)Entries).Count;

        public bool IsReadOnly => ((IList<ParamRow>)Entries).IsReadOnly;

        public IEnumerator<ParamRow> GetEnumerator()
        {
            return ((IList<ParamRow>)Entries).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<ParamRow>)Entries).GetEnumerator();
        }
    }
}
