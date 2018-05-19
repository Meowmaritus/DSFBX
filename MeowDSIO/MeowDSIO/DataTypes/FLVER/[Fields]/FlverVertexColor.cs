using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverVertexColor
    {
        public byte R { get; set; } = 1;
        public byte G { get; set; } = 1;
        public byte B { get; set; } = 1;
        public byte A { get; set; } = 1;

        public static implicit operator Color(FlverVertexColor v)
        {
            return new Color(v.R, v.G, v.B, v.A);
        }

        public static implicit operator FlverVertexColor(Color v)
        {
            return new FlverVertexColor()
            {
                R = v.R,
                G = v.G,
                B = v.B,
                A = v.A,
            };
        }

        public static explicit operator Vector3(FlverVertexColor v)
        {
            return new Vector3(v.R / 255.0f, v.G / 255.0f, v.B / 255.0f);
        }

        public static implicit operator Vector4(FlverVertexColor v)
        {
            return new Vector4(v.R / 255.0f, v.G / 255.0f, v.B / 255.0f, v.A / 255.0f);
        }

        public static implicit operator FlverVertexColor(Vector3 v)
        {
            return new FlverVertexColor()
            {
                R = (byte)(int)(v.X * 255.0f),
                G = (byte)(int)(v.Y * 255.0f),
                B = (byte)(int)(v.Z * 255.0f),
                A = 255,
            };
        }

        public static implicit operator FlverVertexColor(Vector4 v)
        {
            return new FlverVertexColor()
            {
                R = (byte)(int)(v.X * 255.0f),
                G = (byte)(int)(v.Y * 255.0f),
                B = (byte)(int)(v.Z * 255.0f),
                A = (byte)(int)(v.W * 255.0f),
            };
        }

        public static FlverVertexColor operator +(FlverVertexColor a, FlverVertexColor b)
        {
            return (Vector4)a + (Vector4)b;
        }

        public static FlverVertexColor operator -(FlverVertexColor a, FlverVertexColor b)
        {
            return (Vector4)a - (Vector4)b;
        }

        public static FlverVertexColor operator *(FlverVertexColor a, FlverVertexColor b)
        {
            return (Vector4)a * (Vector4)b;
        }

        public static FlverVertexColor operator /(FlverVertexColor a, FlverVertexColor b)
        {
            return (Vector4)a / (Vector4)b;
        }

        public static FlverVertexColor operator +(FlverVertexColor a, Vector4 b)
        {
            return (Vector4)a + (Vector4)b;
        }

        public static FlverVertexColor operator -(FlverVertexColor a, Vector4 b)
        {
            return (Vector4)a - (Vector4)b;
        }

        public static FlverVertexColor operator *(FlverVertexColor a, Vector4 b)
        {
            return (Vector4)a * (Vector4)b;
        }

        public static FlverVertexColor operator /(FlverVertexColor a, Vector4 b)
        {
            return (Vector4)a / (Vector4)b;
        }

        public static FlverVertexColor operator *(FlverVertexColor a, float b)
        {
            return (Vector4)a * b;
        }

        public static FlverVertexColor operator /(FlverVertexColor a, float b)
        {
            return (Vector4)a / b;
        }

    }
}
