extern alias PIPE;

using MeowDSIO;
using MeowDSIO.DataFiles;
using MeowDSIO.DataTypes.FLVER;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FbxPipeline = PIPE::Microsoft.Xna.Framework;

namespace DSFBX
{
    public class DSFBXImporter
    {
        public string FbxPath = null;
        public string EntityBndPath = null;
        public int EntityModelIndex = 0;
        public double ScalePercent = 100.0;
        public string ImportSkeletonPath = null;
        public bool IsDoubleSided = true;
        public bool GenerateBackup = true;
        public double ImportedSkeletonScalePercent = 100.0;

        public string PlaceholderMaterialName = "P_Metal[DSB]";


        const float FBX_IMPORT_SCALE_BASE = 1.0f / 100.0f;
        public float FinalScaleMultiplier => (float)(FBX_IMPORT_SCALE_BASE * (ScalePercent / 100.0));

        public readonly Solvers.BoneSolver BoneSolver;
        public readonly Solvers.NormalSolver NormalSolver;
        public readonly Solvers.OrientationSolver OrientationSolver;
        public readonly Solvers.TangentSolver TangentSolver;

        public DSFBXImporter()
        {
            BoneSolver = new Solvers.BoneSolver(this);
            NormalSolver = new Solvers.NormalSolver(this);
            OrientationSolver = new Solvers.OrientationSolver(this);
            TangentSolver = new Solvers.TangentSolver(this);

            CheckResourceLoad();
        }

        private static T LoadEmbRes<T>(string relResName, Func<DSBinaryReader, T> getResFunc)
            where T : class
        {
            T result = null;
            using (var embResStream = typeof(DSFBXImporter).Assembly
                .GetManifestResourceStream($"DSFBX.EmbeddedResources.{relResName}"))
                using (var embResReader = new DSBinaryReader(relResName, embResStream))
                    result = getResFunc.Invoke(embResReader);
            return result;
        }

        private static void CheckResourceLoad()
        {
            lock (_RESOURCE_LOAD_LOCKER)
            {
                if (MTDs == null)
                    MTDs = LoadEmbRes("Mtd.mtdbnd", bin => bin.ReadAsDataFile<MtdBND>());
                if (DSFBX_PLACEHOLDER_DIFFUSE == null)
                    DSFBX_PLACEHOLDER_DIFFUSE =
                        LoadEmbRes("DSFBX_PLACEHOLDER_DIFFUSE.dds", bin => bin.ReadAllBytes());
                if (DSFBX_PLACEHOLDER_SPECULAR == null)
                    DSFBX_PLACEHOLDER_SPECULAR =
                        LoadEmbRes("DSFBX_PLACEHOLDER_SPECULAR.dds", bin => bin.ReadAllBytes());
                if (DSFBX_PLACEHOLDER_BUMPMAP == null)
                    DSFBX_PLACEHOLDER_BUMPMAP =
                        LoadEmbRes("DSFBX_PLACEHOLDER_BUMPMAP.dds", bin => bin.ReadAllBytes());
            }
        }


        public event EventHandler<DSFBXGenericEventArgs<string>> InfoTextOutputted;
        public event EventHandler<DSFBXGenericEventArgs<string>> WarningTextOutputted;
        public event EventHandler<DSFBXGenericEventArgs<string>> ErrorTextOutputted;

        public event EventHandler ImportStarted;
        public event EventHandler ImportEnding;

        public event EventHandler<DSFBXGenericEventArgs<NodeContent>> FbxLoaded;
        public event EventHandler<DSFBXGenericEventArgs<FLVER>> FlverGenerated;

        internal void Print(string text)
        {
            OnInfoTextOutputted(text);
        }

        internal void PrintWarning(string text)
        {
            OnWarningTextOutputted(text);
        }

        internal void PrintError(string text)
        {
            OnErrorTextOutputted(text);
        }

        protected virtual void OnInfoTextOutputted(string text)
        {
            var handler = InfoTextOutputted;
            handler?.Invoke(this, new DSFBXGenericEventArgs<string>(text));
        }

        protected virtual void OnWarningTextOutputted(string text)
        {
            var handler = WarningTextOutputted;
            handler?.Invoke(this, new DSFBXGenericEventArgs<string>(text));
        }

        protected virtual void OnErrorTextOutputted(string text)
        {
            var handler = ErrorTextOutputted;
            handler?.Invoke(this, new DSFBXGenericEventArgs<string>(text));
        }

        protected virtual void OnImportStarted()
        {
            var handler = ImportStarted;
            handler?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnImportEnding()
        {
            var handler = ImportEnding;
            handler?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnFbxLoaded(NodeContent fbx)
        {
            var handler = FbxLoaded;
            handler?.Invoke(this, new DSFBXGenericEventArgs<NodeContent>(fbx));
        }

        protected virtual void OnFlverGenerated(FLVER flver)
        {
            var handler = FlverGenerated;
            handler?.Invoke(this, new DSFBXGenericEventArgs<FLVER>(flver));
        }


        static object _RESOURCE_LOAD_LOCKER = new object();
        static MtdBND MTDs;
        static byte[] DSFBX_PLACEHOLDER_DIFFUSE;
        static byte[] DSFBX_PLACEHOLDER_SPECULAR;
        static byte[] DSFBX_PLACEHOLDER_BUMPMAP;
        const int FACESET_MAX_TRIANGLES = 65535;
        static readonly char[] CHAR_NUMS = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };


        //static readonly Matrix FBX_IMPORT_MATRIX = Matrix.CreateScale(FBX_IMPORT_SCALE);

        //static void AddAllBoneChildrenAsHitboxes(FLVER flver, NodeContent boneContent, int parentBoneIndex)
        //{
        //    foreach (var dummyNode in boneContent.Children)
        //    {
        //        if (dummyNode is NodeContent dummyBone)
        //        {
        //            var dmy = new FlverDummy(flver);

        //            var dummyNumber = int.Parse(Util.GetAngleBracketContents(dummyBone.Name));

        //            dmy.Position = /*Vector3.Transform(*/new Vector3(dummyBone.AbsoluteTransform.Translation.X,
        //                dummyBone.AbsoluteTransform.Translation.Y,
        //                dummyBone.AbsoluteTransform.Translation.Z) * FBX_IMPORT_SCALE/*, Matrix.CreateRotationX(-MathHelper.PiOver2))*/;

        //            dmy.Row2 = new Vector3(0, -0.180182f, 0);

        //            dmy.Row3 = new Vector3(0, 0, -0.077194f);

        //            dmy.ParentBoneIndex = (short)parentBoneIndex;

        //            dmy.TypeID = (short)dummyNumber;

        //            flver.Dummies.Add(dmy);
        //        }
        //    }
        //}

        //static void HandleDummyBone(FLVER flver, NodeContent boneContent, FlverBone flverBone)
        //{
        //    flverBone.Name = "dymmy";

        //    flverBone.Scale = Vector3.One;
        //    flverBone.Translation = Vector3.Zero;
        //    flverBone.EulerRadian = Vector3.Zero;

        //    int myIndex = flver.Bones.IndexOf(flverBone);

        //    AddAllBoneChildrenAsHitboxes(flver, boneContent, myIndex);
        //}

        //static void HandleSfxBone(FLVER flver, NodeContent boneContent, FlverBone flverBone)
        //{
        //    flverBone.Name = "SFX用";

        //    flverBone.Scale = Vector3.One;
        //    flverBone.Translation = Vector3.Zero;
        //    flverBone.EulerRadian = Vector3.Zero;

        //    int myIndex = flver.Bones.IndexOf(flverBone);

        //    AddAllBoneChildrenAsHitboxes(flver, boneContent, myIndex);
        //}

        bool LoadFbxIntoFlver(NodeContent fbx, FLVER flver)
        {
            var FBX_Bones = new List<NodeContent>();
            var FBX_RootBones = new List<NodeContent>();
            var FBX_Meshes = new Dictionary<FlverSubmesh, MeshContent>();

            var FLVER_VertexStructLayoutChecks = new List<FlverVertexStructLayoutCheck>();

            foreach (var fbxComponent in fbx.Children)
            {
                if (fbxComponent is MeshContent meshContent)
                {
                    FBX_Meshes.Add(new FlverSubmesh(flver), meshContent);
                }
                else if (fbxComponent is NodeContent boneContent)
                {
                    if (boneContent.Name.Trim().ToUpper() == "SKELETON")
                    {
                        foreach (var childBone in boneContent.Children)
                        {
                            if (childBone.Name.ToUpper().Trim() == "ROOT")
                                FBX_RootBones.Add(childBone);
                            else
                                FBX_Bones.Add(childBone);
                        }
                    }
                    else
                    {
                        if (boneContent.Name.ToUpper().Trim() == "ROOT")
                            FBX_RootBones.Add(boneContent);
                        else
                            FBX_Bones.Add(boneContent);
                    }
                        
                }

            }

            if (fbx is MeshContent topLevelMeshContent)
            {
                FBX_Meshes.Add(new FlverSubmesh(flver), topLevelMeshContent);
            }

            var topLevelBoneIndices = new List<int>();

            string shortModelName = EntityBndPath;
            shortModelName = shortModelName.Substring(shortModelName.LastIndexOf('\\') + 1);
            shortModelName = shortModelName.Substring(0, shortModelName.IndexOf('.'));

            var flverRootBoneNameMap = new Dictionary<FlverBone, string>();

            if (ImportSkeletonPath == null)
            {

                foreach (var boneContent in FBX_RootBones)
                {
                    var nextRootBoneIndex = BoneSolver.SolveBone(flver, fbx, boneContent, -1);
                    topLevelBoneIndices.Add(nextRootBoneIndex);
                    flver.Bones[nextRootBoneIndex].Name = shortModelName;
                    flverRootBoneNameMap.Add(flver.Bones[nextRootBoneIndex], boneContent.Name);
                }

                foreach (var boneContent in FBX_Bones)
                {
                    topLevelBoneIndices.Add(BoneSolver.SolveBone(flver, fbx, boneContent, -1));
                }

                for (int i = 0; i < topLevelBoneIndices.Count; i++)
                {
                    if (i > 0)
                    {
                        flver.Bones[topLevelBoneIndices[i]].PreviousSiblingIndex = (short)topLevelBoneIndices[i - 1];
                    }
                    else
                    {
                        flver.Bones[topLevelBoneIndices[i]].PreviousSiblingIndex = (short)-1;
                    }

                    if (i < topLevelBoneIndices.Count - 1)
                    {
                        flver.Bones[topLevelBoneIndices[i]].NextSiblingIndex = (short)topLevelBoneIndices[i + 1];
                    }
                    else
                    {
                        flver.Bones[topLevelBoneIndices[i]].NextSiblingIndex = (short)-1;
                    }
                }

                
            }

            if (flver.Bones.Count == 0)
            {
                flver.Bones.Add(new FlverBone(flver)
                {
                    Name = shortModelName,
                });
            }

            var bonesByName = new Dictionary<string, FlverBone>();

            if (ImportSkeletonPath != null)
            {
                EntityBND skeletonSourceEntityBnd = null;

                if (ImportSkeletonPath.ToUpper().EndsWith(".DCX"))
                {
                    skeletonSourceEntityBnd = DataFile.LoadFromDcxFile<EntityBND>(ImportSkeletonPath);
                }
                else
                {
                    skeletonSourceEntityBnd = DataFile.LoadFromFile<EntityBND>(ImportSkeletonPath);
                }

                flver.Bones = skeletonSourceEntityBnd.Models[0].Mesh.Bones;
            }

            for (int i = 0; i < flver.Bones.Count; i++)
            {
                //if (!flver.Bones[i].IsNub)
                //{
                //    flver.Bones[i].BoundingBoxMin = new Vector3(-1, -0.25f, -0.25f) * 0.0015f;
                //    flver.Bones[i].BoundingBoxMax = new Vector3(1, 0.25f, 0.25f) * 0.0015f;
                //}

                if (flverRootBoneNameMap.ContainsKey(flver.Bones[i]))
                {
                    bonesByName.Add(flverRootBoneNameMap[flver.Bones[i]], flver.Bones[i]);
                }
                else
                {
                    string boneName = flver.Bones[i].Name;

                    if (flver.Bones[i].IsNub)
                        boneName = $"[{boneName}]";

                    if (!bonesByName.ContainsKey(boneName))
                        bonesByName.Add(boneName, flver.Bones[i]);
                }

                
            }

            foreach (var kvp in FBX_Meshes)
            {
                var flverMesh = kvp.Key;
                var fbxMesh = kvp.Value;

                var bonesReferencedByThisMesh = new List<FlverBone>();

                FlverVertexStructLayoutCheck structLayoutChecker = new FlverVertexStructLayoutCheck();

                var submeshHighQualityNormals = new List<Vector3>();
                var submeshHighQualityTangents = new List<Vector4>();
                var submeshVertexHighQualityBasePositions = new List<Vector3>();
                var submeshVertexHighQualityBaseUVs = new List<Vector2>();

                foreach (var geometryNode in fbxMesh.Geometry)
                {

                    if (geometryNode is GeometryContent geometryContent)
                    {
                        int numTriangles = geometryContent.Indices.Count / 3;

                        int numFacesets = numTriangles / FACESET_MAX_TRIANGLES;

                        /*
                            FACE SET ADDING/SPLITTING
                        */
                        {
                            var faceSet = new FlverFaceSet();

                            faceSet.CullBackfaces = !IsDoubleSided;

                            //for (int i = geometryContent.Indices.Count - 1; i >= 0; i--)
                            for (int i = 0; i < geometryContent.Indices.Count; i += 3)
                            {
                                if (faceSet.VertexIndices.Count >= FACESET_MAX_TRIANGLES * 3)
                                {
                                    flverMesh.FaceSets.Add(faceSet);
                                    faceSet = new FlverFaceSet();
                                }
                                else
                                {
                                    faceSet.VertexIndices.Add((ushort)geometryContent.Indices[i + 2]);
                                    faceSet.VertexIndices.Add((ushort)geometryContent.Indices[i + 1]);
                                    faceSet.VertexIndices.Add((ushort)geometryContent.Indices[i + 0]);
                                }
                            }

                            if (faceSet.VertexIndices.Count > 0)
                            {
                                flverMesh.FaceSets.Add(faceSet);
                            }

                        }

                        if (flverMesh.FaceSets.Count > 1)
                        {
                            PrintWarning($"Mesh '{fbxMesh.Name}' has {flverMesh.Vertices.Count} " +
                                $"vertices. \nEach individual triangle list can only support up to " +
                                $"65535 vertices due to file format limitations. Because of this, " +
                                $"the mesh had to be automatically be split into multiple " +
                                $"triangle lists. \n\nThis can be problematic for weapons in " +
                                $"particular because the game ignores ALL additional triangle " +
                                $"lists after the first one, on weapons specifically; " +
                                $"Only the triangles connected with the " +
                                $"first 65535 vertices will be shown ingame. \nIf you are " +
                                $"experiencing this issue, split the mesh into multiple " +
                                $"separate mesh objects, each with no more than 65535 vertices.");
                        }


                        var materialOverrides = fbxMesh.Children.Where(x => x.Name.StartsWith("MaterialOverride"));

                        string mtdName = null;

                        Dictionary<string, string> matTextures = new Dictionary<string, string>();

                        if (materialOverrides.Any())
                        {
                            if (materialOverrides.Count() > 1)
                            {
                                PrintWarning($"Mesh '{fbxMesh.Name}' has 2 " +
                                    $"material override nodes parented to it. Using the first one " +
                                    $"as the mesh's material and ignoring the material stored in " +
                                    $"the FBX's geometry data as well as any other material " +
                                    $"override nodes parented to this mesh.");
                            }
                            else
                            {
                                Print($"Material override node was found parented to mesh '{fbxMesh.Name}'. " +
                                "Using this override node as the mesh's material and ignoring the material stored in the FBX's geometry data.");
                            }

                            var matOverride = materialOverrides.First();

                            string matName = Util.GetAngleBracketContents(matOverride.Name);
                            if (!string.IsNullOrWhiteSpace(matName))
                            {
                                mtdName = matName;
                            }

                            
                        }

                        if (mtdName == null)
                        {
                            if (geometryContent.Material != null)
                            {
                                string fbxMaterialName = geometryContent.Material.Name;
                                if (fbxMaterialName.Contains("#"))
                                {
                                    fbxMaterialName = fbxMaterialName.Substring(0, fbxMaterialName.IndexOf('#'));
                                }
                                mtdName = fbxMaterialName.Trim() + ".mtd";

                                foreach (var texKvp in geometryContent.Material.Textures)
                                {
                                    if (texKvp.Key == "Texture")
                                    {
                                        if (!matTextures.ContainsKey("g_Diffuse"))
                                        {
                                            matTextures.Add("g_Diffuse", texKvp.Value.Filename);
                                        }
                                    }
                                    else if (texKvp.Key == "Specular" || texKvp.Key == "SpecularFactor")
                                    {
                                        if (!matTextures.ContainsKey("g_Specular"))
                                        {
                                            matTextures.Add("g_Specular", texKvp.Value.Filename);
                                        }
                                    }
                                    else if (texKvp.Key == "NormalMap")
                                    {
                                        if (!matTextures.ContainsKey("g_Bumpmap"))
                                        {
                                            matTextures.Add("g_Bumpmap", texKvp.Value.Filename);
                                        }
                                    }
                                    //TODO: OTHER TEXTURE TYPES
                                }

                            }
                            else
                            {
                                mtdName = null;
                                PrintWarning("No material override nodes nor FBX materials " +
                                    $"defined at all for mesh '{fbxMesh.Name}'. \n" +
                                    "Defaulting to placeholder material, " +
                                    $"'{PlaceholderMaterialName}', with placeholder textures.");
                            }
                        }

                        if (mtdName == null)
                        {
                            flverMesh.Material = new FlverMaterial()
                            {
                                Name = "DSFBX_Placeholder"
                            };
                        }
                        else if (MTDs.ContainsKey(mtdName))
                        {
                            flverMesh.Material = new FlverMaterial()
                            {
                                Name = "DSFBX_Material_",
                                MTDName = mtdName
                            };

                            var missingTextures = new List<string>();

                            foreach (var extParam in MTDs[flverMesh.Material.MTDName].ExternalParams)
                            {
                                var newMatParam = new FlverMaterialParameter();
                                newMatParam.Name = extParam.Name;

                                if (matTextures.ContainsKey(extParam.Name))
                                {
                                    newMatParam.Value = matTextures[extParam.Name];
                                }
                                else
                                {
                                    newMatParam.Value = "";
                                    missingTextures.Add(extParam.Name);
                                }
                            }

                            if (missingTextures.Count > 0)
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine($"Mesh '{fbxMesh.Name}' has no textures for the " +
                                    $"following inputs in its assigned ingame material ('{mtdName}'):");

                                foreach (var mt in missingTextures)
                                {
                                    sb.AppendLine("    " + mt);
                                }

                                PrintWarning(sb.ToString());
                            }
                        }
                        else
                        {
                            PrintWarning($"The material assigned to " +
                                $"mesh '{fbxMesh.Name}' is named " +
                                $"'{mtdName.Substring(0, mtdName.Length - 4)/*Remove .mtd*/}'" +
                                $", which is not a valid " +
                                $"ingame material name.\nDefaulting to " +
                                $"placeholder material, '{PlaceholderMaterialName}'.");

                            flverMesh.Material = new FlverMaterial()
                            {
                                Name = "DSFBX_Placeholder"
                            };
                        }

                        if (flverMesh.Material != null && flverMesh.Material.MTDName != null)
                        {
                            foreach (var extParam in MTDs[flverMesh.Material.MTDName].ExternalParams)
                            {
                                flverMesh.Material.Parameters.Add(new FlverMaterialParameter()
                                {
                                    Name = extParam.Name,
                                    Value = matTextures.ContainsKey(extParam.Name) ? matTextures[extParam.Name] : ""
                                });
                            }
                        }

                        for (int i = 0; i < geometryContent.Vertices.Positions.Count; i++)
                        {
                            var nextPosition = geometryContent.Vertices.Positions[i];
                            var posVec3 = FbxPipeline.Vector3.Transform(
                                new FbxPipeline.Vector3(-nextPosition.X, nextPosition.Y, nextPosition.Z)
                                , fbxMesh.AbsoluteTransform * FbxPipeline.Matrix.CreateScale(FinalScaleMultiplier)
                                //* FbxPipeline.Matrix.CreateScale(FinalScaleMultiplier)
                                //* FbxPipeline.Matrix.CreateRotationZ(MathHelper.Pi)

                                );

                            var newVert = new FlverVertex()
                            {
                                //Position = scaledPosition
                                Position = new FlverVector3(posVec3.X, posVec3.Y, posVec3.Z),
                                VertexColor = Color.White,
                            };

                            //TODO: MAYBE TRY VERTEX COLOR FROM FBX?

                            flverMesh.Vertices.Add(newVert);

                            //var euler = Util.GetEuler(fbxMesh.AbsoluteTransform);

                            //submeshVertexHighQualityBasePositions.Add(new Vector3(nextPosition.X, nextPosition.Y, nextPosition.Z));
                            //submeshVertexHighQualityBasePositions.Add(Vector3.Transform(new Vector3(nextPosition.X, nextPosition.Y, nextPosition.Z),
                            //    Matrix.CreateRotationY(euler.Y) * Matrix.CreateRotationZ(euler.Z) * Matrix.CreateRotationX(euler.X)
                            //    ));

                            submeshVertexHighQualityBasePositions.Add(new FlverVector3(posVec3.X, posVec3.Y, posVec3.Z));

                        }

                        ////TEST
                        //foreach (var pos in geometryContent.Vertices.Positions)
                        //{
                        //    flverMesh.Vertices.Add(new FlverVertex()
                        //    {
                        //        //Position = scaledPosition
                        //        Position = new Vector3(pos.X, pos.Y, pos.Z) * FBX_IMPORT_SCALE
                        //    });
                        //}
                        ////TEST

                        bool hasWeights = false;

                        foreach (var channel in geometryContent.Vertices.Channels)
                        {
                            if (channel.Name == "Normal0")
                            {
                                for (int i = 0; i < flverMesh.Vertices.Count; i++)
                                {
                                    var channelValue = (FbxPipeline.Vector3)(channel[i]);

                                    //var euler = Util.GetEuler(fbxMesh.AbsoluteTransform);


                                    var rotatedNormal = 
                                        FbxPipeline.Vector3.TransformNormal(
                                        new FbxPipeline.Vector3(-channelValue.X, channelValue.Y, channelValue.Z)
                                        , //fbxMesh.Transform
                                          //* FbxPipeline.Matrix.CreateScale(FinalScaleMultiplier)
                                          // 
                                          // *
                                          // 

                                        fbxMesh.AbsoluteTransform * FbxPipeline.Matrix.CreateScale(FinalScaleMultiplier)
                                        //FbxPipeline.Matrix.Identity
                                        // * FbxPipeline.Matrix.CreateRotationX(-MathHelper.Pi)


                                        // * FbxPipeline.Matrix.CreateRotationX(-MathHelper.Pi)
                                        // * FbxPipeline.Matrix.CreateRotationX(MathHelper.Pi)

                                        );

                                    rotatedNormal = FbxPipeline.Vector3.Normalize(rotatedNormal);

                                    //DEBUG TEST
                                    //rotatedNormal = new FbxPipeline.Vector3(0, 0, 1);
                                    ////////////

                                    flverMesh.Vertices[i].Normal = new FlverPackedVector4()
                                    {
                                        X = rotatedNormal.X,
                                        Y = rotatedNormal.Y,
                                        Z = rotatedNormal.Z,
                                        W = 0,
                                    };
                                    submeshHighQualityNormals.Add(new Vector3(rotatedNormal.X, rotatedNormal.Y, rotatedNormal.Z));
                                }
                            }
                            else if (channel.Name.StartsWith("TextureCoordinate"))
                            {
                                var uvIndex = int.Parse(channel.Name.Substring(channel.Name.IndexOfAny(CHAR_NUMS)));

                                if (uvIndex > 2)
                                {
                                    PrintWarning($"Found a UV vertex data channel with an abnormally " +
                                        $"high index ({uvIndex}) in FBX mesh '{fbxMesh.Name}'. This UV map " +
                                        $"will be ignored, as the game only ever reads UV channels 0 or 1 " +
                                        $"(and 2 in a few map meshes).");
                                }

                                bool isBaseUv = submeshVertexHighQualityBaseUVs.Count == 0;

                                for (int i = 0; i < flverMesh.Vertices.Count; i++)
                                {
                                    var channelValue = (FbxPipeline.Vector2)channel[i];

                                    var uv = new FlverUV()
                                    {
                                        U = channelValue.X,
                                        V = channelValue.Y,
                                    };

                                    if (flverMesh.Vertices[i].UVs.Count > uvIndex)
                                    {
                                        flverMesh.Vertices[i].UVs[uvIndex] = uv;
                                    }
                                    else if (flverMesh.Vertices[i].UVs.Count == uvIndex)
                                    {
                                        flverMesh.Vertices[i].UVs.Add(uv);
                                    }
                                    else if (uvIndex <= 2)
                                    {
                                        while (flverMesh.Vertices[i].UVs.Count <= uvIndex)
                                        {
                                            flverMesh.Vertices[i].UVs.Add(Vector2.Zero);
                                        }
                                    }

                                    if (isBaseUv)
                                    {
                                        submeshVertexHighQualityBaseUVs.Add(
                                            new Vector2(channelValue.X, channelValue.Y));
                                    }
                                }
                            }
                            else if (channel.Name == "Weights0")
                            {
                                hasWeights = true;
                                for (int i = 0; i < flverMesh.Vertices.Count; i++)
                                {
                                    var channelValue = (BoneWeightCollection)channel[i];

                                    var flverBoneIndices = new FlverBoneIndices(flverMesh);
                                    var flverBoneWeights = new FlverBoneWeights();

                                    if (channelValue.Count >= 1)
                                    {
                                        if (bonesByName.ContainsKey(channelValue[0].BoneName))
                                        {
                                            var bone = bonesByName[channelValue[0].BoneName];
                                            float weight = channelValue[0].Weight;
                                            sbyte index = 0;

                                            if (bonesReferencedByThisMesh.Contains(bone))
                                            {
                                                index = (sbyte)bonesReferencedByThisMesh.IndexOf(bone);
                                            }
                                            else
                                            {
                                                bonesReferencedByThisMesh.Add(bone);
                                                index = (sbyte)bonesReferencedByThisMesh.IndexOf(bone);
                                            }

                                            flverBoneIndices.A = index;
                                            flverBoneWeights.A = weight;
                                        }
                                        //else
                                        //{
                                        //    PrintWarning($"Warning: Bone '{channelValue[0].BoneName}' does not exist.");
                                        //}
                                    }

                                    if (channelValue.Count >= 2)
                                    {
                                        if (bonesByName.ContainsKey(channelValue[1].BoneName))
                                        {
                                            var bone = bonesByName[channelValue[1].BoneName];
                                            float weight = channelValue[1].Weight;
                                            sbyte index = 0;

                                            if (bonesReferencedByThisMesh.Contains(bone))
                                            {
                                                index = (sbyte)bonesReferencedByThisMesh.IndexOf(bone);
                                            }
                                            else
                                            {
                                                bonesReferencedByThisMesh.Add(bone);
                                                index = (sbyte)bonesReferencedByThisMesh.IndexOf(bone);
                                            }

                                            flverBoneIndices.B = index;
                                            flverBoneWeights.B = weight;
                                        }
                                        //else
                                        //{
                                        //    PrintWarning($"Warning: Bone '{channelValue[1].BoneName}' does not exist.");
                                        //}
                                    }

                                    if (channelValue.Count >= 3)
                                    {
                                        if (bonesByName.ContainsKey(channelValue[2].BoneName))
                                        {
                                            var bone = bonesByName[channelValue[2].BoneName];
                                            float weight = channelValue[2].Weight;
                                            sbyte index = 0;

                                            if (bonesReferencedByThisMesh.Contains(bone))
                                            {
                                                index = (sbyte)bonesReferencedByThisMesh.IndexOf(bone);
                                            }
                                            else
                                            {
                                                bonesReferencedByThisMesh.Add(bone);
                                                index = (sbyte)bonesReferencedByThisMesh.IndexOf(bone);
                                            }

                                            flverBoneIndices.C = index;
                                            flverBoneWeights.C = weight;
                                        }
                                        //else
                                        //{
                                        //    PrintWarning($"Warning: Bone '{channelValue[2].BoneName}' does not exist.");
                                        //}
                                    }

                                    if (channelValue.Count >= 4)
                                    {
                                        if (bonesByName.ContainsKey(channelValue[3].BoneName))
                                        {
                                            var bone = bonesByName[channelValue[3].BoneName];
                                            float weight = channelValue[3].Weight;
                                            sbyte index = 0;

                                            if (bonesReferencedByThisMesh.Contains(bone))
                                            {
                                                index = (sbyte)bonesReferencedByThisMesh.IndexOf(bone);
                                            }
                                            else
                                            {
                                                bonesReferencedByThisMesh.Add(bone);
                                                index = (sbyte)bonesReferencedByThisMesh.IndexOf(bone);
                                            }

                                            flverBoneIndices.D = index;
                                            flverBoneWeights.D = weight;
                                        }
                                        //else
                                        //{
                                        //    PrintWarning($"Warning: Bone '{channelValue[3].BoneName}' does not exist.");
                                        //}
                                    }

                                    flverMesh.Vertices[i].BoneIndices = flverBoneIndices;
                                    flverMesh.Vertices[i].BoneWeights = flverBoneWeights;
                                }
                            }
                            else
                            {
                                PrintWarning($"Found an unfamiliar vertex data " +
                                    $"channel ('{channel.Name}') in FBX mesh '{fbxMesh.Name}'.");
                            }
                        }

                        if (!hasWeights)
                        {
                            foreach (var vert in flverMesh.Vertices)
                            {
                                vert.BoneIndices = new FlverBoneIndices(flverMesh, 0, 0, 0, 0);

                                //vert.BoneWeights = new FlverBoneWeights(0, 0, 0, 0);

                                //vert.BoneWeights = new FlverBoneWeights(65535, 65535, 65535, 65535);
                                //TODO: FIND OUT WHY AUTO MAX WEIGHT WAS TURNING OBJECT THE COLOR OF ITS NORMALS INGAME????????!!!!!!!!!!!!!!!
                            }
                            flverMesh.IsDynamic = false;
                        }
                        else
                        {
                            flverMesh.IsDynamic = true;
                        }



                    }
                }

                if (bonesReferencedByThisMesh.Count == 0)
                {
                    bonesReferencedByThisMesh.Add(flver.Bones[0]);
                }

                

                foreach (var refBone in bonesReferencedByThisMesh)
                {
                    flverMesh.BoneIndices.Add(flver.Bones.IndexOf(refBone));
                }

                //foreach (var faceset in flverMesh.FaceSets)
                //{
                //    for (int i = 0; i < faceset.VertexIndices.Count; i += 3)
                //    {
                //        var a = faceset.VertexIndices[i];
                //        //var b = faceset.VertexIndices[i + 1];
                //        var c = faceset.VertexIndices[i + 2];

                //        faceset.VertexIndices[i] = c;
                //        //faceset.VertexIndices[i + 1] = b;
                //        faceset.VertexIndices[i + 2] = a;
                //    }
                //}

                var submeshVertexIndices = flverMesh.GetAllVertexIndices();

                //submeshVertexIndices.Reverse();
                //submeshVertexIndices = submeshVertexIndices.Reverse<ushort>().ToList();

                //submeshHighQualityNormals =
                //    NormalSolver.SolveNormals(submeshVertexIndices, flverMesh.Vertices,
                //    submeshVertexHighQualityBasePositions, onOutput, onError);

                if (submeshVertexHighQualityBaseUVs.Count == 0)
                {
                    PrintError($"Mesh '{fbxMesh.Name}' has no UVs. " +
                        $"UVs are needed to calculate the mesh tangents properly.");
                    return false;
                }

                submeshHighQualityTangents = TangentSolver.SolveTangents(flverMesh, submeshVertexIndices, 
                    submeshHighQualityNormals,
                    submeshVertexHighQualityBasePositions,
                    submeshVertexHighQualityBaseUVs);



                //for (int i = 0; i < flverMesh.Vertices.Count; i++)
                //{
                //    Vector3 thingy = Vector3.Cross(submeshHighQualityNormals[i],
                //        new Vector3(submeshHighQualityTangents[i].X,
                //        submeshHighQualityTangents[i].Y,
                //        submeshHighQualityTangents[i].Z));

                //    //Vector3 thingy = Vector3.Normalize(new Vector3(submeshHighQualityTangents[i].X, submeshHighQualityTangents[i].Y, submeshHighQualityTangents[i].Z));

                //    //var transThingy = FbxPipeline.Vector3.Transform(new FbxPipeline.Vector3(thingy.X, thingy.Y, thingy.Z),
                //    //    FbxPipeline.Matrix.CreateRotationZ(MathHelper.PiOver2));

                //    //transThingy = FbxPipeline.Vector3.Normalize(transThingy);

                //    flverMesh.Vertices[i].BiTangent = new Vector4(thingy.X, thingy.Y, thingy.Z, 0);

                //    //DEBUG TEST//
                //    //flverMesh.Vertices[i].BiTangent = new Vector4(0, 0, 1, 1);
                //}

                //Because vertices are homogenous, a simple check of the very first one should work just fine
                structLayoutChecker.ApplyCheck(flverMesh.Vertices[0]);
                int structLayoutIndex = FlverVertexStructLayoutCheck.AddToListOnlyIfUniqueAndReturnIndex(FLVER_VertexStructLayoutChecks, structLayoutChecker);

                FlverVertexStructLayout actualStructLayout = null;

                if (structLayoutIndex < flver.VertexStructLayouts.Count)
                {
                    actualStructLayout = flver.VertexStructLayouts[structLayoutIndex];
                }
                else
                {
                    var newlyBuildVertexStructLayout = structLayoutChecker.BuildVertexStructLayout();
                    flver.VertexStructLayouts.Add(newlyBuildVertexStructLayout);
                    actualStructLayout = newlyBuildVertexStructLayout;
                }

                flverMesh.VertexGroups.Add(new FlverVertexGroup(flverMesh)
                {
                    VertexStructLayoutIndex = structLayoutIndex,
                    VertexCount = flverMesh.Vertices.Count,
                    VertexSize = actualStructLayout.GetVertexSize(),
                });

                if (flverMesh.DefaultBoneIndex < 0)
                {
                    var defaultBone = flver.FindBone(fbxMesh.Name, ignoreErrors: true);
                    if (defaultBone != null)
                    {
                        flverMesh.DefaultBoneIndex = flver.Bones.IndexOf(defaultBone);
                    }
                    else
                    {
                        flverMesh.DefaultBoneIndex = -1;
                    }
                }

                flver.Submeshes.Add(flverMesh);
            }

            OrientationSolver.SolveOrientation(flver, ImportSkeletonPath == null);



            return true;
        }

        

        public bool Import()
        {
            CheckResourceLoad();

            EntityBND entityBnd = null;

            if (EntityBndPath.ToUpper().EndsWith(".DCX"))
            {
                entityBnd = DataFile.LoadFromDcxFile<EntityBND>(EntityBndPath);
            }
            else
            {
                entityBnd = DataFile.LoadFromFile<EntityBND>(EntityBndPath);
            }

            if (entityBnd.Models.Count <= EntityModelIndex)
            {
                PrintError($"Entity Model BND '{EntityBndPath}' " +
                    $"does not include a model with index {EntityModelIndex}.");
                return false;
            }

            var fbxImporter = new Microsoft.Xna.Framework.Content.Pipeline.FbxImporter();
            var context = new DSFBXContentImporterContext();
            var fbx = fbxImporter.Import(FbxPath, context);

            OnFbxLoaded(fbx);

            var flver = new FLVER();

            if (!LoadFbxIntoFlver(fbx, flver))
            {
                PrintError("Import failed.");
                return false;
            }

            string shortModelName = EntityBndPath;
            shortModelName = shortModelName.Substring(shortModelName.LastIndexOf('\\') + 1);
            shortModelName = shortModelName.Substring(0, shortModelName.IndexOf('.'));

            
            var rootBones = flver.Bones.Where(x => x.Name.ToUpper() == "ROOT").ToList();

            int rootBoneIndex = 0;

            //if (importSkeletonPath == null)
            //{
            //    if (rootBones.Count == 0)
            //    {
            //        rootBoneIndex = flver.Bones.Count;

            //        var previousSibling = flver.Bones
            //            .Where(b => b.ParentIndex == -1)
            //            .Last();

            //        previousSibling.NextSiblingIndex = (short)rootBoneIndex;

            //        flver.Bones.Add(new FlverBone(flver)
            //        {
            //            Name = shortModelName,
            //            PreviousSiblingIndex = (short)(flver.Bones.IndexOf(previousSibling)),
            //        });
            //    }
            //    //else
            //    //{
            //    //    for (int i = rootBones.Count - 1; i >= 0; i--)
            //    //    {
            //    //        rootBones[i].Name = shortModelName;
            //    //        flver.Bones.Remove(rootBones[i]);
            //    //        flver.Bones.Insert(0, rootBones[i]);
            //    //    }
            //    //    rootBoneIndex = 0;
            //    //}
            //}


            //// TEST ////
            //flver.Dummies.Clear();
            //flver.Bones.Clear();
            //// TEST ////
            

            //flver.Bones.Add(new FlverBone(flver)
            //{
            //    Name = shortModelName,
            //    NextSiblingIndex = 1,
            //});

            entityBnd.Models[EntityModelIndex].Mesh = flver;

            entityBnd.Models[EntityModelIndex].Textures.Clear();

            foreach (var submesh in flver.Submeshes)
            {
                if (submesh.Material.MTDName == null)
                {
                    if (submesh.Material.Name == "DSFBX_Placeholder")
                    {
                        submesh.Material.MTDName = "P_Metal[DSB].mtd";

                        string placeholderName = $"{shortModelName.Replace('.', '_')}_DSFBX_Placeholder";

                        while (entityBnd.Models[EntityModelIndex].Textures.ContainsKey(placeholderName)
                            || entityBnd.Models[EntityModelIndex].Textures.ContainsKey(placeholderName + "_n")
                            || entityBnd.Models[EntityModelIndex].Textures.ContainsKey(placeholderName + "_s")

                            || entityBnd.Models[EntityModelIndex].TextureFlags.ContainsKey(placeholderName)
                            || entityBnd.Models[EntityModelIndex].TextureFlags.ContainsKey(placeholderName + "_n")
                            || entityBnd.Models[EntityModelIndex].TextureFlags.ContainsKey(placeholderName + "_s")
                            )
                        {
                            placeholderName = Util.GetIncrementedName(placeholderName);
                        }

                        entityBnd.Models[EntityModelIndex].Textures.Add(placeholderName, DSFBX_PLACEHOLDER_DIFFUSE);
                        entityBnd.Models[EntityModelIndex].Textures.Add(placeholderName + "_n", DSFBX_PLACEHOLDER_BUMPMAP);
                        entityBnd.Models[EntityModelIndex].Textures.Add(placeholderName + "_s", DSFBX_PLACEHOLDER_SPECULAR);

                        entityBnd.Models[EntityModelIndex].TextureFlags.Add(placeholderName, 0);
                        entityBnd.Models[EntityModelIndex].TextureFlags.Add(placeholderName + "_n", 0);
                        entityBnd.Models[EntityModelIndex].TextureFlags.Add(placeholderName + "_s", 0);

                        submesh.Material.Parameters = new List<FlverMaterialParameter>();

                        submesh.Material.Parameters.Add(new FlverMaterialParameter()
                        {
                            Name = "g_Diffuse",
                            Value = placeholderName
                        });

                        submesh.Material.Parameters.Add(new FlverMaterialParameter()
                        {
                            Name = "g_Specular",
                            Value = placeholderName + "_s"
                        });

                        submesh.Material.Parameters.Add(new FlverMaterialParameter()
                        {
                            Name = "g_Bumpmap",
                            Value = placeholderName + "_n"
                        });

                        submesh.Material.Parameters.Add(new FlverMaterialParameter()
                        {
                            Name = "g_DetailBumpmap",
                            Value = ""
                        });
                    }
                    else
                    {
                        throw new Exception("Material was null but a placeholder material did not take its place!");
                    }
                }
                else
                {
                    foreach (var matParam in submesh.Material.Parameters.OrderBy(x => x.Value))
                    {
                        if (!string.IsNullOrWhiteSpace(matParam.Value))
                        {
                            string ShortName = matParam.Value;
                            ShortName = ShortName.Substring(ShortName.LastIndexOf('\\') + 1);
                            ShortName = ShortName.Substring(0, ShortName.IndexOf('.'));

                            if (entityBnd.Models[EntityModelIndex].Textures.ContainsKey(ShortName))
                            {
                                PrintWarning($"Texture '{ShortName}' is referenced by multiple materials in the " +
                                    $"same entity. Duplicating data so the game can load it properly. Consider " +
                                    $"reworking the textures to be their own separate maps to prevent redundant" +
                                    $" data in the future.");

                                ShortName = Util.GetIncrementedName(ShortName);
                            }

                            var ddsBytes = File.ReadAllBytes(matParam.Value);
                            if (!entityBnd.Models[EntityModelIndex].Textures.ContainsKey(ShortName))
                                entityBnd.Models[EntityModelIndex].Textures.Add(ShortName, ddsBytes);

                            matParam.Value = ShortName;


                            if (!entityBnd.Models[EntityModelIndex].TextureFlags.ContainsKey(ShortName))
                            {

                                if (matParam.Name == "g_Diffuse")
                                {
                                    entityBnd.Models[EntityModelIndex].TextureFlags.Add(ShortName, 0x5);
                                }
                                else if (matParam.Name == "g_Specular")
                                {
                                    entityBnd.Models[EntityModelIndex].TextureFlags.Add(ShortName, 0x5);
                                }
                                else if (matParam.Name == "g_Bumpmap")
                                {
                                    entityBnd.Models[EntityModelIndex].TextureFlags.Add(ShortName, 0x18);
                                }
                                else
                                {
                                    entityBnd.Models[EntityModelIndex].TextureFlags.Add(ShortName, 0);
                                }

                            }
                        }

                        //if (submesh.DefaultBoneIndex == -1)
                        //{
                        //    submesh.DefaultBoneIndex = rootBoneIndex;
                        //}
                    }
                }
                
            }

            if (ImportSkeletonPath != null)
            {
                EntityBND skeletonSourceEntityBnd = null;

                if (ImportSkeletonPath.ToUpper().EndsWith(".DCX"))
                {
                    skeletonSourceEntityBnd = DataFile.LoadFromDcxFile<EntityBND>(ImportSkeletonPath);
                }
                else
                {
                    skeletonSourceEntityBnd = DataFile.LoadFromFile<EntityBND>(ImportSkeletonPath);
                }

                entityBnd.Models[EntityModelIndex].Mesh.Bones = skeletonSourceEntityBnd.Models[0].Mesh.Bones;
                entityBnd.Models[EntityModelIndex].Mesh.Dummies = skeletonSourceEntityBnd.Models[0].Mesh.Dummies;

                foreach (var b in entityBnd.Models[EntityModelIndex].Mesh.Bones)
                {
                    b.Translation *= (float)(ImportedSkeletonScalePercent / 100);
                    if (b.BoundingBoxMax != null)
                    {
                        b.BoundingBoxMax *= (float)(ImportedSkeletonScalePercent / 100);
                    }

                    if (b.BoundingBoxMin != null)
                    {
                        b.BoundingBoxMin *= (float)(ImportedSkeletonScalePercent / 100);
                    }
                }

                foreach (var d in entityBnd.Models[EntityModelIndex].Mesh.Dummies)
                {
                    d.Position *= (float)(ImportedSkeletonScalePercent / 100);
                }

                //entityBnd.Models[EntityModelIndex].Mesh.Header = skeletonSourceEntityBnd.Models[0].Mesh.Header;
            }

            OnFlverGenerated(entityBnd.Models[EntityModelIndex].Mesh);

            

            if (EntityBndPath.ToUpper().EndsWith(".DCX"))
            {
                DataFile.ResaveDcx(entityBnd);
            }
            else
            {
                DataFile.Resave(entityBnd);
            }

            Print("\n\nImport complete.");

            return true;
        }

        public async Task<bool> BeginImport()
        {
            bool taskSuccess = await Task.Run(() =>
            {
                OnImportStarted();

                if (GenerateBackup && !File.Exists(EntityBndPath + ".bak"))
                {
                    File.Copy(EntityBndPath, EntityBndPath + ".bak");
                }

                bool isSuccess = false;

                try
                {
                    isSuccess = Import();
                }
                catch (Exception ex)
                {
                    PrintError($"Exception encountered while attempting to import:\n\n{ex}");

                    if (File.Exists(EntityBndPath + ".bak"))
                        File.Copy(EntityBndPath + ".bak", EntityBndPath, true);
                    else
                        PrintError("Unfortunately, the automatically-generated " +
                            "backup file was mysteriously gone and you are now most likely left " +
                            "with an empty entity BND file (please notify me, " +
                            "Meowmaritus, if you ever see this error message).");
                }

                OnImportEnding();

                return isSuccess;
            });

            return taskSuccess;
        }
    }
}
