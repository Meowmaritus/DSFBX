using MeowDSIO.DataTypes.BND;
using MeowDSIO.DataTypes.PARAMDEFBND;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace MeowDSIO.DataFiles
{
    public class PARAMDEFBND : DataFile, IList<PARAMDEFBNDEntry>
    {
        const string PARAMDEF_DIR = @"N:\FRPG\data\INTERROOT_win32\paramdef";

        public List<PARAMDEFBNDEntry> entries { get; set; } = new List<PARAMDEFBNDEntry>();
        private Dictionary<string, PARAMDEFBNDEntry> entryQuickLookup = new Dictionary<string, PARAMDEFBNDEntry>();

        public BNDHeader Header { get; set; } = new BNDHeader();

        public PARAMDEF this[string paramName]
        {
            get
            {
                if (entryQuickLookup.ContainsKey(paramName))
                {
                    return entryQuickLookup[paramName].ParamDef;
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
                    entryQuickLookup[paramName].ParamDef = value;
                }
                else
                {
                    Add(new PARAMDEFBNDEntry(paramName, value));
                }
            }
        }

        protected override void Read(DSBinaryReader bin, IProgress<(int, int)> prog)
        {
            var bnd = bin.ReadAsDataFile<BND>();

            Header = bnd.Header;

            entries.Clear();

            foreach (var bndEntry in bnd)
            {
                var paramDef = bndEntry.ReadDataAs<PARAMDEF>();
                entries.Add(new PARAMDEFBNDEntry(paramDef.ID, paramDef));
            }
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
                    $@"{PARAMDEF_DIR}\{entry.Name}.paramdef",
                    null, DataFile.SaveAsBytes(entry.ParamDef, entry.Name)));
            }

            bin.WriteDataFile(bnd, bin.FileName);
        }

        private void entry_NameChanged(object sender, string oldName, string newName)
        {
            var entry = sender as PARAMDEFBNDEntry;

            if (!entryQuickLookup.ContainsKey(newName))
            {
                entryQuickLookup.Remove(oldName);
                entryQuickLookup.Add(newName, entry);
            }
            else
            {
                throw new InvalidOperationException($"A {nameof(PARAMDEFBNDEntry)} already " +
                    $"exists in this {nameof(PARAMDEFBND)} with the name '{newName}'.");
            }
        }

        public int IndexOf(PARAMDEFBNDEntry item)
        {
            return ((IList<PARAMDEFBNDEntry>)entries).IndexOf(item);
        }

        public void Insert(int index, PARAMDEFBNDEntry item)
        {
            if (!Contains(item))
            {
                if (!entryQuickLookup.ContainsKey(item.Name))
                {
                    entryQuickLookup.Add(item.Name, item);
                }
                else
                {
                    throw new InvalidOperationException($"A {nameof(PARAMDEFBNDEntry)} already " +
                        $"exists in this {nameof(PARAMDEFBND)} with the name '{item.Name}'.");
                }

                item.NameChanged += entry_NameChanged;
            }

            ((IList<PARAMDEFBNDEntry>)entries).Insert(index, item);
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

            ((IList<PARAMDEFBNDEntry>)entries).RemoveAt(index);
        }

        public PARAMDEFBNDEntry this[int index]
        {
            get => ((IList<PARAMDEFBNDEntry>)entries)[index];
            set
            {
                if (!entryQuickLookup.ContainsKey(value.Name))
                {
                    ((IList<PARAMDEFBNDEntry>)entries)[index].NameChanged -= entry_NameChanged;

                    if (entryQuickLookup.ContainsKey(((IList<PARAMDEFBNDEntry>)entries)[index].Name))
                    {
                        entryQuickLookup.Remove(((IList<PARAMDEFBNDEntry>)entries)[index].Name);
                    }

                    ((IList<PARAMDEFBNDEntry>)entries)[index] = value;

                    entryQuickLookup.Add(value.Name, value);

                    value.NameChanged += entry_NameChanged;
                }
                else if (((IList<PARAMDEFBNDEntry>)entries)[index].Name == value.Name)
                {
                    ((IList<PARAMDEFBNDEntry>)entries)[index].NameChanged -= entry_NameChanged;

                    ((IList<PARAMDEFBNDEntry>)entries)[index] = value;

                    entryQuickLookup[value.Name] = value;
                }
                else
                {
                    throw new InvalidOperationException($"A {nameof(PARAMDEFBNDEntry)} already " +
                        $"exists in this {nameof(PARAMDEFBND)} with the name '{value.Name}'.");
                }
            }
        }

        public void Add(PARAMDEFBNDEntry item)
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
                    throw new InvalidOperationException($"A {nameof(PARAMDEFBNDEntry)} already " +
                        $"exists in this {nameof(PARAMDEFBND)} with the name '{item.Name}'.");
                }
            }

            ((IList<PARAMDEFBNDEntry>)entries).Add(item);
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

            ((IList<PARAMDEFBNDEntry>)entries).Clear();
        }

        public bool Contains(PARAMDEFBNDEntry item)
        {
            return ((IList<PARAMDEFBNDEntry>)entries).Contains(item);
        }

        public void CopyTo(PARAMDEFBNDEntry[] array, int arrayIndex)
        {
            ((IList<PARAMDEFBNDEntry>)entries).CopyTo(array, arrayIndex);
        }

        public bool Remove(PARAMDEFBNDEntry item)
        {
            bool removed = ((IList<PARAMDEFBNDEntry>)entries).Remove(item);
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

        public int Count => ((IList<PARAMDEFBNDEntry>)entries).Count;

        public bool IsReadOnly => ((IList<PARAMDEFBNDEntry>)entries).IsReadOnly;

        public IEnumerator<PARAMDEFBNDEntry> GetEnumerator()
        {
            return ((IList<PARAMDEFBNDEntry>)entries).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<PARAMDEFBNDEntry>)entries).GetEnumerator();
        }
    }
}
