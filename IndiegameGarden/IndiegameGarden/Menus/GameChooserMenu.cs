﻿// (c) 2010-2012 TranceTrance.com. Distributed under the FreeBSD license in LICENSE.txt

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TTengine.Core;
using TTengine.Modifiers;

using IndiegameGarden.Base;
using IndiegameGarden.Install;

namespace IndiegameGarden.Menus
{
    /// <summary>
    /// main menu to choose a game; uses a GamePanel to delegate thumbnail rendering and navigation to
    /// </summary>
    public class GameChooserMenu: Gamelet
    {
        const double MIN_MENU_CHANGE_DELAY = 0.2f; 
        
        GameCollection gamesList;

        float lastKeypressTime = 0;
        double timeEscapeIsPressed = 0;
        // used to launch/start a game and track its state
        GameLauncherTask launcher;
        // the game thumbnails or items selection panel
        GamesPanel panel;        
        ThreadedTask launchGameThread;

        /// <summary>
        /// construct new menu
        /// </summary>
        public GameChooserMenu(): base(new StateChooserMenu())
        {
            SetNextState(new StateChooserMenu());
            panel = new GardenGamesPanel(this);
            panel.Position = new Vector2(0.0f, 0.0f);

            // get the items to display
            gamesList = GardenGame.Instance.GameLib.GetList();

            // set my panel and games list
            Add(panel);
            panel.OnUpdateList(gamesList);

            // background
            Spritelet bg = new Spritelet("flower");
            bg.Position = new Vector2(0.66667f, 0.5f);
            bg.DrawColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            bg.Add(new MyFuncyModifier( delegate(float v) { return v/25.0f; }, "Rotate"));
            Add(bg);

        }

        protected override void OnDraw(ref DrawParams p)
        {
            base.OnDraw(ref p);
        }

        /// <summary>
        /// handles all keyboard input into the menu, transforming that into events sent to GUI components
        /// </summary>
        /// <param name="p">UpdateParams from TTEngine OnUpdate()</param>
        protected void KeyboardControls(ref UpdateParams p)
        {
            KeyboardState st = Keyboard.GetState();

            // time bookkeeping
            float timeSinceLastKeypress = p.simTime - lastKeypressTime;

            // check esc key
            if (st.IsKeyDown(Keys.Escape))
            {
                if (timeEscapeIsPressed == 0f)
                {
                    panel.OnUserInput(GamesPanel.UserInput.QUITTING);
                }
                timeEscapeIsPressed += p.dt;
            }
            else if (timeEscapeIsPressed > 0f)
            {
                // if ESC was pressed then released
                timeEscapeIsPressed = 0f;
                panel.OnUserInput(GamesPanel.UserInput.ABORT_QUITTING);
            }

            // check - only proceed if a key pressed and some minimal delay has passed...            
            if (timeSinceLastKeypress < MIN_MENU_CHANGE_DELAY)
                return ;
            // if no keys pressed, skip
            if (st.GetPressedKeys().Length == 0)
                return;
            
            // -- a key is pressed - check all keys and generate action(s)
            if (st.IsKeyDown(Keys.Left)) {
                panel.OnUserInput(GamesPanel.UserInput.LEFT);                
            }
            else if (st.IsKeyDown(Keys.Right)) {
                panel.OnUserInput(GamesPanel.UserInput.RIGHT);
            }

            else if (st.IsKeyDown(Keys.Up)) {
                panel.OnUserInput(GamesPanel.UserInput.UP);
            }

            else if (st.IsKeyDown(Keys.Down)){
                panel.OnUserInput(GamesPanel.UserInput.DOWN);
            }

            else if (st.IsKeyDown(Keys.Enter))
            {
                panel.OnUserInput(GamesPanel.UserInput.SELECT);
            }

            // (time) bookkeeping for next keypress
            lastKeypressTime = p.simTime;
        }
       
        /// <summary>
        /// called by a child GUI component to install a game
        /// </summary>
        /// <param name="g">game to install</param>
        public void DownloadAndInstallGame(IndieGame g)
        {
            // check if download+install task needs to start or not
            if (g.DlAndInstallTask==null && g.ThreadedDlAndInstallTask==null && !g.IsInstalled)
            {
                g.DlAndInstallTask = new GameDownloadAndInstallTask(g);
                g.ThreadedDlAndInstallTask = new ThreadedTask(g.DlAndInstallTask);
                g.ThreadedDlAndInstallTask.Start();
            }
        }

        /// <summary>
        /// called by a child GUI component to launch a game
        /// </summary>
        /// <param name="g">game to launch</param>
        public void LaunchGame(IndieGame g)
        {
            if (g.IsInstalled)
            {
                // if installed, then launch it if possible
                if ( (launcher == null || launcher.IsFinished() == true) &&
                     (launchGameThread == null || launchGameThread.IsFinished()) )
                {
                    SetNextState(new StatePlayingGame());

                    launcher = new GameLauncherTask(g);
                    launchGameThread = new ThreadedTask(launcher);
                    launchGameThread.TaskSuccessEvent += new TaskEventHandler(taskThread_TaskFinishedEvent);
                    launchGameThread.TaskFailEvent += new TaskEventHandler(taskThread_TaskFinishedEvent);
                    launchGameThread.Start();
                }
            }
        }


        // when a launched process concludes
        void taskThread_TaskFinishedEvent(object sender)
        {
            SetNextState(new StateChooserMenu() );
        }

        protected override void OnUpdate(ref UpdateParams p)
        {
            base.OnUpdate(ref p);

            // check keyboard inputs from user
            KeyboardControls(ref p);

            // TODO
            if (!Visible)
                GardenGame.Instance.SuppressDraw();
        }

    }
}
