﻿namespace SEToolbox.ViewModels
{
    using Sandbox.CommonLib.ObjectBuilders;
    using SEToolbox.Interfaces;
    using SEToolbox.Interop;
    using SEToolbox.Models;
    using SEToolbox.Properties;
    using SEToolbox.Services;
    using SEToolbox.Support;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Windows.Forms;
    using System.Windows.Input;
    using System.Windows.Media.Media3D;
    using VRageMath;

    public class Import3dModelViewModel : BaseViewModel
    {
        #region Fields

        private readonly IDialogService dialogService;
        private readonly Func<IOpenFileDialog> openFileDialogFactory;
        private Import3dModelModel dataModel;

        private bool? closeResult;
        private bool isBusy;

        #endregion

        #region Constructors

        public Import3dModelViewModel(BaseViewModel parentViewModel, Import3dModelModel dataModel)
            : this(parentViewModel, dataModel, ServiceLocator.Resolve<IDialogService>(), () => ServiceLocator.Resolve<IOpenFileDialog>())
        {
        }

        public Import3dModelViewModel(BaseViewModel parentViewModel, Import3dModelModel dataModel, IDialogService dialogService, Func<IOpenFileDialog> openFileDialogFactory)
            : base(parentViewModel)
        {
            Contract.Requires(dialogService != null);
            Contract.Requires(openFileDialogFactory != null);

            this.dialogService = dialogService;
            this.openFileDialogFactory = openFileDialogFactory;
            this.dataModel = dataModel;
            this.dataModel.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e)
            {
                // Will bubble property change events from the Model to the ViewModel.
                this.OnPropertyChanged(e.PropertyName);
            };

            this.IsMultipleScale = true;
            this.MultipleScale = 1;
            this.MaxLengthScale = 100;
            this.ClassType = ImportClassType.SmallShip;
            this.ArmorType = ImportArmorType.Light;
        }

        #endregion

        #region Properties

        public ICommand Browse3dModelCommand
        {
            get
            {
                return new DelegateCommand(new Action(Browse3dModelExecuted), new Func<bool>(Browse3dModelCanExecute));
            }
        }

        public ICommand CreateCommand
        {
            get
            {
                return new DelegateCommand(new Action(CreateExecuted), new Func<bool>(CreateCanExecute));
            }
        }

        public ICommand CancelCommand
        {
            get
            {
                return new DelegateCommand(new Action(CancelExecuted), new Func<bool>(CancelCanExecute));
            }
        }

        /// <summary>
        /// Gets or sets the DialogResult of the View.  If True or False is passed, this initiates the Close().
        /// </summary>
        public bool? CloseResult
        {
            get
            {
                return this.closeResult;
            }

            set
            {
                this.closeResult = value;
                this.RaisePropertyChanged(() => CloseResult);
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the View is currently in the middle of an asynchonise operation.
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return this.isBusy;
            }

            set
            {
                if (value != this.isBusy)
                {
                    this.isBusy = value;
                    this.RaisePropertyChanged(() => IsBusy);
                    if (this.isBusy)
                    {
                        System.Windows.Forms.Application.DoEvents();
                    }
                }
            }
        }

        public string Filename
        {
            get
            {
                return this.dataModel.Filename;
            }

            set
            {
                this.dataModel.Filename = value;
                this.FilenameChanged();
            }
        }

        public bool IsValidModel
        {
            get
            {
                return this.dataModel.IsValidModel;
            }

            set
            {
                this.dataModel.IsValidModel = value;
            }
        }

        public BindableSize3DModel OriginalModelSize
        {
            get
            {
                return this.dataModel.OriginalModelSize;
            }

            set
            {
                this.dataModel.OriginalModelSize = value;
            }
        }

        public BindableSize3DModel NewModelSize
        {
            get
            {
                return this.dataModel.NewModelSize;
            }

            set
            {
                this.dataModel.NewModelSize = value;
                this.ProcessModelScale();
            }
        }

        public BindablePoint3DModel NewModelScale
        {
            get
            {
                return this.dataModel.NewModelScale;
            }

            set
            {
                this.dataModel.NewModelScale = value;
            }
        }

        public BindablePoint3DModel Position
        {
            get
            {
                return this.dataModel.Position;
            }

            set
            {
                this.dataModel.Position = value;
            }
        }

        public BindableVector3DModel Forward
        {
            get
            {
                return this.dataModel.Forward;
            }

            set
            {
                this.dataModel.Forward = value;
            }
        }

        public BindableVector3DModel Up
        {
            get
            {
                return this.dataModel.Up;
            }

            set
            {
                this.dataModel.Up = value;
            }
        }

        public ImportClassType ClassType
        {
            get
            {
                return this.dataModel.ClassType;
            }

            set
            {
                this.dataModel.ClassType = value;
                this.ProcessModelScale();
            }
        }

        public ImportArmorType ArmorType
        {
            get
            {
                return this.dataModel.ArmorType;
            }

            set
            {
                this.dataModel.ArmorType = value;
            }
        }


        public double MultipleScale
        {
            get
            {
                return this.dataModel.MultipleScale;
            }

            set
            {
                this.dataModel.MultipleScale = value;
                this.ProcessModelScale();
            }
        }

        public double MaxLengthScale
        {
            get
            {
                return this.dataModel.MaxLengthScale;
            }

            set
            {
                this.dataModel.MaxLengthScale = value;
                this.ProcessModelScale();
            }
        }

        public double BuildDistance
        {
            get
            {
                return this.dataModel.BuildDistance;
            }

            set
            {
                this.dataModel.BuildDistance = value;
                this.ProcessModelScale();
            }
        }

        public bool IsMultipleScale
        {
            get
            {
                return this.dataModel.IsMultipleScale;
            }

            set
            {
                this.dataModel.IsMultipleScale = value;
                this.ProcessModelScale();
            }
        }

        public bool IsMaxLengthScale
        {
            get
            {
                return this.dataModel.IsMaxLengthScale;
            }

            set
            {
                this.dataModel.IsMaxLengthScale = value;
                this.ProcessModelScale();
            }
        }

        #endregion

        #region methods

        public bool Browse3dModelCanExecute()
        {
            return true;
        }

        public void Browse3dModelExecuted()
        {
            this.IsValidModel = false;

            IOpenFileDialog openFileDialog = openFileDialogFactory();
            openFileDialog.Filter = Resources.ImportModelFilter;
            openFileDialog.Title = Resources.ImportModelTitle;

            // Open the dialog
            DialogResult result = dialogService.ShowOpenFileDialog(this, openFileDialog);

            if (result == DialogResult.OK)
            {
                this.Filename = openFileDialog.FileName;
            }
        }

        private void FilenameChanged()
        {
            this.ProcessFilename(this.Filename);
        }

        public bool CreateCanExecute()
        {
            return this.IsValidModel;
        }

        public void CreateExecuted()
        {
            this.CloseResult = true;
        }

        public bool CancelCanExecute()
        {
            return true;
        }

        public void CancelExecuted()
        {
            this.CloseResult = false;
        }

        #endregion

        #region methods

        private void ProcessFilename(string filename)
        {
            this.IsValidModel = false;
            this.IsBusy = true;

            this.OriginalModelSize = new BindableSize3DModel(0, 0, 0);
            this.NewModelSize = new BindableSize3DModel(0, 0, 0);
            this.Position = new BindablePoint3DModel(0, 0, 0);

            if (File.Exists(filename))
            {
                // validate file is a real model.
                // read model properties.
                var size = Preview3DModel(filename);

                if (size != null && size.Height != 0 && size.Width != 0 && size.Depth != 0)
                {
                    this.OriginalModelSize = size;
                    this.BuildDistance = 10;
                    this.IsValidModel = true;
                    this.ProcessModelScale();
                }
            }

            this.IsBusy = false;
        }

        private BindableSize3DModel Preview3DModel(string filename)
        {
            var voxFilename = ToolboxExtensions.ConvertPolyToVox(filename, 0, false);
            BindableSize3DModel size = new BindableSize3DModel();

            if (voxFilename != null)
            {
                using (BinaryReader reader = new BinaryReader(File.Open(voxFilename, FileMode.Open)))
                {
                    size.Width = reader.ReadInt32();
                    size.Depth = reader.ReadInt32();
                    size.Height = reader.ReadInt32();
                }

                File.Delete(voxFilename);
                return size;
            }

            return null;
        }

        private void ProcessModelScale()
        {
            if (this.IsValidModel)
            {
                if (this.IsMaxLengthScale)
                {
                    var factor = this.MaxLengthScale / Math.Max(Math.Max(this.OriginalModelSize.Height, this.OriginalModelSize.Width), this.OriginalModelSize.Depth);

                    this.NewModelSize.Height = (int)(factor * this.OriginalModelSize.Height);
                    this.NewModelSize.Width = (int)(factor * this.OriginalModelSize.Width);
                    this.NewModelSize.Depth = (int)(factor * this.OriginalModelSize.Depth);
                }
                else if (this.IsMultipleScale)
                {
                    this.NewModelSize.Height = (int)(this.MultipleScale * this.OriginalModelSize.Height);
                    this.NewModelSize.Width = (int)(this.MultipleScale * this.OriginalModelSize.Width);
                    this.NewModelSize.Depth = (int)(this.MultipleScale * this.OriginalModelSize.Depth);
                }

                double vectorDistance = this.BuildDistance;
                double scaleMultiplyer = 2.5;

                switch (this.ClassType)
                {
                    case ImportClassType.SmallShip: scaleMultiplyer = 0.5; break;
                    case ImportClassType.LargeShip: scaleMultiplyer = 2.5; break;
                    case ImportClassType.Station: scaleMultiplyer = 2.5; break;
                }
                vectorDistance += this.NewModelSize.Depth * scaleMultiplyer;
                this.NewModelScale = new BindablePoint3DModel(this.NewModelSize.Width * scaleMultiplyer, this.NewModelSize.Height * scaleMultiplyer, this.NewModelSize.Depth * scaleMultiplyer);

                // Figure out where the Character is facing, and plant the new constrcut right in front, by "10" units, facing the Character.
                var vector = new BindableVector3DModel(this.dataModel.CharacterPosition.Forward).Vector3D;
                vector.Normalize();
                vector = Vector3D.Multiply(vector, vectorDistance);
                this.Position = new BindablePoint3DModel(Point3D.Add(new BindablePoint3DModel(this.dataModel.CharacterPosition.Position).Point3D, vector));
                this.Forward = new BindableVector3DModel(this.dataModel.CharacterPosition.Forward);
                this.Up = new BindableVector3DModel(this.dataModel.CharacterPosition.Up);
            }
        }

        #endregion

        #region BuildTestEntity

        public MyObjectBuilder_CubeGrid BuildTestEntity()
        {
            var entity = new MyObjectBuilder_CubeGrid
            {
                EntityId = SpaceEngineersAPI.GenerateEntityId(),
                PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                Skeleton = new System.Collections.Generic.List<BoneInfo>(),
                LinearVelocity = new VRageMath.Vector3(0, 0, 0),
                AngularVelocity = new VRageMath.Vector3(0, 0, 0),
                GridSizeEnum = MyCubeSize.Small
            };

            var blockPrefix = "Small";
            entity.IsStatic = false;
            blockPrefix += "Block";

            // Figure out where the Character is facing, and plant the new constrcut right in front, by "10" units, facing the Character.
            var vector = new BindableVector3DModel(this.dataModel.CharacterPosition.Forward).Vector3D;
            vector.Normalize();
            vector = Vector3D.Multiply(vector, 6);
            this.Position = new BindablePoint3DModel(Point3D.Add(new BindablePoint3DModel(this.dataModel.CharacterPosition.Position).Point3D, vector));
            this.Forward = new BindableVector3DModel(this.dataModel.CharacterPosition.Forward);
            this.Up = new BindableVector3DModel(this.dataModel.CharacterPosition.Up);

            entity.PositionAndOrientation = new MyPositionAndOrientation()
            {
                // TODO: reposition based scale.
                Position = this.Position.ToVector3(),
                Forward = this.Forward.ToVector3(),
                Up = this.Up.ToVector3()
            };

            // Large|Block|ArmorCorner
            // Large|HeavyBlock|ArmorBlock,
            // Small|Block|ArmorSlope,
            // Small|HeavyBlock|ArmorCorner,

            var blockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "ArmorBlock");
            var slopeBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "ArmorSlope");
            var cornerBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "ArmorCorner");
            var inverseCornerBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "ArmorCornerInv");

            entity.CubeBlocks = new System.Collections.Generic.List<MyObjectBuilder_CubeBlock>();

            var fixScale = 0;
            if (this.IsMultipleScale && this.MultipleScale == 1)
            {
                fixScale = 0;
            }
            else
            {
                fixScale = Math.Max(Math.Max(this.NewModelSize.Height, this.NewModelSize.Width), this.NewModelSize.Depth);
            }

            // TODO: fillobject.
            // TODO: smoothing.

            var smoothObject = true;

            #region Read in voxel and set main cube space.

            //var ccubic = new CubeType[9, 9, 9];

            //for (int i = 3; i < 6; i++)
            //{
            //    for (int j = 3; j < 6; j++)
            //    {
            //        ccubic[i, j, 4] = CubeType.Cube;
            //        ccubic[i, 4, j] = CubeType.Cube;
            //        ccubic[4, i, j] = CubeType.Cube;
            //    }
            //}

            //for (int i = 0; i < 9; i++)
            //{
            //    ccubic[i, 4, 4] = CubeType.Cube;
            //    ccubic[4, i, 4] = CubeType.Cube;
            //    ccubic[4, 4, i] = CubeType.Cube;
            //}

            #endregion

            #region Read in voxel and set main cube space.

            // Staggered star.

            var ccubic = new CubeType[9, 9, 9];

            for (var i = 2; i < 7; i++)
            {
                for (var j = 2; j < 7; j++)
                {
                    ccubic[i, j, 4] = CubeType.Cube;
                    ccubic[i, 4, j] = CubeType.Cube;
                    ccubic[4, i, j] = CubeType.Cube;
                }
            }

            for (var i = 0; i < 9; i++)
            {
                ccubic[i, 4, 4] = CubeType.Cube;
                ccubic[4, i, 4] = CubeType.Cube;
                ccubic[4, 4, i] = CubeType.Cube;
            }

            #endregion

            #region Read in voxel and set main cube space.

            //// Tray shape

            //var ccubic = new CubeType[12, 12, 12];

            //for (int i = 0; i < 12; i++)
            //{
            //    for (int j = 0; j < 12; j++)
            //    {
            //        ccubic[i, j, 0] = CubeType.Cube;
            //    }
            //}

            //for (int i = 0; i < 12; i++)
            //{
            //    ccubic[i, 0, 1] = CubeType.Cube;
            //    ccubic[i, 11, 1] = CubeType.Cube;
            //    ccubic[0, i, 1] = CubeType.Cube;
            //    ccubic[11, i, 1] = CubeType.Cube;
            //}

            #endregion

            if (smoothObject)
            {
                CalculateInverseCorners(ccubic);
                CalculateSlopes(ccubic);
                CalculateCorners(ccubic);
            }

            this.BuildStructureFromCubic(entity, ccubic, blockType, slopeBlockType, cornerBlockType, inverseCornerBlockType);

            return entity;
        }

        #endregion

        #region BuildEntity

        public MyObjectBuilder_CubeGrid BuildEntity()
        {
            var entity = new MyObjectBuilder_CubeGrid
            {
                EntityId = SpaceEngineersAPI.GenerateEntityId(),
                PersistentFlags = MyPersistentEntityFlags2.CastShadows | MyPersistentEntityFlags2.InScene,
                Skeleton = new System.Collections.Generic.List<BoneInfo>(),
                LinearVelocity = new VRageMath.Vector3(0, 0, 0),
                AngularVelocity = new VRageMath.Vector3(0, 0, 0)
            };

            var blockPrefix = "";
            switch (this.ClassType)
            {
                case ImportClassType.SmallShip:
                    entity.GridSizeEnum = MyCubeSize.Small;
                    blockPrefix += "Small";
                    entity.IsStatic = false;
                    break;

                case ImportClassType.LargeShip:
                    entity.GridSizeEnum = MyCubeSize.Large;
                    blockPrefix += "Large";
                    entity.IsStatic = false;
                    break;

                case ImportClassType.Station:
                    entity.GridSizeEnum = MyCubeSize.Large;
                    blockPrefix += "Large";
                    entity.IsStatic = true;
                    this.Position = this.Position.RoundOff(2.5);
                    this.Forward = this.Forward.RoundToAxis();
                    this.Up = this.Up.RoundToAxis();
                    break;
            }

            switch (this.ArmorType)
            {
                case ImportArmorType.Heavy: blockPrefix += "HeavyBlock"; break;
                case ImportArmorType.Light: blockPrefix += "Block"; break;
            }

            entity.PositionAndOrientation = new MyPositionAndOrientation()
            {
                // TODO: reposition based scale.
                Position = this.Position.ToVector3(),
                Forward = this.Forward.ToVector3(),
                Up = this.Up.ToVector3()
            };

            // Large|Block|ArmorCorner
            // Large|HeavyBlock|ArmorBlock,
            // Small|Block|ArmorSlope,
            // Small|HeavyBlock|ArmorCorner,

            var blockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "ArmorBlock");
            var slopeBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "ArmorSlope");
            var cornerBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "ArmorCorner");
            var inverseCornerBlockType = (SubtypeId)Enum.Parse(typeof(SubtypeId), blockPrefix + "ArmorCornerInv");

            entity.CubeBlocks = new System.Collections.Generic.List<MyObjectBuilder_CubeBlock>();

            var fixScale = 0;
            if (this.IsMultipleScale && this.MultipleScale == 1)
            {
                fixScale = 0;
            }
            else
            {
                fixScale = Math.Max(Math.Max(this.NewModelSize.Height, this.NewModelSize.Width), this.NewModelSize.Depth);
            }

            // TODO: fillobject UI.
            // TODO: smoothing UI.

            var fillObject = false;
            var smoothObject = true;


            var ccubic = ReadTemporaryVolmetic(this.Filename, fixScale, fillObject);
            //var ccubic = ReadModelVolmetic(this.Filename, 1);

            if (smoothObject)
            {
                CalculateInverseCorners(ccubic);
                CalculateSlopes(ccubic);
                CalculateCorners(ccubic);
            }

            this.BuildStructureFromCubic(entity, ccubic, blockType, slopeBlockType, cornerBlockType, inverseCornerBlockType);

            return entity;
        }

        #endregion

        #region ReadTemporaryVolmetic

        private CubeType[, ,] ReadTemporaryVolmetic(string modelFile, int fixScale, bool fillObject)
        {
            CubeType[, ,] ccubic = null;
            var gapless = false;
            var foundShape = false;
            int tries = 0;

            while (!foundShape && tries < 2)
            {
                var voxFilename = ToolboxExtensions.ConvertPolyToVox(modelFile, fixScale, gapless);

                using (var reader = new BinaryReader(File.Open(voxFilename, FileMode.Open)))
                {
                    // switch the Z and Y axis about, and reverse the Y axis to get the dimension in the right order from the Vox file.

                    var xCount = reader.ReadInt32();
                    var zCount = reader.ReadInt32();
                    var yCount = reader.ReadInt32();
                    ccubic = new CubeType[xCount, yCount, zCount];

                    for (var x = 0; x < xCount; x++)
                    {
                        for (var z = 0; z < zCount; z++)
                        {
                            for (var y = yCount - 1; y >= 0; y--)
                            {
                                var b = reader.ReadByte();

                                switch (b)
                                {
                                    case 0x00: // hollow interior
                                        if (fillObject)
                                        {
                                            ccubic[x, y, z] = CubeType.Cube;
                                        }
                                        else
                                        {
                                            ccubic[x, y, z] = CubeType.Interior;
                                        }
                                        break;

                                    case 0xFF: // space
                                        ccubic[x, y, z] = CubeType.None;
                                        break;

                                    case 0x12: // solid
                                    default:
                                        ccubic[x, y, z] = CubeType.Cube;
                                        foundShape = true;
                                        break;
                                }
                            }
                        }
                    }
                }

                File.Delete(voxFilename);

                if (!foundShape)
                {
                    // Try again, but use the gapless switch.
                    gapless = true;
                }
                tries++;
            }

            return ccubic;
        }

        /// <summary>
        /// Volumes are calculated across axis where they are whole numbers (rounded to 0 decimal places).
        /// </summary>
        /// <param name="modelFile"></param>
        /// <param name="scaleMultiplyier"></param>
        /// <returns></returns>
        public static CubeType[, ,] ReadModelVolmetic(string modelFile, double scaleMultiplyier)
        {
            var model = MeshHelper.Load(modelFile, IgnoreErrors: true);

            // Workaround: Random small offset as RayIntersetTriangle() is unable to detect if the ray is exactly on the Edge of the Triangle.
            // Which is usually because the Model's Verticies were placed with some sort of SnapTo turned on in the Designer.
            const double offset = 0.00000456f;

            if (scaleMultiplyier > 0 && scaleMultiplyier != 1.0f)
            {
                model.TransformScale(scaleMultiplyier);
            }

            var xMin = (int)Math.Floor(model.Bounds.X);
            var yMin = (int)Math.Floor(model.Bounds.Y);
            var zMin = (int)Math.Floor(model.Bounds.Z);

            var xMax = (int)Math.Ceiling(model.Bounds.X + model.Bounds.SizeX);
            var yMax = (int)Math.Ceiling(model.Bounds.Y + model.Bounds.SizeY);
            var zMax = (int)Math.Ceiling(model.Bounds.Z + model.Bounds.SizeZ);

            var xCount = xMax - xMin;
            var yCount = yMax - yMin;
            var zCount = zMax - zMin;

            var ccubic = new CubeType[xCount, yCount, zCount];

            #region basic ray trace of every individual triangle.

            foreach (GeometryModel3D gm in model.Children)
            {
                var g = gm.Geometry as MeshGeometry3D;

                for (var t = 0; t < g.TriangleIndices.Count; t += 3)
                {
                    var p1 = g.Positions[g.TriangleIndices[t]];
                    var p2 = g.Positions[g.TriangleIndices[t + 1]];
                    var p3 = g.Positions[g.TriangleIndices[t + 2]];

                    var minBound = MeshHelper.Min(p1, p2, p3).Floor();
                    var maxBound = MeshHelper.Max(p1, p2, p3).Ceiling();

                    for (var y = minBound.Y; y < maxBound.Y; y++)
                    {
                        for (var z = minBound.Z; z < maxBound.Z; z++)
                        {
                            var r1 = new Point3D(xMin + offset, y + offset, z + offset);
                            var r2 = new Point3D(xMax + offset, y + offset, z + offset);
                            Point3D intersect;

                            if (MeshHelper.RayIntersetTriangleRound(p1, p2, p3, r1, r2, out intersect)) // Ray
                            {
                                ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                            }
                            else if (MeshHelper.RayIntersetTriangleRound(p1, p2, p3, r2, r1, out intersect)) // Reverse Ray
                            {
                                ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                            }
                        }
                    }

                    for (var x = minBound.X; x < maxBound.X; x++)
                    {
                        for (var z = minBound.Z; z < maxBound.Z; z++)
                        {
                            var r1 = new Point3D(x + offset, yMin + offset, z + offset);
                            var r2 = new Point3D(x + offset, yMax + offset, z + offset);
                            Point3D intersect;

                            if (MeshHelper.RayIntersetTriangleRound(p1, p2, p3, r1, r2, out intersect)) // Ray
                            {
                                ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                            }
                            else if (MeshHelper.RayIntersetTriangleRound(p1, p2, p3, r2, r1, out intersect)) // Reverse Ray
                            {
                                ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                            }
                        }
                    }

                    for (var x = minBound.X; x < maxBound.X; x++)
                    {
                        for (var y = minBound.Y; y < maxBound.Y; y++)
                        {
                            var r1 = new Point3D(x + offset, y + offset, zMin + offset);
                            var r2 = new Point3D(x + offset, y + offset, zMax + offset);
                            Point3D intersect;

                            if (MeshHelper.RayIntersetTriangleRound(p1, p2, p3, r1, r2, out intersect)) // Ray
                            {
                                ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                            }
                            else if (MeshHelper.RayIntersetTriangleRound(p1, p2, p3, r2, r1, out intersect)) // Reverse Ray
                            {
                                ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                            }
                        }
                    }
                }
            } 

            #endregion

            CrawlExterior(ccubic);

            return ccubic;
        }

        // WIP.
        public static CubeType[, ,] ReadModelVolmeticAlt(string modelFile, double voxelUnitSize)
        {
            var model = MeshHelper.Load(modelFile, IgnoreErrors: true);

            var min = model.Bounds;
            var max = new Point3D(model.Bounds.Location.X + model.Bounds.Size.X, model.Bounds.Location.X + model.Bounds.Size.Z, model.Bounds.Location.Z + model.Bounds.Size.Z);

            //int xCount = xMax - xMin;
            //int yCount = yMax - yMin;
            //int zCount = zMax - zMin;

            //var ccubic = new CubeType[xCount, yCount, zCount];
            var ccubic = new CubeType[0, 0, 0];
            var blockDict = new Dictionary<Point3D, byte[]>();

            #region basic ray trace of every individual triangle.

            foreach (GeometryModel3D gm in model.Children)
            {
                var g = gm.Geometry as MeshGeometry3D;

                for (int t = 0; t < g.TriangleIndices.Count; t += 3)
                {
                    var p1 = g.Positions[g.TriangleIndices[t]];
                    var p2 = g.Positions[g.TriangleIndices[t + 1]];
                    var p3 = g.Positions[g.TriangleIndices[t + 2]];

                    var minBound = MeshHelper.Min(p1, p2, p3).Floor();
                    var maxBound = MeshHelper.Max(p1, p2, p3).Ceiling();

                    //for (var y = minBound.Y; y < maxBound.Y; y++)
                    //{
                    //    for (var z = minBound.Z; z < maxBound.Z; z++)
                    //    {
                    //        var r1 = new Point3D(xMin, y, z);
                    //        var r2 = new Point3D(xMax, y, z);

                    //        Point3D intersect;
                    //        if (MeshHelper.RayIntersetTriangle(p1, p2, p3, r1, r2, out intersect)) // Ray
                    //        {
                    //            //var blockPoint = intersect.Round();
                    //            //var cornerHit = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };

                    //            //if (!blockDict.ContainsKey(blockPoint))
                    //            //    blockDict[blockPoint] = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
                    //            //if (Math.Round(intersect.X) - intersect.X < 0 && Math.Round(intersect.Y) - intersect.Y < 0 && Math.Round(intersect.Z) - intersect.Z < 0)
                    //            //    cornerHit = new byte[] { 1, 0, 0, 0, 0, 0, 0, 0 };
                    //            //else if (Math.Round(intersect.X) - intersect.X < 0 && Math.Round(intersect.Y) - intersect.Y > 0 && Math.Round(intersect.Z) - intersect.Z < 0)
                    //            //    cornerHit = new byte[] { 0, 1, 0, 0, 0, 0, 0, 0 };
                    //            //else if (Math.Round(intersect.X) - intersect.X < 0 && Math.Round(intersect.Y) - intersect.Y < 0 && Math.Round(intersect.Z) - intersect.Z > 0)
                    //            //    cornerHit = new byte[] { 0, 0, 1, 0, 0, 0, 0, 0 };
                    //            //else if (Math.Round(intersect.X) - intersect.X < 0 && Math.Round(intersect.Y) - intersect.Y > 0 && Math.Round(intersect.Z) - intersect.Z > 0)
                    //            //    cornerHit = new byte[] { 0, 0, 0, 1, 0, 0, 0, 0 };
                    //            //else if (Math.Round(intersect.X) - intersect.X > 0 && Math.Round(intersect.Y) - intersect.Y < 0 && Math.Round(intersect.Z) - intersect.Z < 0)
                    //            //    cornerHit = new byte[] { 0, 0, 0, 0, 1, 0, 0, 0 };
                    //            //else if (Math.Round(intersect.X) - intersect.X > 0 && Math.Round(intersect.Y) - intersect.Y > 0 && Math.Round(intersect.Z) - intersect.Z < 0)
                    //            //    cornerHit = new byte[] { 0, 0, 0, 0, 0, 1, 0, 0 };
                    //            //else if (Math.Round(intersect.X) - intersect.X > 0 && Math.Round(intersect.Y) - intersect.Y < 0 && Math.Round(intersect.Z) - intersect.Z > 0)
                    //            //    cornerHit = new byte[] { 0, 0, 0, 0, 0, 0, 1, 0 };
                    //            //else if (Math.Round(intersect.X) - intersect.X > 0 && Math.Round(intersect.Y) - intersect.Y > 0 && Math.Round(intersect.Z) - intersect.Z > 0)
                    //            //    cornerHit = new byte[] { 0, 0, 0, 0, 0, 0, 0, 1 }; 

                    //            //blockDict[blockPoint]=[int(bool(a+b)) for a,b in zip(blockDict[blockPoint],cornerHit)]


                    //            ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                    //        }
                    //        else if (MeshHelper.RayIntersetTriangle(p1, p2, p3, r2, r1, out intersect)) // Reverse Ray
                    //        {
                    //            ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                    //        }
                    //    }
                    //}

                    //for (var x = minBound.X; x < maxBound.X; x++)
                    //{
                    //    for (var z = minBound.Z; z < maxBound.Z; z++)
                    //    {
                    //        var r1 = new Point3D(x, yMin, z);
                    //        var r2 = new Point3D(x, yMax, z);

                    //        Point3D intersect;
                    //        if (MeshHelper.RayIntersetTriangle(p1, p2, p3, r1, r2, out intersect)) // Ray
                    //        {
                    //            ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                    //        }
                    //        else if (MeshHelper.RayIntersetTriangle(p1, p2, p3, r2, r1, out intersect)) // Reverse Ray
                    //        {
                    //            ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                    //        }
                    //    }
                    //}

                    //for (var x = minBound.X; x < maxBound.X; x++)
                    //{
                    //    for (var y = minBound.Y; y < maxBound.Y; y++)
                    //    {
                    //        var r1 = new Point3D(x, y, zMin);
                    //        var r2 = new Point3D(x, y, zMax);

                    //        Point3D intersect;
                    //        if (MeshHelper.RayIntersetTriangle(p1, p2, p3, r1, r2, out intersect)) // Ray
                    //        {
                    //            ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                    //        }
                    //        else if (MeshHelper.RayIntersetTriangle(p1, p2, p3, r2, r1, out intersect)) // Reverse Ray
                    //        {
                    //            ccubic[(int)Math.Floor(intersect.X) - xMin, (int)Math.Floor(intersect.Y) - yMin, (int)Math.Floor(intersect.Z) - zMin] = CubeType.Cube;
                    //        }
                    //    }
                    //}
                }
            }

            #endregion

            return ccubic;
        }

        public static void CrawlExterior(CubeType[, ,] cubic)
        {
            var xMax = cubic.GetLength(0);
            var yMax = cubic.GetLength(1);
            var zMax = cubic.GetLength(2);
            var list = new Queue<Vector3I>();

            // Add basic check points from the corner blocks.
            if (cubic[0, 0, 0] == CubeType.None)
                list.Enqueue(new Vector3I(0, 0, 0));
            if (cubic[xMax - 1, 0, 0] == CubeType.None)
                list.Enqueue(new Vector3I(xMax - 1, 0, 0));
            if (cubic[0, yMax - 1, 0] == CubeType.None)
                list.Enqueue(new Vector3I(0, yMax - 1, 0));
            if (cubic[0, 0, zMax - 1] == CubeType.None)
                list.Enqueue(new Vector3I(0, 0, zMax - 1));
            if (cubic[xMax - 1, yMax - 1, 0] == CubeType.None)
                list.Enqueue(new Vector3I(xMax - 1, yMax - 1, 0));
            if (cubic[0, yMax - 1, zMax - 1] == CubeType.None)
                list.Enqueue(new Vector3I(0, yMax - 1, zMax - 1));
            if (cubic[xMax - 1, 0, zMax - 1] == CubeType.None)
                list.Enqueue(new Vector3I(xMax - 1, 0, zMax - 1));
            if (cubic[xMax - 1, yMax - 1, zMax - 1] == CubeType.None)
                list.Enqueue(new Vector3I(xMax - 1, yMax - 1, zMax - 1));

            while (list.Count > 0)
            {
                var item = list.Dequeue();

                if (cubic[item.X, item.Y, item.Z] == CubeType.None)
                {
                    cubic[item.X, item.Y, item.Z] = CubeType.Exterior;

                    if (item.X - 1 >= 0 && item.Y >= 0 && item.Z >= 0 && item.X - 1 < xMax && item.Y < yMax && item.Z < zMax && cubic[item.X - 1, item.Y, item.Z] == CubeType.None)
                    {
                        list.Enqueue(new Vector3I(item.X - 1, item.Y, item.Z));
                    }
                    if (item.X >= 0 && item.Y - 1 >= 0 && item.Z >= 0 && item.X < xMax && item.Y - 1 < yMax && item.Z < zMax && cubic[item.X, item.Y - 1, item.Z] == CubeType.None)
                    {
                        list.Enqueue(new Vector3I(item.X, item.Y - 1, item.Z));
                    }
                    if (item.X >= 0 && item.Y >= 0 && item.Z - 1 >= 0 && item.X < xMax && item.Y < yMax && item.Z - 1 < zMax && cubic[item.X, item.Y, item.Z - 1] == CubeType.None)
                    {
                        list.Enqueue(new Vector3I(item.X, item.Y, item.Z - 1));
                    }
                    if (item.X + 1 >= 0 && item.Y >= 0 && item.Z >= 0 && item.X + 1 < xMax && item.Y < yMax && item.Z < zMax && cubic[item.X + 1, item.Y, item.Z] == CubeType.None)
                    {
                        list.Enqueue(new Vector3I(item.X + 1, item.Y, item.Z));
                    }
                    if (item.X >= 0 && item.Y + 1 >= 0 && item.Z >= 0 && item.X < xMax && item.Y + 1 < yMax && item.Z < zMax && cubic[item.X, item.Y + 1, item.Z] == CubeType.None)
                    {
                        list.Enqueue(new Vector3I(item.X, item.Y + 1, item.Z));
                    }
                    if (item.X >= 0 && item.Y >= 0 && item.Z + 1 >= 0 && item.X < xMax && item.Y < yMax && item.Z + 1 < zMax && cubic[item.X, item.Y, item.Z + 1] == CubeType.None)
                    {
                        list.Enqueue(new Vector3I(item.X, item.Y, item.Z + 1));
                    }
                }
            }

            // switch values around to work with current enum logic.
            for (var x = 0; x < xMax; x++)
            {
                for (var y = 0; y < yMax; y++)
                {
                    for (var z = 0; z < zMax; z++)
                    {
                        if (cubic[x, y, z] == CubeType.None)
                            cubic[x, y, z] = CubeType.Interior;
                        else if (cubic[x, y, z] == CubeType.Exterior)
                            cubic[x, y, z] = CubeType.None;
                    }
                }
            }
        }

        #endregion


        /*  Official SE orientation.
                  (top/up)+Y|   /-Z(forward/front)
                            |  /
                            | /
               -X(left)     |/       +X(right)
               -------------+-----------------
                           /|
                          / |
                         /  |
                (back)+Z/   |-Y(bottom/down)
        */

        #region CalculateSlopes

        public static void CalculateSlopes(CubeType[, ,] ccubic)
        {
            var xCount = ccubic.GetLength(0);
            var yCount = ccubic.GetLength(1);
            var zCount = ccubic.GetLength(2);

            for (int x = 0; x < xCount; x++)
            {
                for (int y = 0; y < yCount; y++)
                {
                    for (int z = 0; z < zCount; z++)
                    {
                        if (ccubic[x, y, z] == CubeType.None)
                        {
                            if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, 0, 1, 1))
                            {
                                ccubic[x, y, z] = CubeType.SlopeCenterFrontTop;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, -1, 1, 0))
                            {
                                ccubic[x, y, z] = CubeType.SlopeLeftFrontCenter;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, 1, 1, 0))
                            {
                                ccubic[x, y, z] = CubeType.SlopeRightFrontCenter;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, 0, 1, -1))
                            {
                                ccubic[x, y, z] = CubeType.SlopeCenterFrontBottom;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, -1, 0, 1))
                            {
                                ccubic[x, y, z] = CubeType.SlopeLeftCenterTop;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, 1, 0, 1))
                            {
                                ccubic[x, y, z] = CubeType.SlopeRightCenterTop;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, -1, 0, -1))
                            {
                                ccubic[x, y, z] = CubeType.SlopeLeftCenterBottom;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, 1, 0, -1))
                            {
                                ccubic[x, y, z] = CubeType.SlopeRightCenterBottom;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, 0, -1, 1))
                            {
                                ccubic[x, y, z] = CubeType.SlopeCenterBackTop;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, -1, -1, 0))
                            {
                                ccubic[x, y, z] = CubeType.SlopeLeftBackCenter;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, 1, -1, 0))
                            {
                                ccubic[x, y, z] = CubeType.SlopeRightBackCenter;
                            }
                            else if (CheckAdjacentCubic(ccubic, x, y, z, xCount, yCount, zCount, 0, -1, -1))
                            {
                                ccubic[x, y, z] = CubeType.SlopeCenterBackBottom;
                            }
                        }
                    }
                }
            }

        }

        #endregion

        #region CalculateCorners

        public static void CalculateCorners(CubeType[, ,] ccubic)
        {
            var xCount = ccubic.GetLength(0);
            var yCount = ccubic.GetLength(1);
            var zCount = ccubic.GetLength(2);

            for (int x = 0; x < xCount; x++)
            {
                for (int y = 0; y < yCount; y++)
                {
                    for (int z = 0; z < zCount; z++)
                    {
                        if (ccubic[x, y, z] == CubeType.None)
                        {
                            if ((CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, +1, CubeType.SlopeLeftFrontCenter, -1, 0, 0, CubeType.SlopeCenterFrontTop)) ||
                                (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, +1, CubeType.SlopeLeftFrontCenter, 0, +1, 0, CubeType.SlopeLeftCenterTop)) ||
                                (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, -1, 0, 0, CubeType.SlopeCenterFrontTop, 0, +1, 0, CubeType.SlopeLeftCenterTop)))
                            {
                                ccubic[x, y, z] = CubeType.CornerLeftBackTop;
                            }
                            else if ((CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, +1, CubeType.SlopeRightFrontCenter, +1, 0, 0, CubeType.SlopeCenterFrontTop)) ||
                                (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, +1, CubeType.SlopeRightFrontCenter, 0, +1, 0, CubeType.SlopeRightCenterTop)) ||
                                (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, +1, 0, 0, CubeType.SlopeCenterFrontTop, 0, +1, 0, CubeType.SlopeRightCenterTop)))
                            {
                                ccubic[x, y, z] = CubeType.CornerRightFrontTop;
                            }
                            else if ((CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, -1, CubeType.SlopeLeftFrontCenter, -1, 0, 0, CubeType.SlopeCenterFrontBottom)) ||
                               (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, -1, CubeType.SlopeLeftFrontCenter, 0, +1, 0, CubeType.SlopeLeftCenterBottom)) ||
                               (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, -1, 0, 0, CubeType.SlopeCenterFrontBottom, 0, +1, 0, CubeType.SlopeLeftCenterBottom)))
                            {
                                ccubic[x, y, z] = CubeType.CornerLeftFrontBottom;
                            }
                            else if ((CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, -1, CubeType.SlopeRightFrontCenter, +1, 0, 0, CubeType.SlopeCenterFrontBottom)) ||
                              (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, -1, CubeType.SlopeRightFrontCenter, 0, +1, 0, CubeType.SlopeRightCenterBottom)) ||
                              (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, +1, 0, 0, CubeType.SlopeCenterFrontBottom, 0, +1, 0, CubeType.SlopeRightCenterBottom)))
                            {
                                ccubic[x, y, z] = CubeType.CornerRightFrontBottom;
                            }
                            else if ((CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, +1, CubeType.SlopeLeftBackCenter, -1, 0, 0, CubeType.SlopeCenterBackTop)) ||
                                (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, +1, CubeType.SlopeLeftBackCenter, 0, -1, 0, CubeType.SlopeLeftCenterTop)) ||
                                (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, -1, 0, 0, CubeType.SlopeCenterBackTop, 0, -1, 0, CubeType.SlopeLeftCenterTop)))
                            {
                                ccubic[x, y, z] = CubeType.CornerLeftFrontTop;
                            }
                            else if ((CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, +1, CubeType.SlopeRightBackCenter, +1, 0, 0, CubeType.SlopeCenterBackTop)) ||
                                (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, +1, CubeType.SlopeRightBackCenter, 0, -1, 0, CubeType.SlopeRightCenterTop)) ||
                                (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, +1, 0, 0, CubeType.SlopeCenterBackTop, 0, -1, 0, CubeType.SlopeRightCenterTop)))
                            {
                                ccubic[x, y, z] = CubeType.CornerRightBackTop;
                            }
                            else if ((CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, -1, CubeType.SlopeLeftBackCenter, -1, 0, 0, CubeType.SlopeCenterBackBottom)) ||
                               (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, -1, CubeType.SlopeLeftBackCenter, 0, -1, 0, CubeType.SlopeLeftCenterBottom)) ||
                               (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, -1, 0, 0, CubeType.SlopeCenterBackBottom, 0, -1, 0, CubeType.SlopeLeftCenterBottom)))
                            {
                                ccubic[x, y, z] = CubeType.CornerLeftBackBottom;
                            }
                            else if ((CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, -1, CubeType.SlopeRightBackCenter, +1, 0, 0, CubeType.SlopeCenterBackBottom)) ||
                                (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, 0, 0, -1, CubeType.SlopeRightBackCenter, 0, -1, 0, CubeType.SlopeRightCenterBottom)) ||
                                (CheckAdjacentCubic2(ccubic, x, y, z, xCount, yCount, zCount, +1, 0, 0, CubeType.SlopeCenterBackBottom, 0, -1, 0, CubeType.SlopeRightCenterBottom)))
                            {
                                ccubic[x, y, z] = CubeType.CornerRightBackBottom;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region CalculateInverseCorners

        public static void CalculateInverseCorners(CubeType[, ,] ccubic)
        {
            var xCount = ccubic.GetLength(0);
            var yCount = ccubic.GetLength(1);
            var zCount = ccubic.GetLength(2);

            for (int x = 0; x < xCount; x++)
            {
                for (int y = 0; y < yCount; y++)
                {
                    for (int z = 0; z < zCount; z++)
                    {
                        if (ccubic[x, y, z] == CubeType.None)
                        {
                            if (CheckAdjacentCubic3(ccubic, x, y, z, xCount, yCount, zCount, +1, 0, 0, CubeType.Cube, 0, -1, 0, CubeType.Cube, 0, 0, -1, CubeType.Cube))
                            {
                                ccubic[x, y, z] = CubeType.InverseCornerLeftBackTop;
                            }
                            else if (CheckAdjacentCubic3(ccubic, x, y, z, xCount, yCount, zCount, -1, 0, 0, CubeType.Cube, 0, +1, 0, CubeType.Cube, 0, 0, -1, CubeType.Cube))
                            {
                                ccubic[x, y, z] = CubeType.InverseCornerRightBackTop;
                            }
                            else if (CheckAdjacentCubic3(ccubic, x, y, z, xCount, yCount, zCount, +1, 0, 0, CubeType.Cube, 0, +1, 0, CubeType.Cube, 0, 0, -1, CubeType.Cube))
                            {
                                ccubic[x, y, z] = CubeType.InverseCornerLeftFrontTop;
                            }
                            else if (CheckAdjacentCubic3(ccubic, x, y, z, xCount, yCount, zCount, -1, 0, 0, CubeType.Cube, 0, -1, 0, CubeType.Cube, 0, 0, -1, CubeType.Cube))
                            {
                                ccubic[x, y, z] = CubeType.InverseCornerRightFrontTop;
                            }
                            else if (CheckAdjacentCubic3(ccubic, x, y, z, xCount, yCount, zCount, +1, 0, 0, CubeType.Cube, 0, +1, 0, CubeType.Cube, 0, 0, +1, CubeType.Cube))
                            {
                                ccubic[x, y, z] = CubeType.InverseCornerLeftBackBottom;
                            }
                            else if (CheckAdjacentCubic3(ccubic, x, y, z, xCount, yCount, zCount, -1, 0, 0, CubeType.Cube, 0, +1, 0, CubeType.Cube, 0, 0, +1, CubeType.Cube))
                            {
                                ccubic[x, y, z] = CubeType.InverseCornerRightBackBottom;
                            }
                            else if (CheckAdjacentCubic3(ccubic, x, y, z, xCount, yCount, zCount, +1, 0, 0, CubeType.Cube, 0, -1, 0, CubeType.Cube, 0, 0, +1, CubeType.Cube))
                            {
                                ccubic[x, y, z] = CubeType.InverseCornerLeftFrontBottom;
                            }
                            else if (CheckAdjacentCubic3(ccubic, x, y, z, xCount, yCount, zCount, -1, 0, 0, CubeType.Cube, 0, -1, 0, CubeType.Cube, 0, 0, +1, CubeType.Cube))
                            {
                                ccubic[x, y, z] = CubeType.InverseCornerRightFrontBottom;
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region CheckAdjacentCubic

        private static bool IsValidRange(int x, int y, int z, int xCount, int yCount, int zCount, int xDelta, int yDelta, int zDelta)
        {
            if (x + xDelta >= 0 && x + xDelta < xCount
            && y + yDelta >= 0 && y + yDelta < yCount
            && z + zDelta >= 0 && z + zDelta < zCount)
            {
                return true;
            }

            return false;
        }

        private static bool CheckAdjacentCubic(CubeType[, ,] ccubic, int x, int y, int z, int xCount, int yCount, int zCount, int xDelta, int yDelta, int zDelta)
        {
            if (ccubic[x, y, z] == CubeType.None && IsValidRange(x, y, z, xCount, yCount, zCount, xDelta, yDelta, zDelta))
            {
                if (xDelta != 0 && ccubic[x + xDelta, y, z] == CubeType.Cube &&
                    yDelta != 0 && ccubic[x, y + yDelta, z] == CubeType.Cube &&
                    zDelta == 0)
                {
                    return true;
                }

                if (xDelta != 0 && ccubic[x + xDelta, y, z] == CubeType.Cube &&
                    yDelta == 0 &&
                    zDelta != 0 && ccubic[x, y, z + zDelta] == CubeType.Cube)
                {
                    return true;
                }

                if (xDelta == 0 &&
                    yDelta != 0 && ccubic[x, y + yDelta, z] == CubeType.Cube &&
                    zDelta != 0 && ccubic[x, y, z + zDelta] == CubeType.Cube)
                {
                    return true;
                }

                if (xDelta != 0 && ccubic[x + xDelta, y, z] == CubeType.Cube &&
                    yDelta != 0 && ccubic[x, y + yDelta, z] == CubeType.Cube &&
                    zDelta != 0 && ccubic[x, y, z + zDelta] == CubeType.Cube)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool CheckAdjacentCubic1(CubeType[, ,] ccubic, int x, int y, int z, int xCount, int yCount, int zCount,
           int xDelta, int yDelta, int zDelta, CubeType cubeType)
        {
            if (IsValidRange(x, y, z, xCount, yCount, zCount, xDelta, yDelta, zDelta))
            {
                return ccubic[x + xDelta, y + yDelta, z + zDelta] == cubeType;
            }

            return false;
        }

        private static bool CheckAdjacentCubic2(CubeType[, ,] ccubic, int x, int y, int z, int xCount, int yCount, int zCount,
            int xDelta1, int yDelta1, int zDelta1, CubeType cubeType1,
            int xDelta2, int yDelta2, int zDelta2, CubeType cubeType2)
        {
            if (IsValidRange(x, y, z, xCount, yCount, zCount, xDelta1, yDelta1, zDelta1) && IsValidRange(x, y, z, xCount, yCount, zCount, xDelta2, yDelta2, zDelta2))
            {
                return ccubic[x + xDelta1, y + yDelta1, z + zDelta1] == cubeType1 && ccubic[x + xDelta2, y + yDelta2, z + zDelta2] == cubeType2;
            }

            return false;
        }

        private static bool CheckAdjacentCubic3(CubeType[, ,] ccubic, int x, int y, int z, int xCount, int yCount, int zCount,
            int xDelta1, int yDelta1, int zDelta1, CubeType cubeType1,
            int xDelta2, int yDelta2, int zDelta2, CubeType cubeType2,
            int xDelta3, int yDelta3, int zDelta3, CubeType cubeType3)
        {
            if (IsValidRange(x, y, z, xCount, yCount, zCount, xDelta1, yDelta1, zDelta1) 
                && IsValidRange(x, y, z, xCount, yCount, zCount, xDelta2, yDelta2, zDelta2)
                && IsValidRange(x, y, z, xCount, yCount, zCount, xDelta3, yDelta3, zDelta3))
            {
                return ccubic[x + xDelta1, y + yDelta1, z + zDelta1] == cubeType1 
                    && ccubic[x + xDelta2, y + yDelta2, z + zDelta2] == cubeType2
                    && ccubic[x + xDelta3, y + yDelta3, z + zDelta3] == cubeType3;
            }

            return false;
        }

        #endregion

        #region BuildStructureFromCubic

        private void BuildStructureFromCubic(MyObjectBuilder_CubeGrid entity, CubeType[, ,] ccubic, SubtypeId blockType, SubtypeId slopeBlockType, SubtypeId cornerBlockType, SubtypeId inverseCornerBlockType)
        {
            var xCount = ccubic.GetLength(0);
            var yCount = ccubic.GetLength(1);
            var zCount = ccubic.GetLength(2);

            for (var x = 0; x < xCount; x++)
            {
                for (var y = 0; y < yCount; y++)
                {
                    for (var z = 0; z < zCount; z++)
                    {
                        if (ccubic[x, y, z] != CubeType.None && ccubic[x, y, z] != CubeType.Interior)
                        {
                            MyObjectBuilder_CubeBlock newCube;
                            entity.CubeBlocks.Add(newCube = new MyObjectBuilder_CubeBlock());

                            if (ccubic[x, y, z].ToString().StartsWith("Cube"))
                            {
                                newCube.SubtypeName = blockType.ToString();
                            }
                            else if (ccubic[x, y, z].ToString().StartsWith("Slope"))
                            {
                                newCube.SubtypeName = slopeBlockType.ToString();
                            }
                            else if (ccubic[x, y, z].ToString().StartsWith("Corner"))
                            {
                                newCube.SubtypeName = cornerBlockType.ToString();
                            }
                            else if (ccubic[x, y, z].ToString().StartsWith("Inverse"))
                            {
                                newCube.SubtypeName = inverseCornerBlockType.ToString();
                            }

                            newCube.EntityId = 0;
                            newCube.PersistentFlags = MyPersistentEntityFlags2.None;
                            SpaceEngineersAPI.SetCubeOrientation(newCube, ccubic[x, y, z]);
                            newCube.Min = new VRageMath.Vector3I(x, y, z);
                            newCube.Max = new VRageMath.Vector3I(x, y, z);
                        }
                    }
                }
            }
        }

        #endregion
    }
}
