// (c) 2010-2012 TranceTrance.com. Distributed under the FreeBSD license in LICENSE.txt

using System;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;

namespace IndiegameGarden
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (GardenGame game = new GardenGame())
            {
                Form frm = (Form)Form.FromHandle(game.Window.Handle);
                frm.FormBorderStyle = FormBorderStyle.None; 
                game.Run();
            }
        }
    }
#endif
}

