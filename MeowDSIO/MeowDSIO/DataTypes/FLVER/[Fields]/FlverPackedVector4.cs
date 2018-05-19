using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverPackedVector4
    {
        private sbyte x;
        private sbyte y;
        private sbyte z;
        private sbyte w;

        public float X
        {
            get => (x / 127.0f);
            set => x = (sbyte)(int)(value * 127.0f);
        }

        public float Y
        {
            get => (y / 127.0f);
            set => y = (sbyte)(int)(value * 127.0f);
        }

        public float Z
        {
            get => (z / 127.0f);
            set => z = (sbyte)(int)(value * 127.0f);
        }

        public float W
        {
            get => (w / 127.0f);
            set => w = (sbyte)(int)(value * 127.0f);
        }

        public (sbyte X, sbyte Y, sbyte Z, sbyte W) GetPacked()
        {
            return (x, y, z, w);
        }

        public FlverPackedVector4()
        {

        }

        public FlverPackedVector4(sbyte packedX, sbyte packedY, sbyte packedZ, sbyte packedW)
        {
            x = packedX;
            y = packedY;
            z = packedZ;
            w = packedW;
        }

        public static implicit operator Vector4(FlverPackedVector4 v)
        {
            return new Vector4(v.X, v.Y, v.Z, v.W);
        }

        public static implicit operator FlverPackedVector4(Vector4 v)
        {
            return new FlverPackedVector4()
            {
                X = v.X,
                Y = v.Y,
                Z = v.Z,
                W = v.W,
            };
        }

        public static implicit operator FlverPackedVector4(Vector3 v)
        {
            return new FlverPackedVector4()
            {
                X = v.X,
                Y = v.Y,
                Z = v.Z,
                W = 0,
            };
        }

        public static explicit operator Vector3(FlverPackedVector4 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static FlverPackedVector4 operator +(FlverPackedVector4 a, FlverPackedVector4 b)
        {
            return (Vector4)a + (Vector4)b;
        }

        public static FlverPackedVector4 operator -(FlverPackedVector4 a, FlverPackedVector4 b)
        {
            return (Vector4)a - (Vector4)b;
        }

        public static FlverPackedVector4 operator *(FlverPackedVector4 a, FlverPackedVector4 b)
        {
            return (Vector4)a * (Vector4)b;
        }

        public static FlverPackedVector4 operator /(FlverPackedVector4 a, FlverPackedVector4 b)
        {
            return (Vector4)a / (Vector4)b;
        }

        public static FlverPackedVector4 operator +(FlverPackedVector4 a, Vector4 b)
        {
            return (Vector4)a + (Vector4)b;
        }

        public static FlverPackedVector4 operator -(FlverPackedVector4 a, Vector4 b)
        {
            return (Vector4)a - (Vector4)b;
        }

        public static FlverPackedVector4 operator *(FlverPackedVector4 a, Vector4 b)
        {
            return (Vector4)a * (Vector4)b;
        }

        public static FlverPackedVector4 operator /(FlverPackedVector4 a, Vector4 b)
        {
            return (Vector4)a / (Vector4)b;
        }

        public static FlverPackedVector4 operator *(FlverPackedVector4 a, float b)
        {
            return (Vector4)a * b;
        }

        public static FlverPackedVector4 operator /(FlverPackedVector4 a, float b)
        {
            return (Vector4)a / b;
        }

        public override string ToString()
        {
            return $"<{X}, {Y}, {Z}, {W}>";
        }
    }
}
