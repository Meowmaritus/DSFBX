using MeowDSIO.DataFiles;
using MeowDSIO.DataTypes.FLVER;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSFBX
{
    public static class HumanBodyFixer
    {
        private static readonly Dictionary<string, string> MaterialToEmbeddedFlverMap = new Dictionary<string, string>
        {
            { "HD_M", "FC_M_0000.partsbnd" },
            { "BD_M", "BD_M_0000.partsbnd" },
            { "AM_M", "AM_M_0000.partsbnd" },
            { "LG_M", "LG_M_0000.partsbnd" },
            { "HD_F", "FC_F_0000.partsbnd" },
            { "BD_F", "BD_F_0000.partsbnd" },
            { "AM_F", "AM_F_0000.partsbnd" },
            { "LG_F", "LG_F_0000.partsbnd" },
            { "HD_M_Hollow", "FC_M_0000_M.partsbnd" },
            { "BD_M_Hollow", "BD_M_0000_M.partsbnd" },
            { "AM_M_Hollow", "AM_M_0000_M.partsbnd" },
            { "LG_M_Hollow", "LG_M_0000_M.partsbnd" },
            { "HD_F_Hollow", "FC_F_0000_M.partsbnd" },
            { "BD_F_Hollow", "BD_F_0000_M.partsbnd" },
            { "AM_F_Hollow", "AM_F_0000_M.partsbnd" },
            { "LG_F_Hollow", "LG_F_0000_M.partsbnd" },
        };

        private static Dictionary<string, FLVER> HumanBodyFLVERs 
            = new Dictionary<string, FLVER>();

        private static FLVER LoadEmbeddedFLVER(string name)
        {
            return DSFBXImporter.LoadEmbRes(name, bin => bin.ReadAsDataFile<EntityBND>().Models[0].Mesh);
        }

        private static FLVER GetFlver(string name)
        {
            if (!HumanBodyFLVERs.ContainsKey(name))
                HumanBodyFLVERs.Add(name, LoadEmbeddedFLVER(name));

            return HumanBodyFLVERs[name];
        }

        private static bool IsFloatCloseEnough(float a, float b, float granularity)
        {
            return (Math.Abs(a - b) <= granularity);
        }

        public static bool CheckIfVertexMatches(FlverVertex v,  FlverVertex closeTo)
        {
            return IsFloatCloseEnough(v.Position.X, closeTo.Position.X, 0.001f)
                && IsFloatCloseEnough(v.Position.Y, closeTo.Position.Y, 0.001f)
                && IsFloatCloseEnough(v.Position.Z, closeTo.Position.Z, 0.001f)
                && IsFloatCloseEnough(v.UVs[0].U, closeTo.UVs[0].U, 0.001f)
                && IsFloatCloseEnough(v.UVs[0].U, closeTo.UVs[0].U, 0.001f); ;
        }

        public static (int VertsFixed, int TotalSourceVerts) FixBodyPiece(FlverSubmesh mesh, string bodyPieceType, out string possibleError)
        {
            possibleError = null;
            if (!MaterialToEmbeddedFlverMap.ContainsKey(bodyPieceType))
            {
                possibleError = $"Invalid body piece type: '{bodyPieceType}' in material name of this body part. Below is a list of valid types:" +
                    " -HD_M\n" +
                    " -BD_M\n" +
                    " -AM_M\n" +
                    " -LG_M\n" +
                    " -HD_F\n" +
                    " -BD_F\n" +
                    " -AM_F\n" +
                    " -LG_F\n" +
                    " -HD_M_Hollow\n" +
                    " -BD_M_Hollow\n" +
                    " -AM_M_Hollow\n" +
                    " -LG_M_Hollow\n" +
                    " -HD_F_Hollow\n" +
                    " -BD_F_Hollow\n" +
                    " -AM_F_Hollow\n" +
                    " -LG_F_Hollow\n";
                return (-1, -1);
            }

            var flverName = MaterialToEmbeddedFlverMap[bodyPieceType];
            var flver = GetFlver(flverName);

            List<FlverVertex> possibleVertices = new List<FlverVertex>();
            foreach (var v in flver.Submeshes[0].Vertices)
                possibleVertices.Add(v);

            int numberOfVertsFixed = 0;
            int totalVerts = possibleVertices.Count;

            //List<FlverVertex> possibleVertsCheckedOff = new List<FlverVertex>();

            foreach (var v in mesh.Vertices)
            {
                //(FlverVertex Vert, float Dist) closestDistance = (null, float.MaxValue);

                FlverVertex closest = null;

                foreach (var possible in possibleVertices)
                {
                    //float dist = ((Vector3)(possible.Position - v.Position)).LengthSquared();

                    //if (dist < closestDistance.Dist)
                    //    closestDistance = (possible, dist);

                    if (CheckIfVertexMatches(v, possible))
                    {
                        closest = possible;

                        v.Position = closest.Position;
                        v.Normal = closest.Normal;
                        v.BiTangent = closest.BiTangent;

                        string targetBoneNameA = closest.BoneIndices.A >= 0 ? flver.GetBoneFromIndex(flver.Submeshes[0].BoneIndices[closest.BoneIndices.A])?.Name : null;
                        string targetBoneNameB = closest.BoneIndices.B >= 0 ? flver.GetBoneFromIndex(flver.Submeshes[0].BoneIndices[closest.BoneIndices.B])?.Name : null;
                        string targetBoneNameC = closest.BoneIndices.C >= 0 ? flver.GetBoneFromIndex(flver.Submeshes[0].BoneIndices[closest.BoneIndices.C])?.Name : null;
                        string targetBoneNameD = closest.BoneIndices.D >= 0 ? flver.GetBoneFromIndex(flver.Submeshes[0].BoneIndices[closest.BoneIndices.D])?.Name : null;

                        v.BoneIndices.A = mesh.FindOrAddBoneIndex(targetBoneNameA);
                        v.BoneIndices.B = mesh.FindOrAddBoneIndex(targetBoneNameB);
                        v.BoneIndices.C = mesh.FindOrAddBoneIndex(targetBoneNameC);
                        v.BoneIndices.D = mesh.FindOrAddBoneIndex(targetBoneNameD);

                        v.BoneWeights = closest.BoneWeights;

                        v.UVs = closest.UVs;

                        numberOfVertsFixed++;
                    }
                }

                

                //if (possibleVertsCheckedOff.Count > 0)
                //{
                //    foreach (var checkoff in possibleVertsCheckedOff)
                //        possibleVertices.Remove(checkoff);
                //    possibleVertsCheckedOff.Clear();
                //}
            }

            return (numberOfVertsFixed, totalVerts);
        }
    }
}
