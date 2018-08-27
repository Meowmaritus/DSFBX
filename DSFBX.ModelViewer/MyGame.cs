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
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Forms;

namespace DSFBX.ModelViewer
{
    public class MyGame : Game
    {
        Texture2D DEFAULT_TEXTURE_D;
        Texture2D DEFAULT_TEXTURE_S;
        Texture2D DEFAULT_TEXTURE_N;

        Effect RENDER_EFFECT;
        const string RENDER_EFFECT_NAME = "Content\\NormalMapShader";

        bool oldTextureBrowseKey = false;

        float GUI_PULSATE_HZ = 1;
        float RAINBOW_HZ = 0.1f;

        float ORBIT_CAM_DISTANCE = 0;
        const float SHITTY_CAM_ZOOM_MIN_DIST = 0.2f;

        //デバッグマウスカーソルでレンダーするもの    ファトキャット
        StringBuilder DEBUG_SHIT_ON_MOUSE_CURSOR_FATCAT = new StringBuilder();

        //非常に悪いカメラピッチ制限    ファトキャット
        const float SHITTY_CAM_PITCH_LIMIT_FATCAT = 0.999f;

        //非常に悪いカメラピッチ制限リミッタ    ファトキャット
        const float SHITTY_CAM_PITCH_LIMIT_FATCAT_CLAMP = 0.999f;

        //軌道カムトグルキー押下
        bool oldOrbitCamToggleKeyPressed = false; 

        bool IS_ORBIT_CAM = false;

        Vector3 cameraOrigin = new Vector3(0, 0, 0);

        void DEBUG(string text)
        {
            DEBUG_SHIT_ON_MOUSE_CURSOR_FATCAT.AppendLine(text);
        }

        public void DEBUG_OVERLAY_DRAW()
        {
            //spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.BackToFront);
            //DrawTextOn3DLocation(Vector3.Zero, "3D LABEL TEST!!!", Color.Fuchsia);

            //spriteBatch.DrawString(DEBUG_FONT_SMALL, $"CURRENT CLICK: {(currentMouseClick ? 1 : 0)}", mousePos + Vector2.One * 8, Color.Fuchsia);
            var dummyShitAtOrigin = new Dictionary<string, int>();
            var boneShitAtOrigin = new Dictionary<string, int>();

            if (ModelListWindow.ShowDummyPointTextLabels.IsChecked == true)
            {
                foreach (var dmy in flvers[0].Dummies)
                {
                    if (!ModelListWindow.GetCheckDummy(dmy.TypeID))
                        continue;

                    var bonifiedHitPosition = dmy.Position;

                    if (dmy.ParentBoneIndex >= 0 && dmy.ParentBoneIndex < flvers[0].Bones.Count)
                    {
                        var parentBoneMatrix = flverBoneParentMatrices[dmy.ParentBoneIndex];
                        bonifiedHitPosition = Vector3.Transform(bonifiedHitPosition, parentBoneMatrix);
                    }

                    string text = "dmy" + dmy.TypeID;

                    if (bonifiedHitPosition == Vector3.Zero)
                    {
                        if (dummyShitAtOrigin.ContainsKey(text))
                        {
                            dummyShitAtOrigin[text]++;
                        }
                        else
                        {
                            dummyShitAtOrigin.Add(text, 1);
                        }
                    }
                    else
                    {
                        DrawTextOn3DLocation(bonifiedHitPosition, text, dmyColorMapping[dmy.TypeID]);
                    }
                }
            }

            if (ModelListWindow.ShowBoneNameTextLabels.IsChecked == true)
            {
                for (int b = 0; b < flvers[0].Bones.Count; b++)
                {
                    if (ModelListWindow.GetCheckBone(b) == false)
                        continue;

                    var boneTextPos = Vector3.Transform(Vector3.Zero, flverBoneParentMatrices[b]);

                    string text = flvers[0].Bones[b].Name;

                    if (boneTextPos == Vector3.Zero)
                    {
                        if (boneShitAtOrigin.ContainsKey(text))
                        {
                            boneShitAtOrigin[text]++;
                        }
                        else
                        {
                            boneShitAtOrigin.Add(text, 1);
                        }
                    }
                    else
                    {
                        DrawTextOn3DLocation(boneTextPos, text, flverBoneColors[b]);
                    }

                    
                }
            }

            if (dummyShitAtOrigin.Count > 0 || boneShitAtOrigin.Count > 0)
            {
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Labels at origin:");
                if (dummyShitAtOrigin.Count > 0)
                {
                    sb.AppendLine("    -Dummy Poly(s):");
                    foreach (var kvp in dummyShitAtOrigin)
                    {
                        sb.AppendLine($"        -{kvp.Key} (x{kvp.Value})");
                    }
                }
                if (boneShitAtOrigin.Count > 0)
                {
                    sb.AppendLine("    -Bone(s):");
                    foreach (var kvp in boneShitAtOrigin)
                    {
                        sb.AppendLine($"        -{kvp.Key}" + ((kvp.Value > 1) ? ($" (x{kvp.Value})") : ""));
                    }
                }

                var shitAtOriginString = sb.ToString();

                var shitAtOriginSize = DEBUG_FONT_SMALL.MeasureString(shitAtOriginString);

                DrawThiccText(DEBUG_FONT_SMALL, shitAtOriginString,
                    new Vector2(Window.ClientBounds.Width - shitAtOriginSize.X - 8, 8), Color.Yellow, 1);

                //DrawTextOn3DLocation(Vector3.Zero, sb.ToString(), Color.Fuchsia);
            }

            if (DEBUG_SHIT_ON_MOUSE_CURSOR_FATCAT.Length > 0)
            {
                var sb = new StringBuilder();
                DrawThiccText(DEBUG_FONT_SMALL, DEBUG_SHIT_ON_MOUSE_CURSOR_FATCAT.ToString(), mousePos + new Vector2(24, 24), Color.Fuchsia, 1);
            }

            spriteBatch.End();
            spriteBatch.Begin();

            
        }

        public void DrawTextOn3DLocation(Vector3 location, string text, Color color)
        {
            // Project the 3d position first
            Vector3 screenPos3D = GraphicsDevice.Viewport.Project(location, CAMERA_PROJECTION, CAMERA_VIEW, CAMERA_WORLD);

            //Vector3 camNormal = Vector3.Transform(Vector3.Forward, CAMERA_ROTATION);
            //Vector3 directionFromCam = Vector3.Normalize(location - Vector3.Transform(cameraPosition, CAMERA_WORLD));
            //var normDot = Vector3.Dot(directionFromCam, camNormal);

            if (screenPos3D.Z >= 1)
                return;

            // Just to make it easier to use we create a Vector2 from screenPos3D
            Vector2 screenPos2D = new Vector2(screenPos3D.X, screenPos3D.Y);

            //text += $"[DBG]{screenPos3D.Z}";

            DrawThiccText(DEBUG_FONT_SMALL, text, screenPos2D - new Vector2(GraphicsDevice.Viewport.X, GraphicsDevice.Viewport.Y), color, 1, screenPos3D.Z);
        }

        public bool IsForceBrowse = false;
        bool IsFirstTimeLoadContent = true;
        public bool IsQuickRunFromModelImporter = false;

        Rectangle oldBounds = Rectangle.Empty;

        double prevGameTime = 0;

        SpriteFont DEBUG_FONT;
        string DEBUG_FONT_NAME = "Content\\CJKFontMono12pt";

        SpriteFont DEBUG_FONT_SMALL;
        string DEBUG_FONT_SMALL_NAME = "Content\\CJKFont10pt";

        SpriteFont DEBUG_FONT_BIG;
        string DEBUG_FONT_BIG_NAME = "Content\\CJKFont16x16";

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
        private Dictionary<string, byte[]> TEXTURE_POOL = new Dictionary<string, byte[]>();
        private List<Matrix> flverBoneParentMatrices = new List<Matrix>();
        private List<Vector3> flverBoneNubPositions = new List<Vector3>();
        private List<Color> flverBoneColors = new List<Color>();
        private Vector3 cameraRotation = Vector3.Zero;

        private Vector3 lightRotation = Vector3.Zero;
        Vector3 cameraPositionDefault = Vector3.Zero;
        private Vector3 cameraPosition = Vector3.Zero;
        //private float cameraDistance = 3f;
        //private float cameraHeight = -2f;
        private VertexPositionColorNormalTangentTexture[][][] primitiveDatas;
        private Texture2D[][] tex_diffuse;
        private Texture2D[][] tex_normal;
        private Texture2D[][] tex_specular;
        private VertexPositionColor[] hitboxPrimitives;
        private VertexPositionColor[] hitboxPrimitives_Grid;
        private int[] hitboxIndices;
        private int[] hitboxIndices_Grid;
        private Dictionary<int, Color> dmyColorMapping = new Dictionary<int, Color>();
        private Vector2 mousePos = Vector2.Zero;
        private Vector2 oldMouse = Vector2.Zero;
        private int oldWheel = 0;
        private bool currentMouseClick = false;
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

        private void LoadAllTextures()
        {
            tex_diffuse = new Texture2D[inputFiles.Length][];
            tex_normal = new Texture2D[inputFiles.Length][];
            tex_specular = new Texture2D[inputFiles.Length][];
            for (int i = 0; i < inputFiles.Length; i++)
            {
                var flver = flvers[i];
                tex_diffuse[i] = new Texture2D[flver.Submeshes.Count];
                tex_normal[i] = new Texture2D[flver.Submeshes.Count];
                tex_specular[i] = new Texture2D[flver.Submeshes.Count];
                for (int j = 0; j < flver.Submeshes.Count; j++)
                {
                    var diffuseTex = flver.Submeshes[j].Material.Parameters.Where(x => x.Name.ToUpper().Contains("DIFFUSE"));
                    if (diffuseTex.Any())
                    {
                        var texName = MiscUtil.GetFileNameWithoutDirectoryOrExtension(diffuseTex.First().Value);
                        if (TEXTURE_POOL.ContainsKey(texName))
                        {
                            using (var memStream = new MemoryStream(TEXTURE_POOL[texName]))
                            {
                                tex_diffuse[i][j] = Texture2D.FromStream(GraphicsDevice, memStream);
                            }
                        }
                        else
                        {
                            tex_diffuse[i][j] = null;
                        }
                    }

                    var normalTex = flver.Submeshes[j].Material.Parameters.Where(x => x.Name.ToUpper() == ("G_BUMPMAP"));
                    if (normalTex.Any())
                    {
                        var texName = MiscUtil.GetFileNameWithoutDirectoryOrExtension(normalTex.First().Value);
                        if (TEXTURE_POOL.ContainsKey(texName))
                        {
                            using (var memStream = new MemoryStream(TEXTURE_POOL[texName]))
                            {
                                tex_normal[i][j] = Texture2D.FromStream(GraphicsDevice, memStream);
                            }
                        }
                        else
                        {
                            tex_normal[i][j] = null;
                        }
                    }

                    var specularTex = flver.Submeshes[j].Material.Parameters.Where(x => x.Name.ToUpper() == ("G_SPECULAR"));
                    if (specularTex.Any())
                    {
                        var texName = MiscUtil.GetFileNameWithoutDirectoryOrExtension(specularTex.First().Value);
                        if (TEXTURE_POOL.ContainsKey(texName))
                        {
                            using (var memStream = new MemoryStream(TEXTURE_POOL[texName]))
                            {
                                tex_specular[i][j] = Texture2D.FromStream(GraphicsDevice, memStream);
                            }
                        }
                        else
                        {
                            tex_specular[i][j] = null;
                        }
                    }
                }
            }
        }

        private void BROWSE_FOR_TEXTURES()
        {
            string[] newTextureFiles;
            OpenFileDialog browseDlg = new OpenFileDialog()
            {
                Title = "Open Additional Texture File(s)",
                CheckFileExists = true,
                CheckPathExists = true,
                Multiselect = true,
            };
            var dlgResult = browseDlg.ShowDialog();
            if (dlgResult != DialogResult.OK)
            {
                newTextureFiles = new string[0];
            }
            else
            {
                if (browseDlg.FileNames.Length > 0)
                    newTextureFiles = browseDlg.FileNames;
                else
                    newTextureFiles = new string[] { browseDlg.FileName };
            }

            if (newTextureFiles.Length > 0)
            {
                for (int i = 0; i < newTextureFiles.Length; i++)
                {
                    LoadTexturesFromFile(newTextureFiles[i]);
                }

                LoadAllTextures();
            }

            
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            DEBUG_FONT = Content.Load<SpriteFont>(DEBUG_FONT_NAME);
            DEBUG_FONT_SMALL = Content.Load<SpriteFont>(DEBUG_FONT_SMALL_NAME);
            DEBUG_FONT_BIG = Content.Load<SpriteFont>(DEBUG_FONT_BIG_NAME);
            RENDER_EFFECT = Content.Load<Effect>(RENDER_EFFECT_NAME);

            DEFAULT_TEXTURE_D = new Texture2D(GraphicsDevice, 1, 1);
            DEFAULT_TEXTURE_D.SetData<Color>(new Color[] { Color.Gray });

            DEFAULT_TEXTURE_S = new Texture2D(GraphicsDevice, 1, 1);
            DEFAULT_TEXTURE_S.SetData<Color>(new Color[] { Color.DimGray });

            DEFAULT_TEXTURE_N = new Texture2D(GraphicsDevice, 1, 1);
            DEFAULT_TEXTURE_N.SetData<Color>(new Color[] { new Color(128, 128, 255, 255) });

            bool WAS_MODELS_ACTUALLY_LOADED_FATCAT = true;

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
                    WAS_MODELS_ACTUALLY_LOADED_FATCAT = false;
                }
                else
                {
                    if (browseDlg.FileNames.Length > 0)
                        inputFiles = browseDlg.FileNames;
                    else
                        inputFiles = new string[] { browseDlg.FileName };
                }
            }

            if (!WAS_MODELS_ACTUALLY_LOADED_FATCAT && inputFiles.Length == 0)
            {
                Exit();
                return;
            }

            if (inputFiles.Length == 1)
                IsFirstTimeLoadContent = true;

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

            

            //FONT = Content.Load<SpriteFont>("FONT");

            

            LoadModel();

            for (int i = 0; i < inputFiles.Length; i++)
                BuildGeometry(i);

            CreateModelListWindow();

            for (int i = 0; i < inputFiles.Length; i++)
                BuildModelPrimitives(i);

            LoadAllTextures();

            BuildHitboxPrimitives();
            BuildHitboxPrimitives_Grid();

            System.Windows.Forms.Form myForm
                = (System.Windows.Forms.Form)
                System.Windows.Forms.Form.FromHandle(this.Window.Handle);
            myForm.Focus();
            myForm.Activate();

            if (!IsActive)
                ModelListWindow.Activate();

            IsFirstTimeLoadContent = false;
        }

        private void TEXTURE_POOL_ADD(string name, byte[] tex)
        {
            if (!TEXTURE_POOL.ContainsKey(name))
            {
                TEXTURE_POOL.Add(name, tex);
            }
            else
            {
                TEXTURE_POOL[name] = tex;
            }
            
        }

        private void LoadTexturesFromFile(string file)
        {
            string justFile = new FileInfo(file).Name;

            string fileType = justFile.ToUpper().Substring(justFile.IndexOf('.') + 1);

            if (fileType.Contains("DCX"))
            {
                string innerFileType = fileType.Substring(0, fileType.IndexOf("."));

                if (innerFileType.Contains("BND"))
                {
                    var bnd = DataFile.LoadFromDcxFile<EntityBND>(file);
                    foreach (var kvp in bnd.Models[0].Textures)
                    {
                        TEXTURE_POOL_ADD(kvp.Key, kvp.Value);
                    }
                }
                else if (innerFileType.Contains("TPF"))
                {
                    var tpf = DataFile.LoadFromDcxFile<TPF>(file);
                    foreach (var tex in tpf)
                    {
                        TEXTURE_POOL_ADD(tex.Name, tex.DDSBytes);
                    }
                }
            }
            else
            {
                if (fileType.Contains("BND"))
                {
                    var bnd = DataFile.LoadFromFile<EntityBND>(file);
                    foreach (var kvp in bnd.Models[0].Textures)
                    {
                        TEXTURE_POOL_ADD(kvp.Key, kvp.Value);
                    }
                }
                else if (fileType.Contains("TPF"))
                {
                    var tpf = DataFile.LoadFromFile<TPF>(file);
                    foreach (var tex in tpf)
                    {
                        TEXTURE_POOL_ADD(tex.Name, tex.DDSBytes);
                    }
                }
            }
        }

        private void LoadModel()
        {
            if (inputFiles.Length == 0)
            {
                Exit();
                return;
            }

            flvers = new FLVER[inputFiles.Length];
            //TEXTURE_POOL = new Dictionary<string, byte[]>();
            primitiveDatas = new VertexPositionColorNormalTangentTexture[inputFiles.Length][][];

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
                        foreach (var kvp in bnd.Models[0].Textures)
                        {
                            TEXTURE_POOL_ADD(kvp.Key, kvp.Value);
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"Unrecognized input file extension ({fileType}) (this program accepts more than 1 format of file and uses the extension to determine which).");
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
                        foreach (var kvp in bnd.Models[0].Textures)
                        {
                            TEXTURE_POOL_ADD(kvp.Key, kvp.Value);
                        }
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"Unrecognized input file extension ({fileType}) (this program accepts more than 1 format of file and uses the extension to determine which).");
                        Exit();
                    }
                }

            }

            //if (inputFiles.Length > 1)
            //    System.Windows.MessageBox.Show("Only the first input file will have dummy points or bones.");

        }

        private void BuildGeometry(int f)
        {
            primitiveDatas[f] = new VertexPositionColorNormalTangentTexture[flvers[f].Submeshes.Count][];

            

            //List<Matrix> flverBoneParentMatrices_BeforeLinkedBones = new List<Matrix>();
            if (f == 0)
            {
                flverBoneParentMatrices = new List<Matrix>();
                flverBoneNubPositions = new List<Vector3>();
                flverBoneColors = new List<Color>();

                Matrix GetParentBoneMatrix(FlverBone bone)
                {
                    FlverBone parent = bone;

                    var boneParentMatrix = Matrix.Identity;

                    do
                    {
                        boneParentMatrix *= Matrix.CreateScale(parent.Scale);
                        boneParentMatrix *= Matrix.CreateRotationX(parent.EulerRadian.X);
                        boneParentMatrix *= Matrix.CreateRotationZ(parent.EulerRadian.Z);
                        boneParentMatrix *= Matrix.CreateRotationY(parent.EulerRadian.Y);

                        //boneParentMatrix *= Matrix.CreateRotationY(parent.EulerRadian.Y);
                        //boneParentMatrix *= Matrix.CreateRotationZ(parent.EulerRadian.Z);
                        //boneParentMatrix *= Matrix.CreateRotationX(parent.EulerRadian.X);
                        boneParentMatrix *= Matrix.CreateTranslation(parent.Translation);
                        //boneParentMatrix *= Matrix.CreateScale(parent.Scale);

                        parent = parent.GetParent();
                    }
                    while (parent != null);

                    return boneParentMatrix;
                }

                //Vector3 GetRootScale(FlverBone bone)
                //{
                //    FlverBone parent = bone;

                //    var rootScale = Vector3.One;

                //    do
                //    {
                //        rootScale = parent.Scale;

                //        parent = parent.GetParent();
                //    }
                //    while (parent != null);

                //    return rootScale;
                //}

                for (int i = 0; i < flvers[f].Bones.Count; i++)
                {
                    var m = GetParentBoneMatrix(flvers[f].Bones[i]);
                    flverBoneParentMatrices.Add(m);
                    flverBoneNubPositions.Add(Vector3.Transform(Vector3.Zero, m));
                    flverBoneColors.Add(Color.LightGray);
                }

                ModelListWindow.NumSubmeshes = flvers[f].Submeshes.Count;
                ModelListWindow.SubmeshNames = new List<string>();
                ModelListWindow.SubmeshMaterialNames = new List<string>();
                ModelListWindow.FlverSubmeshes = new List<FlverSubmesh>();

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

                    ModelListWindow.FlverSubmeshes.Add(sm);
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

                primitiveDatas[f][i] = new VertexPositionColorNormalTangentTexture[flvers[f].Submeshes[i].Vertices.Count];

                for (int j = 0; j < flvers[f].Submeshes[i].Vertices.Count; j++)
                {
                    var vert = flvers[f].Submeshes[i].Vertices[j];

                    primitiveDatas[f][i][j] = new VertexPositionColorNormalTangentTexture();

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
                        if (f == 0 && ModelListWindow.CurrentBoneWeightView >= 0 && ModelListWindow.CurrentBoneWeightView < flvers[f].Bones.Count)
                        {
                            var boneA = flvers[f].Submeshes[i].GetBoneFromLocalIndex(vert.BoneIndices.A, true);
                            var boneB = flvers[f].Submeshes[i].GetBoneFromLocalIndex(vert.BoneIndices.B, true);
                            var boneC = flvers[f].Submeshes[i].GetBoneFromLocalIndex(vert.BoneIndices.C, true);
                            var boneD = flvers[f].Submeshes[i].GetBoneFromLocalIndex(vert.BoneIndices.D, true);
                            
                            if (boneA != null && flvers[f].Bones.IndexOf(boneA) == ModelListWindow.CurrentBoneWeightView)
                            {
                                if (vert.BoneWeights == null)
                                    primitiveDatas[f][i][j].Color = new Vector4(1, 0, 0, 1);
                                else
                                primitiveDatas[f][i][j].Color = new Vector4(
                                    0.5f + (vert.BoneWeights.A / 2),
                                    0.5f - (vert.BoneWeights.A / 2),
                                    0.5f - (vert.BoneWeights.A / 2),
                                    1);
                            }
                            else if (boneB != null && flvers[f].Bones.IndexOf(boneB) == ModelListWindow.CurrentBoneWeightView)
                            {
                                if (vert.BoneWeights == null)
                                    primitiveDatas[f][i][j].Color = new Vector4(1, 0, 0, 1);
                                else
                                    primitiveDatas[f][i][j].Color = new Vector4(
                                    0.5f + (vert.BoneWeights.B / 2),
                                    0.5f - (vert.BoneWeights.B / 2),
                                    0.5f - (vert.BoneWeights.B / 2),
                                    1);
                            }
                            else if (boneC != null && flvers[f].Bones.IndexOf(boneC) == ModelListWindow.CurrentBoneWeightView)
                            {
                                if (vert.BoneWeights == null)
                                    primitiveDatas[f][i][j].Color = new Vector4(1, 0, 0, 1);
                                else
                                    primitiveDatas[f][i][j].Color = new Vector4(
                                    0.5f + (vert.BoneWeights.C / 2),
                                    0.5f - (vert.BoneWeights.C / 2),
                                    0.5f - (vert.BoneWeights.C / 2),
                                    1);
                            }
                            else if (boneD != null && flvers[f].Bones.IndexOf(boneD) == ModelListWindow.CurrentBoneWeightView)
                            {
                                if (vert.BoneWeights == null)
                                    primitiveDatas[f][i][j].Color = new Vector4(1, 0, 0, 1);
                                else
                                    primitiveDatas[f][i][j].Color = new Vector4(
                                    0.5f + (vert.BoneWeights.D / 2),
                                    0.5f - (vert.BoneWeights.D / 2),
                                    0.5f - (vert.BoneWeights.D / 2),
                                    1);
                            }
                            else
                            {
                                primitiveDatas[f][i][j].Color = new Vector4(0.5f, 0.5f, 0.5f, 1);
                            }
                        }
                        else
                        {
                            primitiveDatas[f][i][j].Color = new Vector4(0.5f, 0.5f, 0.5f, 1);
                        }

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

                    if (vert.UVs.Count > 0)
                    {
                        primitiveDatas[f][i][j].TextureCoordinate = vert.UVs[0];
                    }
                    else
                    {
                        primitiveDatas[f][i][j].TextureCoordinate = Vector2.Zero;
                    }

                    primitiveDatas[f][i][j].Binormal = primitiveDatas[f][i][j].Normal;

                }
            }

            if (MAX_VERTEX_Y > float.NegativeInfinity)
            {
                cameraPositionDefault = new Vector3(0, (MAX_VERTEX_Y / -2), -(MAX_VERTEX_Y / 2));
                if (IsFirstTimeLoadContent)
                {
                    cameraPosition = cameraPositionDefault;
                    cameraOrigin.Y = cameraPositionDefault.Y;
                    ORBIT_CAM_DISTANCE = (cameraOrigin - (cameraPosition - new Vector3(0, 0, 5))).Length();
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

            ModelListWindow.ModelName = MiscUtil.GetFileNameWithoutDirectoryOrExtension(
                MiscUtil.GetFileNameWithoutDirectoryOrExtension(inputFiles[0]));

            ModelListWindow.Title = ModelListWindow.ModelName;

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


        private Matrix CAMERA_TRANSLATION
        {
            get
            {
                return Matrix.CreateTranslation(cameraPosition.X, cameraPosition.Y, cameraPosition.Z);
            }
        }

        private Matrix CAMERA_ROTATION
        {
            get
            {
                return Matrix.CreateRotationY(cameraRotation.Y)
                    * Matrix.CreateRotationZ(cameraRotation.Z)
                    * Matrix.CreateRotationX(cameraRotation.X);
            }
        }

        private Matrix CAMERA_VIEW
        {
            get
            {
                return CAMERA_TRANSLATION * CAMERA_ROTATION;
            }
        }

        private Matrix CAMERA_WORLD
        {
            get
            {
                return Matrix.CreateRotationY(MathHelper.Pi)
                * Matrix.CreateTranslation(0, 0, -5)
                * Matrix.CreateScale(-1, 1, 1);
            }
        }

        private Matrix CAMERA_PROJECTION
        {
            get
            {
                return Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60),
                    (float)GraphicsDevice.Viewport.Width / (float)GraphicsDevice.Viewport.Height, 0.001f, 1000000.0f);
            }
        }

        private void RENDER_MESH(int f, int i)
        {
            if (f == 0)
            {
                if (!ModelListWindow.GetCheckModel(i))
                    return;

                if (inputFileSubmeshIndex >= 0 && inputFileSubmeshIndex < flvers[f].Submeshes.Count && inputFileSubmeshIndex != i)
                    return;
            }

            VertexBuffer v = new VertexBuffer(GraphicsDevice, typeof(VertexPositionColorNormalTangentTexture), primitiveDatas[f][i].Length, BufferUsage.None);
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

                if (ModelListWindow.RenderDoubleSidedCheckBox.IsChecked == true)
                {
                    if (GraphicsDevice.RasterizerState.CullMode != CullMode.None)
                        GraphicsDevice.RasterizerState = new RasterizerState()
                        {
                            CullMode = CullMode.None,
                            FillMode = GraphicsDevice.RasterizerState.FillMode,
                        };
                }
                else if (ModelListWindow.RenderDoubleSidedCheckBox.IsChecked == false)
                {
                    if (GraphicsDevice.RasterizerState.CullMode != CullMode.CullClockwiseFace)
                        GraphicsDevice.RasterizerState = new RasterizerState()
                        {
                            CullMode = CullMode.CullClockwiseFace,
                            FillMode = GraphicsDevice.RasterizerState.FillMode,
                        };
                }
                else if (ModelListWindow.RenderDoubleSidedCheckBox.IsChecked == null)
                {
                    var cull = faceSet.CullBackfaces ? CullMode.CullClockwiseFace : CullMode.None;
                    if (GraphicsDevice.RasterizerState.CullMode != cull)
                        GraphicsDevice.RasterizerState = new RasterizerState()
                        {
                            CullMode = cull,
                            FillMode = GraphicsDevice.RasterizerState.FillMode,
                        };
                }

                GraphicsDevice.DrawIndexedPrimitives(faceSet.IsTriangleStrip ? PrimitiveType.TriangleStrip : PrimitiveType.TriangleList, 0, 0,
                    faceSet.IsTriangleStrip ? (faceSet.VertexIndices.Count - 2) : (faceSet.VertexIndices.Count / 3));

                lineListIndexBuffer.Dispose();

            }


            v.Dispose();
        }

        private void Draw3D(bool isWireframePass)
        {


            if (IS_CURRENTLY_REBUILDING_ENTIRE_MESH_FATCAT)
            {
                LoadContent();
                IS_CURRENTLY_REBUILDING_ENTIRE_MESH_FATCAT = false;
            }

            var basicEffect = new BasicEffect(GraphicsDevice);

            // Transform your model to place it somewhere in the world
            basicEffect.World = CAMERA_WORLD;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;


            //basicEffect.World = Matrix.Identity; // Use this to leave your model at the origin
            // Transform the entire world around (effectively: place the camera)
            basicEffect.View = CAMERA_VIEW;
            // Specify how 3D points are projected/transformed onto the 2D screen
            basicEffect.Projection = CAMERA_PROJECTION;


            if (!isWireframePass)
            {
                GraphicsDevice.RasterizerState = new RasterizerState()
                {
                    MultiSampleAntiAlias = true,
                    CullMode = CullMode.None,
                    FillMode = FillMode.Solid,
                };

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


                    if (ModelListWindow.CurrentBoneWeightView >= 0 && ModelListWindow.CurrentBoneWeightView < flvers[0].Bones.Count)
                    {
                        basicEffect.LightingEnabled = false;

                        basicEffect.AmbientLightColor = Vector3.One / 2;
                        basicEffect.SpecularPower = 0;
                        basicEffect.SpecularColor = Vector3.One / 2;
                        basicEffect.EmissiveColor = Vector3.One / 2;
                        basicEffect.DiffuseColor = Vector3.One / 2;
                    }
                    else
                    {
                        basicEffect.LightingEnabled = true;

                        basicEffect.EnableDefaultLighting();

                        basicEffect.AmbientLightColor *= 0.80f;

                        basicEffect.DirectionalLight0.Direction = Vector3.Transform(Vector3.Forward,
                            Matrix.CreateRotationY(lightRotation.Y)
                            * Matrix.CreateRotationZ(lightRotation.Z)
                            * Matrix.CreateRotationX(lightRotation.X)
                            );

                        basicEffect.DirectionalLight0.DiffuseColor *= 0.80f;
                        basicEffect.DirectionalLight0.SpecularColor *= 0.25f;

                        //basicEffect.DirectionalLight1.DiffuseColor *= 0.25f;
                        //basicEffect.DirectionalLight1.SpecularColor *= 0.25f;

                        //basicEffect.DirectionalLight2.DiffuseColor *= 0.25f;
                        //basicEffect.DirectionalLight2.SpecularColor *= 0.25f;

                        basicEffect.DirectionalLight0.Enabled = true;
                        basicEffect.DirectionalLight1.Enabled = false;
                        basicEffect.DirectionalLight2.Enabled = false;
                    }



                    basicEffect.VertexColorEnabled = true;
                }
            }
            else
            {
                GraphicsDevice.RasterizerState = new RasterizerState()
                {
                    MultiSampleAntiAlias = true,
                    CullMode = CullMode.None,
                    FillMode = FillMode.WireFrame,
                };

                basicEffect.VertexColorEnabled = false;
                basicEffect.LightingEnabled = false;

                basicEffect.Alpha = (float)(ModelListWindow.SliderWireframeOpacity.Value);

            }




            //if ( ModelListWindow.CurrentBoneWeightView >= 0 && ModelListWindow.CurrentBoneWeightView < flvers[0].Bones.Count)
            //{
            //    basicEffect.LightingEnabled = false;
            //    basicEffect.AmbientLightColor = Vector3.One;
            //    basicEffect.DirectionalLight0.Enabled = false;
            //    basicEffect.DirectionalLight1.Enabled = false;
            //    basicEffect.DirectionalLight2.Enabled = false;
            //    basicEffect.SpecularPower = 0;
            //    basicEffect.SpecularColor = Vector3.One;
            //    basicEffect.EmissiveColor = Vector3.One;
            //    basicEffect.DiffuseColor = Vector3.One;
            //}

            //else 


            // I'm setting this so that *both* sides of your triangle are drawn
            // (so it won't be back-face culled if you move it, or the camera around behind it)
            //GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            //BlendState bs = new BlendState();
            //bs.AlphaSourceBlend = Blend.SourceAlpha;
            //bs.AlphaDestinationBlend = Blend.SourceAlpha;
            //bs.ColorSourceBlend = Blend.SourceColor;
            //bs.ColorDestinationBlend = Blend.SourceColor;
            //bs.ColorBlendFunction = BlendFunction.Add;
            //bs.AlphaBlendFunction = BlendFunction.Add;

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            RENDER_EFFECT.Parameters["World"].SetValue(CAMERA_WORLD);
            RENDER_EFFECT.Parameters["View"].SetValue(CAMERA_VIEW);
            RENDER_EFFECT.Parameters["Projection"].SetValue(CAMERA_PROJECTION);

            RENDER_EFFECT.Parameters["AmbientColor"].SetValue(Vector3.One);
            RENDER_EFFECT.Parameters["AmbientIntensity"].SetValue(0.5f);

            RENDER_EFFECT.Parameters["DiffuseColor"].SetValue(Vector3.One);
            RENDER_EFFECT.Parameters["DiffuseIntensity"].SetValue(1f);

            //RENDER_EFFECT.Parameters["SpecularColor"].SetValue(Vector3.One);

            RENDER_EFFECT.Parameters["LightDirection"].SetValue(basicEffect.DirectionalLight0.Direction);

            RENDER_EFFECT.Parameters["EyePosition"].SetValue(Vector3.Zero);

            if (isWireframePass)
            {
                for (int f = 0; f < flvers.Length; f++)
                {
                    for (int i = 0; i < flvers[f].Submeshes.Count; i++)
                    {
                        foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                        {
                            pass.Apply();

                            RENDER_MESH(f, i);

                        }
                    }
                }
            }
            else
            {
                for (int f = 0; f < flvers.Length; f++)
                {
                    for (int i = 0; i < flvers[f].Submeshes.Count; i++)
                    {
                        if (!isWireframePass && ModelListWindow.ShowTexturesCheckBox.IsChecked == true)
                        {
                            if (tex_diffuse[f][i] != null)
                            {
                                RENDER_EFFECT.Parameters["ColorMap"].SetValue(tex_diffuse[f][i]);
                            }
                            else
                            {
                                RENDER_EFFECT.Parameters["ColorMap"].SetValue(DEFAULT_TEXTURE_D);
                            }

                            if (tex_normal[f][i] != null)
                            {
                                RENDER_EFFECT.Parameters["NormalMap"].SetValue(tex_normal[f][i]);
                            }
                            else
                            {
                                RENDER_EFFECT.Parameters["NormalMap"].SetValue(DEFAULT_TEXTURE_N);
                            }

                            if (tex_normal[f][i] != null)
                            {
                                RENDER_EFFECT.Parameters["SpecularMap"].SetValue(tex_specular[f][i]);
                            }
                            else
                            {
                                RENDER_EFFECT.Parameters["SpecularMap"].SetValue(DEFAULT_TEXTURE_S);
                            }


                        }
                        else
                        {
                            RENDER_EFFECT.Parameters["ColorMap"].SetValue(DEFAULT_TEXTURE_D);
                            RENDER_EFFECT.Parameters["NormalMap"].SetValue(DEFAULT_TEXTURE_N);
                            RENDER_EFFECT.Parameters["SpecularMap"].SetValue(DEFAULT_TEXTURE_S);
                        }

                        foreach (var technique in RENDER_EFFECT.Techniques)
                        {
                            RENDER_EFFECT.CurrentTechnique = technique;
                            foreach (EffectPass pass in RENDER_EFFECT.CurrentTechnique.Passes)
                            {
                                pass.Apply();
                                RENDER_MESH(f, i);
                            }
                        }
                    }
                }
            }

            

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

                

               

            }

            basicEffect.TextureEnabled = false;
            basicEffect.VertexColorEnabled = true;

            //CanDrawContinueWaitHandle.WaitOne();

            //if (CurrentlyRebuildingHitboxPrimitives)
            //    return;

            if (!isWireframePass)
            {
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
            }

            



            basicEffect.Dispose();

            //spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);

            //spriteBatch.DrawString(DEBUG_FONT, $"FPS: {(FPS.AverageFramesPerSecond)}", new Vector2(200, 200), Color.White);

            //spriteBatch.End();

        }

        private void DrawThiccText(SpriteFont font, string text, Vector2 pos, Color color, float thiccness, float depth = 0)
        {
            spriteBatch.DrawString(font, text, pos + new Vector2(1, 0) * thiccness, Color.Black, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, depth + 0.000001f);
            spriteBatch.DrawString(font, text, pos + new Vector2(-1, 0) * thiccness, Color.Black, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, depth + 0.000001f);
            spriteBatch.DrawString(font, text, pos + new Vector2(0, 1) * thiccness, Color.Black, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, depth + 0.000001f);
            spriteBatch.DrawString(font, text, pos + new Vector2(0, -1) * thiccness, Color.Black, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, depth + 0.000001f);

            spriteBatch.DrawString(font, text, pos + new Vector2(-1, 1) * thiccness, Color.Black, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, depth + 0.000001f);
            spriteBatch.DrawString(font, text, pos + new Vector2(-1, -1) * thiccness, Color.Black, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, depth + 0.000001f);
            spriteBatch.DrawString(font, text, pos + new Vector2(1, 1) * thiccness, Color.Black, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, depth + 0.000001f);
            spriteBatch.DrawString(font, text, pos + new Vector2(1, -1) * thiccness, Color.Black, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, depth + 0.000001f);

            spriteBatch.DrawString(font, text, pos, color, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, depth);
        }

        private void Draw2D(GameTime gameTime)
        {

            DEBUG_OVERLAY_DRAW();

            float pulsate = (float)Math.Abs(Math.Sin((gameTime.TotalGameTime.TotalSeconds * GUI_PULSATE_HZ) * MathHelper.Pi));

            Color rainbow = Utils.HSLtoRGB(((float)gameTime.TotalGameTime.TotalSeconds * RAINBOW_HZ) % 1.0f, 1, 0.75f);

            int line = 0;

            DrawThiccText(DEBUG_FONT, $"FPS: {(FPS.AverageFramesPerSecond)}", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);

            line++;

            DrawThiccText(DEBUG_FONT, $"Keyboard Control (Gamepad Control): What The Control Does", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Cyan, 1);

            line++;

            var camModeString = $"Camera Mode: {(IS_ORBIT_CAM ? "Orbit" : "Free-Fly")} (Toggle: F on Keyboard / A on Gamepad)";
            DrawThiccText(DEBUG_FONT, camModeString, new Vector2(8, 8) + new Vector2(0, 16 * line++), rainbow, 1);

            if (IS_ORBIT_CAM)
            {
                DrawThiccText(DEBUG_FONT, $"W/S (RT/LT): Zoom In/Out", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
                DrawThiccText(DEBUG_FONT, $"RMB + Move Mouse (Move Left Stick): Orbit Camera", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
                DrawThiccText(DEBUG_FONT, $"E/Q (Move Right Stick Up/Down): Move Camera Vertically", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            }
            else
            {
                DrawThiccText(DEBUG_FONT, $"W/A/S/D (Move Left Stick): Move Camera Laterally", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
                DrawThiccText(DEBUG_FONT, $"RMB + Move Mouse (Move Right Stick): Turn Camera", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
                DrawThiccText(DEBUG_FONT, $"E/Q (RT/LT): Move Camera Vertically", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            }
            line++;
            DrawThiccText(DEBUG_FONT, $"Shift/Ctrl (LB/RB): Move Slow/Fast", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"R (Click Left Stick): Reset Camera", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"T (Click Right Stick): Point Camera At Model", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"RMB + Spacebar + Move Mouse (D-Pad Up + Move Right Stick): Turn Light", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            
            line++;
            DrawThiccText(DEBUG_FONT, $"Z (X): Toggle All Submeshes", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"X (Y): Toggle All Dummy Polys", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"C (B): Toggle All Bones", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            line++;

            int totalVertices = 0;

            if (primitiveDatas != null)
            {
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
            }

            

            DrawThiccText(DEBUG_FONT, $"# Mesh Vertices: {totalVertices}", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
            DrawThiccText(DEBUG_FONT, $"# Overlay Primitives: {(hitboxPrimitives?.Length ?? 0)}", new Vector2(8, 8) + new Vector2(0, 16 * line++), Color.Yellow, 1);
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
            DEBUG_SHIT_ON_MOUSE_CURSOR_FATCAT.Clear();


            //RebuildHitboxPrimitivesWaitHandle.Set();

            if (flvers != null && flvers.Length > 0 && ModelListWindow.CurrentBoneWeightView >= 0 && ModelListWindow.CurrentBoneWeightView < flvers[0].Bones.Count)
            {
                GraphicsDevice.Clear(Color.CornflowerBlue);
            }
            else
            {
                GraphicsDevice.Clear(Color.Gray);
            }

            try
            {
                INPUT_UPDATE(gameTime);
            }
            catch (Exception e)
            {
                spriteBatch.Begin();
                DrawThiccText(DEBUG_FONT_SMALL, $"INPUT_UPDATE Exception: {e.Message}", Vector2.Zero, Color.Red, 1);
                spriteBatch.End();
            }


            if (flvers != null && flvers.Length > 0)
            {
                Draw3D(isWireframePass: false);
                if (ModelListWindow.ShowWireframeCheckbox.IsChecked == true)
                {
                    Draw3D(isWireframePass: true);
                }
            }

            Draw2D(gameTime);

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
            return Utils.HSLtoRGB((float)RAND.NextDouble(), 1, (float)(RAND.NextDouble() * 0.166667 + 0.66666667));
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
                        //var parentBoneMatrix = Matrix.CreateScale(flverBoneParentMatrices[hit.ParentBoneIndex].Scale);
                        bonifiedHitPosition = Vector3.Transform(bonifiedHitPosition, flverBoneParentMatrices[hit.ParentBoneIndex]);
                    }

                    AddCapsule(bonifiedHitPosition, Vector3.One * HIT_SPHERE_RADIUS * (float)ModelListWindow.SliderDummyRadius.Value, Vector3.Zero, dmyColorMapping[hit.TypeID]);

                    if (ModelListWindow.ShowDummyDirectionalIndicators)
                    {
                        AddLine(bonifiedHitPosition, bonifiedHitPosition + hit.Row2, dmyColorMapping[hit.TypeID], Matrix.Identity);
                        AddLine(bonifiedHitPosition, bonifiedHitPosition + hit.Row3, dmyColorMapping[hit.TypeID], Matrix.Identity);
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

            void AddBoneArrow(Matrix matrix, float length, float girth, Color color)
            {
                girth = Math.Min(length / 2, girth);

                Vector3 pt_start = Vector3.Zero;
                Vector3 pt_cardinal1 = Vector3.Up * girth + Vector3.Right * girth;
                Vector3 pt_cardinal2 = Vector3.Forward * girth + Vector3.Right * girth;
                Vector3 pt_cardinal3 = Vector3.Down * girth + Vector3.Right * girth;
                Vector3 pt_cardinal4 = Vector3.Backward * girth + Vector3.Right * girth;
                Vector3 pt_tip = Vector3.Right * length;

                
                //Start to cardinals
                AddLine(pt_start, pt_cardinal1, color, matrix);
                AddLine(pt_start, pt_cardinal2, color, matrix);
                AddLine(pt_start, pt_cardinal3, color, matrix);
                AddLine(pt_start, pt_cardinal4, color, matrix);

                //Cardinals to end
                AddLine(pt_cardinal1, pt_tip, color, matrix);
                AddLine(pt_cardinal2, pt_tip, color, matrix);
                AddLine(pt_cardinal3, pt_tip, color, matrix);
                AddLine(pt_cardinal4, pt_tip, color, matrix);

                //Connecting the cardinals
                AddLine(pt_cardinal1, pt_cardinal2, color, matrix);
                AddLine(pt_cardinal2, pt_cardinal3, color, matrix);
                AddLine(pt_cardinal3, pt_cardinal4, color, matrix);
                AddLine(pt_cardinal4, pt_cardinal1, color, matrix);
            }

            for (int i = 0; i < flvers[0].Bones.Count; i++)
            {
                var boneDrawState = ModelListWindow.GetCheckBone(i);

                if (boneDrawState == false)
                    continue;
                
                var bone = flvers[0].Bones[i];
                var thisBoneMatrix = flverBoneParentMatrices[i];
                //var bonePos = Vector3.Transform(bone.Translation, boneParentMatrix);
                //var boneScale = Vector3.Transform(bone.Scale, boneParentMatrix);
                //var boneRot = Vector3.Transform(bone.EulerRadian, boneParentMatrix);

                //boneParentMatrix *= Matrix.CreateScale(bone.Scale);
                //boneParentMatrix *= Matrix.CreateTranslation(bone.Translation);
                //boneParentMatrix *= Matrix.CreateRotationY(bone.EulerRadian.Y)
                //    * Matrix.CreateRotationZ(bone.EulerRadian.Z)
                //    * Matrix.CreateRotationX(bone.EulerRadian.X);

                //AddCapsule(bonePos, Vector3.One *  0.1f, Vector3.Zero, Color.Red);
                
                //float scale = ((Vector3)(bone.Translation)).Length() * 0.75f;

                
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
                        AddBoundingBox(bone.BoundingBoxMin, bone.BoundingBoxMax, thisBoneMatrix, Color.White, Color.White);
                    }
                }



                if (boneDrawState != false)
                {
                    float radius = BONE_JOINT_RADIUS * (float)ModelListWindow.SliderBoneJointRadius.Value;

                    

                    //var BONE_PLACE_REE_FATCAT = boneParentMatrix.Translation;

                    //AddBoundingBox(Vector3.Zero - (Vector3.One * radius * 0.5f),
                    //    Vector3.Zero + (Vector3.One * radius * 0.5f),
                    //    Matrix.CreateTranslation(boneParentMatrix.Translation), Color.DarkGray, Color.LightGray);

                    //AddCapsule(boneParentMatrix.Translation, Vector3.One * radius, Vector3.Zero, Color.White);

                    //scale /= boneParentMatrix.Scale.Length();





                    //scale = radius * 2;

                    //AddArrow(boneParentMatrix, scale, scale * 0.25f, radius * 0.25f, Color.White);



                    var parent = bone.GetParent();

                    if (parent != null)
                    {
                        var parentBoneMatrix = flverBoneParentMatrices[bone.ParentIndex];
                        //var matrixFromParentToThis = thisBoneMatrix * Matrix.Invert(parentBoneMatrix);
                        //Don't do the actual size in the bonearrow matrix.
                        var boneArrowMatrix = Matrix.CreateFromQuaternion(parentBoneMatrix.Rotation)
                            * Matrix.CreateTranslation(flverBoneNubPositions[bone.ParentIndex]);

                        var boneArrowLength = (flverBoneNubPositions[i] - flverBoneNubPositions[bone.ParentIndex]).Length();

                        if (ModelListWindow.ProportionallySizedBonesCheckBox.IsChecked == true)
                        {
                            radius = (boneArrowLength / 2) * (float)(ModelListWindow.SliderBoneJointRadius.Value / ModelListWindow.SliderBoneJointRadius.Maximum);
                        }
                        

                        AddBoneArrow(parentBoneMatrix, boneArrowLength, radius, flverBoneColors[i]);

                        //AddLine(flverBoneNubPositions[bone.ParentIndex], flverBoneNubPositions[i], flverBoneColors[i], Matrix.Identity);
                    }
                    else
                    {
                        AddBoundingBox(Vector3.Zero - (Vector3.One * radius * 0.5f),
                            Vector3.Zero + (Vector3.One * radius * 0.5f),
                            thisBoneMatrix, Color.DarkGray, Color.LightGray);
                    }
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

        public static Vector3 GetFlverEulerFromQuaternion(Quaternion quat)
        {
            //This is the code from
            //http://www.mawsoft.com/blog/?p=197
            var rotation = quat;
            double q0 = rotation.W;
            double q1 = rotation.Y;
            double q2 = rotation.X;
            double q3 = rotation.Z;

            Vector3 radAngles = new Vector3();
            radAngles.Y = (float)Math.Atan2(2 * (q0 * q1 + q2 * q3), 1 - 2 * (Math.Pow(q1, 2) + Math.Pow(q2, 2)));
            radAngles.X = (float)Math.Asin(2 * (q0 * q2 - q3 * q1));
            radAngles.Z = (float)Math.Atan2(2 * (q0 * q3 + q1 * q2), 1 - 2 * (Math.Pow(q2, 2) + Math.Pow(q3, 2)));

            return radAngles;
        }

        //public Vector3 GetEulerAngles(float x, float y, float z, float angle)
        //{
        //    double heading = 0, pitch = 0, bank = 0;
        //    double s = Math.Sin(angle);
        //    double c = Math.Cos(angle);
        //    double t = 1 - c;
        //    //  if axis is not already normalised then uncomment this
        //    // double magnitude = Math.sqrt(x*x + y*y + z*z);
        //    // if (magnitude==0) throw error;
        //    // x /= magnitude;
        //    // y /= magnitude;
        //    // z /= magnitude;
        //    if ((x * y * t + z * s) > 0.998)
        //    { // north pole singularity detected
        //        heading = 2 * Math.Atan2(x * Math.Sin(angle / 2), Math.Cos(angle / 2));
        //        pitch = Math.PI / 2;
        //        bank = 0;
        //        return new Vector3((float)pitch, (float)heading, (float)bank);
        //    }
        //    if ((x * y * t + z * s) < -0.998)
        //    { // south pole singularity detected
        //        heading = -2 * Math.Atan2(x * Math.Sin(angle / 2), Math.Cos(angle / 2));
        //        pitch = -Math.PI / 2;
        //        bank = 0;
        //        return new Vector3((float)pitch, (float)heading, (float)bank);
        //    }
        //    heading = Math.Atan2(y * s - x * z * t, 1 - (y * y + z * z) * t);
        //    pitch = Math.Asin(x * y * t + z * s);
        //    bank = Math.Atan2(x * s - y * z * t, 1 - (x * x + z * z) * t);
        //    return new Vector3((float)pitch, (float)heading, (float)bank);
        //}

        private void PointCameraToModel()
        {
            var newLookDir = Vector3.Normalize(cameraOrigin - (cameraPosition - new Vector3(0, 0, 5))) * new Vector3(-1, 1, 1);
            cameraRotation.Y = (float)Math.Atan2(newLookDir.X, newLookDir.Z);
            cameraRotation.X = (float)Math.Asin(newLookDir.Y);
            cameraRotation.Z = 0;
        }

        private void MoveCamera(float x, float y, float z, float speed)
        {
            cameraPosition += Vector3.Transform(new Vector3(-x, -y, z),
                Matrix.CreateRotationX(-cameraRotation.X)
                * Matrix.CreateRotationY(-cameraRotation.Y)
                * Matrix.CreateRotationZ(-cameraRotation.Z)
                ) * speed;
        }

        //private void MoveCamera_Orbit(float x, float y, float z, float speed)
        //{
        //    cameraPosition += Vector3.Transform(new Vector3(-(float)(x / (1 + Math.Abs(Math.Tan(cameraRotation.X)))), -y, z),
        //        Matrix.CreateRotationX(-cameraRotation.X)
        //        * Matrix.CreateRotationY(-cameraRotation.Y)
        //        * Matrix.CreateRotationZ(-cameraRotation.Z)
        //        ) * speed * ORBIT_CAM_DISTANCE;
            

        //    //spriteBatch.Begin();
        //    //DrawThiccText(DEBUG_FONT_SMALL, "tan(camRotX)=" + Math.Abs(Math.Tan(cameraRotation.X)), Vector2.Zero, Color.Fuchsia, 1);
        //    //spriteBatch.End();

        //}

        private void RotateCameraOrbit(float h, float v, float speed)
        {
            cameraRotation.Y -= h * speed;
            cameraRotation.X += v * speed;
            cameraRotation.Z = 0;
        }

        private void MoveCamera_OrbitOriginVertical(float y, float speed)
        {
            cameraPosition.Y -= y * speed;
            cameraOrigin.Y -= y * speed;
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

        private float GetGamepadTriggerDeadzone(float t, float d)
        {
            if (t < d)
                return 0;
            else if (t >= 1)
                return 0;

            return (t - d) * (1.0f / (1.0f - d));
        }

        private void INPUT_UPDATE(GameTime gameTime)
        {
            Mouse.WindowHandle = Window.Handle;
            //ModelListWindow.SetTopmost(base.IsActive);

            

            

            var gamepad = GamePad.GetState(PlayerIndex.One);

            

            
            MouseState mouse = Mouse.GetState();
            mousePos = new Vector2((float)mouse.X, (float)mouse.Y);
            KeyboardState keyboard = Keyboard.GetState();
            int currentWheel = mouse.ScrollWheelValue;

            bool mouseInWindow = mousePos.X > 0 && mousePos.X < Window.ClientBounds.Width && mousePos.Y > 0 && mousePos.Y < Window.ClientBounds.Height;

            currentMouseClick = mouse.RightButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed && mouseInWindow;

            if (ModelListWindow == null || (!IsActive && !ModelListWindow.IsActive))
            {
                SetMouseVisible(true);
                return;
            }

            bool isBackToFileBrowserKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Tab);
            bool isExitApplicationInstantlyKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Escape);
            bool isSpeedupKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftControl) || keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightControl);
            bool isSlowdownKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.LeftShift) || keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.RightShift);
            bool isResetKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.R);
            bool isMoveLightKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space);
            bool isToggleAllSubmeshKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Z);
            bool isToggleAllDummyKeyPressed  = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.X);
            bool isToggleAllBonesKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.C);
            bool isOrbitCamToggleKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F);
            bool isPointCamAtObjectKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.T);
            bool isTextureBrowseKeyPressed = keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.F1);

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
                if (gamepad.IsButtonDown(Buttons.A))
                    isOrbitCamToggleKeyPressed = true;
                if (gamepad.IsButtonDown(Buttons.RightStick))
                    isPointCamAtObjectKeyPressed = true;

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

            if (isTextureBrowseKeyPressed && !oldTextureBrowseKey)
            {
                BROWSE_FOR_TEXTURES();
            }

            if (isResetKeyPressed)
            {
                cameraPosition = cameraPositionDefault;
                cameraOrigin.Y = cameraPositionDefault.Y;
                ORBIT_CAM_DISTANCE = (cameraOrigin - (cameraPosition - new Vector3(0, 0, 5))).Length();
                cameraRotation = Vector3.Zero;
                lightRotation = Vector3.Zero;
            }

            if (isOrbitCamToggleKeyPressed && !oldOrbitCamToggleKeyPressed)
            {
                if (!IS_ORBIT_CAM)
                {
                    cameraOrigin.Y = cameraPositionDefault.Y;
                    ORBIT_CAM_DISTANCE = (cameraOrigin - (cameraPosition - new Vector3(0, 0, 5))).Length();
                }
                IS_ORBIT_CAM = !IS_ORBIT_CAM;
            }

            if (isPointCamAtObjectKeyPressed)
            {
                PointCameraToModel();
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

            var cameraDist = cameraOrigin - cameraPosition;

            if (gamepad.IsConnected)
            {
                var lt = GetGamepadTriggerDeadzone(gamepad.Triggers.Left, 0.1f);
                var rt = GetGamepadTriggerDeadzone(gamepad.Triggers.Right, 0.1f);


                if (IS_ORBIT_CAM && !isMoveLightKeyPressed)
                {
                    float camH = gamepad.ThumbSticks.Left.X * (float)1.5f
                        * (float)gameTime.ElapsedGameTime.TotalSeconds * (float)ModelListWindow.SliderJoystickSpeed.Value;
                    float camV = gamepad.ThumbSticks.Left.Y * (float)1.5f
                        * (float)gameTime.ElapsedGameTime.TotalSeconds * (float)ModelListWindow.SliderJoystickSpeed.Value;

                    


                    //DEBUG($"{(cameraRotation.X / MathHelper.PiOver2)}");
                    if (cameraRotation.X >= MathHelper.PiOver2 * SHITTY_CAM_PITCH_LIMIT_FATCAT)
                    {
                        //DEBUG("UPPER CAM LIMIT");
                        camV = Math.Min(camV, 0);
                    }
                    if (cameraRotation.X <= -MathHelper.PiOver2 * SHITTY_CAM_PITCH_LIMIT_FATCAT)
                    {
                        //DEBUG("LOWER CAM LIMIT");
                        camV = Math.Max(camV, 0);
                    }

                    RotateCameraOrbit(camH, camV, MathHelper.PiOver2);

                    var zoom = gamepad.Triggers.Right - gamepad.Triggers.Left;

                    if (Math.Abs(cameraDist.Length()) <= SHITTY_CAM_ZOOM_MIN_DIST)
                    {
                        zoom = Math.Min(zoom, 0);
                    }


                    ORBIT_CAM_DISTANCE -= zoom * moveMult;




                    //PointCameraToModel();
                    MoveCamera_OrbitOriginVertical(gamepad.ThumbSticks.Right.Y, moveMult);
                }
                else
                {
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
                        MoveCamera(gamepad.ThumbSticks.Left.X, gamepad.Triggers.Right - gamepad.Triggers.Left, gamepad.ThumbSticks.Left.Y, moveMult);

                        

                        cameraRotation.Y += camH;
                        cameraRotation.X -= camV;
                    }
                }

                
            }




            if (IS_ORBIT_CAM)
            {
                if (IsActive)
                {
                    float z = 0;
                    float y = 0;

                    if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W) && Math.Abs(cameraDist.Length()) > 0.1f)
                        z += 1;
                    if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.S))
                        z -= 1;
                    if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.E))
                        y += 1;
                    if (keyboard.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Q))
                        y -= 1;


                    if (Math.Abs(cameraDist.Length()) <= SHITTY_CAM_ZOOM_MIN_DIST)
                    {
                        z = Math.Min(z, 0);
                    }

                    ORBIT_CAM_DISTANCE -= z * moveMult;

                    MoveCamera_OrbitOriginVertical(y, moveMult);
                }
            }
            else
            {
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

            if (currentMouseClick)
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

                if (IS_ORBIT_CAM && !isMoveLightKeyPressed)
                {
                    if (cameraRotation.X >= MathHelper.PiOver2 * SHITTY_CAM_PITCH_LIMIT_FATCAT)
                    {
                        camV = Math.Min(camV, 0);
                    }
                    if (cameraRotation.X <= -MathHelper.PiOver2 * SHITTY_CAM_PITCH_LIMIT_FATCAT)
                    {
                        camV = Math.Max(camV, 0);
                    }

                    RotateCameraOrbit(camH, camV, MathHelper.PiOver2);
                    //PointCameraToModel();
                }
                else if (isMoveLightKeyPressed)
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
            }
            else
            {
                if (oldMouseClick)
                {
                    Mouse.SetPosition((int)oldMouse.X, (int)oldMouse.Y);
                }
                SetMouseVisible(true);
            }

            if (IS_ORBIT_CAM)
            {
                //DEBUG("Dist:" + ORBIT_CAM_DISTANCE);
                //DEBUG("AngX:" + cameraRotation.X / MathHelper.Pi + " PI");
                //DEBUG("AngY:" + cameraRotation.Y / MathHelper.Pi + " PI");
                //DEBUG("AngZ:" + cameraRotation.Z / MathHelper.Pi + " PI");

                cameraRotation.X = MathHelper.Clamp(cameraRotation.X, -MathHelper.PiOver2 * SHITTY_CAM_PITCH_LIMIT_FATCAT_CLAMP, MathHelper.PiOver2 * SHITTY_CAM_PITCH_LIMIT_FATCAT_CLAMP);

                ORBIT_CAM_DISTANCE = Math.Max(ORBIT_CAM_DISTANCE, SHITTY_CAM_ZOOM_MIN_DIST);

                var distanceVectorAfterMove = -Vector3.Transform(Vector3.Forward, Matrix.CreateRotationX(-cameraRotation.X) * Matrix.CreateRotationY(-cameraRotation.Y)
                    * Matrix.CreateRotationZ(-cameraRotation.Z)
                    );// (cameraOrigin - (cameraPosition - new Vector3(0, 0, 5)));
                cameraPosition = cameraOrigin - (Vector3.Normalize(distanceVectorAfterMove) * ORBIT_CAM_DISTANCE) + new Vector3(0, 0, 5);
            }
            else
            {
                cameraRotation.X = MathHelper.Clamp(cameraRotation.X, -MathHelper.PiOver2, MathHelper.PiOver2);
            }

                
            lightRotation.X = MathHelper.Clamp(lightRotation.X, -MathHelper.PiOver2, MathHelper.PiOver2);
            oldWheel = currentWheel;

            prev_isToggleAllSubmeshKeyPressed = isToggleAllSubmeshKeyPressed;
            prev_isToggleAllDummyKeyPressed = isToggleAllDummyKeyPressed;
            prev_isToggleAllBonesKeyPressed = isToggleAllBonesKeyPressed;

            oldMouseClick = currentMouseClick;

            oldOrbitCamToggleKeyPressed = isOrbitCamToggleKeyPressed;

            oldTextureBrowseKey = isTextureBrowseKeyPressed;
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
