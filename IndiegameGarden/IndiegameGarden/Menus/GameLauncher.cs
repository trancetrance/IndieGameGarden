﻿// (c) 2010-2012 TranceTrance.com. Distributed under the FreeBSD license in LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices; //required for SetForegroundWindow

using IndiegameGarden.Download;
using IndiegameGarden.Base;

namespace IndiegameGarden.Menus
{
    /// <summary>
    /// launches a game .exe in a separate process and keeps track of when it has finished
    /// </summary>
    public class GameLauncher: Task
    {
        public Process Proc = null;
        public IndieGame Game;

        string filePath;
        string cdPath;

        //Import the SetForeground API to activate it
        [DllImportAttribute("User32.dll")]
        private static extern IntPtr SetForegroundWindow(int hWnd);

        public GameLauncher(IndieGame g)
        {
            string cwd = System.IO.Directory.GetCurrentDirectory();
            cdPath = cwd + "\\" + GardenGame.Instance.Config.GetGameFolder(g.GameID, g.Version) + "\\" + g.CdPath;
            filePath = g.ExeFile;
        }

        public override void Start()
        {
            status = ITaskStatus.RUNNING;
            try
            {
                string cwd = System.IO.Directory.GetCurrentDirectory();
                System.IO.Directory.SetCurrentDirectory(cdPath);
                Proc = System.Diagnostics.Process.Start(filePath);
                SetForegroundWindow(Proc.MainWindowHandle.ToInt32());
                Proc.Exited += new EventHandler(EvHandlerProcessExited);
                Proc.EnableRaisingEvents = true;
                System.IO.Directory.SetCurrentDirectory(cwd);
            }
            catch (System.ComponentModel.Win32Exception)
            {
                status = ITaskStatus.FAIL;
            }
            catch (System.ObjectDisposedException)
            {             
                status = ITaskStatus.FAIL;
            }
            catch (System.IO.FileNotFoundException)
            {             
                status = ITaskStatus.FAIL;
            }

        }

        public override double Progress()
        {
            if (IsFinished())
                return 1.0;
            return 0.0;
        }

        private void EvHandlerProcessExited(object sender, System.EventArgs e)
        {
            status = ITaskStatus.SUCCESS;

            // switch back to our Garden app
            Process p = Process.GetCurrentProcess();
            if (p != null)
                SetForegroundWindow(p.MainWindowHandle.ToInt32());            
        }

    }
}
