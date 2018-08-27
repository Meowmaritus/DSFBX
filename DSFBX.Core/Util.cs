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

        public static Vector3 GetFlverEulerFromQuaternion_Bone(Quaternion q)
        {
            // Store the Euler angles in radians
            Vector3 pitchYawRoll = new Vector3();

            double sqw = q.W * q.W;
            double sqx = q.X * q.X;
            double sqy = q.Y * q.Y;
            double sqz = q.Z * q.Z;

            // If quaternion is normalised the unit is one, otherwise it is the correction factor
            double unit = sqx + sqy + sqz + sqw;
            double test = q.X * q.Y + q.Z * q.W;

            if (test > 0.4995f * unit)                              // 0.4999f OR 0.5f - EPSILON
            {
                // Singularity at north pole
                pitchYawRoll.Y = 2f * (float)Math.Atan2(q.X, q.W);  // Yaw
                pitchYawRoll.Z = MathHelper.Pi * 0.5f;                         // Pitch
                pitchYawRoll.X = 0f;                                // Roll
                return pitchYawRoll;
            }
            else if (test < -0.4995f * unit)                        // -0.4999f OR -0.5f + EPSILON
            {
                // Singularity at south pole
                pitchYawRoll.Y = -2f * (float)Math.Atan2(q.X, q.W); // Yaw
                pitchYawRoll.Z = -MathHelper.Pi * 0.5f;                        // Pitch
                pitchYawRoll.X = 0f;                                // Roll
                return pitchYawRoll;
            }
            else
            {
                pitchYawRoll.Y = (float)Math.Atan2(2f * q.Y * q.W - 2f * q.X * q.Z, sqx - sqy - sqz + sqw);       // Yaw
                pitchYawRoll.Z = (float)Math.Asin(2f * test / unit);                                             // Pitch
                pitchYawRoll.X = (float)Math.Atan2(2f * q.X * q.W - 2f * q.Y * q.Z, -sqx + sqy - sqz + sqw);      // Roll
            }

            return pitchYawRoll;
        }

        //private static Vector3 WrapEulers(Vector3 v)
        //{
        //    return new Vector3(MathHelper.WrapAngle(v.X), MathHelper.WrapAngle(v.Y), MathHelper.WrapAngle(v.Z));
        //}

        //public static Vector3 GetFlverEulerFromQuaternion(Quaternion quat)
        //{
        //    //This is the code from
        //    //http://www.mawsoft.com/blog/?p=197
        //    var rotation = quat;
        //    double q0 = rotation.W;
        //    double q1 = rotation.Y;
        //    double q2 = rotation.X;
        //    double q3 = rotation.Z;

        //    Vector3 radAngles = new Vector3();
        //    radAngles.Y = (float)Math.Atan2(2 * (q0 * q1 + q2 * q3), 1 - 2 * (Math.Pow(q1, 2) + Math.Pow(q2, 2)));
        //    radAngles.X = (float)Math.Asin(2 * (q0 * q2 - q3 * q1));
        //    radAngles.Z = (float)Math.Atan2(2 * (q0 * q3 + q1 * q2), 1 - 2 * (Math.Pow(q2, 2) + Math.Pow(q3, 2)));

        //    return radAngles;
        //}

    }
}
