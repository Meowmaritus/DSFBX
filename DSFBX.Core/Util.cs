extern alias PIPE;

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FbxPipeline = PIPE::Microsoft.Xna.Framework;

namespace DSFBX
{
    public static class Util
    {
        public static string GetAngleBracketContents(string str)
        {
            var leftBracket = str.IndexOf('<');
            var rightBracket = str.LastIndexOf('>');

            if (leftBracket == -1 || rightBracket == -1)
                return null;

            return str.Substring(
                    leftBracket + 1,
                    Math.Max(rightBracket - leftBracket - 1, 0));
        }

        public static string GetBracketContents(string str)
        {
            var leftBracket = str.IndexOf('[');
            var rightBracket = str.LastIndexOf(']');

            if (leftBracket == -1 || rightBracket == -1)
                return null;

            return str.Substring(
                    leftBracket + 1,
                    Math.Max(rightBracket - leftBracket - 1, 0));
        }

        static readonly char[] numbers = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        public static string GetIncrementedName(string str)
        {
            if (Char.IsDigit(str.Last()))
            {
                int numIndex = str.LastIndexOfAny(numbers);
                int num = int.Parse(str.Substring(numIndex));

                num++;

                return str.Substring(0, numIndex) + num.ToString();
            }
            else
            {
                return str + "__2";
            }
        }

        public static string Frankenpath(params string[] pathParts)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < pathParts.Length; i++)
            {
                sb.Append(pathParts[i].Trim('\\'));
                if (i < pathParts.Length - 1)
                    sb.Append('\\');
            }

            return sb.ToString();
        }

        public static Vector3 GetFlverEulerFromQuaternion(Quaternion quat)
        {
            float xx = quat.X * quat.X;
            float xy = quat.X * quat.Y;
            float xz = quat.X * quat.Z;
            float xw = quat.X * quat.W;
            float yy = quat.Y * quat.Y;
            float yz = quat.Y * quat.Z;
            float yw = quat.Y * quat.W;
            float zz = quat.Z * quat.Z;
            float zw = quat.Z * quat.W;
            float zwxy = zw + xy;
            if ((zwxy - 0.5f) < 1E-05f)
            {
                zwxy = 0.5f;
            }
            else if ((zwxy - (-0.5f)) < 1E-05f)
            {
                zwxy = -0.5f;
            }
            Vector3 result;
            result.Y = (float)Math.Atan2(2f * (yw - xz), 1f - 2f * (yy + zz));
            result.Z = (float)Math.Asin(2f * zwxy);
            result.X = (float)Math.Atan2(2f * (xw - yz), 1f - 2f * (zz + xx));
            if (zwxy == 0.5f)
            {
                result.X = 0f;
                result.Y = 2f * (float)Math.Atan2(quat.Y, quat.W);
            }
            else if (zwxy == -0.5f)
            {
                result.X = 0f;
                result.Y = -2f * (float)Math.Atan2(quat.Y, quat.W);
            }
            return result;
        }

        public static Vector3 GetEuler(FbxPipeline.Matrix m)
        {
            //float rotX = (float)Math.Asin(-m.M32);
            //float rotY = 0;
            //float rotZ = 0;
            //if (Math.Cos(rotX) > 0.0001)
            //{
            //    rotY = (float)Math.Atan2(m.M31, m.M33);
            //    rotZ = (float)Math.Atan2(m.M12, m.M22);
            //}
            //else
            //{
            //    rotY = 0.0f;
            //    rotZ = (float)Math.Atan2(-m.M21, m.M11);
            //}
            //return new Vector3(rotX, rotY, rotZ);





            //double rotX = Math.Atan2(-m.M23, m.M33);
            //double cosY = Math.Sqrt(Math.Pow(m.M13, 2) + Math.Pow(m.M12, 2));
            //double rotY = Math.Atan2(m.M13, cosY);
            //double sinX = Math.Sin(rotX);
            //double cosX = Math.Cos(rotX);
            //double rotZ = Math.Atan2(cosX * m.M21 + sinX * m.M31, cosX * m.M22 + sinX * m.M32);

            //return new Vector3((float)rotY, (float)rotZ, (float)rotX);



            if (m.Decompose(out _, out var quat, out _))
            {
                return GetFlverEulerFromQuaternion(new Quaternion(quat.X, quat.Y, quat.Z, quat.W));
            }
            else
            {
                
                return Vector3.Zero;
            }

        }

    }
}
