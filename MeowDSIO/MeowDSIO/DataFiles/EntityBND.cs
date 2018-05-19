using MeowDSIO.DataTypes.BND;
using MeowDSIO.DataTypes.EntityBND;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataFiles
{
    public class EntityBND : DataFile
    {
        public BNDHeader Header { get; set; } = new BNDHeader();
        public List<EntityModel> Models { get; set; } = new List<EntityModel>();

        static readonly char[] pathSep = new char[] { '\\', '/' };

        public const int ID_TPF_START = 100;
        public const int ID_FLVER_START = 200;
        public const int ID_BodyHKX_START = 300;
        public const int ID_ANIBND_START = 400;
        public const int ID_HKXPWV_START = 500;
        public const int ID_BSIPWV_START = 600;
        public const int ID_ClothHKX_START = 700;
        public const int ID_CHRTPFBHD_START = 800;

        public const int ID_TPF_END = 199;
        public const int ID_FLVER_END = 299;
        public const int ID_BodyHKX_END = 399;
        public const int ID_ANIBND_END = 499;
        public const int ID_HKXPWV_END = 599;
        public const int ID_BSIPWV_END = 699;
        public const int ID_ClothHKX_END = 799;
        public const int ID_CHRTPFBHD_END = 899;

        protected override void Read(DSBinaryReader bin, IProgress<(int, int)> prog)
        {
            var bnd = bin.ReadAsDataFile<BND>(FilePath ?? VirtualUri);

            Header = bnd.Header;

            var sortedBndEntries = bnd.Entries.OrderBy(x => x.ID);

            Models = new List<EntityModel>();

            foreach (var entry in sortedBndEntries)
            {
                int modelIdx = entry.ID % 100;

                if (Models.Count <= modelIdx)
                {
                    Models.Add(new EntityModel());
                }

                if (entry.ID >= ID_TPF_START && entry.ID <= ID_TPF_END)
                {
                    var tpf = entry.ReadDataAs<TPF>();
                    foreach (var tex in tpf)
                    {
                        Models[modelIdx].Textures.Add(tex.Name, tex.GetBytes());
                    }
                }
                else if (entry.ID >= ID_FLVER_START && entry.ID <= ID_FLVER_END)
                {
                    Models[modelIdx].Mesh = entry.ReadDataAs<FLVER>();
                }
                else if (entry.ID >= ID_BodyHKX_START && entry.ID <= ID_BodyHKX_END)
                {
                    Models[modelIdx].BodyHKX = entry.GetBytes();
                }
                else if (entry.ID >= ID_ANIBND_START && entry.ID <= ID_ANIBND_END)
                {
                    //Models[modelIdx].AnimContainer = entry.ReadDataAs<ANIBND>();
                    Models[modelIdx].AnimContainer = entry.ReadDataAs<BND>();

                }
                else if (entry.ID >= ID_HKXPWV_START && entry.ID <= ID_HKXPWV_END)
                {
                    Models[modelIdx].BodyHKX = entry.GetBytes();
                }
                else if (entry.ID >= ID_BSIPWV_START && entry.ID <= ID_BSIPWV_END)
                {
                    Models[modelIdx].BodyHKX = entry.GetBytes();
                }
                else if (entry.ID >= ID_ClothHKX_START && entry.ID <= ID_ClothHKX_END)
                {
                    Models[modelIdx].BodyHKX = entry.GetBytes();
                }
                else if (entry.ID >= ID_CHRTPFBHD_START && entry.ID <= ID_CHRTPFBHD_END)
                {
                    Models[modelIdx].BodyHKX = entry.GetBytes();
                }
                else
                {
                    throw new Exception($"Invalid Entity BND File ID: {entry.ID}");
                }
            }
        }

        protected override void Write(DSBinaryWriter bin, IProgress<(int, int)> prog)
        {
            string ShortName = (FilePath ?? VirtualUri);
            ShortName = ShortName.Substring(ShortName.LastIndexOfAny(pathSep) + 1);
            ShortName = ShortName.Substring(0, ShortName.IndexOf('.'));

            var BND = new BND()
            {
                Header = Header
            };

            int ID = ID_TPF_START;

            string idx() => (ID % 100) > 0 ? $"_{(ID % 100)}" : "";

            foreach (var mdl in Models)
            {
                
                if (mdl.Textures.Count > 0)
                {
                    var tpf = new TPF();
                    foreach (var tex in mdl.Textures)
                    {
                        tpf.Add(new DataTypes.TPF.TPFEntry(tex.Key, mdl.TextureFlags[tex.Key], 0, tex.Value));
                    }
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.tpf", null, DataFile.SaveAsBytes(tpf, $"{ShortName}.tpf")));
                }

                ID++;
            }

            ID = ID_FLVER_START;
            foreach (var mdl in Models)
            {
                if (mdl.Mesh != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.flver", null, DataFile.SaveAsBytes(mdl.Mesh, $"{ShortName}.flver")));

                ID++;
            }

            ID = ID_BodyHKX_START;
            foreach (var mdl in Models)
            {
                if (mdl.BodyHKX != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.hkx", null, mdl.BodyHKX));
            }

            ID = ID_ANIBND_START;
            foreach (var mdl in Models)
            {
                if (mdl.AnimContainer != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.anibnd", null, DataFile.SaveAsBytes(mdl.AnimContainer, $"{ShortName}{idx()}.anibnd")));
            }

            ID = ID_HKXPWV_START;
            foreach (var mdl in Models)
            {
                if (mdl.HKXPWV != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.hkxpwv", null, mdl.HKXPWV));
            }

            ID = ID_BSIPWV_START;
            foreach (var mdl in Models)
            {
                if (mdl.BSIPWV != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.bsipwv", null, mdl.BSIPWV));
            }

            ID = ID_ClothHKX_START;
            foreach (var mdl in Models)
            {
                if (mdl.ClothHKX != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}_c.hkx", null, mdl.ClothHKX));
            }

            ID = ID_CHRTPFBHD_START;
            foreach (var mdl in Models)
            {
                if (mdl.CHRTPFBHD != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.chrtpfbhd", null, mdl.CHRTPFBHD));
            }

            bin.WriteDataFile(BND, FilePath ?? VirtualUri);
        }
    }
}
