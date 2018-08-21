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

        public readonly byte[] ANIM_DUMMY_DATA = new byte[] {
            0x83, 0x4C, 0x83, 0x83, 0x83, 0x89, 0x83, 0x6F, 0x83, 0x43, 0x83, 0x93, 0x83, 0x68, 0x82, 0xCC, 0x34, 0x30, 0x30, 0x94,
            0xD4, 0x82, 0xC9, 0x82, 0xB1, 0x82, 0xCC, 0x83, 0x5F, 0x83, 0x7E, 0x81, 0x5B, 0x83, 0x74, 0x83, 0x40, 0x83, 0x43, 0x83,
            0x8B, 0x82, 0xF0, 0x92, 0xC7, 0x89, 0xC1, 0x82, 0xB5, 0x82, 0xDC, 0x82, 0xB7, 0x81, 0x42, 0x82, 0xB1, 0x82, 0xCC, 0x83,
            0x74, 0x83, 0x40, 0x83, 0x43, 0x83, 0x8B, 0x82, 0xAA, 0x82, 0xA0, 0x82, 0xE9, 0x82, 0xC6, 0x90, 0xEA, 0x97, 0x70, 0x82,
            0xCC, 0x45, 0x73, 0x64, 0x83, 0x74, 0x83, 0x40, 0x83, 0x43, 0x83, 0x8B, 0x82, 0xF0, 0x8E, 0x67, 0x97, 0x70, 0x82, 0xB5,
            0x82, 0xDC, 0x82, 0xB7, 0x81, 0x42
        };

        public static readonly string ANIM_DUMMY_NAME = "chrbnd.dmy";

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

                if (entry.ID == ID_TPF_START + modelIdx)
                {
                    var tpf = entry.ReadDataAs<TPF>();
                    foreach (var tex in tpf)
                    {
                        Models[modelIdx].Textures.Add(tex.Name, tex.GetBytes());
                        Models[modelIdx].TextureFlags.Add(tex.Name, tex.FlagsA);
                    }
                }
                else if (entry.ID == ID_FLVER_START + modelIdx)
                {
                    Models[modelIdx].Mesh = entry.ReadDataAs<FLVER>();
                }
                else if (entry.ID == ID_BodyHKX_START + modelIdx)
                {
                    Models[modelIdx].BodyHKX = entry.GetBytes();
                }
                else if (entry.ID == ID_ANIBND_START + modelIdx)
                {
                    if (entry.Name.ToUpper().EndsWith(".DMY"))
                    {
                        Models[modelIdx].IncludesAnimContainerDummy = true;
                    }
                    else
                    {
                        //Models[modelIdx].AnimContainer = entry.ReadDataAs<ANIBND>();
                        Models[modelIdx].AnimContainer = entry.ReadDataAs<BND>();
                    }
                }
                else if (entry.ID == ID_BSIPWV_START + modelIdx)
                {
                    Models[modelIdx].BSIPWV = entry.GetBytes();
                }
                else if (entry.ID == ID_HKXPWV_START + modelIdx)
                {
                    Models[modelIdx].HKXPWV = entry.GetBytes();
                }
                else if (entry.ID == ID_ClothHKX_START + modelIdx)
                {
                    Models[modelIdx].ClothHKX = entry.GetBytes();
                }
                else if (entry.ID == ID_CHRTPFBHD_START + modelIdx)
                {
                    Models[modelIdx].CHRTPFBHD = entry.GetBytes();
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
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.tpf", null, DataFile.SaveAsBytes(tpf, $"{ShortName}{idx()}.tpf")));
                    ID++;
                }

                ID++;
            }

            ID = ID_FLVER_START;
            foreach (var mdl in Models)
            {
                if (mdl.Mesh != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.flver", null, DataFile.SaveAsBytes(mdl.Mesh, $"{ShortName}{idx()}.flver")));
                ID++;
            }

            ID = ID_BodyHKX_START;
            foreach (var mdl in Models)
            {
                if (mdl.BodyHKX != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.hkx", null, mdl.BodyHKX));
                ID++;
            }

            ID = ID_ANIBND_START;
            foreach (var mdl in Models)
            {
                if (mdl.AnimContainer != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.anibnd", null, DataFile.SaveAsBytes(mdl.AnimContainer, $"{ShortName}{idx()}.anibnd")));
                else if (mdl.IncludesAnimContainerDummy)
                    BND.Add(new BNDEntry(ID, ANIM_DUMMY_NAME, null, ANIM_DUMMY_DATA));
                ID++;
            }

            ID = ID_HKXPWV_START;
            foreach (var mdl in Models)
            {
                if (mdl.HKXPWV != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.hkxpwv", null, mdl.HKXPWV));
                ID++;
            }

            ID = ID_BSIPWV_START;
            foreach (var mdl in Models)
            {
                if (mdl.BSIPWV != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.bsipwv", null, mdl.BSIPWV));
                ID++;
            }

            ID = ID_ClothHKX_START;
            foreach (var mdl in Models)
            {
                if (mdl.ClothHKX != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}_c.hkx", null, mdl.ClothHKX));
                ID++;
            }

            ID = ID_CHRTPFBHD_START;
            foreach (var mdl in Models)
            {
                if (mdl.CHRTPFBHD != null)
                    BND.Add(new BNDEntry(ID, $"{ShortName}{idx()}.chrtpfbhd", null, mdl.CHRTPFBHD));
                ID++;
            }

            bin.WriteDataFile(BND, FilePath ?? VirtualUri);
        }
    }
}
