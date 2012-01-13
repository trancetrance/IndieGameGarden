﻿// (c) 2010-2012 TranceTrance.com. Distributed under the FreeBSD license in LICENSE.txt

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

using TTengine.Core;
using TTengine.Modifiers;

using IndiegameGarden.Base;
using IndiegameGarden.Util;

namespace IndiegameGarden.Menus
{
    /**
     * a GamesPanel that arranges games in a large rectangular matrix, the "garden". User can travel
     * the garden with a cursor. Only a part of the garden is shown at a time.
     */
    public class GardenGamesPanel: GamesPanel
    {
        // below: UI constants
        public const float LAYER_BACK = 1.0f;
        public const float LAYER_FRONT = 0.0f;
        public const float LAYER_ZOOMING_ITEM = 0.1f;
        public const float LAYER_DODGING_ITEM = 0.3f;
        public const float LAYER_GRID_ITEMS = 0.9f;

        public const float PANEL_ZOOM_REGULAR = 1f; //0.16f;
        public const float PANEL_SCALE_GRID_X = 0.16f;
        public const float PANEL_SCALE_GRID_Y = 0.16f;
        public const float PANEL_SPEED_SHIFT = 2.1f;
        public const float PANEL_SIZE_X = 1.333f;
        public const float PANEL_SIZE_Y = 1.0f;
        public const float PANEL_ZOOM_TARGET_QUITTING = 0.001f;
        public const float PANEL_ZOOM_SPEED_QUITTING = 0.005f;
        public const float PANEL_ZOOM_SPEED_ABORTQUITTING = 0.05f;

        public const float CURSOR_SCALE_REGULAR = 0.95f; //5.9375f;
        public const float THUMBNAIL_SCALE_UNSELECTED = 0.28f; //1.5625f;
        public const float THUMBNAIL_SCALE_SELECTED = 0.35f; //2f;
        public const float THUMBNAIL_SCALE_SELECTED1 = 2.857f;
        static Vector2 INFOBOX_SHOWN_POSITION = new Vector2(0.05f, 0.85f);
        static Vector2 INFOBOX_HIDDEN_POSITION = new Vector2(0.05f, 0.95f);
        const float INFOBOX_SPEED_MOVE = 2.8f;
        const float TIME_BEFORE_GAME_LAUNCH = 0.7f;
        const float TIME_BEFORE_EXIT = 0.9f;

        // maximum sizes of grid
        public double GridMaxX=32, GridMaxY=32;

        // zoom, scale etc. related vars for panel
        public float ZoomTarget = 1.0f;
        public float ZoomSpeed = 0f;

        Dictionary<string, GameThumbnail> thumbnailsCache = new Dictionary<string, GameThumbnail>();
        
        // cursor is the graphics selection thingy         
        GameThumbnailCursor cursor;

        // box showing info of a game such as title and download progressContributionSingleFile
        GameInfoBox infoBox;
        
        // UI related vars - related to whether user indicates to quit program or user cancelled this
        bool isExiting = false;
        bool isGameLaunchOngoing = false;
        float timeExiting = 0f;
        float timeLaunching = 0f;
        Vector2 PanelShiftPos = Vector2.Zero;
        int selectionLevel = 0;
        GameChooserMenu parentMenu;

        public GardenGamesPanel(GameChooserMenu parent)
        {
            parentMenu = parent;
            
            // cursor
            cursor = new GameThumbnailCursor();
            Add(cursor);
            cursor.Scale = CURSOR_SCALE_REGULAR;
            Zoom = PANEL_ZOOM_REGULAR;
            //cursor.Visible = false;

            // info box
            infoBox = new GameInfoBox();
            infoBox.Position = INFOBOX_HIDDEN_POSITION;
            parent.Add(infoBox);

        }

        public override void OnUpdateList(GameCollection gl)
        {
            // first process old list - start fading away of items
            for (int i = 0; i < gl.Count; i++)
            {
                IndieGame g = gl[i];
                if (thumbnailsCache.ContainsKey(g.GameID))
                {
                    GameThumbnail th = thumbnailsCache[g.GameID];
                    th.FadeToTarget(0f,4f);
                }
            }
            this.gl = gl;

            // update selection
            if (gl.Count > 0)
            {
                if (SelectedGame == null)
                {
                    SelectedGame = gl[0];
                    cursor.SetToGame(SelectedGame);
                }
                else
                {
                    if (!gl.Contains(SelectedGame))
                    {
                        SelectedGame = gl[0];
                        cursor.SetToGame(SelectedGame);
                    }
                    else
                    {
                        // gl contains the previously selected game. Relocate it in new list.
                        cursor.SetToGame(SelectedGame);
                    }
                }
            }
        }

        // shorthand method to select the game currently indicated by cursor
        protected void SelectGameBelowCursor()
        {
            IndieGame g = gl.FindGameAt(cursor.GridPosition);
            SelectedGame = g;                
        }

        protected override void OnUpdate(ref UpdateParams p)
        {
            GameThumbnail th = null;

            base.OnUpdate(ref p);

            // update text box with currently selected game info
            infoBox.SetGameInfo(SelectedGame);

            // handle download/install/launching of a game
            if (isGameLaunchOngoing)
            {
                timeLaunching += p.dt;
                /*
                ZoomTarget = THUMBNAIL_SCALE_SELECTED1 * (1+timeLaunching);
                ZoomCenter = thumbnailsCache[SelectedGame.GameID].PositionAbs;
                ZoomSpeed = 0.01f;
                 */
                th = thumbnailsCache[SelectedGame.GameID];
                th.ScaleModifier *= (1 + timeLaunching); // blow up size of thumbnail while user requests launch

                if (timeLaunching > TIME_BEFORE_GAME_LAUNCH)
                {
                    if (SelectedGame.IsInstalled)
                    {
                        parentMenu.ActionLaunchGame(SelectedGame);
                    }
                    else
                    {
                        parentMenu.ActionDownloadAndInstallGame(SelectedGame);
                    }
                    isGameLaunchOngoing = false;
                }
            }

            // handle exit key
            if (isExiting)
            {
                timeExiting += p.dt;
                if (timeExiting > TIME_BEFORE_EXIT)
                {
                    GardenGame.Instance.ExitGame();
                    isExiting = false;
                    return;
                }
            }
            else
            {
                timeExiting = 0f;
            }

            // handle dynamic zooming
            if (Zoom < ZoomTarget && ZoomSpeed > 0f)
            {
                Zoom *= (1.0f + ZoomSpeed);
                if (Zoom > ZoomTarget)
                    Zoom = ZoomTarget;
            }
            else if (Zoom > ZoomTarget && ZoomSpeed > 0f)
            {
                Zoom /= (1.0f + ZoomSpeed);
                if (Zoom < ZoomTarget)
                    Zoom = ZoomTarget;
            }

            //-- loop all games adapt their display properties where needed
            if (gl == null)
                return;
            IndieGame g;
            for (int i = 0; i < gl.Count; i++)
            {
                // fetch that game from list
                g = gl[i];

                // if GameThumbnail for current game does not exist yet, create it                
                if (!thumbnailsCache.ContainsKey(g.GameID))
                {
                    // create now
                    th = new GameThumbnail(g);
                    Add(th);
                    thumbnailsCache.Add(g.GameID, th);
                    //th.Position = new Vector2(RandomMath.RandomBetween(-0.4f,2.0f), RandomMath.RandomBetween(-0.4f,1.4f) );
                    //th.Scale = RandomMath.RandomBetween(0.01f, 0.09f); 
                    th.Position = new Vector2(0.5f, 0.5f);
                    th.Scale = 0.01f;

                    th.LayerDepth = LAYER_GRID_ITEMS;
                    th.Visible = false;
                    th.Intensity = 0.0f;
                    th.Alpha = 0f;
                }else{
                    // retrieve GameThumbnail from cache
                    th = thumbnailsCache[g.GameID];
                }
                
                // check if thnail visible and in range. If so, start displaying it (fade in)
                if (!th.Visible && cursor.GameletInRange(th))
                {
                    th.Visible = true;
                    th.Intensity = 0f;
                    th.FadeToTarget(1.0f, 4.3f);
                }

                // displaying selected thumbnails larger
                if (g == SelectedGame)
                {
                    th.ScaleTarget = THUMBNAIL_SCALE_SELECTED;
                    th.ScaleSpeed = 0.01f;
                }
                else
                {
                    th.ScaleTarget = THUMBNAIL_SCALE_UNSELECTED;
                    th.ScaleSpeed = 0.02f;
                }

                // coordinate position where to move a game thumbnail to 
                Vector2 targetPos = (g.Position - PanelShiftPos) * new Vector2(PANEL_SCALE_GRID_X,PANEL_SCALE_GRID_Y);
                th.Target = targetPos;
                th.TargetSpeed = 4f;

                // cursor where to move to
                cursor.Target = (cursor.GridPosition - PanelShiftPos) * new Vector2(PANEL_SCALE_GRID_X,PANEL_SCALE_GRID_Y);

                // panel shift effect when cursor hits edges of panel
                Vector2 cp = cursor.PositionAbs;
                float chw = cursor.WidthAbs / 2.0f; // cursor-half-width
                float chh = cursor.HeightAbs / 2.0f; // cursor-half-height
                float dx = PANEL_SPEED_SHIFT * p.dt;
                if (cp.X <= chw)
                {
                    PanelShiftPos.X -= dx;
                }
                else if (cp.X >= PANEL_SIZE_X - chw)
                {
                    PanelShiftPos.X += dx;
                }
                if (cp.Y <= chh)
                {
                    PanelShiftPos.Y -= dx;
                }
                else if (cp.Y >= PANEL_SIZE_Y - chh)
                {
                    PanelShiftPos.Y += dx;
                }

            }
        }

        protected override void OnDraw(ref DrawParams p)
        {
            base.OnDraw(ref p);

            // DEBUG
            if (SelectedGame != null)
                Screen.DebugText(0f, 0f, "Selected: " + gl.IndexOf(SelectedGame) + " " + SelectedGame.GameID );
            Screen.DebugText(0f, 0.1f, "Zoom: " + Zoom);
        }

        public override void OnChangedSelectedGame(IndieGame newSel, IndieGame oldSel)
        {
            // unselect the previous game
            if (oldSel != null)
            {
                GameThumbnail th = thumbnailsCache[oldSel.GameID];
                if (th != null)
                {
                    th.ScaleTarget = THUMBNAIL_SCALE_UNSELECTED;
                    th.ScaleSpeed = 0.01f;
                }
            }
        }

        public override void OnUserInput(GamesPanel.UserInput inp)
        {
            switch (inp)
            {
                case UserInput.DOWN:
                    if (cursor.GridPosition.Y < GridMaxY -1 )
                    {
                        cursor.GridPosition.Y += 1f;
                        SelectGameBelowCursor();
                    }
                    break;
               
                case UserInput.UP:
                    if (cursor.GridPosition.Y > 0)
                    {
                        cursor.GridPosition.Y -= 1f;
                        SelectGameBelowCursor();
                    }
                    break;
                
                case UserInput.LEFT:
                    if (cursor.GridPosition.X > 0)
                    {
                        cursor.GridPosition.X -= 1f;
                        SelectGameBelowCursor();
                    }
                    break;
                
                case UserInput.RIGHT:
                    if (cursor.GridPosition.X < GridMaxX - 1)
                    {
                        cursor.GridPosition.X += 1f;
                        SelectGameBelowCursor();
                    }
                    break;
                
                case UserInput.START_EXIT:
                    isExiting = true;
                    selectionLevel = 0;
                    ZoomTarget = PANEL_ZOOM_TARGET_QUITTING ;
                    ZoomSpeed = PANEL_ZOOM_SPEED_QUITTING ;
                    break;
                
                case UserInput.STOP_EXIT:
                    isExiting = false;
                    selectionLevel = 0;
                    ZoomTarget = PANEL_ZOOM_REGULAR;
                    ZoomSpeed = PANEL_ZOOM_SPEED_ABORTQUITTING ;
                    break;

                case UserInput.START_SELECT:
                    if (SelectedGame != null)
                    {                        
                        GameThumbnail th = thumbnailsCache[SelectedGame.GameID];
                        if (th != null)
                        {
                            switch (selectionLevel)
                            {
                                case 0:
                                    // select once - zoom in on selected game
                                    ZoomTarget = THUMBNAIL_SCALE_SELECTED1;
                                    ZoomCenter = th.PositionAbs;
                                    ZoomSpeed = 0.05f;
                                    infoBox.Target = INFOBOX_SHOWN_POSITION;
                                    infoBox.TargetSpeed = INFOBOX_SPEED_MOVE;
                                    selectionLevel++;
                                    break;
                                case 1:
                                    // select again - install or launch game if selection key pressed long enough.
                                    isGameLaunchOngoing = true;
                                    break;
                            }


                        }
                    }
                    break;

                case UserInput.STOP_SELECT:
                    isGameLaunchOngoing = false;
                    timeLaunching = 0f;
                    break;

            } // switch(inp)

            if (selectionLevel == 0)
            {
                infoBox.Target = INFOBOX_HIDDEN_POSITION;
                infoBox.TargetSpeed = INFOBOX_SPEED_MOVE;
            }

        }
    }
}
