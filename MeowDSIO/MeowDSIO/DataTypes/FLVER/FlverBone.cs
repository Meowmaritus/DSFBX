using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverBone
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Collapsed)]
        [Newtonsoft.Json.JsonIgnore]
        public DataFiles.FLVER ContainingFlver { get; set; } = null;

        public FlverBone(DataFiles.FLVER ContainingFlver)
        {
            this.ContainingFlver = ContainingFlver;
        }

        public Vector3 Translation { get; set; } = Vector3.Zero;

        public string Name { get; set; } = null;

        //"When generating a quaternion: y * z * x (order matters!)" -nyx
        public FlverVector3 EulerRadian { get; set; } = Vector3.Zero;

        public FlverVector3 Scale { get; set; } = Vector3.One;

        public short ParentIndex { get; set; } = -1;
        public FlverBone GetParent() => ContainingFlver?.GetBoneFromIndex(ParentIndex, true);

        public short FirstChildIndex { get; set; } = -1;
        public FlverBone GetFirstChild() => ContainingFlver?.GetBoneFromIndex(FirstChildIndex, true);

        public short NextSiblingIndex { get; set; } = -1;
        public FlverBone GetNextSibling() => ContainingFlver?.GetBoneFromIndex(NextSiblingIndex, true);

        public short PreviousSiblingIndex { get; set; } = -1;
        public FlverBone GetPreviousSibling() => ContainingFlver?.GetBoneFromIndex(PreviousSiblingIndex, true);

        public FlverVector3 BoundingBoxMin { get; set; } = null;

        public bool IsNub { get; set; }

        public ushort UnknownUShort1 { get; set; }

        public FlverVector3 BoundingBoxMax { get; set; } = null;

        public ushort UnknownUShort2 { get; set; }
        public ushort UnknownUShort3 { get; set; }

        public int UnknownInt1 { get; set; }
        public int UnknownInt2 { get; set; }
        public int UnknownInt3 { get; set; }
        public int UnknownInt4 { get; set; }
        public int UnknownInt5 { get; set; }
        public int UnknownInt6 { get; set; }
        public int UnknownInt7 { get; set; }
        public int UnknownInt8 { get; set; }
        public int UnknownInt9 { get; set; }
        public int UnknownInt10 { get; set; }
        public int UnknownInt11 { get; set; }
        public int UnknownInt12 { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
