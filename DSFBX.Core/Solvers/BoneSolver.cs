extern alias PIPE;

using MeowDSIO.DataFiles;
using MeowDSIO.DataTypes.FLVER;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FbxPipeline = PIPE::Microsoft.Xna.Framework;

namespace DSFBX.Solvers
{
    public class BoneSolver
    {
        private readonly DSFBXImporter Importer;
        public BoneSolver(DSFBXImporter Importer)
        {
            this.Importer = Importer;
        }

        private (Vector3 Min, Vector3 Max) GetBoneBoundingBox(FlverBone bone, float thickness)
        {
            var length = bone.Translation.Length();

            return (new Vector3(-length / 2, -thickness, -thickness), new Vector3(length / 2, thickness, thickness));
        }

        public int SolveBone(FLVER flver,
            NodeContent fbx,
            NodeContent boneContent,
            int parentIndex)
        {
            var newBone = new FlverBone(flver);

            newBone.Name = boneContent.Name;

            //if (parentIndex == -1 && (boneContent.Name.ToUpper().StartsWith("DUMMY") || boneContent.Name.ToUpper().StartsWith("DYMMY")))
            //{
            //    newBone.Name = "dymmy";
            //}
            //else if (parentIndex == -1 && boneContent.Name.ToUpper().StartsWith("SFX"))
            //{
            //    newBone.Name = "SFX用";
            //}

            FbxPipeline.Matrix boneTrans_Xna = boneContent.Transform;// * FbxPipeline.Matrix.CreateScale(Importer.FinalScaleMultiplier);

            //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationZ(-MathHelper.PiOver2);
            //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationY(-MathHelper.PiOver2);

            //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationY(MathHelper.Pi);

            //if (boneContent.Parent != null && boneContent.Parent != fbx)
            //{
            //    boneTrans_Xna *= FbxPipeline.Matrix.Invert(boneContent.Parent.AbsoluteTransform);
            //    //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationZ(MathHelper.Pi);
            //}

            string parentName = boneContent.Parent?.Name;
            bool isMainRootBone = (parentName == null || parentName == "RootNode");

            //bool isRegularChildBone = false;

            //var parentLevel1 = boneContent.Parent;

            //if (parentLevel1 != null)
            //{
            //    var parentLevel2 = parentLevel1.Parent;

            //    if (parentLevel2 != null)
            //    {
            //        if (parentLevel2.Name.Trim().ToUpper() == "ROOT")
            //        {
            //            //boneTrans_Xna *= FbxPipeline.Matrix.Invert(boneContent.Parent.AbsoluteTransform);
            //            //boneTrans_Xna = boneContent.AbsoluteTransform;

            //            //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationX(MathHelper.PiOver2);

            //            //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationY(MathHelper.PiOver2);
            //            //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationZ(MathHelper.PiOver2);
            //            //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationX(MathHelper.PiOver2);
            //            //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationZ(MathHelper.PiOver2);

            //            isRegularChildBone = true;
            //            //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationX(-MathHelper.PiOver4);
            //        }
            //    }


            //}

            //if (isMainRootBone)
            //{
            //    boneTrans_Xna = boneContent.AbsoluteTransform;
            //}

            //if (isRegularChildBone)
            //{
            //    boneTrans_Xna = boneContent.AbsoluteTransform;
            //}
            //else if (isMainRootBone || boneContent.Name.ToUpper() == "ROOT")
            //{
            //    //boneTrans_Xna = FbxPipeline.Matrix.Identity;
            //}



            //if (boneContent.Name.ToUpper() == "ROOT" || isMainRootBone)
            //{
            //    boneTrans_Xna *= FbxPipeline.Matrix.CreateScale(1, 1, -1);
            //}

            //if (!isMainRootBone)
            //{
            //    boneTrans_Xna *= FbxPipeline.Matrix.Invert(boneContent.Parent.AbsoluteTransform);
            //}

            //boneTrans_Xna *= FbxPipeline.Matrix.Invert(boneContent.Parent.Transform);

            //Matrix boneTrans_MonoGame = new Matrix(boneTrans_Xna.M11, boneTrans_Xna.M12, boneTrans_Xna.M13, boneTrans_Xna.M14, 
            //    boneTrans_Xna.M21, boneTrans_Xna.M22, boneTrans_Xna.M23, boneTrans_Xna.M24, 
            //    boneTrans_Xna.M31, boneTrans_Xna.M32, boneTrans_Xna.M33, boneTrans_Xna.M34,
            //    boneTrans_Xna.M41, boneTrans_Xna.M42, boneTrans_Xna.M43, boneTrans_Xna.M44);

            //boneTrans_MonoGame *= Matrix.CreateScale(Importer.FinalScaleMultiplier);



            //bool boneScaleWarning = false;

            //newBone.Scale = boneTrans_MonoGame.Scale;// / Importer.FinalScaleMultiplier;

            //if (isMainRootBone)
            //{
            //    newBone.Scale.Y *= -1;
            //}

            //newBone.Scale = new FlverVector3(Math.Abs(newBone.Scale.X), Math.Abs(newBone.Scale.Y), Math.Abs(newBone.Scale.Z));

            //Quaternion q = Quaternion.CreateFromRotationMatrix(boneTrans_MonoGame);

            //boneTrans_Xna *= FbxPipeline.Matrix.CreateRotationY(MathHelper.Pi);

            if (boneTrans_Xna.Decompose(out FbxPipeline.Vector3 scale, out FbxPipeline.Quaternion rotation, out FbxPipeline.Vector3 translation))
            {
                newBone.Scale = new Vector3(scale.X, scale.Y, scale.Z);
                newBone.EulerRadian = Util.GetFlverEulerFromQuaternion_Bone(new Quaternion(-rotation.X, rotation.Y, rotation.Z, -rotation.W)) * new Vector3(1, 1, 1);

                //newBone.EulerRadian.X = MathHelper.WrapAngle(newBone.EulerRadian.X + MathHelper.Pi);

                newBone.Translation = new Vector3(-translation.X, translation.Y, translation.Z) * Importer.FinalScaleMultiplier;
            }
            else
            {
                throw new Exception("FBX Bone Content Transform Matrix " +
                    "-> Decompose(out Vector3 scale, " +
                    "out Quaternion rotation, out Vector3 translation) " +
                    ">>FAILED<<");
            }


            newBone.BoundingBoxMax = Vector3.One * 0.1f;
            newBone.BoundingBoxMin = Vector3.One * -0.1f;

            //var extractedBoneEuler = Util.GetFlverEulerFromQuaternion_Bone(boneTrans_MonoGame.Rotation);

            //newBone.EulerRadian.X = MathHelper.WrapAngle(extractedBoneEuler.X + 0);
            //newBone.EulerRadian.Y = MathHelper.WrapAngle(extractedBoneEuler.Y + 0);
            //newBone.EulerRadian.Z = MathHelper.WrapAngle(extractedBoneEuler.Z + 0);

            //newBone.EulerRadian = new Vector3(newBone.EulerRadian.X, newBone.EulerRadian.Z, newBone.EulerRadian.Y);

            //var extractedBoneTrans = boneTrans_MonoGame.Translation * Importer.FinalScaleMultiplier;

            //newBone.Translation = new Vector3(extractedBoneTrans.X, extractedBoneTrans.Y, extractedBoneTrans.Z);

            //if (boneContent.Name.ToUpper() == "MASTER")
            //{
            //    //newBone.Translation *= new Vector3(1, -1, 1);
            //    //newBone.EulerRadian.X = MathHelper.WrapAngle(newBone.EulerRadian.X + MathHelper.PiOver2);
            //    //newBone.EulerRadian.Y = MathHelper.WrapAngle(newBone.EulerRadian.Y + MathHelper.PiOver2);
            //    //newBone.EulerRadian.Z = MathHelper.WrapAngle(newBone.EulerRadian.Z + MathHelper.PiOver2);
            //}
            //else
            //{

            //}

            //if (boneContent.Parent != null)
            //{
            //    newBone.Translation *= new Vector3(1, -1, 1);
            //}

            //newBone.Translation *= new Vector3(-1, 1, 1);

            //if (newBone.Scale.X < 0)
            //{
            //    newBone.Scale *= new Vector3(-1, 1, 1);
            //    //newBone.EulerRadian *= new Vector3(1, -1, 1);
            //    //newBone.Translation *= new Vector3(1, -1, -1);
            //}





            //newBone.EulerRadian = Util.GetEuler(boneTrans);

            //newBone.EulerRadian.Y -= MathHelper.PiOver2;

            if (newBone.Name.ToUpper().StartsWith("DUMMY") && newBone.Name.Contains("<") && newBone.Name.Contains(">"))
            {
                var dmy = new FlverDummy(flver);
                dmy.ParentBoneIndex = (short)parentIndex;

                //var dmyParentEuler = Util.GetEuler(boneContent.Parent.Transform);

                var parentBoneTrans = boneContent.Parent.AbsoluteTransform;

                Matrix parentBoneTrans_MonoGame = new Matrix(parentBoneTrans.M11, parentBoneTrans.M12, parentBoneTrans.M13, parentBoneTrans.M14,
                    parentBoneTrans.M21, parentBoneTrans.M22, parentBoneTrans.M23, parentBoneTrans.M24,
                    parentBoneTrans.M31, parentBoneTrans.M32, parentBoneTrans.M33, parentBoneTrans.M34,
                    parentBoneTrans.M41, parentBoneTrans.M42, parentBoneTrans.M43, parentBoneTrans.M44);

                dmy.Position = Vector3.Transform(new Vector3(-boneContent.Transform.Translation.X,
                        boneContent.Transform.Translation.Y,
                        boneContent.Transform.Translation.Z)/*,
                        
                        //Matrix.CreateRotationY(dmyParentEuler.Y)
                        //* Matrix.CreateRotationZ(dmyParentEuler.Z)
                        //* Matrix.CreateRotationX(dmyParentEuler.X)

                        )*/
                        
                        * Importer.FinalScaleMultiplier
                        ,
                        //Matrix.Invert(Matrix.CreateScale(parentBoneTrans_MonoGame.Scale.X, parentBoneTrans_MonoGame.Scale.Y, parentBoneTrans_MonoGame.Scale.Z))
                        Matrix.Identity
                        );
                var thisScale = new FbxPipeline.Vector3(newBone.Scale.X, newBone.Scale.Y, newBone.Scale.Z);

                var upPoint = FbxPipeline.Vector3.Normalize(boneContent.AbsoluteTransform.Forward) * 0.1f * thisScale;

                var forwardPoint = FbxPipeline.Vector3.Normalize(boneContent.AbsoluteTransform.Up) * 0.05f * thisScale;

                dmy.Row2 = new Vector3(upPoint.X, upPoint.Y, upPoint.Z);
                dmy.Row3 = new Vector3(forwardPoint.X, forwardPoint.Y, forwardPoint.Z);
                var dmyTypeID = int.Parse(Util.GetAngleBracketContents(boneContent.Name));
                dmy.TypeID = (short)dmyTypeID;

                flver.Dummies.Add(dmy);

                foreach (var c in boneContent.Children)
                {
                    if (c is NodeContent n)
                    {
                        Importer.PrintWarning($"Non-dummy node '{n.Name}' is parented " +
                            $"to a dummy node ('{boneContent.Name}') and will be ignored " +
                            $"due to Dark Souls engine limitations. To include the node, " +
                            $"parent it to something that is not a dummy node.");
                    }
                }


                return -1;
            }
            //else if ((newBone.Name.StartsWith("[") && newBone.Name.EndsWith("]")))
            //{
            //    newBone.Name = newBone.Name.Substring(1, newBone.Name.Length - 2);
            //    newBone.IsNub = true;
            //}
            else if (newBone.Name.ToUpper().EndsWith("NUB"))
            {
                newBone.IsNub = true;
            }

            //float transX = boneContent.Transform.Translation.X;
            //float transY = boneContent.Transform.Translation.Y;
            //float transZ = boneContent.Transform.Translation.Z;

            //if (boneScaleWarning)
            //{
            //    Importer.PrintWarning($"Bone '{boneContent.Name}' has a scale of <{scale.X}, {scale.Y}, {scale.Z}>. " +
            //            $"Any scale different than <1.0, 1.0, 1.0> might cause the game to " +
            //            $"try to \"correct\" the scale and break something.");
            //}

            newBone.ParentIndex = (short)parentIndex;

            flver.Bones.Add(newBone);

            int myIndex = flver.Bones.Count - 1;

            var myChildrenIndices = new List<int>();

            foreach (var childNode in boneContent.Children)
            {
                if (childNode is NodeContent childBone)
                {
                    myChildrenIndices.Add(SolveBone(flver, fbx, childBone, myIndex));
                }
            }

            if (myChildrenIndices.Count > 0)
            {
                newBone.FirstChildIndex = (short)myChildrenIndices[0];

                for (int i = 0; i < myChildrenIndices.Count; i++)
                {
                    if (myChildrenIndices[i] > -1)
                    {
                        var currentChild = flver.Bones[myChildrenIndices[i]];

                        if (i > 0)
                        {
                            currentChild.PreviousSiblingIndex = (short)myChildrenIndices[i - 1];
                        }
                        else
                        {
                            currentChild.PreviousSiblingIndex = (short)-1;
                        }

                        if (i < myChildrenIndices.Count - 1)
                        {
                            currentChild.NextSiblingIndex = (short)myChildrenIndices[i + 1];
                        }
                        else
                        {
                            currentChild.NextSiblingIndex = (short)-1;
                        }
                    }
                }
            }


            return myIndex;
        }

    }
}
