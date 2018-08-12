extern alias PIPE;

using MeowDSIO;
using MeowDSIO.DataFiles;
using MeowDSIO.DataTypes.FLVER;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSFBX.CMD
{
    class DSFBX_CMD
    {
        static bool DoImport(string[] args, string debugCopyFlverPath = null, string importBonesPath = null)
        {
            Action<string> onOutput = (s) => Console.WriteLine(s);
            Action<string> onError = (s) => Console.Error.WriteLine(s);

            Action<NodeContent> onFbx = null;
            Action<FLVER> onFlver = null;

            string inputFbx = null;
            string outputBnd = null;
            int modelIdx = 0;

            if (args.Length >= 2)
            {
                inputFbx = args[0];
                outputBnd = args[1];
            }

            if (args.Length >= 3)
            {
                if (int.TryParse(args[2], out int modelIdxParsed))
                {
                    modelIdx = modelIdxParsed;
                }
                else
                {
                    onError?.Invoke($"Could not parse given model index number: '{args[2]}'");
                    return false;
                }

            }

            if (inputFbx == null || outputBnd == null)
            {
                onError?.Invoke("Usage: DSFBX-CMD <InputFBX> <OutputBND> [Model Index (for BNDs with multiple models)]");
                return false;
            }

            var importer = new DSFBXImporter()
            {
                EntityBndPath = outputBnd,
                EntityModelIndex = modelIdx,
                FbxPath = inputFbx,
                ImportSkeletonPath = importBonesPath,
                ScalePercent = 100.0
            };

            bool success = importer.Import();

            if (success && debugCopyFlverPath != null)
            {
                onOutput?.Invoke($"DEBUG: Copying FLVER from entity BND to output file '{debugCopyFlverPath}'...");
                var entityBnd = DataFile.LoadFromFile<EntityBND>(outputBnd);
                DataFile.SaveToFile(entityBnd.Models[modelIdx].Mesh, debugCopyFlverPath);
                onOutput?.Invoke($"Done.");
            }

            return success;
        }

        static void MEOW_DEBUG()
        {
            //// TESTING ////

            string fbxPathhhh = @"D:\FRPG_MOD\FBX Import Test\fluted_m_test\fluted_m_test.FBX";
            string debugPartsbndPath = @"G:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_A_9550.partsbnd";
            string importBonesPath = null;// @"G:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_A_2870.partsbnd";


            DoImport(new string[]
                {
                   fbxPathhhh,
                    debugPartsbndPath
                }
            , null,
            importBonesPath
            //,@"E:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_F_9450.flver"
            //,@"E:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_F_9450.partsbnd.bak"
            );

            debugPartsbndPath = @"G:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\AM_A_9550.partsbnd";

            DoImport(new string[]
                {
                    fbxPathhhh,
                    debugPartsbndPath
                }
            , null,
            importBonesPath
            //,@"E:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_F_9450.flver"
            //,@"E:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_F_9450.partsbnd.bak"
            );

            debugPartsbndPath = @"G:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\HD_A_9550.partsbnd";

            DoImport(new string[]
                {
                    fbxPathhhh,
                    debugPartsbndPath
                }
            , null,
            importBonesPath
            //,@"E:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_F_9450.flver"
            //,@"E:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_F_9450.partsbnd.bak"
            );

            debugPartsbndPath = @"G:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\LG_M_9550.partsbnd";

            DoImport(new string[]
                {
                    fbxPathhhh,
                    debugPartsbndPath
                }
            , null,
            importBonesPath
            //,@"E:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_F_9450.flver"
            //,@"E:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_F_9450.partsbnd.bak"
            );

            Process.Start(@"C:\Users\Meowmaritus\GitHub\MDSMDVTT-Reconstruction\bin\Debug\DS1MDV.exe", $"\"{debugPartsbndPath}\"");
            return;
            //DoImport(new string[]
            //{
            //    @"E:\FRPG_MOD\FBX Import Test\volutelum_export\volutelum.FBX",
            //    @"E:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\WP_A_0221.partsbnd"
            //});

            //var dragonBody = DataFile.LoadFromFile<EntityBND>(
            //    @"E:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\parts\BD_A_9600.partsbnd");

            //void print_bone(FlverBone bone, string indent)
            //{
            //    Console.WriteLine(indent + "-" + bone.Name);
            //    foreach (var b in dragonBody.Models[0].Mesh.Bones.Where(bn => bn.ParentIndex == (short)dragonBody.Models[0].Mesh.Bones.IndexOf(bone)))
            //    {
            //        print_bone(b, indent + "  ");
            //    }
            //}

            //foreach (var bone in dragonBody.Models[0].Mesh.Bones.Where(x => x.ParentIndex == -1))
            //{
            //    print_bone(bone, "");
            //}

            //// TESTING ////
        }

        static void Main(string[] args)
        {
            //MEOW_DEBUG();
            DoImport(args);

            Console.WriteLine();
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(true);
        }
    }
}
