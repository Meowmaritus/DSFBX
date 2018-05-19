using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverBoneIndices
    {
        [System.Diagnostics.DebuggerBrowsable(System.Diagnostics.DebuggerBrowsableState.Never)]
        [Newtonsoft.Json.JsonIgnore]
        public FlverSubmesh ContainingSubmesh { get; set; }

        private sbyte a = -1;
        private sbyte b = -1;
        private sbyte c = -1;
        private sbyte d = -1;

        public int A
        {
            get => a;
            set
            {
                if (value > sbyte.MaxValue || value < sbyte.MinValue)
                {
                    throw new InvalidOperationException($"FLVER Bone Indices are stored as signed " +
                        $"bytes and can only store a value within the range [-127, 127]. " +
                        $"Therefore you cannot store the value of {value} in this property.");
                }

                a = (sbyte)value;
            }
        }

        public int B
        {
            get => b;
            set
            {
                if (value > sbyte.MaxValue || value < sbyte.MinValue)
                {
                    throw new InvalidOperationException($"FLVER Bone Indices are stored as signed " +
                        $"bytes and can only store a value within the range [-127, 127]. " +
                        $"Therefore you cannot store the value of {value} in this property.");
                }

                b = (sbyte)value;
            }
        }

        public int C
        {
            get => c;
            set
            {
                if (value > sbyte.MaxValue || value < sbyte.MinValue)
                {
                    throw new InvalidOperationException($"FLVER Bone Indices are stored as signed " +
                        $"bytes and can only store a value within the range [-127, 127]. " +
                        $"Therefore you cannot store the value of {value} in this property.");
                }

                c = (sbyte)value;
            }
        }

        public int D
        {
            get => d;
            set
            {
                if (value > sbyte.MaxValue || value < sbyte.MinValue)
                {
                    throw new InvalidOperationException($"FLVER Bone Indices are stored as signed " +
                        $"bytes and can only store a value within the range [-127, 127]. " +
                        $"Therefore you cannot store the value of {value} in this property.");
                }

                d = (sbyte)value;
            }
        }

        public (sbyte A, sbyte B, sbyte C, sbyte D) GetPacked()
        {
            return (a, b, c, d);
        }

        public FlverBoneIndices(FlverSubmesh ContainingSubmesh)
        {
            this.ContainingSubmesh = ContainingSubmesh;
        }

        public FlverBoneIndices(FlverSubmesh ContainingSubmesh, sbyte packedA, sbyte packedB, sbyte packedC, sbyte packedD)
        {
            this.ContainingSubmesh = ContainingSubmesh;
            a = packedA;
            b = packedB;
            c = packedC;
            d = packedD;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.Append("[");

            if (ContainingSubmesh != null)
            {
                if (A == -1)
                {
                    sb.Append("<NONE>");
                }
                else
                {
                    var bone = ContainingSubmesh.GetBoneFromLocalIndex(A, true);

                    if (bone != null)
                        sb.Append($"\"{bone.Name}\" <{A}>, ");
                    else
                        sb.Append($"<INVALID INDEX: {A}>, ");
                }

                if (B == -1)
                {
                    sb.Append("<NONE>");
                }
                else
                {
                    var bone = ContainingSubmesh.GetBoneFromLocalIndex(B, true);

                    if (bone != null)
                        sb.Append($"\"{bone.Name}\" <{B}>, ");
                    else
                        sb.Append($"<INVALID INDEX: {B}>, ");
                }

                if (C == -1)
                {
                    sb.Append("<NONE>");
                }
                else
                {
                    var bone = ContainingSubmesh.GetBoneFromLocalIndex(C, true);

                    if (bone != null)
                        sb.Append($"\"{bone.Name}\" <{C}>, ");
                    else
                        sb.Append($"<INVALID INDEX: {C}>, ");
                }

                if (D == -1)
                {
                    sb.Append("<NONE>");
                }
                else
                {
                    var bone = ContainingSubmesh.GetBoneFromLocalIndex(D, true);

                    if (bone != null)
                        sb.Append($"\"{bone.Name}\" <{D}>");
                    else
                        sb.Append($"<INVALID INDEX: {D}>");
                }
            }
            else
            {
                sb.Append($"{A}, {B}, {C}, {D}");
            }

            sb.Append("]");

            return sb.ToString();
        }
    }
}
