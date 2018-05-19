using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverBoneWeights
    {
        private short a = 0;
        private short b = 0;
        private short c = 0;
        private short d = 0;

        [JsonIgnore]
        public float A
        {
            get => (a / 32767.0f);
            set => a = (short)(int)(value / 32767.0f);
        }

        [JsonIgnore]
        public float B
        {
            get => (b / 32767.0f);
            set => b = (short)(int)(value / 32767.0f);
        }

        [JsonIgnore]
        public float C
        {
            get => (c / 32767.0f);
            set => c = (short)(int)(value / 32767.0f);
        }

        [JsonIgnore]
        public float D
        {
            get => (d / 32767.0f);
            set => d = (short)(int)(value / 32767.0f);
        }

        public (short A, short B, short C, short D) GetPacked()
        {
            return (a, b, c, d);
        }

        public FlverBoneWeights()
        {

        }

        public FlverBoneWeights(short packedA, short packedB, short packedC, short packedD)
        {
            a = packedA;
            b = packedB;
            c = packedC;
            d = packedD;
        }

        public static implicit operator Vector4(FlverBoneWeights v)
        {
            return new Vector4(v.A, v.B, v.C, v.D);
        }

        public static implicit operator FlverBoneWeights(Vector4 v)
        {
            return new FlverBoneWeights()
            {
                A = v.X,
                B = v.Y,
                C = v.Z,
                D = v.W,
            };
        }

        public static FlverBoneWeights operator +(FlverBoneWeights a, FlverBoneWeights b)
        {
            return (Vector4)a + (Vector4)b;
        }

        public static FlverBoneWeights operator -(FlverBoneWeights a, FlverBoneWeights b)
        {
            return (Vector4)a - (Vector4)b;
        }

        public static FlverBoneWeights operator *(FlverBoneWeights a, FlverBoneWeights b)
        {
            return (Vector4)a * (Vector4)b;
        }

        public static FlverBoneWeights operator /(FlverBoneWeights a, FlverBoneWeights b)
        {
            return (Vector4)a / (Vector4)b;
        }

        public static FlverBoneWeights operator +(FlverBoneWeights a, Vector4 b)
        {
            return (Vector4)a + (Vector4)b;
        }

        public static FlverBoneWeights operator -(FlverBoneWeights a, Vector4 b)
        {
            return (Vector4)a - (Vector4)b;
        }

        public static FlverBoneWeights operator *(FlverBoneWeights a, Vector4 b)
        {
            return (Vector4)a * (Vector4)b;
        }

        public static FlverBoneWeights operator /(FlverBoneWeights a, Vector4 b)
        {
            return (Vector4)a / (Vector4)b;
        }

        public static FlverBoneWeights operator *(FlverBoneWeights a, float b)
        {
            return (Vector4)a * b;
        }

        public static FlverBoneWeights operator /(FlverBoneWeights a, float b)
        {
            return (Vector4)a / b;
        }

        public override string ToString()
        {
            return $"[{A}, {B}, {C}, {D}]";
        }
    }
}
