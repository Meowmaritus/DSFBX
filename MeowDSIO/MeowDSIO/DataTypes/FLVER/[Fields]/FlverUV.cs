using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverUV
    {
        private ushort u;
        private ushort v;

        const float UNIT = 1024;

        public float U
        {
            get => u / UNIT;
            set
            {
                var newVal = value * UNIT;

                if (newVal > ushort.MaxValue)
                    newVal = ushort.MaxValue;
                else if (newVal < ushort.MinValue)
                    newVal = ushort.MinValue;

                u = (ushort)(int)(newVal);
            }
        }

        public float V
        {
            get => v / UNIT;
            set
            {
                var newVal = value * UNIT;

                if (newVal > ushort.MaxValue)
                    newVal = ushort.MaxValue;
                else if (newVal < ushort.MinValue)
                    newVal = ushort.MinValue;

                v = (ushort)(int)(newVal);
            }
        }

        public (ushort U, ushort V) GetPacked()
        {
            return (u, v);
        }

        public FlverUV()
        {

        }

        public FlverUV(ushort packedU, ushort packedV)
        {
            u = packedU;
            v = packedV;
        }

        public override string ToString()
        {
            return $"<{U}, {V}>";
        }

        public static implicit operator Vector2(FlverUV v)
        {
            return new Vector2(v.U, v.V);
        }

        public static implicit operator FlverUV(Vector2 v)
        {
            return new FlverUV()
            {
                U = v.X,
                V = v.Y,
            };
        }

        public static FlverUV operator +(FlverUV a, FlverUV b)
        {
            return (Vector2)a + (Vector2)b;
        }

        public static FlverUV operator -(FlverUV a, FlverUV b)
        {
            return (Vector2)a - (Vector2)b;
        }

        public static FlverUV operator *(FlverUV a, FlverUV b)
        {
            return (Vector2)a * (Vector2)b;
        }

        public static FlverUV operator /(FlverUV a, FlverUV b)
        {
            return (Vector2)a / (Vector2)b;
        }

        public static FlverUV operator +(FlverUV a, Vector2 b)
        {
            return (Vector2)a + (Vector2)b;
        }

        public static FlverUV operator -(FlverUV a, Vector2 b)
        {
            return (Vector2)a - (Vector2)b;
        }

        public static FlverUV operator *(FlverUV a, Vector2 b)
        {
            return (Vector2)a * (Vector2)b;
        }

        public static FlverUV operator /(FlverUV a, Vector2 b)
        {
            return (Vector2)a / (Vector2)b;
        }

        public static FlverUV operator *(FlverUV a, float b)
        {
            return (Vector2)a * b;
        }

        public static FlverUV operator /(FlverUV a, float b)
        {
            return (Vector2)a / b;
        }


    }
}
