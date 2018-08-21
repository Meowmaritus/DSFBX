using MeowDSIO;
using MeowDSIO.DataFiles;
using MeowDSIO.DataTypes.BND;
using MeowDSIO.DataTypes.FLVER;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace DSFBX.ModelViewer
{
    public class MyGame : Game
    {
        public bool IsForceBrowse = false;
        bool IsFirstTimeLoadContent = true;
        public bool IsQuickRunFromModelImporter = false;

        Rectangle oldBounds = Rectangle.Empty;

        double prevGameTime = 0;

        SpriteFont DEBUG_FONT;
        string DEBUG_FONT_NAME = "Content\\CJKFontMono12pt";

        SpriteFont DEBUG_FONT_SMALL;
        string DEBUG_FONT_SMALL_NAME = "Content\\CJKFont10pt";

        private static FrameCounter FPS = new FrameCounter();
        public ModelList ModelListWindow = new ModelList();
        //public bool CurrentlyRebuildingHitboxPrimitives = false;
        //public EventWaitHandle RebuildHitboxPrimitivesWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        //public EventWaitHandle CanDrawContinueWaitHandle = new EventWaitHandle(true, EventResetMode.ManualReset);
        //public const bool ENABLE_BONE_ARROW = true;
        public const float BONE_JOINT_RADIUS = 0.025f;
        //public const bool ENABLE_BONE_BOUNDING_BOX = true;
        public const bool ENABLE_HIT_SPHERE = true;
        public const float HIT_SPHERE_RADIUS = 0.01f;
        private static Random RAND = new Random();
        private SpriteBatch spriteBatch;
        public string[] inputFiles = new string[0];
        private int inputFileSubmeshIndex = -1;
        private GraphicsDeviceManager graphics;
        private FLVER[] flvers;
        private List<Matrix> flverBoneParentMatrices = new List<Matrix>();
        private List<Color> flverBoneColors = new List<Color>();
        private Vector3 cameraRotation = Vector3.Zero;

        private Vector3 lightRotation = Vector3.Zero;
        Vector3 cameraPositionDefault = Vector3.Zero;
        private Vector3 cameraPosition = Vector3.Zero;
        //private float cameraDistance = 3f;
        //private float cameraHeight = -2f;
        private VertexPositionColorNormalTangent[][][] primitiveDatas;
        private VertexPositionColor[] hitboxPrimitives;
        private VertexPositionColor[] hitboxPrimitives_Grid;
        private Texture2D[] submeshTextures;
        private int[] hitboxIndices;
        private int[] hitboxIndices_Grid;
        private Dictionary<int, Color> dmyColorMapping = new Dictionary<int, Color>();
        private Vector2 oldMouse = Vector2.Zero;
        private int oldWheel = 0;
        private bool oldMouseClick = false;
        private List<int> DummyIdList;

        private bool prev_isToggleAllSubmeshKeyPressed = false;
        private bool prev_isToggleAllDummyKeyPressed = false;
        private bool prev_isToggleAllBonesKeyPressed = false;

        public MyGame()
        {
            graphics = new GraphicsDeviceManager(this);
            IsMouseVisible = true;
            base.Window.Title = "DSFBX Model Viewer";
        }

        protected override void Initialize()
        {
            
            base.IsFixedTimeStep = false;
            base.Window.AllowUserResizing = true;
            base.Window.ClientSizeChanged += Window_ClientSizeChanged;

            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            graphics.ApplyChanges();
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width - 360;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height - 128;
            graphics.ApplyChanges();
            Window.Position = new Microsoft.Xna.Framework.Point(320, 32);

            base.Initialize();
        }

        public void LoadNewModels(string[] newModels)
        {
            inputFiles = newModels;
            IsForceBrowse = false;
            LoadContent();
        }

        protected override void LoadContent()
        {
            if (inputFiles.Length < 1 || IsForceBrowse)
            {
                IsForceBrowse = false;
                OpenFileDialog browseDlg = new OpenFileDialog()
                {
                    Title = "Open Model File",
                    CheckFileExists = true,
                    CheckPathExists = true,
                    Multiselect = true,
                };
                var dlgResult = browseDlg.ShowDialog();
                if (dlgResult != DialogResult.OK)
                {
                    return;
                }
                else
                {
                    if (browseDlg.FileNames.Length > 0)
                        inputFiles = browseDlg.FileNames;
                    else
                        inputFiles = new string[] { browseDlg.FileName };
                }
            }

            if (ModelListWindow != null)
            {
                if (ModelListWindow.IsDebugNormalShader.IsChecked == true)
                {
                    if (!inputFiles.Contains(@"Content\WP_A_0221.partsbnd"))
                    {
                        Array.Resize(ref inputFiles, inputFiles.Length + 1);
                        inputFiles[inputFiles.Length - 1] = @"Content\WP_A_0221.partsbnd";
                    }
                }
                else
                {
                    if (inputFiles.Contains(@"Content\WP_A_0221.partsbnd"))
                    {
                        var newInputFiles = inputFiles.ToList();
                        newInputFiles.Remove(@"Content\WP_A_0221.partsbnd");
                        inputFiles = newInputFiles.ToArray();
                    }
                }
                
            }

            spriteBatch = new SpriteBatch(GraphicsDevice);

            //FONT = Content.Load<SpriteFont>("FONT");

            DEBUG_FONT = Content.Load<SpriteFont>(DEBUG_FONT_NAME);
            DEBUG_FONT_SMALL = Content.Load<SpriteFont>(DEBUG_FONT_SMALL_NAME);

            LoadModel();

            for (int i = 0; i < inputFiles.Length; i++)
                BuildGeometry(i);

            CreateModelListWindow();

            for (int i = 0; i < inputFiles.Length; i++)
                BuildModelPrimitives(i);

            BuildHitboxPrimitives();
            BuildHitboxPrimitives_Grid();

            System.Windows.Forms.Form myForm
                = (System.Windows.Forms.Form)
                System.Windows.Forms.Form.FromHandle(this.Window.Handle);
            myForm.Focus();
            myForm.Activate();

            //ModelListWindow.Activate();

            IsFirstTimeLoadContent = false;
        }

        private void LoadModel()
        {
            if (inputFiles.Length == 0)
            {
                Exit();
                return;
            }

            flvers = new FLVER[inputFiles.Length];
            primitiveDatas = new VertexPositionColorNormalTangent[inputFiles.Length][][];

            for (int i = 0; i < inputFiles.Length; i++)
            {
                // Console.Write($"File {++num} of {inputFiles.Length}...");

                string file = inputFiles[i];

                string justFile = new FileInfo(file).Name;

                string fileType = justFile.ToUpper().Substring(justFile.IndexOf('.') + 1);

                if (fileType.Contains("DCX"))
                {
                    string innerFileType = fileType.Substring(0, fileType.IndexOf("."));

                    if (innerFileType == "FLVER")
                    {
                        flvers[i] = DataFile.LoadFromDcxFile<FLVER>(file);
                    }
                    else if (innerFileType.Contains("BND"))
                    {
                        var bnd = DataFile.LoadFromDcxFile<EntityBND>(file);
                        //bnd.CreateBackup();
                        flvers[i] = bnd.Models[0].Mesh;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Unrecognized input file extension (this program accepts more than 1 format of file and uses the extension to determine which).");
                        Exit();
                    }
                }
                else
                {
                    if (fileType == "FLVER")
                    {
                        flvers[i] = DataFile.LoadFromFile<FLVER>(file);
                    }
                    else if (fileType.Contains("BND"))
                    {
                        var bnd = DataFile.LoadFromFile<EntityBND>(file);
                        //bnd.CreateBackup();
                        flvers[i] = bnd.Models[0].Mesh;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show("Unrecognized input file extension (this program accepts more than 1 format of file and uses the extension to determine which).");
                        Exit();
                    }
                }

            }

            //if (inputFiles.Length > 1)
            //    System.Windows.MessageBox.Show("Only the first input file will have dummy points or bones.");

        }

        private void BuildGeometry(int f)
        {
            primitiveDatas[f] = new VertexPositionColorNormalTangent[flvers[f].Submeshes.Count][];

            

            //List<Matrix> flverBoneParentMatrices_BeforeLinkedBones = new List<Matrix>();
            if (f == 0)
            {
                flverBoneParentMatrices = new List<Matrix>();
                flverBoneColors = new List<Color>();

                Matrix GetParentBoneMatrix(FlverBone bone)
                {
                    FlverBone parent = bone;

                    var boneParentMatrix = Matrix.Identity;

                    do
                    {
                        if (parent != null)
                        {
                            boneParentMatrix *= Matrix.CreateScale(parent.Scale);
                            boneParentMatrix *= Matrix.CreateRotationX(parent.EulerRadian.X);
                            boneParentMatrix *= Matrix.CreateRotationZ(parent.EulerRadian.Z);
                            boneParentMatrix *= Matrix.CreateRotationY(parent.EulerRadian.Y);
                            boneParentMatrix *= Matrix.CreateTranslation(parent.Translation);

                            parent = parent.GetParent();
                        }


                    }
                    while (parent != null);

                    return boneParentMatrix;
                }

                for (int i = 0; i < flvers[f].Bones.Count; i++)
                {
                    flverBoneParentMatrices.Add(GetParentBoneMatrix(flvers[f].Bones[i]));
                    flverBoneColors.Add(Color.LightGray);
                }

                ModelListWindow.NumSubmeshes = flvers[f].Submeshes.Count;
                ModelListWindow.SubmeshNames = new List<string>();
                ModelListWindow.SubmeshMaterialNames = new List<string>();

                foreach (var sm in flvers[f].Submeshes)
                {
                    if (sm.NameBoneIndex >= 0 && sm.NameBoneIndex < flvers[f].Bones.Count)
                    {
                        ModelListWindow.SubmeshNames.Add(flvers[f].Bones[sm.NameBoneIndex].Name);
                    }
                    else
                    {
                        ModelListWindow.SubmeshNames.Add(null);
                    }

                    if (sm.Material != null)
                    {
                        ModelListWindow.SubmeshMaterialNames.Add($"{sm.Material.Name}" +
                            $" | {MiscUtil.GetFileNameWithoutDirectoryOrExtension(sm.Material.MTDName)}");
                    }
                    else
                    {
                        ModelListWindow.SubmeshMaterialNames.Add(null);
                    }
                }

                DummyIdList = new List<int>();

                foreach (var dmy in flvers[f].Dummies)
                {
                    if (!DummyIdList.Contains(dmy.TypeID))
                        DummyIdList.Add(dmy.TypeID);
                }

                DummyIdList = DummyIdList.OrderBy(x => x).ToList();

                dmyColorMapping.Clear();

                foreach (var dmyId in DummyIdList)
                {
                    dmyColorMapping.Add(dmyId, GET_RANDOM_DUMMY_COLOR());
                }
            }
            
        }

        private void BuildModelPrimitives(int f)
        {
            //for (int i = 0; i < flver.Bones.Count; i++)
            //{
            //    var bone = flver.Bones[i];



            //    var boneParentMatrix = flverBoneParentMatrices_BeforeLinkedBones[i];



            //    //var bonesControllingThisOne = flver.Bones.Where(b => b.LinkedBoneIndex == flver.Bones.IndexOf(bone));
            //    //foreach (var linkedBone in bonesControllingThisOne)
            //    //{
            //    //    var thisIndex = flver.Bones.IndexOf(linkedBone);
            //    //    //var linkedBone = flver.Bones[bone.LinkedBoneIndex];

            //    //    boneParentMatrix *= flverBoneParentMatrices_BeforeLinkedBones[thisIndex];

            //    //    boneParentMatrix *= Matrix.CreateScale(linkedBone.Scale);
            //    //    boneParentMatrix *= Matrix.CreateRotationX(linkedBone.EulerRadian.X);
            //    //    boneParentMatrix *= Matrix.CreateRotationZ(linkedBone.EulerRadian.Z);
            //    //    boneParentMatrix *= Matrix.CreateRotationY(linkedBone.EulerRadian.Y);
            //    //    boneParentMatrix *= Matrix.CreateTranslation(linkedBone.Translation);
            //    //}



            //    if (bone.LinkedBoneIndex >= 0 && bone.LinkedBoneIndex < flver.Bones.Count)
            //    {
            //        //boneParentMatrix *= flverBoneParentMatrices_BeforeLinkedBones[bone.LinkedBoneIndex];

            //        var linkedBone = flver.Bones[bone.LinkedBoneIndex];

            //        boneParentMatrix *= Matrix.CreateScale(linkedBone.Scale);
            //        boneParentMatrix *= Matrix.CreateRotationX(linkedBone.EulerRadian.X);
            //        boneParentMatrix *= Matrix.CreateRotationZ(linkedBone.EulerRadian.Z);
            //        boneParentMatrix *= Matrix.CreateRotationY(linkedBone.EulerRadian.Y);
            //        boneParentMatrix *= Matrix.CreateTranslation(linkedBone.Translation);
            //    }

            //    flverBoneParentMatrices.Add(boneParentMatrix);
            //}

            float MAX_VERTEX_Y = float.NegativeInfinity;

            for (int i = 0; i < flvers[f].Submeshes.Count; i++)
            {
                if (inputFileSubmeshIndex >= 0 && inputFileSubmeshIndex < flvers[f].Submeshes.Count && inputFileSubmeshIndex != i)
                    continue;

                primitiveDatas[f][i] = new VertexPositionColorNormalTangent[flvers[f].Submeshes[i].Vertices.Count];

                for (int j = 0; j < flvers[f].Submeshes[i].Vertices.Count; j++)
                {
                    var vert = flvers[f].Submeshes[i].Vertices[j];

                    primitiveDatas[f][i][j] = new VertexPositionColorNormalTangent();

                    primitiveDatas[f][i][j].Position = vert.Position + new Vector3(f * (float)ModelListWindow.SliderMultimeshSpacing.Value, 0, 0);

                    if (vert.Position.Y > MAX_VERTEX_Y)
                        MAX_VERTEX_Y = vert.Position.Y;



                    

                    //primitiveDatas[f][i][j].Color = new Vector4((Vector3)vert.Normal, 1);

                    if (ModelListWindow.IsDebugNormalShader.IsChecked == true)
                    {
                        primitiveDatas[f][i][j].Color = new Vector4(
                            (vert.Normal.X * 0.25f) + 0.25f, 
                            (vert.Normal.Y * 0.25f) + 0.25f, 
                            (vert.Normal.Z * 0.25f) + 0.25f, 
                            1);

                        primitiveDatas[f][i][j].Normal = Vector3.Forward;
                    }
                    else
                    {
                        primitiveDatas[f][i][j].Color = new Vector4(0.75f, 0.75f, 0.75f, 1);

                        if (vert.Normal != null)
                        {
                            primitiveDatas[f][i][j].Normal = Vector3.Normalize((Vector3)vert.Normal);
                        }
                    }

                    

                    //if (vert.VertexColor != null)
                    //{
                    //    primitiveDatas[f][i][j].Color = (Vector4)vert.VertexColor;

                    //}

                    if (vert.BiTangent != null)
                    {
                        primitiveDatas[f][i][j].Tangent =
                                Vector3.Cross((Vector3)vert.Normal, new Vector3(vert.BiTangent.X, vert.BiTangent.Y, vert.BiTangent.Z))
                                * vert.BiTangent.W;
                    }

                    //if (vert.UVs.Count > 0)
                    //{
                    //    primitiveDatas[f][i][j].TextureCoordinate = vert.UVs[0];
                    //}
                    //else
                    //{
                    //    primitiveDatas[f][i][j].TextureCoordinate = Vector2.Zero;
                    //}

                }
            }

            if (MAX_VERTEX_Y > float.NegativeInfinity)
            {
                cameraPositionDefault = new Vector3(0, (MAX_VERTEX_Y / -2), -(MAX_VERTEX_Y / 2));
                if (IsFirstTimeLoadContent)
                {
                    cameraPosition = cameraPositionDefault;
                }
            }
        }

        private void CreateModelListWindow()
        {
            ModelListWindow.DummyIDs = DummyIdList;
            ModelListWindow.DummyColors = dmyColorMapping.Values.ToList();

            ModelListWindow.BoneNames = new List<string>();
            ModelListWindow.BoneScales = new List<FlverVector3>();
            ModelListWindow.BoneIndents = new List<int>();

            for (int i = 0; i < flvers[0].Bones.Count; i++)
            {
                ModelListWindow.BoneNames.Add(flvers[0].Bones[i].Name);
                ModelListWindow.BoneScales.Add(flvers[0].Bones[i].Scale);

                int indent = 0;

                int parentIndex = flvers[0].Bones[i].ParentIndex;

                while (parentIndex >= 0 && parentIndex < flvers[0].Bones.Count)
                {
                    indent++;
                    var parent = flvers[0].Bones[parentIndex];
                    parentIndex = parent.ParentIndex;
                }

                ModelListWindow.BoneIndents.Add(indent);
            }

            if (IsFirstTimeLoadContent)
            {
                ModelListWindow.CheckChangedOrSomething += ModelListWindow_CheckChangedOrSomething;
                ModelListWindow.CheckChangedOrSomething_Grid += ModelListWindow_CheckChangedOrSomething_Grid;
                ModelListWindow.CheckChangedOrSomething_FullMeshRebuild += ModelListWindow_CheckChangedOrSomething_FullMeshRebuild;
                ModelListWindow.RandomizedDummyColors += ModelListWindow_RandomizedDummyColors;
            }

            ModelListWindow.InitCheckboxes();

            ModelListWindow.Show();
        }

        private void ModelListWindow_CheckChangedOrSomething_FullMeshRebuild(object sender, EventArgs e)
        {
            IS_CURRENTLY_REBUILDING_ENTIRE_MESH_FATCAT = true;
        }

        private bool IS_CURRENTLY_REBUILDING_ENTIRE_MESH_FATCAT = false;
        private bool IS_CURRENTLY_UPDATING_HITBOXES_FATCAT = false;
        private bool IS_CURRENTLY_UPDATING_GRID_FATCAT = false;

        private void ModelListWindow_CheckChangedOrSomething(object sender, EventArgs e)
        {
            //if (IS_CURRENTLY_UPDATING_FATCAT)
            //    return;
            IS_CURRENTLY_UPDATING_HITBOXES_FATCAT = true;

            //RebuildHitboxPrimitivesWaitHandle.WaitOne();
            //CanDrawContinueWaitHandle.Reset();
            //BuildHitboxPrimitives();
            //CanDrawContinueWaitHandle.Set();
            //IS_CURRENTLY_UPDATING_FATCAT = false;
        }

        private void ModelListWindow_CheckChangedOrSomething_Grid(object sender, EventArgs e)
        {
            IS_CURRENTLY_UPDATING_GRID_FATCAT = true;
        }

        private Matrix CAMERA_MATRIX()
        {
            return Matrix.CreateTranslation(cameraPosition.X, cameraPosition.Y, cameraPosition.Z)
                
                * Matrix.CreateRotationY(cameraRotation.Y)
                * Matrix.CreateRotationZ(cameraRotation.Z)
                * Matrix.CreateRotationX(cameraRotation.X)
                
                ;
        }

        private void Draw3D()
        {
            if (IS_CURRENTLY_REBUILDING_ENTIRE_MESH_FATCAT)
            {
                LoadContent();
                IS_CURRENTLY_REBUILDING_ENTIRE_MESH_FATCAT = false;
            }

            var basicEffect = new BasicEffect(GraphicsDevice);

            // Transform your model to place it somewhere in the world
            basicEffect.World = Matrix.CreateRotationY(MathHelper.Pi)
                * Matrix.CreateTranslation(0, 0, -5)
                * Matrix.CreateScale(-1, 1, 1);
                ;


            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            GraphicsDevice.RasterizerState = new RasterizerState()
            {
                MultiSampleAntiAlias = true,
                CullMode = CullMode.CullClockwiseFace,
            };
            //basicEffect.World = Matrix.Identity; // Use this to leave your model at the origin
            // Transform the entire world around (effectively: place the camera)
            basicEffect.View = CAMERA_MATRIX();// Matrix.CreateLookAt(new Vector3(0, 0, -1), Vector3.Zero, Vector3.Up);
            // Specify how 3D points are projected/transformed onto the 2D screen
            basicEffect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60),
                    (float)GraphicsDevice.Viewport.Width / (float)GraphicsDevice.Viewport.Height, 0.001f, 1000000.0f);



            if (IS_CURRENTLY_UPDATING_GRID_FATCAT)
            {
                BuildHitboxPrimitives_Grid();
                IS_CURRENTLY_UPDATING_GRID_FATCAT = false;
            }

            if (hitboxPrimitives_Grid.Length > 0)
            {
                basicEffect.LightingEnabled = false;
                basicEffect.VertexColorEnabled = true;

                GraphicsDevice.DepthStencilState = DepthStencilState.None;

                // Render with a BasicEffect that was created in LoadContent
                // (BasicEffect only has one pass - but effects in general can have many rendering passes)
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    VertexBuffer hitboxVertBuffer_Grid = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), hitboxPrimitives_Grid.Length, BufferUsage.None);

                    hitboxVertBuffer_Grid.SetData(hitboxPrimitives_Grid);

                    IndexBuffer hitboxIndexBuffer_Grid = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, hitboxIndices_Grid.Length, BufferUsage.None);
                    hitboxIndexBuffer_Grid.SetData(hitboxIndices_Grid);

                    GraphicsDevice.SetVertexBuffer(hitboxVertBuffer_Grid);
                    GraphicsDevice.Indices = hitboxIndexBuffer_Grid;
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, hitboxIndices_Grid.Length);

                    hitboxVertBuffer_Grid.Dispose();
                    hitboxIndexBuffer_Grid.Dispose();
                }
            }

            basicEffect.VertexColorEnabled = true;

            if (ModelListWindow.IsDebugNormalShader.IsChecked == true)
            {
                basicEffect.LightingEnabled = false;
                basicEffect.AmbientLightColor = Vector3.One;
                basicEffect.DirectionalLight0.Enabled = false;
                basicEffect.DirectionalLight1.Enabled = false;
                basicEffect.DirectionalLight2.Enabled = false;
                basicEffect.SpecularPower = 0;
                basicEffect.SpecularColor = Vector3.One;
                basicEffect.EmissiveColor = Vector3.One;
                basicEffect.DiffuseColor = Vector3.One;
            }
            else
            {
                basicEffect.LightingEnabled = true;

                basicEffect.EnableDefaultLighting();

                basicEffect.AmbientLightColor *= 0.66f;

                basicEffect.DirectionalLight0.Direction = Vector3.Transform(Vector3.Forward,
                    Matrix.CreateRotationY(lightRotation.Y)
                    * Matrix.CreateRotationZ(lightRotation.Z)
                    * Matrix.CreateRotationX(lightRotation.X)
                    );

                basicEffect.DirectionalLight0.DiffuseColor *= 0.66f;
                basicEffect.DirectionalLight0.SpecularColor *= 0.15f;

                //basicEffect.DirectionalLight1.DiffuseColor *= 0.25f;
                //basicEffect.DirectionalLight1.SpecularColor *= 0.25f;

                //basicEffect.DirectionalLight2.DiffuseColor *= 0.25f;
                //basicEffect.DirectionalLight2.SpecularColor *= 0.25f;

                basicEffect.DirectionalLight0.Enabled = true;
                basicEffect.DirectionalLight1.Enabled = false;
                basicEffect.DirectionalLight2.Enabled = false;
            }

            

            basicEffect.TextureEnabled = false;

            

            // I'm setting this so that *both* sides of your triangle are drawn
            // (so it won't be back-face culled if you move it, or the camera around behind it)
            //GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            GraphicsDevice.DepthStencilState = DepthStencilState.Default;

            // Render with a BasicEffect that was created in LoadContent
            // (BasicEffect only has one pass - but effects in general can have many rendering passes)
            foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
            {
                // This is the all-important line that sets the effect, and all of its settings, on the graphics device
                pass.Apply();

                //if (ModelListWindow.IsDebugNormalShader.IsChecked == true)
                //{
                //    var debugNormalStacks = 20;
                //    var debugNormalSlices = 20;
                //    var debugNormalRadius = 0.1f;

                //    var debugNormalPrimitives = new VertexPositionColor[(debugNormalSlices + 1) * (debugNormalStacks + 1)];

                //    float phi, theta;
                //    float dphi = MathHelper.Pi / debugNormalStacks;
                //    float dtheta = MathHelper.TwoPi / debugNormalSlices;
                //    float x, y, z, sc;
                //    int index = 0;

                //    for (int stack = 0; stack <= debugNormalStacks; stack++)
                //    {
                //        phi = MathHelper.PiOver2 - stack * dphi;
                //        y = debugNormalRadius * (float)Math.Sin(phi);
                //        sc = -debugNormalRadius * (float)Math.Cos(phi);

                //        for (int slice = 0; slice <= debugNormalSlices; slice++)
                //        {
                //            theta = slice * dtheta;
                //            x = sc * (float)Math.Sin(theta);
                //            z = sc * (float)Math.Cos(theta);
                //            var normal = Vector3.Normalize(new Vector3(-x, y, z));
                //            debugNormalPrimitives[index++] = new VertexPositionColor(new Vector3(x + cameraPositionDefault.Z, y, z),
                //                new Color((normal.X * 0.25f) + 0.25f, (normal.Y * 0.25f) + 0.25f, (normal.Z * 0.25f) + 0.25f));
                //        }
                //    }

                //    var debugNormalIndices = new int[debugNormalSlices * debugNormalStacks * 6];
                //    index = 0;
                //    int k = debugNormalSlices + 1;

                //    for (int stack = 0; stack < debugNormalStacks; stack++)
                //    {
                //        for (int slice = 0; slice < debugNormalSlices; slice++)
                //        {
                //            debugNormalIndices[index++] = (stack + 0) * k + slice;
                //            debugNormalIndices[index++] = (stack + 1) * k + slice;
                //            debugNormalIndices[index++] = (stack + 0) * k + slice + 1;

                //            debugNormalIndices[index++] = (stack + 0) * k + slice + 1;
                //            debugNormalIndices[index++] = (stack + 1) * k + slice;
                //            debugNormalIndices[index++] = (stack + 1) * k + slice + 1;
                //        }
                //    }

                //    VertexBuffer v = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), debugNormalPrimitives.Length, BufferUsage.None);

                //    v.SetData(debugNormalPrimitives);

                //    GraphicsDevice.SetVertexBuffer(v);

                //    IndexBuffer lineListIndexBuffer = new IndexBuffer(
                //                GraphicsDevice,
                //                IndexElementSize.ThirtyTwoBits,
                //                sizeof(int) * debugNormalIndices.Length,
                //                BufferUsage.None);

                //    lineListIndexBuffer.SetData(debugNormalIndices);

                //    GraphicsDevice.Indices = lineListIndexBuffer;

                //    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, debugNormalIndices.Length / 3);

                //    lineListIndexBuffer.Dispose();


                //    v.Dispose();

                //}

                for (int f = 0; f < flvers.Length; f++)
                {
                    for (int i = 0; i < flvers[f].Submeshes.Count; i++)
                    {
                        if (f == 0)
                        {
                            if (!ModelListWindow.GetCheckModel(i))
                                continue;

                            if (inputFileSubmeshIndex >= 0 && inputFileSubmeshIndex < flvers[f].Submeshes.Count && inputFileSubmeshIndex != i)
                                continue;
                        }

                        VertexBuffer v = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColorNormalTangent), primitiveDatas[f][i].Length, BufferUsage.None);

                        v.SetData(primitiveDatas[f][i]);

                        GraphicsDevice.SetVertexBuffer(v);

                        foreach (var faceSet in flvers[f].Submeshes[i].FaceSets)
                        {
                            IndexBuffer lineListIndexBuffer = new IndexBuffer(
                                GraphicsDevice,
                                IndexElementSize.SixteenBits,
                                sizeof(short) * faceSet.VertexIndices.Count,
                                BufferUsage.None);

                            lineListIndexBuffer.SetData(faceSet.VertexIndices
                                .Select(x =>
                                {
                                    if (x == ushort.MaxValue)
                                        return (short)(-1);
                                    else
                                        return (short)x;
                                })
                                .ToArray());

                            GraphicsDevice.Indices = lineListIndexBuffer;


                            GraphicsDevice.DrawIndexedPrimitives(faceSet.IsTriangleStrip ? PrimitiveType.TriangleStrip : PrimitiveType.TriangleList, 0, 0,
                                faceSet.IsTriangleStrip ? (faceSet.VertexIndices.Count - 2) : (faceSet.VertexIndices.Count / 3));

                            lineListIndexBuffer.Dispose();

                        }


                        v.Dispose();


                    }
                }

               

            }

            //CanDrawContinueWaitHandle.WaitOne();

            //if (CurrentlyRebuildingHitboxPrimitives)
            //    return;

            if (IS_CURRENTLY_UPDATING_HITBOXES_FATCAT)
            {
                BuildHitboxPrimitives();
                IS_CURRENTLY_UPDATING_HITBOXES_FATCAT = false;
            }

            if (hitboxPrimitives.Length > 0)
            {
                basicEffect.LightingEnabled = false;
                basicEffect.VertexColorEnabled = true;

                GraphicsDevice.DepthStencilState = DepthStencilState.None;

                // Render with a BasicEffect that was created in LoadContent
                // (BasicEffect only has one pass - but effects in general can have many rendering passes)
                foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    VertexBuffer hitboxVertBuffer = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColor), hitboxPrimitives.Length, BufferUsage.None);

                    hitboxVertBuffer.SetData(hitboxPrimitives);

                    IndexBuffer hitboxIndexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, hitboxIndices.Length, BufferUsage.None);
                    hitboxIndexBuffer.SetData(hitboxIndices);

                    GraphicsDevice.SetVertexBuffer(hitboxVertBuffer);
                    GraphicsDevice.Indices = hitboxIndexBuffer;
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.LineList, 0, 0, hitboxIndices.Length);

                    hitboxVertBuffer.Dispose();
                    hitboxIndexBuffer.Dispose();
                }
            }



            basicEffect.Dispose();

            //spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);

            //spriteBatch.DrawString(DEBUG_FONT, $"FPS: {(FPS.AverageFramesPerSecond)}", new Vector2(200, 200), Color.White);

            //spriteBatch.End();

        }

        private void DrawThiccText(SpriteFont font, string text, Vector2 pos, Color color, float thiccness)
        {
            spriteBatch.DrawString(font, text, pos + new Vector2(1, 0) * thiccness, Color.Black);
            spriteBatch.DrawString(font, text, pos + new Vector2(-1, 0) * thiccness, Color.Black);
            spriteBatch.DrawString(font, text, pos + new Vector2(0, 1) * thiccness, Color.Black);
            spriteBatch.DrawString(font, text, pos + new Vector2(0, -1) * thiccness, Color.Black);

            spriteBatch.DrawString(font, text, pos + new Vector2(-1, 1) * thiccness, Color.Black);
            spriteBatch.DrawString(font, text, pos + new Vector2(-1, -1) * thiccness, Color.Black);
            spriteBatch.DrawString(font, text, pos + new Vector2(1, 1) * thiccness, Color.Black);
            spriteBatch.DrawString(font, text, pos + new Vector2(1, -1) * thiccness, Color.Black);

            spriteBatch.DrawString(font, text, pos, color);
        }

        private void Draw2D()
        {
            spriteBatch.Begin();

            int line = 0;

            DrawThiccText(DEBUG_FONT, $"FPS: {(FPS.AverageFramesPerSecond)}", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);

            line++;

            DrawThiccText(DEBUG_FONT, $"Keyboard Control (Gamepad Control): What The Control Does", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Fuchsia, 1);

            line++;

            DrawThiccText(DEBUG_FONT, $"W/A/S/D (Move Left Stick): Move Lateral", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"LMB + Move Mouse (Move Right Stick): Turn Camera", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"LMB + Spacebar + Move Mouse (D-Pad Up + Move Right Stick): Turn Light", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"Q/E (LT/RT): Move Vertical", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"Shift/Ctrl (LB/RB): Move Slow/Fast", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"R (Click Left Stick): Reset Camera", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            line++;
            DrawThiccText(DEBUG_FONT, $"Z (X): Toggle All Submeshes", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"X (Y): Toggle All Dummy Polys", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"C (B): Toggle All Bones", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            line++;

            int totalVertices = 0;

            foreach (var primA in primitiveDatas)
            {
                foreach (var primB in primA)
                {
                    foreach (var primC in primB)
                    {
                        totalVertices++;
                    }
                }
            }

            DrawThiccText(DEBUG_FONT, $"# Mesh Vertices: {totalVertices}", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"# Overlay Primitives: {hitboxPrimitives.Length}", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            line++;
            DrawThiccText(DEBUG_FONT, $"Tab (Back): TO FILE BROWSER", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"F5 (Start): RELOAD MODEL", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"Escape (LB + RB + A + Start): EXIT APPLICATION", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            line++;
            DrawThiccText(DEBUG_FONT, $"Dummy Colors:",
                new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);

            float scale = 1f;

            int i = 1;
            int j = 0;
            foreach (var kvp in dmyColorMapping)
            {
                bool isDummyActive = ModelListWindow.CheckMap_Dummy[kvp.Key].IsChecked == true;

                if (isDummyActive)
                {
                    DrawThiccText(DEBUG_FONT_SMALL, $"{kvp.Key}",
                    new Vector2(8, 8) + new Vector2(0, 16 * line) + new Vector2(12 + j * 32, i++ * 14 * scale),
                    kvp.Value, 1);
                }

                

                if (i >= 24)
                {
                    i = 1;
                    j++;
                }
            }

            spriteBatch.End();
        }

        protected override void Draw(GameTime gameTime)
        {
            

            INPUT_UPDATE(gameTime);
            //RebuildHitboxPrimitivesWaitHandle.Set();

            

            

            GraphicsDevice.Clear(Color.Gray);
            
            Draw3D();
            Draw2D();

            FPS.Update((float)(gameTime.TotalGameTime.TotalSeconds - prevGameTime));

            base.Draw(gameTime);

            prevGameTime = gameTime.TotalGameTime.TotalSeconds;
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
            graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
            graphics.ApplyChanges();

            oldMouseClick = false;
        }

        private Color GET_RANDOM_DUMMY_COLOR()
        {
            return Utils.HSLtoRGB((float)RAND.NextDouble(), 1, (float)(RAND.NextDouble() * 0.25 + 0.5));
        }

        private void ModelListWindow_RandomizedDummyColors(object sender, EventArgs e)
        {
            var allKeys = dmyColorMapping.Keys.ToList();
            foreach (var k in allKeys)
            {
                dmyColorMapping[k] = GET_RANDOM_DUMMY_COLOR();


            }

            ModelListWindow.DummyColors = dmyColorMapping.Values.ToList();
            ModelListWindow.UpdateDummyColors();
        }

        private void BuildHitboxPrimitives()
        {
            hitboxPrimitives = new VertexPositionColor[0];

            var hitboxIndexList = new List<int>();

            void AddBoundingBox(Vector3 min, Vector3 max, Matrix m, Color colorA, Color colorB)
            {
                var origLength = hitboxPrimitives.Length;
                Array.Resize(ref hitboxPrimitives, origLength + 8);

                //Top Front Left
                hitboxPrimitives[origLength] = new VertexPositionColor(new Vector3(min.X, max.Y, max.Z), colorA);
                //Top Front Right
                hitboxPrimitives[origLength + 1] = new VertexPositionColor(new Vector3(max.X, max.Y, max.Z), colorB);
                //Bottom Front Right
                hitboxPrimitives[origLength + 2] = new VertexPositionColor(new Vector3(max.X, min.Y, max.Z), colorA);
                //Bottom Front Left
                hitboxPrimitives[origLength + 3] = new VertexPositionColor(new Vector3(min.X, min.Y, max.Z), colorB);


                //Back Top Left
                hitboxPrimitives[origLength + 4] = new VertexPositionColor(new Vector3(min.X, max.Y, min.Z), colorA);
                //Back Top Right
                hitboxPrimitives[origLength + 5] = new VertexPositionColor(new Vector3(max.X, max.Y, min.Z), colorB);
                //Back Bottom Right
                hitboxPrimitives[origLength + 6] = new VertexPositionColor(new Vector3(max.X, min.Y, min.Z), colorA);
                //Back Bottom Left
                hitboxPrimitives[origLength + 7] = new VertexPositionColor(new Vector3(min.X, min.Y, min.Z), colorB);

                for (int i = origLength; i < hitboxPrimitives.Length; i++)
                {
                    hitboxPrimitives[i].Position = Vector3.Transform(hitboxPrimitives[i].Position, m);
                }

                //Front (0, 1, 2, 3):
                hitboxIndexList.Add(origLength + 0); hitboxIndexList.Add(origLength + 1);
                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 2);
                hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 3);
                hitboxIndexList.Add(origLength + 3); hitboxIndexList.Add(origLength + 0);

                //Top (0, 1, 5, 4):
                hitboxIndexList.Add(origLength + 0); hitboxIndexList.Add(origLength + 1);
                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 5);
                hitboxIndexList.Add(origLength + 5); hitboxIndexList.Add(origLength + 4);
                hitboxIndexList.Add(origLength + 4); hitboxIndexList.Add(origLength + 0);

                //Back (4, 5, 6, 7):
                hitboxIndexList.Add(origLength + 4); hitboxIndexList.Add(origLength + 5);
                hitboxIndexList.Add(origLength + 5); hitboxIndexList.Add(origLength + 6);
                hitboxIndexList.Add(origLength + 6); hitboxIndexList.Add(origLength + 7);
                hitboxIndexList.Add(origLength + 7); hitboxIndexList.Add(origLength + 4);

                //Bottom (2, 3, 7, 6):
                hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 3);
                hitboxIndexList.Add(origLength + 3); hitboxIndexList.Add(origLength + 7);
                hitboxIndexList.Add(origLength + 7); hitboxIndexList.Add(origLength + 6);
                hitboxIndexList.Add(origLength + 6); hitboxIndexList.Add(origLength + 2);

                //Left (0, 3, 7, 4):
                hitboxIndexList.Add(origLength + 0); hitboxIndexList.Add(origLength + 3);
                hitboxIndexList.Add(origLength + 3); hitboxIndexList.Add(origLength + 7);
                hitboxIndexList.Add(origLength + 7); hitboxIndexList.Add(origLength + 4);
                hitboxIndexList.Add(origLength + 4); hitboxIndexList.Add(origLength + 0);

                //Right (1, 2, 6, 5):
                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 2);
                hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 6);
                hitboxIndexList.Add(origLength + 6); hitboxIndexList.Add(origLength + 5);
                hitboxIndexList.Add(origLength + 5); hitboxIndexList.Add(origLength + 1);
            }

            void AddArrow(Matrix matrix, float length, float tipLength, float tipGirth, Color color)
            {
                var origLength = hitboxPrimitives.Length;
                Array.Resize(ref hitboxPrimitives, origLength + 6);

                //Base
                hitboxPrimitives[origLength] = new VertexPositionColor((new Vector3(0, 0, 0)), color);

                //Tip
                hitboxPrimitives[origLength + 1] = new VertexPositionColor((new Vector3(0, 0, length)), color);


                hitboxPrimitives[origLength + 2] = new VertexPositionColor((new Vector3(tipGirth, 0, length - tipLength)), color);
                hitboxPrimitives[origLength + 3] = new VertexPositionColor((new Vector3(-tipGirth, 0, length - tipLength)), color);
                hitboxPrimitives[origLength + 4] = new VertexPositionColor((new Vector3(0, tipGirth, length - tipLength)), color);
                hitboxPrimitives[origLength + 5] = new VertexPositionColor((new Vector3(0, -tipGirth, length - tipLength)), color);




                for (int i = origLength; i < hitboxPrimitives.Length; i++)
                {
                    hitboxPrimitives[i].Position = Vector3.Transform(hitboxPrimitives[i].Position, Matrix.CreateRotationY(MathHelper.PiOver2) * matrix);
                }

                hitboxIndexList.Add(origLength); hitboxIndexList.Add(origLength + 1);

                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 2);
                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 3);
                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 4);
                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 5);

                //hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 3);
                //hitboxIndexList.Add(origLength + 4); hitboxIndexList.Add(origLength + 5);

                hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 4);
                hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 5);

                hitboxIndexList.Add(origLength + 3); hitboxIndexList.Add(origLength + 4);
                hitboxIndexList.Add(origLength + 3); hitboxIndexList.Add(origLength + 5);
            }

            //void AddArrow2(Vector3 translation, Vector3 rotation, Vector3 scale, float tipLength, float tipGirth, Color color)
            //{
            //    var matrix = Matrix.CreateScale(scale)
            //        * Matrix.CreateRotationY(rotation.Y)
            //        * Matrix.CreateRotationZ(rotation.Z)
            //        * Matrix.CreateRotationX(rotation.X)
            //        * Matrix.CreateTranslation(translation);

            //    AddArrow(matrix, tipLength, tipGirth, color);
            //}

            void AddCapsule(Vector3 position, Vector3 size, Vector3 rotation, Color color, Matrix? matrixOverride = null)
            {
                var origLength = hitboxPrimitives.Length;
                Array.Resize(ref hitboxPrimitives, origLength + 12);

                float t = (float)(1.0 + Math.Sqrt(5.0f)) / 2.0f;

                hitboxPrimitives[origLength] = new VertexPositionColor((size * new Vector3(-1, 0, t)), color);
                hitboxPrimitives[origLength + 1] = new VertexPositionColor((size * new Vector3(1, 0, t)), color);
                hitboxPrimitives[origLength + 2] = new VertexPositionColor((size * new Vector3(-1, 0, -t)), color);
                hitboxPrimitives[origLength + 3] = new VertexPositionColor((size * new Vector3(1, 0, -t)), color);

                hitboxPrimitives[origLength + 4] = new VertexPositionColor((size * new Vector3(0, t, 1)), color);
                hitboxPrimitives[origLength + 5] = new VertexPositionColor((size * new Vector3(0, t, -1)), color);
                hitboxPrimitives[origLength + 6] = new VertexPositionColor((size * new Vector3(0, -t, 1)), color);
                hitboxPrimitives[origLength + 7] = new VertexPositionColor((size * new Vector3(0, -t, -1)), color);

                hitboxPrimitives[origLength + 8] = new VertexPositionColor((size * new Vector3(t, 1, 0)), color);
                hitboxPrimitives[origLength + 9] = new VertexPositionColor((size * new Vector3(-t, 1, 0)), color);
                hitboxPrimitives[origLength + 10] = new VertexPositionColor((size * new Vector3(t, -1, 0)), color);
                hitboxPrimitives[origLength + 11] = new VertexPositionColor((size * new Vector3(-t, -1, 0)), color);

                var matrix = Matrix.Identity;

                if (matrixOverride.HasValue)
                {
                    matrix = matrixOverride.Value;
                }
                else
                {
                    matrix = Matrix.CreateRotationY(rotation.Y)
                        * Matrix.CreateRotationZ(rotation.Z)
                        * Matrix.CreateRotationX(rotation.X)
                        * Matrix.CreateTranslation(position);
                }

                for (int i = origLength; i < hitboxPrimitives.Length; i++)
                {
                    hitboxPrimitives[i].Position = Vector3.Transform(hitboxPrimitives[i].Position, matrix);
                }

                hitboxIndexList.Add(origLength + 0); hitboxIndexList.Add(origLength + 6);
                hitboxIndexList.Add(origLength + 6); hitboxIndexList.Add(origLength + 1);
                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 0);

                hitboxIndexList.Add(origLength + 0); hitboxIndexList.Add(origLength + 11);
                hitboxIndexList.Add(origLength + 11); hitboxIndexList.Add(origLength + 6);
                hitboxIndexList.Add(origLength + 6); hitboxIndexList.Add(origLength + 0);

                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 4);
                hitboxIndexList.Add(origLength + 4); hitboxIndexList.Add(origLength + 0);
                hitboxIndexList.Add(origLength + 0); hitboxIndexList.Add(origLength + 1);

                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 8);
                hitboxIndexList.Add(origLength + 8); hitboxIndexList.Add(origLength + 4);
                hitboxIndexList.Add(origLength + 4); hitboxIndexList.Add(origLength + 1);

                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 10);
                hitboxIndexList.Add(origLength + 10); hitboxIndexList.Add(origLength + 8);
                hitboxIndexList.Add(origLength + 8); hitboxIndexList.Add(origLength + 1);

                hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 5);
                hitboxIndexList.Add(origLength + 5); hitboxIndexList.Add(origLength + 3);
                hitboxIndexList.Add(origLength + 3); hitboxIndexList.Add(origLength + 2);

                hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 9);
                hitboxIndexList.Add(origLength + 9); hitboxIndexList.Add(origLength + 5);
                hitboxIndexList.Add(origLength + 5); hitboxIndexList.Add(origLength + 2);

                hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 11);
                hitboxIndexList.Add(origLength + 11); hitboxIndexList.Add(origLength + 9);
                hitboxIndexList.Add(origLength + 9); hitboxIndexList.Add(origLength + 2);

                hitboxIndexList.Add(origLength + 3); hitboxIndexList.Add(origLength + 7);
                hitboxIndexList.Add(origLength + 7); hitboxIndexList.Add(origLength + 2);
                hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 3);

                hitboxIndexList.Add(origLength + 3); hitboxIndexList.Add(origLength + 10);
                hitboxIndexList.Add(origLength + 10); hitboxIndexList.Add(origLength + 7);
                hitboxIndexList.Add(origLength + 7); hitboxIndexList.Add(origLength + 3);

                hitboxIndexList.Add(origLength + 4); hitboxIndexList.Add(origLength + 8);
                hitboxIndexList.Add(origLength + 8); hitboxIndexList.Add(origLength + 5);
                hitboxIndexList.Add(origLength + 5); hitboxIndexList.Add(origLength + 4);

                hitboxIndexList.Add(origLength + 4); hitboxIndexList.Add(origLength + 9);
                hitboxIndexList.Add(origLength + 9); hitboxIndexList.Add(origLength + 0);
                hitboxIndexList.Add(origLength + 0); hitboxIndexList.Add(origLength + 4);

                hitboxIndexList.Add(origLength + 5); hitboxIndexList.Add(origLength + 8);
                hitboxIndexList.Add(origLength + 8); hitboxIndexList.Add(origLength + 3);
                hitboxIndexList.Add(origLength + 3); hitboxIndexList.Add(origLength + 5);

                hitboxIndexList.Add(origLength + 5); hitboxIndexList.Add(origLength + 9);
                hitboxIndexList.Add(origLength + 9); hitboxIndexList.Add(origLength + 4);
                hitboxIndexList.Add(origLength + 4); hitboxIndexList.Add(origLength + 5);

                hitboxIndexList.Add(origLength + 6); hitboxIndexList.Add(origLength + 10);
                hitboxIndexList.Add(origLength + 10); hitboxIndexList.Add(origLength + 1);
                hitboxIndexList.Add(origLength + 1); hitboxIndexList.Add(origLength + 6);

                hitboxIndexList.Add(origLength + 6); hitboxIndexList.Add(origLength + 11);
                hitboxIndexList.Add(origLength + 11); hitboxIndexList.Add(origLength + 7);
                hitboxIndexList.Add(origLength + 7); hitboxIndexList.Add(origLength + 6);

                hitboxIndexList.Add(origLength + 7); hitboxIndexList.Add(origLength + 10);
                hitboxIndexList.Add(origLength + 10); hitboxIndexList.Add(origLength + 6);
                hitboxIndexList.Add(origLength + 6); hitboxIndexList.Add(origLength + 7);

                hitboxIndexList.Add(origLength + 7); hitboxIndexList.Add(origLength + 11);
                hitboxIndexList.Add(origLength + 11); hitboxIndexList.Add(origLength + 2);
                hitboxIndexList.Add(origLength + 2); hitboxIndexList.Add(origLength + 7);

                hitboxIndexList.Add(origLength + 8); hitboxIndexList.Add(origLength + 10);
                hitboxIndexList.Add(origLength + 10); hitboxIndexList.Add(origLength + 3);
                hitboxIndexList.Add(origLength + 3); hitboxIndexList.Add(origLength + 8);

                hitboxIndexList.Add(origLength + 9); hitboxIndexList.Add(origLength + 11);
                hitboxIndexList.Add(origLength + 11); hitboxIndexList.Add(origLength + 0);
                hitboxIndexList.Add(origLength + 0); hitboxIndexList.Add(origLength + 9);

            }

            void AddLine(Vector3 start, Vector3 end, Color color, Matrix m)
            {
                var origLength = hitboxPrimitives.Length;
                Array.Resize(ref hitboxPrimitives, origLength + 2);

                hitboxPrimitives[origLength] = new VertexPositionColor(Vector3.Transform(start, m), color);
                hitboxPrimitives[origLength + 1] = new VertexPositionColor(Vector3.Transform(end, m), color);



                hitboxIndexList.Add(origLength);
                hitboxIndexList.Add(origLength + 1);
            }


            //for (int i = 0; i < flver.Hitboxes.Count; i++)
            //{
            //    //hitboxPrimitives[(i * LENGTH) + 0].Color = Color.Yellow;
            //    //hitboxPrimitives[(i * LENGTH) + 1].Color = Color.Yellow;
            //    //hitboxPrimitives[(i * LENGTH) + 2].Color = Color.Yellow;

            //    //hitboxPrimitives[(i * LENGTH) + 3].Color = Color.Yellow;

            //    //hitboxPrimitives[(i * LENGTH) + 4].Color = Color.Fuchsia;
            //    //hitboxPrimitives[(i * LENGTH) + 5].Color = Color.Fuchsia;

            //    //if (flver.Hitboxes[i].BoneIndex >= 0 && flver.Hitboxes[i].BoneIndex < flver.Bones.Count)
            //    //{
            //    //    var bone = flver.Bones[flver.Hitboxes[i].BoneIndex];

            //    //    if (bone.BoundingBoxMin.HasValue && bone.BoundingBoxMax.HasValue)
            //    //    {
            //    //        hitboxPrimitives[(i * LENGTH)].Position = new Vector3(bone.BoundingBoxMin.Value.X, bone.BoundingBoxMin.Value.Y, bone.BoundingBoxMin.Value.Z);
            //    //        hitboxPrimitives[(i * LENGTH) + 1].Position = new Vector3(bone.BoundingBoxMax.Value.X, bone.BoundingBoxMax.Value.Y, bone.BoundingBoxMax.Value.Z);
            //    //    }



            //    //}

            //    //float depth = Math.Abs(flver.Hitboxes[i].Row2.Z);
            //    //float thickness

            //    const float MAX_BONE_SCALE = 0.1f;



            //    if (flver.Hitboxes[i].UnknownID1 >= 0 && flver.Hitboxes[i].UnknownID1 < flver.Bones.Count)
            //    {
            //        var bone = flver.Bones[flver.Hitboxes[i].UnknownID1];

            //        var boneParentMatrix = flverBoneParentMatrices[flver.Hitboxes[i].UnknownID1];
            //        var bonePos = Vector3.Transform(bone.Translation, boneParentMatrix);
            //        var boneScale = Vector3.Transform(bone.Scale, boneParentMatrix);
            //        var boneRot = Vector3.Transform(bone.EulerRadian, boneParentMatrix);

            //        AddCapsule(bonePos, Vector3.One * MAX_BONE_SCALE, boneRot, Color.Fuchsia);
            //    }

            //    if (flver.Hitboxes[i].DummyBoneIndex >= 0 && flver.Hitboxes[i].DummyBoneIndex < flver.Bones.Count)
            //    {
            //        var bone = flver.Bones[flver.Hitboxes[i].DummyBoneIndex];

            //        var boneParentMatrix = flverBoneParentMatrices[flver.Hitboxes[i].DummyBoneIndex];
            //        var bonePos = Vector3.Transform(bone.Translation, boneParentMatrix);
            //        var boneScale = Vector3.Transform(bone.Scale, boneParentMatrix);
            //        var boneRot = Vector3.Transform(bone.EulerRadian, boneParentMatrix);

            //        AddCapsule(bonePos, Vector3.One * MAX_BONE_SCALE, boneRot, Color.LimeGreen);
            //    }

            //    if (flver.Hitboxes[i].UnknownID3 >= 0 && flver.Hitboxes[i].UnknownID3 < flver.Bones.Count)
            //    {
            //        var bone = flver.Bones[flver.Hitboxes[i].UnknownID3];

            //        var boneParentMatrix = flverBoneParentMatrices[flver.Hitboxes[i].UnknownID3];
            //        var bonePos = Vector3.Transform(bone.Translation, boneParentMatrix);
            //        var boneScale = Vector3.Transform(bone.Scale, boneParentMatrix);
            //        var boneRot = Vector3.Transform(bone.EulerRadian, boneParentMatrix);

            //        AddCapsule(bonePos, Vector3.One * MAX_BONE_SCALE, boneRot, Color.Red);
            //    }

            //    //hitboxPrimitives[(i * LENGTH) + 0].Position = flver.Hitboxes[i].Position;
            //    //hitboxPrimitives[(i * LENGTH) + 1].Position = flver.Hitboxes[i].Position + flver.Hitboxes[i].Row2;
            //    //hitboxPrimitives[(i * LENGTH) + 2].Position = flver.Hitboxes[i].Position + flver.Hitboxes[i].Row3;



            //    //hitboxPrimitives[(i * LENGTH) + 5].Position = hitboxPrimitives[(i * LENGTH) + 4].Position + new Vector3(flver.Hitboxes[i].Row3.X, flver.Hitboxes[i].Row3.Y, flver.Hitboxes[i].Row3.Z);

            //    //hitboxIndexList.Add((i * LENGTH) + 0);
            //    //hitboxIndexList.Add((i * LENGTH) + 1);
            //    //hitboxIndexList.Add((i * LENGTH) + 1);
            //    //hitboxIndexList.Add((i * LENGTH) + 2);
            //    //hitboxIndexList.Add((i * LENGTH) + 2);
            //    //hitboxIndexList.Add((i * LENGTH) + 0);

            //    //AddCapsule(flver.Hitboxes[i].Position, Vector3.One * 0.01f, )
            //}

            //for (int i = 0; i < flver.Hitboxes.Count; i++)
            //{
            //    var off1 = new Vector3(flver.Hitboxes[i].Row2.X, flver.Hitboxes[i].Row2.Y, flver.Hitboxes[i].Row2.Z);
            //    var off2 = new Vector3(flver.Hitboxes[i].Row3.X, flver.Hitboxes[i].Row3.Y, flver.Hitboxes[i].Row3.Z);

            //    AddBoundingBox(new Vector3(flver.Hitboxes[i].Row1.X, flver.Hitboxes[i].Row1.Y, flver.Hitboxes[i].Row1.Z),
            //        -Vector3.One * (off1.Length() / 2)
            //        ,
            //        Vector3.One * (off1.Length() / 2)
            //        ,
            //        off2,
            //        Color.Lime,
            //        Color.Yellow);

            //}

            foreach (var hit in flvers[0].Dummies)
            {
                if (!ModelListWindow.GetCheckDummy(hit.TypeID))
                    continue;

                if (ENABLE_HIT_SPHERE)
                {
                    //if (hit.ParentBoneIndex >= 0 && hit.ParentBoneIndex < flver.Bones.Count)
                    //{
                    //    if (flverBoneColors[hit.ParentBoneIndex] == Color.LightGray)
                    //    {
                    //        flverBoneColors[hit.ParentBoneIndex] = Utils.HSLtoRGB((float)RAND.NextDouble(), 1, (float)(RAND.NextDouble() * 0.5 + 0.25));

                    //        if (flver.Bones[hit.ParentBoneIndex].BoundingBoxMin != null && flver.Bones[hit.ParentBoneIndex].BoundingBoxMax != null)
                    //        {
                    //            AddBoundingBox(flver.Bones[hit.ParentBoneIndex].BoundingBoxMin,
                    //                flver.Bones[hit.ParentBoneIndex].BoundingBoxMax,
                    //                flverBoneParentMatrices[hit.ParentBoneIndex],
                    //                dmyColorMapping[hit.TypeID],
                    //                dmyColorMapping[hit.TypeID]);
                    //        }

                    //    }

                    //    AddCapsule(Vector3.Zero,
                    //        Vector3.One * HIT_SPHERE_RADIUS, Vector3.Zero, flverBoneColors[hit.ParentBoneIndex], (Matrix.CreateTranslation(hit.Position) * flverBoneParentMatrices[hit.ParentBoneIndex]));
                    //}
                    //else if (hit.SomeSortOfParentIndex >= 0 && hit.SomeSortOfParentIndex < flver.Bones.Count)
                    //{
                    //    if (flverBoneColors[hit.SomeSortOfParentIndex] == Color.LightGray)
                    //    {
                    //        flverBoneColors[hit.SomeSortOfParentIndex] = Utils.HSLtoRGB((float)RAND.NextDouble(), 1, (float)(RAND.NextDouble() * 0.5 + 0.25));
                    //        if (flver.Bones[hit.SomeSortOfParentIndex].BoundingBoxMin != null && flver.Bones[hit.SomeSortOfParentIndex].BoundingBoxMax != null)
                    //        {
                    //            AddBoundingBox(flver.Bones[hit.SomeSortOfParentIndex].BoundingBoxMin,
                    //                flver.Bones[hit.SomeSortOfParentIndex].BoundingBoxMax,
                    //                flverBoneParentMatrices[hit.SomeSortOfParentIndex],
                    //                dmyColorMapping[hit.TypeID],
                    //                dmyColorMapping[hit.TypeID]);
                    //        }

                    //    }

                    //    AddCapsule(Vector3.Zero,
                    //        Vector3.One * HIT_SPHERE_RADIUS, Vector3.Zero, dmyColorMapping[hit.TypeID], (Matrix.CreateTranslation(hit.Position) * flverBoneParentMatrices[hit.SomeSortOfParentIndex]));
                    //}
                    //else
                    //{
                    //    AddCapsule(hit.Position, Vector3.One * HIT_SPHERE_RADIUS, Vector3.Zero, dmyColorMapping[hit.TypeID]);
                    //}

                    var bonifiedHitPosition = hit.Position;

                    if (hit.ParentBoneIndex >= 0 && hit.ParentBoneIndex < flvers[0].Bones.Count)
                    {
                        var parentBoneMatrix = flverBoneParentMatrices[hit.ParentBoneIndex];
                        bonifiedHitPosition = Vector3.Transform(bonifiedHitPosition, parentBoneMatrix);
                    }

                    AddCapsule(bonifiedHitPosition, Vector3.One * HIT_SPHERE_RADIUS * (float)ModelListWindow.SliderDummyRadius.Value, Vector3.Zero, dmyColorMapping[hit.TypeID]);

                    if (ModelListWindow.ShowDummyDirectionalIndicators)
                    {
                        AddLine(bonifiedHitPosition, hit.Position + hit.Row2, dmyColorMapping[hit.TypeID], Matrix.Identity);
                        AddLine(bonifiedHitPosition, hit.Position + hit.Row3, dmyColorMapping[hit.TypeID], Matrix.Identity);
                    }
                    //AddLine(hit.Position, hit.Position + hit.Row2, Color.Red, Matrix.Identity);
                    //AddLine(hit.Position, hit.Position + hit.Row3, Color.Red, Matrix.Identity);
                }
            }

            //foreach (var submesh in flver.Submeshes)
            //{
            //    foreach (var vert in submesh.Vertices)
            //    {
            //        AddLine(vert.Position, vert.Position + ((Vector3)vert.Normal * 0.025f), Color.Fuchsia, Matrix.Identity);
            //    }
            //}

            for (int i = 0; i < flvers[0].Bones.Count; i++)
            {
                var boneDrawState = ModelListWindow.GetCheckBone(i);

                if (boneDrawState == false)
                    continue;
                
                var bone = flvers[0].Bones[i];
                var boneParentMatrix = flverBoneParentMatrices[i];
                //var bonePos = Vector3.Transform(bone.Translation, boneParentMatrix);
                //var boneScale = Vector3.Transform(bone.Scale, boneParentMatrix);
                //var boneRot = Vector3.Transform(bone.EulerRadian, boneParentMatrix);

                //boneParentMatrix *= Matrix.CreateScale(bone.Scale);
                //boneParentMatrix *= Matrix.CreateTranslation(bone.Translation);
                //boneParentMatrix *= Matrix.CreateRotationY(bone.EulerRadian.Y)
                //    * Matrix.CreateRotationZ(bone.EulerRadian.Z)
                //    * Matrix.CreateRotationX(bone.EulerRadian.X);

                //AddCapsule(bonePos, Vector3.One *  0.1f, Vector3.Zero, Color.Red);
                
                float scale = ((Vector3)(bone.Translation)).Length() * 0.75f;

                
                ////float scale = boneParentMatrix.Translation.Length();
                ////float scale = ((Vector3)(bone.GetParent()?.Translation ?? Vector3.One)).Length();

                //bool foundChildScale = false;

                //var firstChild = bone.GetFirstChild();

                //if (firstChild != null)
                //{
                //    scale = ((Vector3)(firstChild.Translation)).Length();
                //    foundChildScale = true;
                //}

                if (bone.BoundingBoxMax != null && bone.BoundingBoxMin != null)
                {
                    ////if (!foundChildScale)
                    ////{
                    //var bbLen = ((Vector3)(bone.BoundingBoxMax - bone.BoundingBoxMin)).Length();

                    //if (bbLen > 0.001f)
                    //    scale = bbLen * 0.25f;
                    ////}

                    if (boneDrawState == null || ModelListWindow.ShowBoneBoxes)
                    {
                        AddBoundingBox(bone.BoundingBoxMin, bone.BoundingBoxMax, boneParentMatrix, Color.White, Color.White);
                    }
                }



                if (boneDrawState != false)
                {
                    float radius = BONE_JOINT_RADIUS * (float)ModelListWindow.SliderBoneJointRadius.Value;

                    AddBoundingBox(Vector3.Zero - (Vector3.One * radius * 0.5f),
                        Vector3.Zero + (Vector3.One * radius * 0.5f),
                        boneParentMatrix, Color.DarkGray, Color.LightGray);

                    //var BONE_PLACE_REE_FATCAT = boneParentMatrix.Translation;

                    //AddBoundingBox(Vector3.Zero - (Vector3.One * radius * 0.5f),
                    //    Vector3.Zero + (Vector3.One * radius * 0.5f),
                    //    Matrix.CreateTranslation(boneParentMatrix.Translation), Color.DarkGray, Color.LightGray);

                    //AddCapsule(boneParentMatrix.Translation, Vector3.One * radius, Vector3.Zero, Color.White);

                    //scale /= boneParentMatrix.Scale.Length();

                    scale = radius * 2;

                    AddArrow(boneParentMatrix, scale, scale * 0.25f, radius * 0.25f, Color.White);
                }


            }

            if (ModelListWindow.ShowDebugNormals.IsChecked == true)
                foreach (var f in primitiveDatas) //Each flver
                    foreach (var s in f) //Each submesh
                        foreach (var v in s) //Each vertex
                            AddLine(v.Position, v.Position + (v.Normal * (float)ModelListWindow.SliderDebugNormalLength.Value), Color.Blue, Matrix.Identity);

            if (ModelListWindow.ShowDebugTangents.IsChecked == true)
                foreach (var f in primitiveDatas) //Each flver
                    foreach (var s in f) //Each submesh
                        foreach (var v in s) //Each vertex
                            AddLine(v.Position, v.Position + (v.Tangent * (float)ModelListWindow.SliderDebugTangentLength.Value), Color.Blue, Matrix.Identity);

            hitboxIndices = hitboxIndexList.ToArray();
        }

        private void BuildHitboxPrimitives_Grid()
        {
            hitboxPrimitives_Grid = new VertexPositionColor[0];

            var hitboxIndexList_Grid = new List<int>();

            void AddArrow_Grid(Matrix matrix, float length, float tipLength, float tipGirth, Color color)
            {
                var origLength = hitboxPrimitives_Grid.Length;
                Array.Resize(ref hitboxPrimitives_Grid, origLength + 6);

                //Base
                hitboxPrimitives_Grid[origLength] = new VertexPositionColor((new Vector3(0, 0, 0)), color);

                //Tip
                hitboxPrimitives_Grid[origLength + 1] = new VertexPositionColor((new Vector3(0, 0, length)), color);


                hitboxPrimitives_Grid[origLength + 2] = new VertexPositionColor((new Vector3(tipGirth, 0, length - tipLength)), color);
                hitboxPrimitives_Grid[origLength + 3] = new VertexPositionColor((new Vector3(-tipGirth, 0, length - tipLength)), color);
                hitboxPrimitives_Grid[origLength + 4] = new VertexPositionColor((new Vector3(0, tipGirth, length - tipLength)), color);
                hitboxPrimitives_Grid[origLength + 5] = new VertexPositionColor((new Vector3(0, -tipGirth, length - tipLength)), color);




                for (int i = origLength; i < hitboxPrimitives_Grid.Length; i++)
                {
                    hitboxPrimitives_Grid[i].Position = Vector3.Transform(hitboxPrimitives_Grid[i].Position, Matrix.CreateRotationY(MathHelper.PiOver2) * matrix);
                }

                hitboxIndexList_Grid.Add(origLength); hitboxIndexList_Grid.Add(origLength + 1);

                hitboxIndexList_Grid.Add(origLength + 1); hitboxIndexList_Grid.Add(origLength + 2);
                hitboxIndexList_Grid.Add(origLength + 1); hitboxIndexList_Grid.Add(origLength + 3);
                hitboxIndexList_Grid.Add(origLength + 1); hitboxIndexList_Grid.Add(origLength + 4);
                hitboxIndexList_Grid.Add(origLength + 1); hitboxIndexList_Grid.Add(origLength + 5);

                //hitboxIndexList_Grid.Add(origLength + 2); hitboxIndexList_Grid.Add(origLength + 3);
                //hitboxIndexList_Grid.Add(origLength + 4); hitboxIndexList_Grid.Add(origLength + 5);

                hitboxIndexList_Grid.Add(origLength + 2); hitboxIndexList_Grid.Add(origLength + 4);
                hitboxIndexList_Grid.Add(origLength + 2); hitboxIndexList_Grid.Add(origLength + 5);

                hitboxIndexList_Grid.Add(origLength + 3); hitboxIndexList_Grid.Add(origLength + 4);
                hitboxIndexList_Grid.Add(origLength + 3); hitboxIndexList_Grid.Add(origLength + 5);
            }

            void AddLine_Grid(Vector3 start, Vector3 end, Color color, Matrix m)
            {
                var origLength = hitboxPrimitives_Grid.Length;
                Array.Resize(ref hitboxPrimitives_Grid, origLength + 2);

                hitboxPrimitives_Grid[origLength] = new VertexPositionColor(Vector3.Transform(start, m), color);
                hitboxPrimitives_Grid[origLength + 1] = new VertexPositionColor(Vector3.Transform(end, m), color);



                hitboxIndexList_Grid.Add(origLength);
                hitboxIndexList_Grid.Add(origLength + 1);
            }

            int gridCount = (int)ModelListWindow.SliderGridSpan.Value;
            float gridSpacing = (float)ModelListWindow.SliderGridSpacing.Value;
            float gridOpacity = (float)ModelListWindow.SliderGridOpacity.Value;

            if (gridCount > 0 && gridSpacing > 0 && gridOpacity > 0)
            {
                //Latitude
                for (int i = -gridCount; i <= gridCount; i++)
                {
                    if (i == 0)
                        continue;
                    AddLine_Grid(new Vector3(i, 0, gridCount), new Vector3(i, 0, -gridCount), Color.Lime * gridOpacity, Matrix.CreateScale(gridSpacing));
                }

                //Longitude
                for (int i = -gridCount; i <= gridCount; i++)
                {
                    if (i == 0)
                        continue;
                    AddLine_Grid(new Vector3(gridCount, 0, i), new Vector3(-gridCount, 0, i), Color.Lime * gridOpacity, Matrix.CreateScale(gridSpacing));
                }

                float gridArrowHeadLength = (gridSpacing / 8);
                float gridArrowLength = ((gridCount * gridSpacing) + gridSpacing);


                AddArrow_Grid(Matrix.Identity
                    , gridArrowLength, gridArrowHeadLength, gridArrowHeadLength / 2, Color.Black * gridOpacity);

                AddArrow_Grid(Matrix.CreateRotationY(MathHelper.PiOver2)
                    , gridArrowLength, gridArrowHeadLength, gridArrowHeadLength / 2, Color.Black * gridOpacity);

                AddArrow_Grid(Matrix.CreateRotationY(MathHelper.Pi)
                    , gridArrowLength, gridArrowHeadLength, gridArrowHeadLength / 2, Color.Black * gridOpacity);

                AddArrow_Grid(Matrix.CreateRotationY(MathHelper.PiOver2 * 3)
                    , gridArrowLength, gridArrowHeadLength, gridArrowHeadLength / 2, Color.Black * gridOpacity);
            }

            hitboxIndices_Grid = hitboxIndexList_Grid.ToArray();
        }

        protected override void OnExiting(object sender, EventArgs args)
        {
            if (ModelListWindow != null)
            {
                ModelListWindow.RequestExit = true;
            }
            ModelListWindow?.Close();
            base.OnExiting(sender, args);
        }

        private void SetMouseVisible(bool b)
        {
            IsMouseVisible = b;
        }

        private void MoveCamera(float x, float y, float z, float speed)
        {
            cameraPosition += Vector3.Transform(new Vector3(-x, -y, z),
                Matrix.CreateRotationX(-cameraRotation.X)
                * Matrix.CreateRotationY(-cameraRotation.Y)
                * Matrix.CreateRotationZ(-cameraRotation.Z)
                ) * speed;
        }

        //private void RotateCamera(float x, float y, float z, float speed)
        //{
        //    var cam = CAMERA_MATRIX();

        //    var pointToward = Matrix.CreateTranslation(0, 0, 1) * Matrix.CreateRotationX(-cameraRotation.X)
        //        * Matrix.CreateRotationY(-cameraRotation.Y)
        //        * Matrix.CreateRotationZ(-cameraRotation.Z);

        //    pointToward *= Matrix.CreateRotationX(x);
        //    pointToward *= Matrix.CreateRotationY(y);
        //    pointToward *= Matrix.CreateRotationZ(z);

        //    cameraRotation += pointToward.Translation;
        //}

        private void INPUT_UPDATE(GameTime gameTime)
        {
            if (ModelListWindow == null || (!IsActive && !ModelListWindow.IsActive))
                return;

            ModelListWindow.SetTopmost(base.IsActive);

            var gamepad = GamePad.GetState(PlayerIndex.One);

            

            
            MouseState mouse = Mouse.GetState();
            Vector2 mousePos = new Vector2((float)mouse.X, (float)mouse.Y);
            KeyboardState keyboard = Keyboard.GetState();
            int currentWheel = mouse.ScrollWheelValue;

            bool isBackToFileBrowserKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Tab);
            bool isExitApplicationInstantlyKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
            bool isSpeedupKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) || keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);
            bool isSlowdownKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);
            bool isResetKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.R);
            bool isMoveLightKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space);
            bool isToggleAllSubmeshKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Z);
            bool isToggleAllDummyKeyPressed  = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.X);
            bool isToggleAllBonesKeyPressed  = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.C);

            bool isReloadModelKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F5);

            if (gamepad.IsConnected)
            {
                if (gamepad.IsButtonDown(Buttons.LeftShoulder))
                    isSlowdownKeyPressed = true;
                if (gamepad.IsButtonDown(Buttons.RightShoulder))
                    isSpeedupKeyPressed = true;
                if (gamepad.IsButtonDown(Buttons.LeftStick))
                    isResetKeyPressed = true;
                if (gamepad.IsButtonDown(Buttons.DPadUp))
                    isMoveLightKeyPressed = true;
                if (gamepad.IsButtonDown(Buttons.X))
                    isToggleAllSubmeshKeyPressed = true;
                if (gamepad.IsButtonDown(Buttons.Y))
                    isToggleAllDummyKeyPressed = true;
                if (gamepad.IsButtonDown(Buttons.B))
                    isToggleAllBonesKeyPressed = true;
                if (gamepad.IsButtonDown(Buttons.Back))
                    isBackToFileBrowserKeyPressed = true;
                if (gamepad.IsButtonDown(Buttons.Start))
                    isReloadModelKeyPressed = true;

                if (gamepad.IsButtonDown(Buttons.LeftShoulder) &&
                    gamepad.IsButtonDown(Buttons.RightShoulder) &&
                    gamepad.IsButtonDown(Buttons.A) &&
                    gamepad.IsButtonDown(Buttons.Start))
                {
                    isExitApplicationInstantlyKeyPressed = true;
                }
            }

            if (isExitApplicationInstantlyKeyPressed)
            {
                Exit();
                return;
            }

            if (isBackToFileBrowserKeyPressed)
            {
                //System.Windows.MessageBox.Show("TODO");
                //System.Diagnostics.Process.Start(System.Windows.Forms.Application.ExecutablePath);
                //Exit();
                //Program.Main(new string[] { });
                IsForceBrowse = true;
                LoadContent();
                return;
            }

            if (isReloadModelKeyPressed)
            {
                LoadContent();
                //System.Diagnostics.Process.Start(System.Windows.Forms.Application.ExecutablePath, $"\"{inputFiles[0]}\"");
                //base.Run(GameRunBehavior.Synchronous);
                //Exit();
                //Program.Main(inputFiles);
                return;
            }

            if (isResetKeyPressed)
            {
                cameraPosition = cameraPositionDefault;
                cameraRotation = Vector3.Zero;
                lightRotation = Vector3.Zero;
            }

            float moveMult = (float)gameTime.ElapsedGameTime.TotalSeconds * 3f * (float)ModelListWindow.SliderCamMoveSpeed.Value;

            if (isSpeedupKeyPressed)
            {
                moveMult *= 10f;
            }

            if (isSlowdownKeyPressed)
            {
                moveMult /= 10f;
            }

            if (gamepad.IsConnected)
            {
                MoveCamera(gamepad.ThumbSticks.Left.X, gamepad.Triggers.Right - gamepad.Triggers.Left, gamepad.ThumbSticks.Left.Y, moveMult);

                float camH = gamepad.ThumbSticks.Right.X * (float)1.5f 
                    * (float)gameTime.ElapsedGameTime.TotalSeconds * (float)ModelListWindow.SliderJoystickSpeed.Value;
                float camV = gamepad.ThumbSticks.Right.Y * (float)1.5f
                    * (float)gameTime.ElapsedGameTime.TotalSeconds * (float)ModelListWindow.SliderJoystickSpeed.Value;

                if (isMoveLightKeyPressed)
                {
                    lightRotation.Y += camH;
                    lightRotation.X -= camV;
                }
                else
                {
                    cameraRotation.Y += camH;
                    cameraRotation.X -= camV;
                }
            }

            bool currentClick = mouse.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && base.Window.ClientBounds.Contains(mousePos + new Vector2((float)base.Window.ClientBounds.X, (float)base.Window.ClientBounds.Y)) && base.IsActive;

            if (IsActive)
            {
                float x = 0;
                float y = 0;
                float z = 0;

                if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D))
                    x += 1;
                if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A))
                    x -= 1;
                if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.E))
                    y += 1;
                if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Q))
                    y -= 1;
                if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W))
                    z += 1;
                if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S))
                    z -= 1;

                MoveCamera(x, y, z, moveMult);
            }

            if (isToggleAllSubmeshKeyPressed && !prev_isToggleAllSubmeshKeyPressed)
            {
                ModelListWindow.TOGGLE_ALL_SUBMESH();
            }

            if (isToggleAllDummyKeyPressed && !prev_isToggleAllDummyKeyPressed)
            {
                ModelListWindow.TOGGLE_ALL_DUMMY();
            }

            if (isToggleAllBonesKeyPressed && !prev_isToggleAllBonesKeyPressed)
            {
                ModelListWindow.TOGGLE_ALL_BONES();
            }

            if (currentClick)
            {
                if (!oldMouseClick)
                {
                    SetMouseVisible(false);
                    oldMouse = mousePos;
                    Mouse.SetPosition(base.Window.ClientBounds.Width / 2, base.Window.ClientBounds.Height / 2);
                    mousePos = new Vector2(base.Window.ClientBounds.Width / 2, base.Window.ClientBounds.Height / 2);
                    //oldMouseClick = true;
                    //return;
                }
                SetMouseVisible(false);
                Vector2 mouseDelta = mousePos - new Vector2((float)(base.Window.ClientBounds.Width / 2), (float)(base.Window.ClientBounds.Height / 2));

                float camH = mouseDelta.X * 0.025f * (float)3f * (float)gameTime.ElapsedGameTime.TotalSeconds * (float)ModelListWindow.SliderMouseSpeed.Value;
                float camV = mouseDelta.Y * -0.025f * (float)3f * (float)gameTime.ElapsedGameTime.TotalSeconds * (float)ModelListWindow.SliderMouseSpeed.Value;

                if (isMoveLightKeyPressed)
                {
                    lightRotation.Y += camH;
                    lightRotation.X -= camV;
                }
                else
                {
                    cameraRotation.Y += camH;
                    cameraRotation.X -= camV;
                }

                
                //cameraRotation.Z -= (float)Math.Cos(MathHelper.PiOver2 - cameraRotation.Y) * camV;

                //RotateCamera(mouseDelta.Y * -0.01f * (float)moveMult, 0, 0, moveMult);
                //RotateCamera(0, mouseDelta.X * 0.01f * (float)moveMult, 0, moveMult);

                Mouse.SetPosition(base.Window.ClientBounds.Width / 2, base.Window.ClientBounds.Height / 2);
                oldMouseClick = true;
            }
            else
            {
                if (oldMouseClick)
                {
                    Mouse.SetPosition((int)oldMouse.X, (int)oldMouse.Y);
                }
                SetMouseVisible(true);
                oldMouseClick = false;
            }
            cameraRotation.X = MathHelper.Clamp(cameraRotation.X, -MathHelper.PiOver2, MathHelper.PiOver2);
            lightRotation.X = MathHelper.Clamp(lightRotation.X, -MathHelper.PiOver2, MathHelper.PiOver2);
            oldWheel = currentWheel;

            prev_isToggleAllSubmeshKeyPressed = isToggleAllSubmeshKeyPressed;
            prev_isToggleAllDummyKeyPressed = isToggleAllDummyKeyPressed;
            prev_isToggleAllBonesKeyPressed = isToggleAllBonesKeyPressed;
        }

        protected override void Update(GameTime gameTime)
        {
            try
            {
                if (Window.ClientBounds != oldBounds)
                {
                    graphics.PreferredBackBufferWidth = Window.ClientBounds.Width;
                    graphics.PreferredBackBufferHeight = Window.ClientBounds.Height;
                    graphics.ApplyChanges();
                }
            }
            catch
            {

            }
        }
    }
}
