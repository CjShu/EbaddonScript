using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EloBuddy;
using SharpDX;
using TW.Common;

namespace ezEvade
{
    internal class Evade
    {
        public static AIHeroClient myHero { get { return ObjectManager.Player; } }

        public static SpellDetector spellDetector;
        private static SpellDrawer spellDrawer;
        private static EvadeTester evadeTester;
        private static PingTester pingTester;
        private static EvadeSpell evadeSpell;
        private static SpellTester spellTester;
        private static AutoSetPing autoSetPing;

        public static SpellSlot lastSpellCast;
        public static float lastSpellCastTime = 0;

        public static float lastWindupTime = 0;

        public static float lastTickCount = 0;
        public static float lastStopEvadeTime = 0;

        public static Vector3 lastMovementBlockPos = Vector3.Zero;
        public static float lastMovementBlockTime = 0;

        public static float lastEvadeOrderTime = 0;
        public static float lastIssueOrderGameTime = 0;
        public static float lastIssueOrderTime = 0;
        public static PlayerIssueOrderEventArgs lastIssueOrderArgs = null;

        public static Vector2 lastMoveToPosition = Vector2.Zero;
        public static Vector2 lastMoveToServerPos = Vector2.Zero;
        public static Vector2 lastStopPosition = Vector2.Zero;

        public static DateTime assemblyLoadTime = DateTime.Now;

        public static bool isDodging = false;
        public static bool dodgeOnlyDangerous = false;

        public static bool hasGameEnded = false;
        public static bool isChanneling = false;
        public static bool devModeOn;
        public static Vector2 channelPosition = Vector2.Zero;

        public static PositionInfo lastPosInfo;

        public static EvadeCommand lastEvadeCommand = new EvadeCommand { isProcessed = true, timestamp = EvadeUtils.TickCount };

        public static EvadeCommand lastBlockedUserMoveTo = new EvadeCommand { isProcessed = true, timestamp = EvadeUtils.TickCount };
        public static float lastDodgingEndTime = 0;

        public static Menu menu;

        public static float sumCalculationTime = 0;
        public static float numCalculationTime = 0;
        public static float avgCalculationTime = 0;

        public Evade()
        {
            LoadAssembly();
        }

        private void LoadAssembly()
        {
            EloBuddy.SDK.Events.Loading.OnLoadingComplete += Game_OnGameLoad;
        }

        private void Game_OnGameLoad(EventArgs args)
        {
            try
            {
                devModeOn = true;

                EloBuddy.Player.OnIssueOrder += Game_OnIssueOrder;
                Spellbook.OnCastSpell += Game_OnCastSpell;
                Game.OnUpdate += Game_OnGameUpdate;

                AIHeroClient.OnProcessSpellCast += Game_OnProcessSpell;

                Game.OnEnd += Game_OnGameEnd;
                SpellDetector.OnProcessDetectedSpells += SpellDetector_OnProcessDetectedSpells;
                Orbwalking.BeforeAttack += Orbwalking_BeforeAttack;

                /*Console.WriteLine("<font color=\"#66CCFF\" >Yomie's </font><font color=\"#CCFFFF\" >ezEvade</font> - " +
                   "<font color=\"#FFFFFF\" >Version " + Assembly.GetExecutingAssembly().GetName().Version + "</font>");
                */

                Chat.Print("<font size='27'>[ezEvade]&#x4F5C;&#x8005;</font> <font color='#E6CAFF'>By: Kurisu</font>");
                Chat.Print("<font size='27'>[ezEvade]&#x4F5C;&#x8005;</font> <font color='#E6CAFF'>By: Soulcaliber</font>");

                menu = new Menu("ezEvade \u4e2d\u6587\u7248", "ezEvade", true);

                Menu mainMenu = new Menu("\u4e3b\u83dc\u55ae", "Main");
                mainMenu.AddItem(new MenuItem("DodgeSkillShots", "\u8eb2\u907f\u6280\u80fd").SetValue(new KeyBind('K', KeyBindType.Toggle, true)));
                mainMenu.Item("DodgeSkillShots").Permashow();
                mainMenu.AddItem(new MenuItem("ActivateEvadeSpells", "\u4f7f\u7528\u6280\u80fd \u8eb2\u907f").SetValue(new KeyBind('K', KeyBindType.Toggle, true)));
                mainMenu.Item("ActivateEvadeSpells").Permashow();
                mainMenu.AddItem(new MenuItem("DodgeDangerous", "\u53ea\u8eb2\u907f\u5371\u96aa \u6280\u80fd").SetValue(false));
                mainMenu.AddItem(new MenuItem("DodgeFOWSpells", "\u8eb2\u907f \u6230\u722d\u8ff7\u9727 \u4e2d\u7684\u6280\u80fd").SetValue(true));
                mainMenu.AddItem(new MenuItem("DodgeCircularSpells", "\u8eb2\u907f\u5713\u5f62\u6280\u80fd").SetValue(true));
                menu.AddSubMenu(mainMenu);

                //var keyBind = mainMenu.Item("DodgeSkillShots").GetValue<KeyBind>();
                //mainMenu.Item("DodgeSkillShots").SetValue(new KeyBind(keyBind.Key, KeyBindType.Toggle, true));

                spellDetector = new SpellDetector(menu);
                evadeSpell = new EvadeSpell(menu);

                Menu keyMenu = new Menu("\u6309\u9375 \u8a2d\u7f6e", "KeySettings");
                keyMenu.AddItem(new MenuItem("DodgeDangerousKeyEnabled", "\u555f\u52d5 \u5371\u96aa\u6280\u80fd\u6309\u9375 \u529f\u80fd").SetValue(false));
                keyMenu.AddItem(new MenuItem("DodgeDangerousKey", "\u53ea\u9583\u8eb2\u5371\u96aa\u6280\u80fd\u7684 \u6309\u9375").SetValue(new KeyBind(32, KeyBindType.Press)));
                keyMenu.AddItem(new MenuItem("DodgeDangerousKey2", "\u53ea\u9583\u8eb2\u5371\u96aa\u6280\u80fd\u7684 \u6309\u9375 2").SetValue(new KeyBind('V', KeyBindType.Press)));
                keyMenu.AddItem(new MenuItem("DodgeOnlyOnComboKeyEnabled", "\u555f\u52d5 \u8eb2\u907f\u53ea\u5728\u9023\u62db \u529f\u80fd").SetValue(false));
                keyMenu.AddItem(new MenuItem("DodgeComboKey", "\u53ea\u5728\u9023\u62db\u4e2d\u8eb2\u907f \u6309\u9375").SetValue(new KeyBind(32, KeyBindType.Press)));
                keyMenu.AddItem(new MenuItem("DontDodgeKeyEnabled", "\u555f\u52d5 \u4e0d\u8981\u8eb2\u907f \u529f\u80fd").SetValue(false));
                keyMenu.AddItem(new MenuItem("DontDodgeKey", "\u4e0d\u8981\u8eb2\u907f \u6309\u9375").SetValue(new KeyBind('Z', KeyBindType.Press)));
                menu.AddSubMenu(keyMenu);

                Menu miscMenu = new Menu("\u5176\u4ed6 \u8a2d\u5b9a", "MiscSettings");
                miscMenu.AddItem(new MenuItem("HigherPrecision", "\u63d0\u9ad8 \u8eb2\u907f \u6e96\u78ba\u5ea6").SetValue(false));
                miscMenu.AddItem(new MenuItem("RecalculatePosition", "\u91cd\u65b0\u8a08\u7b97 \u8def\u5f91").SetValue(true));
                miscMenu.AddItem(new MenuItem("ContinueMovement", "\u5ef6\u7e8c\u6700\u5f8c\u7684 \u79fb\u52d5").SetValue(true));
                miscMenu.AddItem(new MenuItem("CalculateWindupDelay", "\u8a08\u7b97 \u666e\u653b\u52d5\u756b \u5ef6\u9072").SetValue(true));
                miscMenu.AddItem(new MenuItem("CheckSpellCollision", "\u6aa2\u67e5\u6280\u80fd \u78b0\u649e").SetValue(false));
                miscMenu.AddItem(new MenuItem("PreventDodgingUnderTower", "\u907f\u514d\u8eb2\u907f\u5230 \u9632\u79a6\u5854\u4e0b").SetValue(false));
                miscMenu.AddItem(new MenuItem("PreventDodgingNearEnemy", "\u907f\u514d\u56e0\u8eb2\u907f \u63a5\u8fd1\u6575\u4eba").SetValue(true));
                miscMenu.AddItem(new MenuItem("AdvancedSpellDetection", "\u9ad8\u7d1a\u6cd5\u8853 \u6aa2\u6e2c").SetValue(false));
                //miscMenu.AddItem(new MenuItem("AllowCrossing", "Allow Crossing").SetValue(false));
                //miscMenu.AddItem(new MenuItem("CalculateHeroPos", "Calculate Hero Position").SetValue(false));
                miscMenu.AddItem(new MenuItem("ResetConfig", "\u91cd\u7f6e \u8eb2\u907f \u8a2d\u5b9a").SetValue(false));
                miscMenu.AddItem(new MenuItem("EvadeMode", "\u8eb2\u907f \u6a21\u5f0f")
                    .SetValue(new StringList(new[] { "\u9806\u66a2", "\u975e\u5e38\u9806\u66a2", "\u6700\u5feb\u8eb2\u907f", "Hawk \u6a21\u5f0f", "Kurisu \u6a21\u5f0f", "GuessWho \u6a21\u5f0f" }, 0)));
                miscMenu.Item("EvadeMode").ValueChanged += OnEvadeModeChange;

                Menu limiterMenu = new Menu("\u4eba\u6027\u5316\u8a2d\u5b9a", "Limiter");
                limiterMenu.AddItem(new MenuItem("ClickOnlyOnce", "\u53ea\u9ede\u64ca\u4e00\u6b21").SetValue(true));
                limiterMenu.AddItem(new MenuItem("EnableEvadeDistance", "\u555f\u52d5\u8eb2\u907f \u8ddd\u96e2").SetValue(false));
                limiterMenu.AddItem(new MenuItem("TickLimiter", "\u6642\u9593 \u9650\u5236\u5668").SetValue(new Slider(100, 0, 500)));
                limiterMenu.AddItem(new MenuItem("SpellDetectionTime", "\u6aa2\u6e2c \u6280\u80fd \u6642\u9593").SetValue(new Slider(0, 0, 1000)));
                limiterMenu.AddItem(new MenuItem("ReactionTime", "\u53cd\u61c9 \u6642\u9593").SetValue(new Slider(0, 0, 500)));
                limiterMenu.AddItem(new MenuItem("DodgeInterval", "\u8eb2\u907f \u9593\u9694").SetValue(new Slider(0, 0, 2000)));
                limiterMenu.AddItem(new MenuItem("L11", "\u6642\u9593 (1000 = 1\u79d2) \u6beb\u79d2\u8a08\u7b97 | \u8eb2\u907f \u4e00\u500b \u6280\u80fd\u6642\u9593"));
                limiterMenu.AddItem(new MenuItem("L12", "\u9593\u9694 (1000 = 1\u79d2) \u6beb\u79d2\u8a08\u7b97 | \u8eb2\u907f \u4e0b\u4e00\u6b21 \u7684\u6280\u80fd \u9593\u9694\u6642\u9593"));
                limiterMenu.AddItem(new MenuItem("L13", "\u6aa2\u6e2c (1000 = 1\u79d2) \u6beb\u79d2\u8a08\u7b97 | \u9810\u5148\u6aa2\u67e5\u6575\u4eba \u6280\u80fd\u4f4d\u7f6e \u6642\u9593"));
                limiterMenu.AddItem(new MenuItem("L14", "\u9650\u5236\u5668 (500 = 0.5) \u6beb\u79d2\u8a08\u7b97 | \u9650\u5236\u59b3\u5728\u8207 \u81ea\u52d5\u8eb2\u907f\u6642 \u53cd\u61c9\u6642\u9593"));
                miscMenu.AddSubMenu(limiterMenu);

                Menu fastEvadeMenu = new Menu("\u5feb\u901f\u8eb2\u907f\u8a2d\u5b9a", "FastEvade");
                fastEvadeMenu.AddItem(new MenuItem("FastMovementBlock", "\u555f\u52d5 \u53ef\u4ee5\u5de6\u9375 \u963b\u6b62\u79fb\u52d5")).SetValue(false);
                fastEvadeMenu.AddItem(new MenuItem("FastEvadeActivationTime", "\u5feb\u901f\u8eb2\u907f \u6fc0\u6d3b \u6642\u9593").SetValue(new Slider(65, 0, 500)));
                fastEvadeMenu.AddItem(new MenuItem("SpellActivationTime", "\u6280\u80fd \u6fc0\u6d3b \u6642\u9593").SetValue(new Slider(400, 0, 1000)));
                fastEvadeMenu.AddItem(new MenuItem("RejectMinDistance", "\u78b0\u649e \u8ddd\u96e2 \u7de9\u885d").SetValue(new Slider(10, 0, 100)));
                fastEvadeMenu.AddItem(new MenuItem("F11", "\u6642\u9593 (1000 = 1 \u79d2) \u6beb\u79d2\u8a08\u7b97"));
                fastEvadeMenu.AddItem(new MenuItem("F12", "\u8ddd\u96e2\u7de9\u885d (\u8207\u6575\u65b9\u6700\u4f4e\u8ddd\u96e2)"));
                miscMenu.AddSubMenu(fastEvadeMenu);

                /*Menu evadeSpellSettingsMenu = new Menu("Evade Spell", "EvadeSpellMisc");
                evadeSpellSettingsMenu.AddItem(new MenuItem("EvadeSpellActivationTime", "Evade Spell Activation Time").SetValue(new Slider(150, 0, 500)));

                miscMenu.AddSubMenu(evadeSpellSettingsMenu);*/

                Menu bufferMenu = new Menu("\u984d\u5916\u7de9\u885d\u8a2d\u5b9a", "ExtraBuffers");
                bufferMenu.AddItem(new MenuItem("ExtraPingBuffer", "\u984d\u5916 Ping \u7de9\u885d").SetValue(new Slider(65, 0, 200)));
                bufferMenu.AddItem(new MenuItem("ExtraCPADistance", "\u984d\u5916 \u78b0\u649e \u8ddd\u96e2").SetValue(new Slider(10, 0, 150)));
                bufferMenu.AddItem(new MenuItem("ExtraSpellRadius", "\u984d\u5916 \u6280\u80fd \u534a\u5f91").SetValue(new Slider(0, 0, 100)));
                bufferMenu.AddItem(new MenuItem("ExtraEvadeDistance", "\u984d\u5916 \u8eb2\u907f \u8ddd\u96e2").SetValue(new Slider(100, 0, 300)));
                bufferMenu.AddItem(new MenuItem("ExtraAvoidDistance", "\u984d\u5916 \u8ff4\u907f \u8ddd\u96e2").SetValue(new Slider(50, 0, 300)));
                bufferMenu.AddItem(new MenuItem("MinComfortZone", "\u6700\u4f4e \u8ddd\u96e2 \u8207 \u82f1\u96c4").SetValue(new Slider(550, 0, 1000)));

                miscMenu.AddSubMenu(bufferMenu);

                Menu loadTestMenu = new Menu("\u6e2c\u8a66\u6a21\u5f0f\u8a2d\u5b9a", "LoadTests");

                loadTestMenu.AddItem(new MenuItem("LoadPingTester", "\u6ce8\u5165 Ping \u6e2c\u8a66").SetValue(false));
                loadTestMenu.AddItem(new MenuItem("LoadSpellTester", "\u6ce8\u5165 \u6280\u80fd \u6e2c\u8a66").SetValue(false));
                loadTestMenu.AddItem(new MenuItem("Load11", "\u6c92\u4e8b\u7684\u4eba\u8acb\u4e0d\u8981\u96a8\u610f\u4e82\u958b"));
                loadTestMenu.AddItem(new MenuItem("Load12", "\u9664\u975e\u59b3\u6703\u770b\u4ee3\u78bc\u80fd\u5920\u5e6b\u52a9\u6211\u7684\u4eba\u60f3\u66f4\u597d\u7684\u8eb2\u907f\u5728\u6253\u958b"));
                loadTestMenu.Item("LoadPingTester").ValueChanged += OnLoadPingTesterChange;
                loadTestMenu.Item("LoadSpellTester").ValueChanged += OnLoadSpellTesterChange;

                miscMenu.AddSubMenu(loadTestMenu);

                menu.AddSubMenu(miscMenu);
                menu.AddToMainMenu();

                spellDrawer = new SpellDrawer(menu);

                //autoSetPing = new AutoSetPing(menu);

                var initCache = ObjectCache.myHeroCache;

                //evadeTester = new EvadeTester(menu);
                //LeagueSharp.Common.Utility.DelayAction.Add(100, () => loadTestMenu.Item("LoadSpellTester").SetValue(true));

                Console.WriteLine("ezEvade Loaded");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void ResetConfig(bool kappa = true)
        {
            menu.Item("DodgeSkillShots").SetValue(new KeyBind('K', KeyBindType.Toggle, true));
            menu.Item("ActivateEvadeSpells").SetValue(new KeyBind('K', KeyBindType.Toggle, true));
            menu.Item("DodgeDangerous").SetValue(false);
            menu.Item("DodgeFOWSpells").SetValue(true);
            menu.Item("DodgeCircularSpells").SetValue(true);

            menu.Item("HigherPrecision").SetValue(false);
            menu.Item("RecalculatePosition").SetValue(true);
            menu.Item("ContinueMovement").SetValue(true);
            menu.Item("CalculateWindupDelay").SetValue(true);
            menu.Item("CheckSpellCollision").SetValue(false);
            menu.Item("PreventDodgingUnderTower").SetValue(false);
            menu.Item("PreventDodgingNearEnemy").SetValue(true);
            menu.Item("AdvancedSpellDetection").SetValue(false);
            menu.Item("LoadPingTester").SetValue(true);

            menu.Item("ClickOnlyOnce").SetValue(true);
            menu.Item("EnableEvadeDistance").SetValue(false);
            menu.Item("TickLimiter").SetValue(new Slider(100, 0, 500));
            menu.Item("SpellDetectionTime").SetValue(new Slider(0, 0, 1000));
            menu.Item("ReactionTime").SetValue(new Slider(0, 0, 500));
            menu.Item("DodgeInterval").SetValue(new Slider(0, 0, 2000));

            menu.Item("FastMovementBlock").SetValue(false);
            menu.Item("FastEvadeActivationTime").SetValue(new Slider(65, 0, 500));
            menu.Item("SpellActivationTime").SetValue(new Slider(200, 0, 1000));
            menu.Item("RejectMinDistance").SetValue(new Slider(10, 0, 100));

            menu.Item("ExtraPingBuffer").SetValue(new Slider(65, 0, 200));
            menu.Item("ExtraCPADistance").SetValue(new Slider(10, 0, 150));
            menu.Item("ExtraSpellRadius").SetValue(new Slider(0, 0, 100));
            menu.Item("ExtraEvadeDistance").SetValue(new Slider(100, 0, 300));
            menu.Item("ExtraAvoidDistance").SetValue(new Slider(50, 0, 300));
            menu.Item("MinComfortZone").SetValue(new Slider(550, 0, 1000));

            // drawings
            menu.Item("DrawSkillShots").SetValue(true);
            menu.Item("ShowStatus").SetValue(true);
            menu.Item("DrawSpellPos").SetValue(false);
            menu.Item("DrawEvadePosition").SetValue(false);

            if (kappa)
            {
                // profiles
                menu.Item("EvadeMode")
                    .SetValue(new StringList(new[] { "\u9806\u66a2", "\u975e\u5e38\u9806\u66a2", "\u6700\u5feb\u8eb2\u907f", "Hawk \u6a21\u5f0f", "Kurisu \u6a21\u5f0f", "GuessWho \u6a21\u5f0f" }, 0));

                // keys
                menu.Item("DodgeDangerousKeyEnabled").SetValue(false);
                menu.Item("DodgeDangerousKey").SetValue(new KeyBind(32, KeyBindType.Press));
                menu.Item("DodgeDangerousKey2").SetValue(new KeyBind('V', KeyBindType.Press));
                menu.Item("DodgeOnlyOnComboKeyEnabled").SetValue(false);
                menu.Item("DodgeComboKey").SetValue(new KeyBind(32, KeyBindType.Press));
                menu.Item("DontDodgeKeyEnabled").SetValue(false);
                menu.Item("DontDodgeKey").SetValue(new KeyBind('Z', KeyBindType.Press));
            }
        }

        private void OnEvadeModeChange(object sender, OnValueChangeEventArgs e)
        {
            var mode = e.GetNewValue<StringList>().SelectedValue;

            if (mode == "\u6700\u5feb\u8eb2\u907f")
            {
                ResetConfig(false);
                menu.Item("FastMovementBlock").SetValue(true);
                menu.Item("FastEvadeActivationTime").SetValue(new Slider(120, 0, 500));
                menu.Item("RejectMinDistance").SetValue(new Slider(25, 0, 100));
                menu.Item("ExtraCPADistance").SetValue(new Slider(25, 0, 150));
                menu.Item("ExtraPingBuffer").SetValue(new Slider(80, 0, 200));
            }
            else if (mode == "\u975e\u5e38\u9806\u66a2")
            {
                ResetConfig(false);
                menu.Item("FastEvadeActivationTime").SetValue(new Slider(0, 0, 500));
                menu.Item("RejectMinDistance").SetValue(new Slider(0, 0, 100));
                menu.Item("ExtraCPADistance").SetValue(new Slider(0, 0, 150));
                menu.Item("ExtraPingBuffer").SetValue(new Slider(40, 0, 200));
                menu.Item("AdvancedSpellDetection").SetValue(true);
            }
            else if (mode == "\u9806\u66a2")
            {
                ResetConfig(false);
                menu.Item("FastMovementBlock").SetValue(true);
                menu.Item("FastEvadeActivationTime").SetValue(new Slider(65, 0, 500));
                menu.Item("RejectMinDistance").SetValue(new Slider(10, 0, 100));
                menu.Item("ExtraCPADistance").SetValue(new Slider(10, 0, 150));
                menu.Item("ExtraPingBuffer").SetValue(new Slider(65, 0, 200));
            }

            else if (mode == "Hawk \u6a21\u5f0f")
            {
                ResetConfig(false);
                menu.Item("DodgeDangerous").SetValue(false);
                menu.Item("DodgeFOWSpells").SetValue(false);
                menu.Item("DodgeCircularSpells").SetValue(false);
                menu.Item("DodgeDangerousKeyEnabled").SetValue(true);
                menu.Item("HigherPrecision").SetValue(true);
                menu.Item("RecalculatePosition").SetValue(true);
                menu.Item("ContinueMovement").SetValue(true);
                menu.Item("CalculateWindupDelay").SetValue(true);
                menu.Item("CheckSpellCollision").SetValue(true);
                menu.Item("PreventDodgingUnderTower").SetValue(false);
                menu.Item("PreventDodgingNearEnemy").SetValue(true);
                menu.Item("AdvancedSpellDetection").SetValue(true);
                menu.Item("ClickOnlyOnce").SetValue(true);
                menu.Item("EnableEvadeDistance").SetValue(true);
                menu.Item("TickLimiter").SetValue(new Slider(200, 0, 500));
                menu.Item("SpellDetectionTime").SetValue(new Slider(375, 0, 1000));
                menu.Item("ReactionTime").SetValue(new Slider(285, 0, 500));
                menu.Item("DodgeInterval").SetValue(new Slider(235, 0, 2000));
                menu.Item("FastEvadeActivationTime").SetValue(new Slider(0, 0, 500));
                menu.Item("SpellActivationTime").SetValue(new Slider(200, 0, 1000));
                menu.Item("RejectMinDistance").SetValue(new Slider(0, 0, 100));
                menu.Item("ExtraPingBuffer").SetValue(new Slider(65, 0, 200));
                menu.Item("ExtraCPADistance").SetValue(new Slider(0, 0, 150));
                menu.Item("ExtraSpellRadius").SetValue(new Slider(0, 0, 100));
                menu.Item("ExtraEvadeDistance").SetValue(new Slider(200, 0, 300));
                menu.Item("ExtraAvoidDistance").SetValue(new Slider(200, 0, 300));
                menu.Item("MinComfortZone").SetValue(new Slider(550, 0, 1000));
            }

            else if (mode == "Kurisu \u6a21\u5f0f")
            {
                ResetConfig(false);
                menu.Item("DodgeDangerous").SetValue(false);
                menu.Item("DodgeFOWSpells").SetValue(false);
                menu.Item("DodgeCircularSpells").SetValue(true);
                menu.Item("DodgeDangerousKeyEnabled").SetValue(true);
                menu.Item("HigherPrecision").SetValue(false);
                menu.Item("RecalculatePosition").SetValue(true);
                menu.Item("ContinueMovement").SetValue(true);
                menu.Item("CalculateWindupDelay").SetValue(true);
                menu.Item("CheckSpellCollision").SetValue(true);
                menu.Item("FastMovementBlock").SetValue(true);
                menu.Item("PreventDodgingUnderTower").SetValue(true);
                menu.Item("PreventDodgingNearEnemy").SetValue(true);
                menu.Item("AdvancedSpellDetection").SetValue(false);
                menu.Item("ClickOnlyOnce").SetValue(true);
                menu.Item("EnableEvadeDistance").SetValue(false);
                menu.Item("TickLimiter").SetValue(new Slider(100, 0, 500));
                menu.Item("SpellDetectionTime").SetValue(new Slider(0, 0, 1000));
                menu.Item("ReactionTime").SetValue(new Slider(0, 0, 500));
                menu.Item("DodgeInterval").SetValue(new Slider(0, 0, 2000));
                menu.Item("FastEvadeActivationTime").SetValue(new Slider(60, 0, 500));
                menu.Item("SpellActivationTime").SetValue(new Slider(200, 0, 1000));
                menu.Item("RejectMinDistance").SetValue(new Slider(10, 0, 100));
                menu.Item("ExtraPingBuffer").SetValue(new Slider(65, 0, 200));
                menu.Item("ExtraCPADistance").SetValue(new Slider(10, 0, 150));
                menu.Item("ExtraSpellRadius").SetValue(new Slider(0, 0, 100));
                menu.Item("ExtraEvadeDistance").SetValue(new Slider(165, 0, 300));
                menu.Item("ExtraAvoidDistance").SetValue(new Slider(60, 0, 300));
                menu.Item("MinComfortZone").SetValue(new Slider(420, 0, 1000));
            }

            else if (mode == "GuessWho \u6a21\u5f0f")
            {
                ResetConfig(false);
                menu.Item("DodgeDangerousKeyEnabled").SetValue(true);
                menu.Item("DodgeDangerousKey2").SetValue(new KeyBind(109, KeyBindType.Press));
                menu.Item("HigherPrecision").SetValue(true);
                menu.Item("PreventDodgingUnderTower").SetValue(true);
                menu.Item("ShowStatus").SetValue(false);
                menu.Item("DrawSpellPos").SetValue(true);
            }
        }

        private void OnLoadPingTesterChange(object sender, OnValueChangeEventArgs e)
        {
            e.Process = false;

            if (pingTester == null)
            {
                pingTester = new PingTester();
            }
        }

        private void OnLoadSpellTesterChange(object sender, OnValueChangeEventArgs e)
        {
            e.Process = false;

            if (spellTester == null)
            {
                spellTester = new SpellTester();
            }
        }

        private void Game_OnGameEnd(GameEndEventArgs args)
        {
            hasGameEnded = true;
        }

        private void Game_OnCastSpell(Spellbook spellbook, SpellbookCastSpellEventArgs args)
        {
            if (!spellbook.Owner.IsMe)
                return;

            var sData = spellbook.GetSpell(args.Slot);
            string name;

            if (SpellDetector.channeledSpells.TryGetValue(sData.Name, out name))
            {
                //Evade.isChanneling = true;
                //Evade.channelPosition = ObjectCache.myHeroCache.serverPos2D;
                lastStopEvadeTime = EvadeUtils.TickCount + ObjectCache.gamePing + 100;
            }

            //block spell commmands if evade spell just used
            if (EvadeSpell.lastSpellEvadeCommand != null && 
                EvadeSpell.lastSpellEvadeCommand.timestamp + ObjectCache.gamePing + 150 > EvadeUtils.TickCount)
            {
                args.Process = false;
            }

            lastSpellCast = args.Slot;
            lastSpellCastTime = EvadeUtils.TickCount;

            //moved from processPacket

            /*if (args.Slot == SpellSlot.Recall)
            {
                lastStopPosition = myHero.ServerPosition.To2D();
            }*/

            if (Situation.ShouldDodge())
            {
                if (isDodging && SpellDetector.spells.Any())
                {
                    foreach (KeyValuePair<String, SpellData> entry in SpellDetector.windupSpells)
                    {
                        SpellData spellData = entry.Value;

                        if (spellData.spellKey == args.Slot) //check if it's a spell that we should block
                        {
                            args.Process = false;
                            return;
                        }
                    }
                }
            }

            foreach (var evadeSpell in EvadeSpell.evadeSpells)
            {
                if (evadeSpell.isItem == false &&  evadeSpell.spellKey == args.Slot && evadeSpell.untargetable == false)
                {
                    if (evadeSpell.evadeType == EvadeType.Blink)
                    {
                        var dir = (args.StartPosition.To2D() - myHero.ServerPosition.To2D()).Normalized();

                        var end = myHero.ServerPosition.To2D() + dir * myHero.ServerPosition.To2D().Distance(Game.CursorPos.To2D());
                        if (evadeSpell.fixedRange || end.Distance(myHero.ServerPosition.To2D()) > evadeSpell.range)
                        {
                            end = myHero.ServerPosition.To2D() + dir * evadeSpell.range;
                        }

                        var posInfo = EvadeHelper.CanHeroWalkToPos(end, evadeSpell.speed, ObjectCache.gamePing, 0);
                        if (posInfo.posDangerCount < 1)
                        {
                            if (lastPosInfo != null)                              
                                lastPosInfo = posInfo;

                            if (lastPosInfo == null)
                                lastPosInfo = posInfo;

                            if (isDodging || EvadeUtils.TickCount < lastDodgingEndTime + 250)
                            {
                                EvadeCommand.MoveTo(Game.CursorPos.To2D());
                                lastStopEvadeTime = EvadeUtils.TickCount + ObjectCache.gamePing + 100;
                            }
                            return;
                        }
                    }

                    if (evadeSpell.evadeType == EvadeType.Dash)
                    {
                        var dashPos = args.StartPosition.To2D(); 

                        if (args.Target != null)
                        {
                            dashPos = args.Target.Position.To2D();
                        }

                        if (evadeSpell.fixedRange || dashPos.Distance(myHero.ServerPosition.To2D()) > evadeSpell.range)
                        {
                            var dir = (dashPos - myHero.ServerPosition.To2D()).Normalized();
                            dashPos = myHero.ServerPosition.To2D() + dir * evadeSpell.range;
                        }

                        var posInfo = EvadeHelper.CanHeroWalkToPos(dashPos, evadeSpell.speed, ObjectCache.gamePing, 0);
                        if (posInfo.posDangerLevel > 0)
                        {
                            args.Process = false;
                        }
                        else
                        {
                            if (isDodging || EvadeUtils.TickCount < lastDodgingEndTime + 500)
                            {
                                EvadeCommand.MoveTo(Game.CursorPos.To2D());
                                lastStopEvadeTime = EvadeUtils.TickCount + ObjectCache.gamePing + 100;
                            }

                            return;
                        }
                    }

                    lastPosInfo = PositionInfo.SetAllUndodgeable();
                    return;
                }
            }


        }

        private void Game_OnIssueOrder(Obj_AI_Base hero, PlayerIssueOrderEventArgs args)
        {
            if (!hero.IsMe)
                return;

            if (!Situation.ShouldDodge())
                return;

            if (args.Order == GameObjectOrder.MoveTo)
            {
                if (isDodging && SpellDetector.spells.Any())
                {
                    CheckHeroInDanger();

                    lastBlockedUserMoveTo = new EvadeCommand
                    {
                        order = EvadeOrderCommand.MoveTo,
                        targetPosition = args.TargetPosition.To2D(),
                        timestamp = EvadeUtils.TickCount,
                        isProcessed = false,
                    };

                    args.Process = false;
                }
                else
                {
                    var movePos = args.TargetPosition.To2D();
                    var extraDelay = ObjectCache.menuCache.cache["ExtraPingBuffer"].GetValue<Slider>().Value;
                
                    if (EvadeHelper.CheckMovePath(movePos, ObjectCache.gamePing + extraDelay))
                    {
                        /*if (ObjectCache.menuCache.cache["AllowCrossing"].GetValue<bool>())
                        {
                            var extraDelayBuffer = ObjectCache.menuCache.cache["ExtraPingBuffer"]
                                .GetValue<Slider>().Value + 30;
                            var extraDist = ObjectCache.menuCache.cache["ExtraCPADistance"]
                                .GetValue<Slider>().Value + 10;

                            var tPosInfo = EvadeHelper.CanHeroWalkToPos(movePos, ObjectCache.myHeroCache.moveSpeed, extraDelayBuffer + ObjectCache.gamePing, extraDist);

                            if (tPosInfo.posDangerLevel == 0)
                            {
                                lastPosInfo = tPosInfo;
                                return;
                            }
                        }*/

                        lastBlockedUserMoveTo = new EvadeCommand
                        {
                            order = EvadeOrderCommand.MoveTo,
                            targetPosition = args.TargetPosition.To2D(),
                            timestamp = EvadeUtils.TickCount,
                            isProcessed = false,
                        };

                        args.Process = false; //Block the command

                        if (EvadeUtils.TickCount - lastMovementBlockTime < 500 && lastMovementBlockPos.Distance(args.TargetPosition) < 100)
                        {
                            return;
                        }

                        lastMovementBlockPos = args.TargetPosition;
                        lastMovementBlockTime = EvadeUtils.TickCount;

                        var posInfo = EvadeHelper.GetBestPositionMovementBlock(movePos);
                        if (posInfo != null)
                        {
                            EvadeCommand.MoveTo(posInfo.position);
                        }
                        return;
                    }
                    else
                    {
                        lastBlockedUserMoveTo.isProcessed = true;
                    }
                }
            }
            else //need more logic
            {
                if (isDodging)
                {
                    args.Process = false; //Block the command
                }
                else
                {
                    if (args.Order == GameObjectOrder.AttackUnit)
                    {
                        var target = args.Target;
                        if (target != null && target.IsValid<Obj_AI_Base>())
                        {
                            var baseTarget = target as Obj_AI_Base;
                            if (ObjectCache.myHeroCache.serverPos2D.Distance(baseTarget.ServerPosition.To2D()) >
                                myHero.AttackRange + ObjectCache.myHeroCache.boundingRadius + baseTarget.BoundingRadius)
                            {
                                var movePos = args.TargetPosition.To2D();
                                var extraDelay = ObjectCache.menuCache.cache["ExtraPingBuffer"].GetValue<Slider>().Value;
                                if (EvadeHelper.CheckMovePath(movePos, ObjectCache.gamePing + extraDelay))
                                {
                                    args.Process = false; //Block the command
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            if (args.Process == true)
            {
                lastIssueOrderGameTime = Game.Time * 1000;
                lastIssueOrderTime = EvadeUtils.TickCount;
                lastIssueOrderArgs = args;

                if (args.Order == GameObjectOrder.MoveTo)
                {
                    lastMoveToPosition = args.TargetPosition.To2D();
                    lastMoveToServerPos = myHero.ServerPosition.To2D();
                }

                if (args.Order == GameObjectOrder.Stop)
                {
                    lastStopPosition = myHero.ServerPosition.To2D();
                }
            }
        }

        private void Orbwalking_BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (isDodging)
            {
                args.Process = false; //Block orbwalking
            }
        }

        private void Game_OnProcessSpell(Obj_AI_Base hero, GameObjectProcessSpellCastEventArgs args)
        {
            if (!hero.IsMe)
            {
                return;
            }

            if (hero.NetworkId != myHero.NetworkId)
            {
                Console.WriteLine("IS NOT ME");
            }

            /*if (args.SData.Name.Contains("Recall"))
            {
                var distance = lastStopPosition.Distance(args.Start.To2D());
                float moveTime = 1000 * distance / myHero.MoveSpeed;

                Console.WriteLine("Extra dist: " + distance + " Extra Delay: " + moveTime);
            }*/

            string name;
            if (SpellDetector.channeledSpells.TryGetValue(args.SData.Name, out name))
            {
                Evade.isChanneling = true;
                Evade.channelPosition = myHero.ServerPosition.To2D();
            }

            if (ObjectCache.menuCache.cache["CalculateWindupDelay"].GetValue<bool>())
            {
                var castTime = (hero.Spellbook.CastTime - Game.Time) * 1000;

                if (castTime > 0 && !Orbwalking.IsAutoAttack(args.SData.Name)
                    && Math.Abs(castTime - myHero.AttackCastDelay * 1000) > 1)
                {
                    Evade.lastWindupTime = EvadeUtils.TickCount + castTime - Game.Ping / 2;

                    if (Evade.isDodging)
                    {
                        SpellDetector_OnProcessDetectedSpells(); //reprocess
                    }
                }
            }


        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            try
            {
                ObjectCache.myHeroCache.UpdateInfo();
                CheckHeroInDanger();

                if (isChanneling && channelPosition.Distance(ObjectCache.myHeroCache.serverPos2D) > 50
                    && !myHero.IsChannelingImportantSpell())
                {
                    isChanneling = false;
                }

                if (ObjectCache.menuCache.cache["ResetConfig"].GetValue<bool>())
                {
                    ResetConfig();
                    menu.Item("ResetConfig").SetValue(false);
                }

                var limitDelay = ObjectCache.menuCache.cache["TickLimiter"].GetValue<Slider>().Value; //Tick limiter                
                if (EvadeHelper.fastEvadeMode || EvadeUtils.TickCount - lastTickCount > limitDelay&& EvadeUtils.TickCount > lastStopEvadeTime)
                {
                    DodgeSkillShots(); //walking           
                    ContinueLastBlockedCommand();
                    lastTickCount = EvadeUtils.TickCount;
                }

                EvadeSpell.UseEvadeSpell(); //using spells
                CheckDodgeOnlyDangerous();
                RecalculatePath();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private void RecalculatePath()
        {
            if (ObjectCache.menuCache.cache["RecalculatePosition"].GetValue<bool>() && isDodging)//recheck path
            {
                if (lastPosInfo != null && !lastPosInfo.recalculatedPath)
                {
                    var path = myHero.Path;
                    if (path.Length > 0)
                    {
                        var movePos = path.Last().To2D();

                        if (movePos.Distance(lastPosInfo.position) < 5) //more strict checking
                        {
                            var posInfo = EvadeHelper.CanHeroWalkToPos(movePos, ObjectCache.myHeroCache.moveSpeed, 0, 0, false);
                            if (posInfo.posDangerCount > lastPosInfo.posDangerCount)
                            {
                                lastPosInfo.recalculatedPath = true;

                                if (EvadeSpell.PreferEvadeSpell())
                                {
                                    lastPosInfo = PositionInfo.SetAllUndodgeable();
                                }
                                else
                                {
                                    var newPosInfo = EvadeHelper.GetBestPosition();
                                    if (newPosInfo.posDangerCount < posInfo.posDangerCount)
                                    {
                                        lastPosInfo = newPosInfo;
                                        CheckHeroInDanger();
                                        DodgeSkillShots();
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ContinueLastBlockedCommand()
        {
            if (ObjectCache.menuCache.cache["ContinueMovement"].GetValue<bool>()
                && Situation.ShouldDodge())
            {
                var movePos = lastBlockedUserMoveTo.targetPosition;
                var extraDelay = ObjectCache.menuCache.cache["ExtraPingBuffer"].GetValue<Slider>().Value;

                if (isDodging == false && lastBlockedUserMoveTo.isProcessed == false
                    && EvadeUtils.TickCount - lastEvadeCommand.timestamp > ObjectCache.gamePing + extraDelay
                    && EvadeUtils.TickCount - lastBlockedUserMoveTo.timestamp < 1500)
                {
                    movePos = movePos + (movePos - ObjectCache.myHeroCache.serverPos2D).Normalized()
                        * EvadeUtils.random.NextFloat(1, 65);

                    if (!EvadeHelper.CheckMovePath(movePos, ObjectCache.gamePing + extraDelay))
                    {
                        //Console.WriteLine("Continue Movement");
                        //myHero.IssueOrder(GameObjectOrder.MoveTo, movePos.To3D());
                        EvadeCommand.MoveTo(movePos);
                        lastBlockedUserMoveTo.isProcessed = true;
                    }
                }
            }
        }

        private void CheckHeroInDanger()
        {
            bool playerInDanger = false;

            foreach (KeyValuePair<int, Spell> entry in SpellDetector.spells)
            {
                Spell spell = entry.Value;

                if (lastPosInfo != null && lastPosInfo.dodgeableSpells.Contains(spell.spellID))
                {
                    if (myHero.ServerPosition.To2D().InSkillShot(spell, ObjectCache.myHeroCache.boundingRadius))
                    {
                        playerInDanger = true;
                        break;
                    }

                    if (ObjectCache.menuCache.cache["EnableEvadeDistance"].GetValue<bool>() &&
                        EvadeUtils.TickCount < lastPosInfo.endTime)
                    {
                        playerInDanger = true;
                        break;
                    }
                }
            }

            if (isDodging && !playerInDanger)
            {
                lastDodgingEndTime = EvadeUtils.TickCount;
            }

            if (isDodging == false && !Situation.ShouldDodge())
                return;

            isDodging = playerInDanger;
        }

        private void DodgeSkillShots()
        {
            if (!Situation.ShouldDodge())
            {
                isDodging = false;
                return;
            }

            /*
            if (isDodging && playerInDanger == false) //serverpos test
            {
                myHero.IssueOrder(GameObjectOrder.HoldPosition, myHero, false);
            }*/

            if (isDodging)
            {
                if (lastPosInfo != null)
                {
                    /*foreach (KeyValuePair<int, Spell> entry in SpellDetector.spells)
                    {
                        Spell spell = entry.Value;

                        Console.WriteLine("" + (int)(TickCount-spell.startTime));
                    }*/


                    Vector2 lastBestPosition = lastPosInfo.position;

                    if (ObjectCache.menuCache.cache["ClickOnlyOnce"].GetValue<bool>() == false
                        || !(myHero.Path.Count() > 0 && lastPosInfo.position.Distance(myHero.Path.Last().To2D()) < 5))
                    //|| lastPosInfo.timestamp > lastEvadeOrderTime)
                    {
                        EvadeCommand.MoveTo(lastBestPosition);
                        lastEvadeOrderTime = EvadeUtils.TickCount;
                    }
                }
            }
            else //if not dodging
            {
                //Check if hero will walk into a skillshot
                var path = myHero.Path;
                if (path.Length > 0)
                {
                    var movePos = path[path.Length - 1].To2D();

                    if (EvadeHelper.CheckMovePath(movePos))
                    {
                        /*if (ObjectCache.menuCache.cache["AllowCrossing"].GetValue<bool>())
                        {
                            var extraDelayBuffer = ObjectCache.menuCache.cache["ExtraPingBuffer"]
                                .GetValue<Slider>().Value + 30;
                            var extraDist = ObjectCache.menuCache.cache["ExtraCPADistance"]
                                .GetValue<Slider>().Value + 10;

                            var tPosInfo = EvadeHelper.CanHeroWalkToPos(movePos, ObjectCache.myHeroCache.moveSpeed, extraDelayBuffer + ObjectCache.gamePing, extraDist);

                            if (tPosInfo.posDangerLevel == 0)
                            {
                                lastPosInfo = tPosInfo;
                                return;
                            }
                        }*/

                        var posInfo = EvadeHelper.GetBestPositionMovementBlock(movePos);
                        if (posInfo != null)
                        {
                            EvadeCommand.MoveTo(posInfo.position);
                        }
                        return;
                    }
                }
            }
        }

        public void CheckLastMoveTo()
        {
            if (EvadeHelper.fastEvadeMode || ObjectCache.menuCache.cache["FastMovementBlock"].GetValue<bool>())
            {
                if (isDodging == false)
                {
                    if (lastIssueOrderArgs != null && lastIssueOrderArgs.Order == GameObjectOrder.MoveTo)
                    {
                        if (Game.Time * 1000 - lastIssueOrderGameTime < 500)
                        {
                            Game_OnIssueOrder(myHero, lastIssueOrderArgs);
                            lastIssueOrderArgs = null;
                        }
                    }
                }
            }
        }

        public static bool isDodgeDangerousEnabled()
        {
            if (ObjectCache.menuCache.cache["DodgeDangerous"].GetValue<bool>() == true)
            {
                return true;
            }

            if (ObjectCache.menuCache.cache["DodgeDangerousKeyEnabled"].GetValue<bool>() == true)
            {
                if (ObjectCache.menuCache.cache["DodgeDangerousKey"].GetValue<KeyBind>().Active == true
                || ObjectCache.menuCache.cache["DodgeDangerousKey2"].GetValue<KeyBind>().Active == true)
                    return true;
            }

            return false;
        }

        public static void CheckDodgeOnlyDangerous() //Dodge only dangerous event
        {
            bool bDodgeOnlyDangerous = isDodgeDangerousEnabled();

            if (dodgeOnlyDangerous == false && bDodgeOnlyDangerous)
            {
                spellDetector.RemoveNonDangerousSpells();
                dodgeOnlyDangerous = true;
            }
            else
            {
                dodgeOnlyDangerous = bDodgeOnlyDangerous;
            }
        }

        public static void SetAllUndodgeable()
        {
            lastPosInfo = PositionInfo.SetAllUndodgeable();
        }

        private void SpellDetector_OnProcessDetectedSpells()
        {
            ObjectCache.myHeroCache.UpdateInfo();

            if (ObjectCache.menuCache.cache["DodgeSkillShots"].GetValue<KeyBind>().Active == false)
            {
                lastPosInfo = PositionInfo.SetAllUndodgeable();
                EvadeSpell.UseEvadeSpell();
                return;
            }

            if (ObjectCache.myHeroCache.serverPos2D.CheckDangerousPos(0)
                || ObjectCache.myHeroCache.serverPos2DExtra.CheckDangerousPos(0))
            {
                if (EvadeSpell.PreferEvadeSpell())
                {
                    lastPosInfo = PositionInfo.SetAllUndodgeable();
                }
                else
                {

                    var posInfo = EvadeHelper.GetBestPosition();

                    var calculationTimer = EvadeUtils.TickCount;
                    var caculationTime = EvadeUtils.TickCount - calculationTimer;

                    //computing time
                    /*if (numCalculationTime > 0)
                    {
                        sumCalculationTime += caculationTime;
                        avgCalculationTime = sumCalculationTime / numCalculationTime;
                    }
                    numCalculationTime += 1;*/

                    //Console.WriteLine("CalculationTime: " + caculationTime);

                    /*if (EvadeHelper.GetHighestDetectedSpellID() > EvadeHelper.GetHighestSpellID(posInfo))
                    {
                        return;
                    }*/
                    if (posInfo != null)
                    {
                        lastPosInfo = posInfo.CompareLastMovePos();

                        var travelTime = ObjectCache.myHeroCache.serverPos2DPing.Distance(lastPosInfo.position) / myHero.MoveSpeed;

                        lastPosInfo.endTime = EvadeUtils.TickCount + travelTime * 1000 - 100;
                    }

                    CheckHeroInDanger();
                    DodgeSkillShots(); //walking
                    CheckLastMoveTo();
                    EvadeSpell.UseEvadeSpell(); //using spells
                }
            }
            else
            {
                lastPosInfo = PositionInfo.SetAllDodgeable();
                CheckLastMoveTo();
            }


            //Console.WriteLine("SkillsDodged: " + lastPosInfo.dodgeableSpells.Count + " DangerLevel: " + lastPosInfo.undodgeableSpells.Count);            
        }
    }
}
