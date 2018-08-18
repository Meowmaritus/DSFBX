using Microsoft.Win32;
using Microsoft.Xna.Framework;
using System;

namespace DSFBX.ModelViewer
{
    public class Program
    {
        public Program()
        {
        }

        [STAThread]
        public static void Main(string[] args)
        {
            //MEOW DEBUG//
            //args = new string[] { @"G:\SteamLibrary\steamapps\common\Dark Souls Prepare to Die Edition\DATA\chr\c2430.chrbnd" };
            /////////////

            using (MyGame game = new MyGame())
            {
                game.inputFiles = args;
                game.Run(GameRunBehavior.Synchronous);
            }
        }
    }
}