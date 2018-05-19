using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO.DataTypes.FLVER
{
    public class FlverVector3
    {
        public float X { get; set; } = 0;
        public float Y { get; set; } = 0;
        public float Z { get; set; } = 0;

        public FlverVector3()
        {

        }

        public FlverVector3(float X, float Y, float Z)
        {
            this.X = X;
            this.Y = Y;
            this.Z = Z;
        }

        public static implicit operator Vector3(FlverVector3 v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }

        public static implicit operator FlverVector3(Vector3 v)
        {
            return new FlverVector3(v.X, v.Y, v.Z);
        }

        public static FlverVector3 operator +(FlverVector3 a, FlverVector3 b)
        {
            return (Vector3)a + (Vector3)b;
        }

        public static FlverVector3 operator -(FlverVector3 a, FlverVector3 b)
        {
            return (Vector3)a - (Vector3)b;
        }

        public static FlverVector3 operator *(FlverVector3 a, FlverVector3 b)
        {
            return (Vector3)a * (Vector3)b;
        }

        public static FlverVector3 operator /(FlverVector3 a, FlverVector3 b)
        {
            return (Vector3)a / (Vector3)b;
        }

        public static FlverVector3 operator +(FlverVector3 a, Vector3 b)
        {
            return (Vector3)a + (Vector3)b;
        }

        public static FlverVector3 operator -(FlverVector3 a, Vector3 b)
        {
            return (Vector3)a - (Vector3)b;
        }

        public static FlverVector3 operator *(FlverVector3 a, Vector3 b)
        {
            return (Vector3)a * (Vector3)b;
        }

        public static FlverVector3 operator /(FlverVector3 a, Vector3 b)
        {
            return (Vector3)a / (Vector3)b;
        }

        public static FlverVector3 operator *(FlverVector3 a, float b)
        {
            return (Vector3)a * b;
        }

        public static FlverVector3 operator /(FlverVector3 a, float b)
        {
            return (Vector3)a / b;
        }


    }
}
