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

            if (parentIndex == -1 && (boneContent.Name.ToUpper().StartsWith("DUMMY") || boneContent.Name.ToUpper().StartsWith("DYMMY")))
            {
                newBone.Name = "dymmy";
            }
            else if (parentIndex == -1 && boneContent.Name.ToUpper().StartsWith("SFX"))
            {
                newBone.Name = "SFX用";
            }

            FbxPipeline.Matrix boneTrans = FbxPipeline.Matrix.CreateScale(Importer.FinalScaleMultiplier);

            if (parentIndex == -1)
            {
                boneTrans *= boneContent.AbsoluteTransform;
            }
            else
            {
                boneTrans *= boneContent.Transform;
            }



            //if (parentIndex == -1)
            //{
            //    boneTrans = boneContent.AbsoluteTransform * FbxPipeline.Matrix.Invert(boneContent.Parent.AbsoluteTransform);
            //}
            //else
            //{
            //    boneTrans = boneContent.Transform;
            //}

            //if (parentIndex == -1)
            //{
            //    FbxPipeline.Vector3 __scale = FbxPipeline.Vector3.One;
            //    if (boneTrans.Decompose(out FbxPipeline.Vector3 _scale, out _, out _))
            //    {
            //        __scale = _scale;
            //    }

            //    boneTrans *= FbxPipeline.Matrix.CreateScale(new FbxPipeline.Vector3(1 / __scale.X, 1 / __scale.Y, 1 / __scale.Z));
            //}

            newBone.Translation = new Vector3(boneTrans.Translation.X, boneTrans.Translation.Y, boneTrans.Translation.Z);

            bool boneScaleWarning = false;

            if (boneTrans.Decompose(out FbxPipeline.Vector3 scale, out FbxPipeline.Quaternion rotation, out FbxPipeline.Vector3 translation))
            {
                //var scaledTranslation = Vector3.Transform(new Vector3(translation.X, translation.Y, translation.Z), FBX_IMPORT_MATRIX);
                //newBone.Translation = scaledTranslation;
                //newBone.Translation = new Vector3(translation.X, translation.Y, translation.Z);
                newBone.Scale = new FlverVector3(scale.X, scale.Y, scale.Z) / Importer.FinalScaleMultiplier;

                if (newBone.Scale.X != 1 || newBone.Scale.Y != 1 || newBone.Scale.Z != 1)
                {
                    boneScaleWarning = true;
                }
            }
            else
            {
                throw new Exception("FBX Bone Content Transform Matrix " +
                    "-> Decompose(out Vector3 scale, " +
                    "out Quaternion rotation, out Vector3 translation) " +
                    ">>FAILED<<");
            }

            

            newBone.EulerRadian = Util.GetEuler(boneTrans);

            //newBone.EulerRadian.Y -= MathHelper.PiOver2;

            if (newBone.Name.ToUpper().StartsWith("DUMMY") && newBone.Name.Contains("<") && newBone.Name.Contains(">"))
            {
                var dmy = new FlverDummy(flver);
                dmy.ParentBoneIndex = (short)parentIndex;

                //var dmyParentEuler = Util.GetEuler(boneContent.Parent.Transform);



                dmy.Position = /*Vector3.Transform(*/new Vector3(boneTrans.Translation.X,
                        boneTrans.Translation.Y,
                        boneTrans.Translation.Z)/*,
                        
                        //Matrix.CreateRotationY(dmyParentEuler.Y)
                        //* Matrix.CreateRotationZ(dmyParentEuler.Z)
                        //* Matrix.CreateRotationX(dmyParentEuler.X)

                        )*/;
                dmy.Row2 = new Vector3(0, -0.180182f, 0);
                dmy.Row3 = new Vector3(0, 0, -0.077194f);
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
            else if (newBone.Name.StartsWith("[") && newBone.Name.EndsWith("]"))
            {
                newBone.Name = newBone.Name.Substring(1, newBone.Name.Length - 2);
                newBone.IsNub = true;
            }

            //float transX = boneContent.Transform.Translation.X;
            //float transY = boneContent.Transform.Translation.Y;
            //float transZ = boneContent.Transform.Translation.Z;

            if (boneScaleWarning)
            {
                Importer.PrintWarning($"Bone '{boneContent.Name}' has a scale of <{scale.X}, {scale.Y}, {scale.Z}>. " +
                        $"Any scale different than <1.0, 1.0, 1.0> might cause the game to " +
                        $"try to \"correct\" the scale and break something.");
            }

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
