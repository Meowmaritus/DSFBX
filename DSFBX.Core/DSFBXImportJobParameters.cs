extern alias PIPE;

using MeowDSIO.DataFiles;
using PIPE::Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System;

namespace DSFBX
{
    public class DSFBXImportJobParameters
    {
        public NodeContent Fbx = null;
        public FLVER Flver = null;
        public string ImportSkeletonPath = null;
        public string EntityBndPath = null;
        public float ScalePercent = 100.0f;


        public Action<string> Print = text => Console.WriteLine(text);
        public Action<string> PrintWarning = text =>
        {
            var prevFg = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(text);
            Console.ForegroundColor = prevFg;
        };
        public Action<string> PrintError = text =>
        {
            var prevFg = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ForegroundColor = prevFg;
        };
    }
}
