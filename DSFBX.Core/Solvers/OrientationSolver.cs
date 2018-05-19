using MeowDSIO.DataFiles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSFBX.Solvers
{
    public class OrientationSolver
    {
        private readonly DSFBXImporter Importer;
        public OrientationSolver(DSFBXImporter Importer)
        {
            this.Importer = Importer;
        }

        public void SolveOrientation(FLVER flver, bool solveBones)
        {
            //foreach (var submesh in flver.Submeshes)
            //{
            //    foreach (var vert in submesh.Vertices)
            //    {
            //        //Vector4 newNorm = vert.Normal * new Vector4(1, 1, 1, 1);
            //        //vert.Normal = Vector3.Normalize((Vector3)vert.Normal);
            //        //onOutput?.Invoke($"{new Vector3(newNorm.X, newNorm.Y, newNorm.Z).Length()}");

            //        //vert.BiTangent *= new Vector4(1, 1, 1, -1);

            //        var m = Matrix.CreateRotationX(-MathHelper.PiOver2)
            //        * Matrix.CreateRotationZ(MathHelper.Pi)
            //        * Matrix.CreateRotationZ(MathHelper.PiOver2)
            //        ;

            //        vert.Position = Vector3.Transform(vert.Position, m);
            //        vert.Normal = Vector3.Transform((Vector3)vert.Normal, m);
            //        var rotBitangentVec3 = Vector3.Transform((Vector3)vert.BiTangent, m);
            //        vert.BiTangent = new Vector4(rotBitangentVec3.X, rotBitangentVec3.Y, rotBitangentVec3.Z, vert.BiTangent.W);
            //    }

            //    //var p = submesh.Material.Parameters.Where(pa => pa.Name == "g_Bumpmap").First();
            //    //p.Value = "";
            //}

            //if (solveBones)
            //{
            //    foreach (var bone in flver.Bones.Where(b => b.ParentIndex == -1))
            //    {
            //        var origMatrix = Matrix.CreateTranslation(bone.Translation)
            //            * Matrix.CreateScale(bone.Scale);

            //        var m = Matrix.CreateRotationZ(-MathHelper.PiOver2)
            //            //* Matrix.CreateRotationX(MathHelper.Pi)
            //            ;

            //        if ((origMatrix * m).Decompose(out var scale, out _, out var trans))
            //        {
            //            bone.Translation = trans;
            //            bone.Scale = scale;
            //        }

            //        bone.EulerRadian.Z += -MathHelper.PiOver2;
            //        //bone.EulerRadian.X += MathHelper.Pi;

            //    }
            //}

            //foreach (var dmy in flver.Dummies)
            //{
            //    dmy.Position *= new Vector3(-1, 1, 1);

            //    var m = Matrix.CreateTranslation(dmy.Position)
            //        * Matrix.CreateRotationX(-MathHelper.PiOver2)
            //        * Matrix.CreateRotationZ(MathHelper.Pi);

            //    if (m.Decompose(out var scale, out _, out var trans))
            //    {
            //        dmy.Position = trans * scale;
            //    }
            //}
        }
    }
}
