using MeowDSIO.DataTypes.BND;
using MeowDSIO.DataTypes.PARAMBND;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataFiles
{
    public class PARAMBND : DataFile, IList<PARAMBNDEntry>
    {
        const string PARAM_DIR = @"N:\FRPG\data\INTERROOT_win32\param";

        private List<PARAMBNDEntry> entries { get; set; } = new List<PARAMBNDEntry>();
        private Dictionary<string, PARAMBNDEntry> entryQuickLookup = new Dictionary<string, PARAMBNDEntry>();

        public BNDHeader Header { get; set; } = new BNDHeader();

        public bool IsDrawParams { get; set; } = false;

        public PARAM this[string paramName]
        {
            get
            {
                if (entryQuickLookup.ContainsKey(paramName))
                {
                    return entryQuickLookup[paramName].Param;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (entryQuickLookup.ContainsKey(paramName))
                {
                    entryQuickLookup[paramName].Param = value;
                }
                else
                {
                    Add(new PARAMBNDEntry(paramName, value));
                }
            }
        }

        public void ApplyParamDefBND(PARAMDEFBND pdbnd)
        {
            foreach (var entry in this)
            {
                entry.Param.ApplyPARAMDEFTemplate(pdbnd[entry.Param.ID]);
            }
        }

        protected override void Read(DSBinaryReader bin, IProgress<(int, int)> prog)
        {
            var bnd = bin.ReadAsDataFile<BND>();

            Header = bnd.Header;

            entries.Clear();

            foreach (var bndEntry in bnd)
            {
                entries.Add(new PARAMBNDEntry(MiscUtil.GetFileNameWithoutDirectoryOrExtension(bndEntry.Name),
                    bndEntry.ReadDataAs<PARAM>()));
            }

            IsDrawParams = entries.Any(x => x.Name.Contains("DrawParam"));
        }

        protected override void Write(DSBinaryWriter bin, IProgress<(int, int)> prog)
        {
            var bnd = new BND()
            {
                Header = Header
            };

            int ID = 0;
            foreach (var entry in entries)
            {
                bnd.Add(new BNDEntry(ID++,
                    $@"{PARAM_DIR}\{(IsDrawParams ? "DrawParam" : "GameParam")}\{entry.Name}.param", 
                    null, DataFile.SaveAsBytes(entry.Param, entry.Name)));
            }

            bin.WriteDataFile(bnd, bin.FileName);
        }

        private void entry_NameChanged(object sender, string oldName, string newName)
        {
            var entry = sender as PARAMBNDEntry;

            if (!entryQuickLookup.ContainsKey(newName))
            {
                entryQuickLookup.Remove(oldName);
                entryQuickLookup.Add(newName, entry);
            }
            else
            {
                throw new InvalidOperationException($"A {nameof(PARAMBNDEntry)} already " +
                    $"exists in this {nameof(PARAMBND)} with the name '{newName}'.");
            }
        }

        public int IndexOf(PARAMBNDEntry item)
        {
            return ((IList<PARAMBNDEntry>)entries).IndexOf(item);
        }

        public void Insert(int index, PARAMBNDEntry item)
        {
            if (!Contains(item))
            {
                if (!entryQuickLookup.ContainsKey(item.Name))
                {
                    entryQuickLookup.Add(item.Name, item);
                }
                else
                {
                    throw new InvalidOperationException($"A {nameof(PARAMBNDEntry)} already " +
                        $"exists in this {nameof(PARAMBND)} with the name '{item.Name}'.");
                }

                item.NameChanged += entry_NameChanged;
            }

            ((IList<PARAMBNDEntry>)entries).Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            if (index >= 0 && index < Count)
            {
                if (entryQuickLookup.ContainsKey(this[index].Name))
                {
                    entryQuickLookup.Remove(this[index].Name);
                }

                this[index].NameChanged -= entry_NameChanged;
            }

            ((IList<PARAMBNDEntry>)entries).RemoveAt(index);
        }

        public PARAMBNDEntry this[int index]
        {
            get => ((IList<PARAMBNDEntry>)entries)[index];
            set
            {
                if (!entryQuickLookup.ContainsKey(value.Name))
                {
                    ((IList<PARAMBNDEntry>)entries)[index].NameChanged -= entry_NameChanged;

                    if (entryQuickLookup.ContainsKey(((IList<PARAMBNDEntry>)entries)[index].Name))
                    {
                        entryQuickLookup.Remove(((IList<PARAMBNDEntry>)entries)[index].Name);
                    }

                    ((IList<PARAMBNDEntry>)entries)[index] = value;

                    entryQuickLookup.Add(value.Name, value);

                    value.NameChanged += entry_NameChanged;
                }
                else if (((IList<PARAMBNDEntry>)entries)[index].Name == value.Name)
                {
                    ((IList<PARAMBNDEntry>)entries)[index].NameChanged -= entry_NameChanged;

                    ((IList<PARAMBNDEntry>)entries)[index] = value;

                    entryQuickLookup[value.Name] = value;
                }
                else
                {
                    throw new InvalidOperationException($"A {nameof(PARAMBNDEntry)} already " +
                        $"exists in this {nameof(PARAMBND)} with the name '{value.Name}'.");
                }
            }
        }

        public void Add(PARAMBNDEntry item)
        {
            if (!Contains(item))
            {
                item.NameChanged += entry_NameChanged;

                if (!entryQuickLookup.ContainsKey(item.Name))
                {
                    entryQuickLookup.Add(item.Name, item);
                }
                else
                {
                    throw new InvalidOperationException($"A {nameof(PARAMBNDEntry)} already " +
                        $"exists in this {nameof(PARAMBND)} with the name '{item.Name}'.");
                }
            }

            ((IList<PARAMBNDEntry>)entries).Add(item);
        }

        public void Clear()
        {
            foreach (var e in entries)
            {
                e.NameChanged -= entry_NameChanged;

                if (entryQuickLookup.ContainsKey(e.Name))
                {
                    entryQuickLookup.Remove(e.Name);
                }
            }

            ((IList<PARAMBNDEntry>)entries).Clear();
        }

        public bool Contains(PARAMBNDEntry item)
        {
            return ((IList<PARAMBNDEntry>)entries).Contains(item);
        }

        public void CopyTo(PARAMBNDEntry[] array, int arrayIndex)
        {
            ((IList<PARAMBNDEntry>)entries).CopyTo(array, arrayIndex);
        }

        public bool Remove(PARAMBNDEntry item)
        {
            bool removed = ((IList<PARAMBNDEntry>)entries).Remove(item);
            if (removed)
            {
                item.NameChanged -= entry_NameChanged;

                if (entryQuickLookup.ContainsKey(item.Name))
                {
                    entryQuickLookup.Remove(item.Name);
                }
            }
            return removed;
        }

        public int Count => ((IList<PARAMBNDEntry>)entries).Count;

        public bool IsReadOnly => ((IList<PARAMBNDEntry>)entries).IsReadOnly;

        public IEnumerator<PARAMBNDEntry> GetEnumerator()
        {
            return ((IList<PARAMBNDEntry>)entries).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<PARAMBNDEntry>)entries).GetEnumerator();
        }
    }
}
