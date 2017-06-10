using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Color = System.Drawing.Color;

using EloBuddy;
using TW.Common;
using TW.Common.Extensions;
using SharpDX;

namespace ezEvade
{
    internal class SpellDrawer
    {
        public static Menu menu;

        private static AIHeroClient myHero { get { return ObjectManager.Player; } }


        public SpellDrawer(Menu mainMenu)
        {
            Drawing.OnDraw += Drawing_OnDraw;

            menu = mainMenu;
            Game_OnGameLoad();
        }

        private void Game_OnGameLoad()
        {
            //Console.WriteLine("SpellDrawer loaded");

            Menu drawMenu = new Menu("\u986f\u793a", "Draw");
            drawMenu.AddItem(new MenuItem("DrawSkillShots", "\u986f\u793a \u6280\u80fd\u5c04\u51fa").SetValue(true));
            drawMenu.AddItem(new MenuItem("ShowStatus", "\u986f\u793a\u8eb2\u907f \u72c0\u614b").SetValue(true));
            drawMenu.AddItem(new MenuItem("DrawSpellPos", "\u986f\u793a\u6280\u80fd \u4f4d\u7f6e").SetValue(false));
            drawMenu.AddItem(new MenuItem("DrawEvadePosition", "\u986f\u793a\u8eb2\u907f \u4f4d\u7f6e").SetValue(false));

            Menu dangerMenu = new Menu("\u5371\u96aa\u7b49\u7d1a \u986f\u793a", "DangerLevelDrawings");
            Menu lowDangerMenu = new Menu("\u4f4e", "LowDrawing");
            lowDangerMenu.AddItem(new MenuItem("\u4f4eWidth", "\u7dda \u5bec\u5ea6").SetValue(new Slider(3, 1, 15)));
            lowDangerMenu.AddItem(new MenuItem("\u4f4eColor", "\u984f\u8272").SetValue(new Circle(true, Color.FromArgb(60, 255, 255, 255))));

            Menu normalDangerMenu = new Menu("\u6b63\u5e38", "NormalDrawing");
            normalDangerMenu.AddItem(new MenuItem("\u6b63\u5e38Width", "\u7dda \u5bec\u5ea6").SetValue(new Slider(3, 1, 15)));
            normalDangerMenu.AddItem(new MenuItem("\u6b63\u5e38Color", "\u984f\u8272").SetValue(new Circle(true, Color.FromArgb(140, 255, 255, 255))));

            Menu highDangerMenu = new Menu("\u9ad8", "HighDrawing");
            highDangerMenu.AddItem(new MenuItem("\u9ad8Width", "\u7dda \u5bec\u5ea6").SetValue(new Slider(3, 1, 15)));
            highDangerMenu.AddItem(new MenuItem("\u9ad8Color", "\u984f\u8272").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));

            Menu extremeDangerMenu = new Menu("\u975e\u5e38\u9ad8", "ExtremeDrawing");
            extremeDangerMenu.AddItem(new MenuItem("\u975e\u5e38\u9ad8Width", "\u7dda \u5bec\u5ea6").SetValue(new Slider(4, 1, 15)));
            extremeDangerMenu.AddItem(new MenuItem("\u975e\u5e38\u9ad8Color", "\u984f\u8272").SetValue(new Circle(true, Color.FromArgb(255, 255, 255, 255))));

            /*
            Menu undodgeableDangerMenu = new Menu("Undodgeable", "Undodgeable");
            undodgeableDangerMenu.AddItem(new MenuItem("Width", "Line Width").SetValue(new Slider(6, 1, 15)));
            undodgeableDangerMenu.AddItem(new MenuItem("Color", "Color").SetValue(new Circle(true, Color.FromArgb(255, 255, 0, 0))));*/

            dangerMenu.AddSubMenu(lowDangerMenu);
            dangerMenu.AddSubMenu(normalDangerMenu);
            dangerMenu.AddSubMenu(highDangerMenu);
            dangerMenu.AddSubMenu(extremeDangerMenu);

            drawMenu.AddSubMenu(dangerMenu);

            menu.AddSubMenu(drawMenu);
        }

        private void DrawLineRectangle(Vector2 start, Vector2 end, int radius, int width, Color color)
        {
            var dir = (end - start).Normalized();
            var pDir = dir.Perpendicular();

            var rightStartPos = start + pDir * radius;
            var leftStartPos = start - pDir * radius;
            var rightEndPos = end + pDir * radius;
            var leftEndPos = end - pDir * radius;

            var rStartPos = Drawing.WorldToScreen(new Vector3(rightStartPos.X, rightStartPos.Y, myHero.Position.Z));
            var lStartPos = Drawing.WorldToScreen(new Vector3(leftStartPos.X, leftStartPos.Y, myHero.Position.Z));
            var rEndPos = Drawing.WorldToScreen(new Vector3(rightEndPos.X, rightEndPos.Y, myHero.Position.Z));
            var lEndPos = Drawing.WorldToScreen(new Vector3(leftEndPos.X, leftEndPos.Y, myHero.Position.Z));

            Drawing.DrawLine(rStartPos, rEndPos, width, color);
            Drawing.DrawLine(lStartPos, lEndPos, width, color);
            Drawing.DrawLine(rStartPos, lStartPos, width, color);
            Drawing.DrawLine(lEndPos, rEndPos, width, color);
        }

        private void DrawEvadeStatus()
        {
            if (ObjectCache.menuCache.cache["ShowStatus"].GetValue<bool>())
            {
                var heroPos = Drawing.WorldToScreen(ObjectManager.Player.Position);
                var dimension = Drawing.GetTextEntent("Evade: ON", 0);

                if (ObjectCache.menuCache.cache["DodgeSkillShots"].GetValue<KeyBind>().Active)
                {                    
                    if (Evade.isDodging)
                    {
                        Drawing.DrawText(heroPos.X - dimension.Width / 2, heroPos.Y, Color.Red, "Evade: ON");
                    }
                    else
                    {
                        if (ObjectCache.menuCache.cache["DodgeOnlyOnComboKeyEnabled"].GetValue<bool>() == true
                         && ObjectCache.menuCache.cache["DodgeComboKey"].GetValue<KeyBind>().Active == false)
                        {
                            Drawing.DrawText(heroPos.X - dimension.Width / 2, heroPos.Y, Color.Gray, "Evade: OFF");
                        }
                        else
                        {
                            if (Evade.isDodgeDangerousEnabled())
                                Drawing.DrawText(heroPos.X - dimension.Width / 2, heroPos.Y, Color.Yellow, "Evade: ON");
                            else
                                Drawing.DrawText(heroPos.X - dimension.Width / 2, heroPos.Y, Color.Lime, "Evade: ON");
                        }                        
                    }
                }
                else
                {
                    if (ObjectCache.menuCache.cache["ActivateEvadeSpells"].GetValue<KeyBind>().Active)
                    {
                        if (ObjectCache.menuCache.cache["DodgeOnlyOnComboKeyEnabled"].GetValue<bool>() == true
                         && ObjectCache.menuCache.cache["DodgeComboKey"].GetValue<KeyBind>().Active == false)
                        {
                            Drawing.DrawText(heroPos.X - dimension.Width / 2, heroPos.Y, Color.Gray, "Evade: OFF");
                        }
                        else
                        {
                            if (Evade.isDodgeDangerousEnabled())
                                Drawing.DrawText(heroPos.X - dimension.Width / 2, heroPos.Y, Color.Yellow, "Evade: Spell");
                            else
                                Drawing.DrawText(heroPos.X - dimension.Width / 2, heroPos.Y, Color.DeepSkyBlue, "Evade: Spell");
                        }
                    }
                    else
                    {
                        Drawing.DrawText(heroPos.X - dimension.Width / 2, heroPos.Y, Color.Gray, "Evade: OFF");
                    }
                }



            }
        }

        private void Drawing_OnDraw(EventArgs args)
        {

            if (ObjectCache.menuCache.cache["DrawEvadePosition"].GetValue<bool>())
            {
                //Render.Circle.DrawCircle(myHero.Position.ExtendDir(dir, 500), 65, Color.Red, 10);

                /*foreach (var point in myHero.Path)
                {
                    Render.Circle.DrawCircle(point, 65, Color.Red, 10);
                }*/

                if (Evade.lastPosInfo != null)
                {
                    var pos = Evade.lastPosInfo.position; //Evade.lastEvadeCommand.targetPosition;
                    Render.Circle.DrawCircle(new Vector3(pos.X, pos.Y, myHero.Position.Z), 65, Color.Red, 10);
                }
            }

            DrawEvadeStatus();

            if (ObjectCache.menuCache.cache["DrawSkillShots"].GetValue<bool>() == false)
            {
                return;
            }

            foreach (KeyValuePair<int, Spell> entry in SpellDetector.drawSpells)
            {
                Spell spell = entry.Value;

                var dangerStr = spell.GetSpellDangerString();
                var spellDrawingConfig = ObjectCache.menuCache.cache[dangerStr + "Color"].GetValue<Circle>();
                var spellDrawingWidth = ObjectCache.menuCache.cache[dangerStr + "Width"].GetValue<Slider>().Value;

                if (ObjectCache.menuCache.cache[spell.info.spellName + "DrawSpell"].GetValue<bool>()
                    && spellDrawingConfig.Active)
                {
                    bool canEvade = true;
                    //bool canEvade = !(Evade.lastPosInfo != null && Evade.lastPosInfo.undodgeableSpells.Contains(spell.spellID));
                  
                    if (spell.spellType == SpellType.Line)
                    {
                        Vector2 spellPos = spell.currentSpellPosition;
                        Vector2 spellEndPos = spell.GetSpellEndPosition();

                        
                        DrawLineRectangle(spellPos, spellEndPos, (int)spell.radius, spellDrawingWidth, !canEvade ? Color.Yellow : spellDrawingConfig.Color);

                        /*foreach (var hero in ObjectManager.Get<AIHeroClient>())
                        {
                            Render.Circle.DrawCircle(new Vector3(hero.ServerPosition.X, hero.ServerPosition.Y, myHero.Position.Z), (int)spell.radius, Color.Red, 5);
                        }*/

                        if (ObjectCache.menuCache.cache["DrawSpellPos"].GetValue<bool>())// && spell.spellObject != null)
                        {
                            //spellPos = SpellDetector.GetCurrentSpellPosition(spell, true, ObjectCache.gamePing);

                            /*if (true)
                            {
                                var spellPos2 = spell.startPos + spell.direction * spell.info.projectileSpeed * (Evade.GetTickCount - spell.startTime - spell.info.spellDelay) / 1000 + spell.direction * spell.info.projectileSpeed * ((float)ObjectCache.gamePing / 1000);
                                Render.Circle.DrawCircle(new Vector3(spellPos2.X, spellPos2.Y, myHero.Position.Z), (int)spell.radius, Color.Red, 8);
                            }*/

                            /*if (spell.spellObject != null && spell.spellObject.IsValid && spell.spellObject.IsVisible &&
                                  spell.spellObject.Position.To2D().Distance(ObjectCache.myHeroCache.serverPos2D) < spell.info.range + 1000)*/

                            Render.Circle.DrawCircle(new Vector3(spellPos.X, spellPos.Y, spell.height), (int) spell.radius, !canEvade ? Color.Yellow : spellDrawingConfig.Color, spellDrawingWidth);
                        }

                    }
                    else if (spell.spellType == SpellType.Circular)
                    {
                        Render.Circle.DrawCircle(new Vector3(spell.endPos.X, spell.endPos.Y, spell.height), (int) spell.radius, !canEvade ? Color.Yellow : spellDrawingConfig.Color, spellDrawingWidth);

                        if (spell.info.spellName == "VeigarEventHorizon")
                        {
                            Render.Circle.DrawCircle(new Vector3(spell.endPos.X, spell.endPos.Y, spell.height), (int) spell.radius - 125, !canEvade ? Color.Yellow : spellDrawingConfig.Color, spellDrawingWidth);
                        }
                    }
                    else if (spell.spellType == SpellType.Arc)
                    {                      
                        /*var spellRange = spell.startPos.Distance(spell.endPos);
                        var midPoint = spell.startPos + spell.direction * (spellRange / 2);

                        Render.Circle.DrawCircle(new Vector3(midPoint.X, midPoint.Y, myHero.Position.Z), (int)spell.radius, spellDrawingConfig.Color, spellDrawingWidth);
                        
                        Drawing.DrawLine(Drawing.WorldToScreen(spell.startPos.To3D()),
                                         Drawing.WorldToScreen(spell.endPos.To3D()), 
                                         spellDrawingWidth, spellDrawingConfig.Color);*/
                    }
                    else if (spell.spellType == SpellType.Cone)
                    {

                    }
                }
            }
        }
    }
}
