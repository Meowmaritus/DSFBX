using Microsoft.Xna.Framework;
using System;

namespace DSFBX.ModelViewer
{
    public static class Utils
    {
        private static double GetColorComponent(double temp1, double temp2, double temp3)
        {
            double num;
            temp3 = Utils.MoveIntoRange(temp3);
            if (temp3 < 0.166666666666667)
            {
                num = temp1 + (temp2 - temp1) * 6 * temp3;
            }
            else if (temp3 >= 0.5)
            {
                num = (temp3 >= 0.666666666666667 ? temp1 : temp1 + (temp2 - temp1) * (0.666666666666667 - temp3) * 6);
            }
            else
            {
                num = temp2;
            }
            return num;
        }

        private static double GetTemp2(float H, float S, float L)
        {
            double temp2;
            temp2 = ((double)L >= 0.5 ? (double)(L + S - L * S) : (double)L * (1 + (double)S));
            return temp2;
        }

        public static Color HSLtoRGB(float H, float S, float L)
        {
            double r = 0;
            double g = 0;
            double b = 0;
            if (L != 0f)
            {
                if (S != 0f)
                {
                    double temp2 = Utils.GetTemp2(H, S, L);
                    double temp1 = 2 * (double)L - temp2;
                    r = Utils.GetColorComponent(temp1, temp2, (double)H + 0.333333333333333);
                    g = Utils.GetColorComponent(temp1, temp2, (double)H);
                    b = Utils.GetColorComponent(temp1, temp2, (double)H - 0.333333333333333);
                }
                else
                {
                    double l = (double)L;
                    b = l;
                    g = l;
                    r = l;
                }
            }
            Color color = Color.FromNonPremultiplied(new Vector4((float)r, (float)g, (float)b, 1f));
            return color;
        }

        private static double MoveIntoRange(double temp3)
        {
            if (temp3 < 0)
            {
                temp3 += 1;
            }
            else if (temp3 > 1)
            {
                temp3 -= 1;
            }
            return temp3;
        }
    }
}