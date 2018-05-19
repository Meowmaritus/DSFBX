using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.Xna.Framework;
using System.Text;
using System.Threading.Tasks;

namespace MeowDSIO
{
    public static class DSBinaryExtensions
    {
        public static Vector4 ReadPackedTangent(this DSBinaryReader bin)
        {
            byte x = bin.ReadByte();
            byte y = bin.ReadByte();
            byte z = bin.ReadByte();
            byte w = bin.ReadByte();
            return new Vector4((x - 127.0f) / 127.0f, (y - 127.0f) / 127.0f, (z - 127.0f) / 127.0f, (w - 127.0f) / 127.0f);
        }

        public static void WritePackedTangent(this DSBinaryWriter bin, Vector4 v)
        {
            bin.Write((byte)((v.X * 127.0f) + 127f));
            bin.Write((byte)((v.Y * 127.0f) + 127f));
            bin.Write((byte)((v.Z * 127.0f) + 127f));
            bin.Write((byte)((v.W * 127.0f) + 127f));
        }

        public static Vector2 ReadUV(this DSBinaryReader bin)
        {
            short x = bin.ReadInt16();
            short y = bin.ReadInt16();
            return new Vector2(x / 1024.0f, y / 1024.0f);
        }

        public static void WriteUV(this DSBinaryWriter bin, Vector2 v)
        {
            bin.Write((short)(v.X * 1024.0f));
            bin.Write((short)(v.Y * 1024.0f));
        }


        public static Color ReadColor(this DSBinaryReader bin)
        {
            byte r = bin.ReadByte();
            byte g = bin.ReadByte();
            byte b = bin.ReadByte();
            byte a = bin.ReadByte();
            return new Color(r, g, b, a);
        }

        public static void WriteColor(this DSBinaryWriter bin, Color v)
        {
            bin.Write(new byte[] { v.R, v.G, v.B, v.A });
        }



        public static Vector3 ReadVector3(this DSBinaryReader bin)
        {
            float x = bin.ReadSingle();
            float y = bin.ReadSingle();
            float z = bin.ReadSingle();
            return new Vector3(x, y, z);
        }

        public static void WriteVector3(this DSBinaryWriter bin, Vector3 v)
        {
            bin.Write(v.X);
            bin.Write(v.Y);
            bin.Write(v.Z);
        }
    }
}
