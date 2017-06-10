using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Color = System.Drawing.Color;

using EloBuddy;
using TW.Common;
using TW.Common.Extensions;
using SharpDX;

namespace ezEvade
{
    class SpellTester
    {
        public static Menu menu;
        public static Menu selectSpellMenu;

        private static AIHeroClient myHero { get { return ObjectManager.Player; } }

        private static Dictionary<string, Dictionary<string, SpellData>> spellCache
                 = new Dictionary<string, Dictionary<string, SpellData>>();

        public static Vector3 spellStartPosition = myHero.ServerPosition;
        public static Vector3 spellEndPostion = myHero.ServerPosition
                              + (myHero.Direction.To2D().Perpendicular() * 500).To3D();

        public static float lastSpellFireTime = 0;

        public SpellTester()
        {
            menu = new Menu("\u6280\u80fd\u6e2c\u8a66\u5100", "DummySpellTester", true);

            selectSpellMenu = new Menu("\u9078\u64c7\u6280\u80fd", "SelectSpellMenu");
            menu.AddSubMenu(selectSpellMenu);

            Menu setSpellPositionMenu = new Menu("\u8a2d\u5b9a\u6280\u80fd\u4f4d\u7f6e", "SetPositionMenu");
            setSpellPositionMenu.AddItem(new MenuItem("SetDummySpellStartPosition", "\u8a2d\u5b9a \u958b\u59cb\u4f4d\u7f6e").SetValue(false));
            setSpellPositionMenu.AddItem(new MenuItem("SetDummySpellEndPosition", "\u8a2d\u5b9a \u7d50\u675f\u4f4d\u7f6e").SetValue(false));
            setSpellPositionMenu.Item("SetDummySpellStartPosition").ValueChanged += OnSpellStartChange;
            setSpellPositionMenu.Item("SetDummySpellEndPosition").ValueChanged += OnSpellEndChange;

            menu.AddSubMenu(setSpellPositionMenu);

            Menu fireDummySpellMenu = new Menu("\u5c0d\u6230\u9810\u5148\u63a2\u6e2c\u6280\u80fd", "FireDummySpellMenu");
            fireDummySpellMenu.AddItem(new MenuItem("FireDummySpell", "\u865b\u64ec\u6280\u80fd\u6309\u9375").SetValue(new KeyBind('O', KeyBindType.Press)));

            fireDummySpellMenu.AddItem(new MenuItem("SpellInterval", "\u6280\u80fd \u9593\u9694").SetValue(new Slider(2500, 0, 5000)));

            menu.AddSubMenu(fireDummySpellMenu);

            menu.AddToMainMenu();

            LoadSpellDictionary();

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
        }

        private void Drawing_OnDraw(EventArgs args)
        {
            foreach (var spell in SpellDetector.drawSpells.Values)
            {
                Vector2 spellPos = spell.currentSpellPosition;

                if (spell.heroID == myHero.NetworkId)
                {
                    if (spell.spellType == SpellType.Line)
                    {
                        if (spellPos.Distance(myHero) <= myHero.BoundingRadius + spell.radius
                            && EvadeUtils.TickCount - spell.startTime > spell.info.spellDelay
                            && spell.startPos.Distance(myHero) < spell.info.range)
                        {
                            Draw.RenderObjects.Add(new Draw.RenderCircle(spellPos, 1000, Color.Red,
                                (int)spell.radius, 10));
                            DelayAction.Add(1, () => SpellDetector.DeleteSpell(spell.spellID));
                        }
                        else
                        {
                            Render.Circle.DrawCircle(new Vector3(spellPos.X, spellPos.Y, myHero.Position.Z), (int)spell.radius, Color.White, 5);
                        }
                    }
                    else if (spell.spellType == SpellType.Circular)
                    {
                        if (EvadeUtils.TickCount - spell.startTime >= spell.endTime - spell.startTime)
                        {
                            if (myHero.ServerPosition.To2D().InSkillShot(spell, myHero.BoundingRadius))
                            {
                                Draw.RenderObjects.Add(new Draw.RenderCircle(spellPos, 1000, Color.Red, (int) spell.radius, 5));
                                DelayAction.Add(1, () => SpellDetector.DeleteSpell(spell.spellID));
                            }
                        }
                    }
                }
            }
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (menu.Item("FireDummySpell").GetValue<KeyBind>().Active == true)
            {
                float interval = menu.Item("SpellInterval").GetValue<Slider>().Value;

                if (EvadeUtils.TickCount - lastSpellFireTime > interval)
                {
                    var charName = selectSpellMenu.Item("DummySpellHero").GetValue<StringList>().SelectedValue;
                    var spellName = selectSpellMenu.Item("DummySpellList").GetValue<StringList>().SelectedValue;
                    var spellData = spellCache[charName][spellName];

                    if (!ObjectCache.menuCache.cache.ContainsKey(spellName + "DodgeSpell"))
                    {
                        SpellDetector.LoadDummySpell(spellData);
                    }

                    SpellDetector.CreateSpellData(myHero, spellStartPosition, spellEndPostion, spellData);
                    lastSpellFireTime = EvadeUtils.TickCount;
                }
            }
        }

        private void OnSpellEndChange(object sender, OnValueChangeEventArgs e)
        {
            e.Process = false;

            spellEndPostion = myHero.ServerPosition;
            Draw.RenderObjects.Add(new Draw.RenderCircle(spellEndPostion.To2D(), 1000, Color.Red, 100, 20));
        }

        private void OnSpellStartChange(object sender, OnValueChangeEventArgs e)
        {
            e.Process = false;

            spellStartPosition = myHero.ServerPosition;
            Draw.RenderObjects.Add(new Draw.RenderCircle(spellStartPosition.To2D(), 1000, Color.Red, 100, 20));
        }

        private void LoadSpellDictionary()
        {
            foreach (var spell in SpellDatabase.Spells)
            {
                if (spellCache.ContainsKey(spell.charName))
                {
                    var spellList = spellCache[spell.charName];
                    if (spellList != null && !spellList.ContainsKey(spell.spellName))
                    {
                        spellList.Add(spell.spellName, spell);
                    }
                }
                else
                {
                    spellCache.Add(spell.charName, new Dictionary<string, SpellData>());
                    var spellList = spellCache[spell.charName];
                    if (spellList != null && !spellList.ContainsKey(spell.spellName))
                    {
                        spellList.Add(spell.spellName, spell);
                    }
                }
            }

            selectSpellMenu.AddItem(new MenuItem("DummySpellDescription", "-- \u9078\u64c7\u4e00\u500b\u865b\u64ec\u7684\u6cd5\u8853  --"));

            var heroList = spellCache.Keys.ToArray();
            selectSpellMenu.AddItem(new MenuItem("DummySpellHero", "\u82f1\u96c4")
                .SetValue(new StringList(heroList, 0)));

            var selectedHeroStr = selectSpellMenu.Item("DummySpellHero").GetValue<StringList>().SelectedValue;
            var selectedHero = spellCache[selectedHeroStr];
            var selectedHeroList = selectedHero.Keys.ToArray();

            selectSpellMenu.AddItem(new MenuItem("DummySpellList", "\u6280\u80fd")
                .SetValue(new StringList(selectedHeroList, 0)));

            selectSpellMenu.Item("DummySpellHero").ValueChanged += OnSpellHeroChange;
        }

        private void OnSpellHeroChange(object sender, OnValueChangeEventArgs e)
        {
            //var previousHeroStr = e.GetOldValue<StringList>().SelectedValue;
            var selectedHeroStr = e.GetNewValue<StringList>().SelectedValue;
            var selectedHero = spellCache[selectedHeroStr];
            var selectedHeroList = selectedHero.Keys.ToArray();

            selectSpellMenu.Item("DummySpellList").SetValue(new StringList(selectedHeroList, 0));
        }
    }
}
