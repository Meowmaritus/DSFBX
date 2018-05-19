using MeowDSIO.DataTypes.PARAMDEF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace MeowDSIO.DataFiles
{
    public class PARAMDEF : DataFile, IList<ParamDefEntry>
    {
        public string ID { get; set; } = null;
        public ushort Unknown1 { get; set; } = 1;
        public ushort Unknown2 { get; set; } = 0;
        public List<ParamDefEntry> Entries { get; set; } = new List<ParamDefEntry>();

        public const ushort ENTRY_LENGTH = 0x00B0;
        public const ushort DESC_OFFSET_OFFSET = 0x0068;

        public ParamDefEntry GetEntry(string entryName)
        {
            try
            {
                return Entries.Where(x => x.Name == entryName).First();
            }
            catch
            {
                return null;
            }
        }

        //public int CalculateEntrySize()
        //{
        //    int totalSize = 0;
        //    int bitField = 0;

        //    foreach (var e in Entries)
        //    {
        //        //bool
        //        if (e.ValueBitCount == 1)
        //        {
        //            bitField++;
        //            if (bitField == 8)
        //            {
        //                bitField = 0;
        //                totalSize++;
        //            }
        //        }
        //        else
        //        {
        //            if (e.InternalValueType == ParamTypeDef.dummy8)
        //            {
        //                if (bitField > 0)
        //                {
        //                    bitField = 0;
        //                    totalSize++;
        //                }
        //            }
        //            else if (e.InternalValueType == ParamTypeDef.u8)
        //            {
        //                if (e.ValueBitCount != 8)
        //                {
        //                    for (int i = 0; i < e.ValueBitCount; i++)
        //                    {
        //                        bitField++;
        //                        if (bitField == 8)
        //                        {
        //                            bitField = 0;
        //                            totalSize++;
        //                        }
        //                    }
        //                }
        //            }

                    
        //        }

        //        totalSize += e.ValueBitCount / 8;
        //    }

        //    return (totalSize);
        //}

        protected override void Read(DSBinaryReader bin, IProgress<(int, int)> prog)
        {
            int length = bin.ReadInt32();

            if (length != bin.Length)
            {
                // If lengths don't match, try opposite endianness.
                bin.BigEndian = !bin.BigEndian;
                bin.Position = 0;
                length = bin.ReadInt32();

                if (length != bin.Length)
                {
                    throw new Exception("ParamDef File Length Value was incorrect on both little endian and big endian. File is not valid.");
                }
            }

            // Always 0x30
            var firstEntryOffset = bin.CheckConsumeValue("First Entry Offset", bin.ReadUInt16, (ushort)0x30);

            Unknown1 = bin.ReadUInt16();

            var entryCount = bin.ReadUInt16();

            bin.CheckConsumeValue("Entry length", bin.ReadUInt16, ENTRY_LENGTH);

            ID = bin.ReadPaddedStringShiftJIS(0x20, padding: null);

            Unknown2 = bin.CheckConsumeValue($"{nameof(Unknown2)} (HeaderTerminator?)", bin.ReadUInt16, (ushort)0);
            bin.CheckConsumeValue("Relative Offset To Offset Of Description", bin.ReadUInt16, DESC_OFFSET_OFFSET);

            var descriptionOffsets = new List<uint>();

            Entries.Clear();

            for (int i = 0; i < entryCount; i++)
            {
                var entry = new ParamDefEntry();

                try
                {
                    entry.DisplayName = bin.ReadPaddedStringShiftJIS(0x40, padding: null);
                }
                catch (Exception e)
                {
                    throw e;
                }

                string _guiValueType_str = bin.ReadPaddedStringShiftJIS(0x8, padding: null);
                if (!Enum.TryParse(_guiValueType_str, out ParamTypeDef guiValueType))
                {
                    throw new Exception($"Invalid [{nameof(ParamTypeDef)} " +
                        $"{nameof(ParamDefEntry)}.{nameof(ParamDefEntry.GuiValueType)}] " +
                        $"value string found in PARAMDEF entry: '{_guiValueType_str}'.");
                }
                entry.GuiValueType = guiValueType;

                entry.GuiValueStringFormat = bin.ReadPaddedStringShiftJIS(0x8, padding: null);
                entry.DefaultValue = bin.ReadSingle();
                entry.Min = bin.ReadSingle();
                entry.Max = bin.ReadSingle();
                entry.Increment = bin.ReadSingle();
                entry.GuiValueDisplayMode = bin.ReadInt32();
                entry.GuiValueByteCount = bin.ReadInt32();

                uint descriptionOffset = bin.ReadUInt32();
                descriptionOffsets.Add(descriptionOffset);

                string _internalValueType_str = bin.ReadPaddedStringShiftJIS(0x20, padding: null);
                if (!Enum.TryParse(_internalValueType_str, out ParamTypeDef internalValueType))
                {
                    throw new Exception($"Invalid [{nameof(ParamTypeDef)} " +
                        $"{nameof(ParamDefEntry)}.{nameof(ParamDefEntry.InternalValueType)}] " +
                        $"value string found in PARAMDEF entry: '{_internalValueType_str}'.");
                }
                entry.InternalValueType = internalValueType;

                entry.Name = bin.ReadPaddedStringShiftJIS(0x20, padding: null);
                entry.ID = bin.ReadInt32();

                if (entry.Name.Contains(":"))
                {
                    entry.ValueBitCount = int.Parse(entry.Name.Split(':')[1]);

                    //if (entry.ValueBitCount > 1)
                    //{
                    //    entry.InternalValueType = ParamTypeDef.u8;
                    //}
                }
                else if (entry.InternalValueType == ParamTypeDef.dummy8)
                {
                    var lbrack = entry.Name.LastIndexOf("[");

                    if (lbrack == -1)
                    {
                        entry.ValueBitCount = 8;
                    }
                    else
                    {
                        var rbrack = entry.Name.LastIndexOf("]");

                        var padSizeStr = entry.Name.Substring(lbrack + 1, rbrack - lbrack - 1);

                        entry.ValueBitCount = int.Parse(padSizeStr) * 8;
                    }

                }
                else
                {
                    entry.ValueBitCount = entry.GuiValueByteCount * 8;

                }

                Entries.Add(entry);

                prog?.Report((i, entryCount * 2));
            }

            for (int i = 0; i < entryCount; i++)
            {
                bin.Position = descriptionOffsets[i];
                Entries[i].Description = bin.ReadStringShiftJIS();

                prog?.Report((entryCount + i, entryCount * 2));
            }

        }

        protected override void Write(DSBinaryWriter bin, IProgress<(int, int)> prog)
        {
            // Placeholder - file length
            bin.Placeholder();
            // First entry offset
            bin.Write((ushort)0x30);
            bin.Write(Unknown1);
            bin.Write((ushort)Entries.Count);
            // Entry length
            bin.Write(ENTRY_LENGTH);
            bin.WritePaddedStringShiftJIS(ID, 0x20, padding: 0x20);
            bin.Write(Unknown2);
            // The offset relative to each entry's start that points to that entry's description offset value
            bin.Write(DESC_OFFSET_OFFSET);

            var descriptionOffsets = new List<uint>();

            var OFF_Entries = bin.Position;

            bin.Position += (Entries.Count * ENTRY_LENGTH);

            for (int i = 0; i < Entries.Count; i++)
            {
                descriptionOffsets.Add((uint)bin.Position);
                bin.WriteStringShiftJIS(Entries[i].Description, true);

                prog?.Report((i, Entries.Count * 2));
            }

            // Fill in length real quick
            bin.Position = 0;
            bin.Write((uint)bin.Length);

            bin.Position = OFF_Entries;

            for (int i = 0; i < Entries.Count; i++)
            {
                bin.WritePaddedStringShiftJIS(Entries[i].DisplayName, 0x40, padding: null);
                bin.WritePaddedStringShiftJIS(Entries[i].GuiValueType.ToString(), 0x8, padding: 0x20);
                bin.WritePaddedStringShiftJIS(Entries[i].GuiValueStringFormat, 0x8, padding: 0x20);
                bin.Write(Entries[i].DefaultValue);
                bin.Write(Entries[i].Min);
                bin.Write(Entries[i].Max);
                bin.Write(Entries[i].Increment);
                bin.Write(Entries[i].GuiValueDisplayMode);
                bin.Write(Entries[i].GuiValueByteCount);
                bin.Write(descriptionOffsets[i]);
                bin.WritePaddedStringShiftJIS(Entries[i].InternalValueType.ToString(), 0x20, padding: 0x20);
                bin.WritePaddedStringShiftJIS(Entries[i].Name, 0x20, padding: 0x20);
                bin.Write(Entries[i].ID);

                prog?.Report((Entries.Count + i, Entries.Count * 2));
            }
        }

        public int IndexOf(ParamDefEntry item)
        {
            return ((IList<ParamDefEntry>)Entries).IndexOf(item);
        }

        public void Insert(int index, ParamDefEntry item)
        {
            ((IList<ParamDefEntry>)Entries).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            ((IList<ParamDefEntry>)Entries).RemoveAt(index);
        }

        public ParamDefEntry this[int index] { get => ((IList<ParamDefEntry>)Entries)[index]; set => ((IList<ParamDefEntry>)Entries)[index] = value; }

        public void Add(ParamDefEntry item)
        {
            ((IList<ParamDefEntry>)Entries).Add(item);
        }

        public void Clear()
        {
            ((IList<ParamDefEntry>)Entries).Clear();
        }

        public bool Contains(ParamDefEntry item)
        {
            return ((IList<ParamDefEntry>)Entries).Contains(item);
        }

        public void CopyTo(ParamDefEntry[] array, int arrayIndex)
        {
            ((IList<ParamDefEntry>)Entries).CopyTo(array, arrayIndex);
        }

        public bool Remove(ParamDefEntry item)
        {
            return ((IList<ParamDefEntry>)Entries).Remove(item);
        }

        public int Count => ((IList<ParamDefEntry>)Entries).Count;

        public bool IsReadOnly => ((IList<ParamDefEntry>)Entries).IsReadOnly;

        public IEnumerator<ParamDefEntry> GetEnumerator()
        {
            return ((IList<ParamDefEntry>)Entries).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<ParamDefEntry>)Entries).GetEnumerator();
        }
    }
}
