using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using EloBuddy;
using TW.Common;
using TW.Common.Extensions;
using SharpDX;

namespace ezEvade
{
    class PingTester
    {
        public static Menu testMenu;

        private static AIHeroClient myHero { get { return ObjectManager.Player; } }

        private static float lastTimerCheck = 0;
        private static bool lastRandomMoveCoeff = false;

        private static float sumPingTime = 0;
        private static float averagePingTime = ObjectCache.gamePing;
        private static int testCount = 0;
        private static int autoTestCount = 0;
        private static float maxPingTime = ObjectCache.gamePing;

        private static bool autoTestPing = false;

        private static EvadeCommand lastTestMoveToCommand;

        public PingTester()
        {
            Game.OnUpdate += Game_OnGameUpdate;

            testMenu = new Menu("Ping \u6e2c\u8a66", "PingTest", true);
            testMenu.AddItem(new MenuItem("AutoSetPing", "\u81ea\u52d5\u8a2d\u5b9a Ping").SetValue(false));
            testMenu.AddItem(new MenuItem("TestMoveTime", "\u6e2c\u8a66\u79fb\u52d5\u6642\u9593 Ping").SetValue(false));
            testMenu.AddItem(new MenuItem("SetMaxPing", "\u8a2d\u5b9a\u6700\u5927 Ping").SetValue(false));
            testMenu.AddItem(new MenuItem("SetAvgPing", "\u8a2d\u5b9a\u5e73\u5747 Ping").SetValue(false));
            testMenu.AddItem(new MenuItem("Test20MoveTime", "\u6e2c\u8a66\u79fb\u52d5\u6642\u9593 x20").SetValue(false));
            testMenu.AddItem(new MenuItem("PrintResults", "\u8ddf\u8e64\u7d50\u679c").SetValue(false));
            testMenu.AddToMainMenu();
        }

        private void IssueTestMove(int recursionCount)
        {

            var movePos = ObjectCache.myHeroCache.serverPos2D;

            Random rand = new Random();

            lastRandomMoveCoeff = !lastRandomMoveCoeff;
            if (lastRandomMoveCoeff)
            {
                movePos.X += 65 + rand.Next(0, 20);
            }
            else
            {
                movePos.X -= 65 + rand.Next(0, 20);
            }

            lastTestMoveToCommand = new EvadeCommand
            {
                order = EvadeOrderCommand.MoveTo,
                targetPosition = movePos,
                timestamp = EvadeUtils.TickCount,
                isProcessed = false
            };
            Player.IssueOrder(GameObjectOrder.MoveTo, movePos.To3D(), true);

            if (recursionCount > 1)
            {
                DelayAction.Add(500, () => IssueTestMove(recursionCount - 1));
            }

        }

        private void SetPing(int ping)
        {
            Evade.menu.Item("ExtraPingBuffer").SetValue(new Slider(ping, 0, 200));
        }

        private void Game_OnGameUpdate(EventArgs args)
        {
            if (testMenu.Item("AutoSetPing").GetValue<bool>())
            {
                Console.WriteLine("Testing Ping...Please wait 10 seconds");

                int testAmount = 20;

                testMenu.Item("AutoSetPing").SetValue(false);
                IssueTestMove(testAmount);
                autoTestCount = testCount + testAmount;
                autoTestPing = true;
                
            }

            if (testMenu.Item("PrintResults").GetValue<bool>())
            {
                testMenu.Item("PrintResults").SetValue(false);

                Console.WriteLine("Average Extra Delay: " + averagePingTime);
                Console.WriteLine("Max Extra Delay: " + maxPingTime);
            }

            if (autoTestPing == true && testCount >= autoTestCount)
            {
                Console.WriteLine("Auto Set Ping Complete");

                Console.WriteLine("Average Extra Delay: " + averagePingTime);
                Console.WriteLine("Max Extra Delay: " + maxPingTime);

                SetPing((int)(averagePingTime+10));
                Console.WriteLine("Set Average extra ping + 10: " + (averagePingTime+10));

                autoTestPing = false;
            }

            if (testMenu.Item("TestMoveTime").GetValue<bool>())
            {
                testMenu.Item("TestMoveTime").SetValue(false);
                IssueTestMove(1);
            }


            if (testMenu.Item("Test20MoveTime").GetValue<bool>())
            {
                testMenu.Item("Test20MoveTime").SetValue(false);
                IssueTestMove(20);
            }

            if (testMenu.Item("SetMaxPing").GetValue<bool>())
            {
                testMenu.Item("SetMaxPing").SetValue(false);

                if (testCount < 10)
                {
                    Console.WriteLine("Please test 10 times before setting ping");
                }
                else
                {
                    Console.WriteLine("Set Max extra ping: " + maxPingTime);
                    SetPing((int)maxPingTime);
                }                
            }

            if (testMenu.Item("SetAvgPing").GetValue<bool>())
            {
                testMenu.Item("SetAvgPing").SetValue(false);

                if (testCount < 10)
                {
                    Console.WriteLine("Please test 10 times before setting ping");
                }
                else
                {
                    Console.WriteLine("Set Average extra ping: " + averagePingTime);
                    SetPing((int)averagePingTime);
                }                         
            }

            if (myHero.IsMoving)
            {
                if (lastTestMoveToCommand != null && lastTestMoveToCommand.isProcessed == false && lastTestMoveToCommand.order == EvadeOrderCommand.MoveTo)
                {
                    var path = myHero.Path;

                    if (path.Length > 0)
                    {
                        var movePos = path[path.Length - 1].To2D();

                        if (movePos.Distance(lastTestMoveToCommand.targetPosition) < 10)
                        {
                            float moveTime = EvadeUtils.TickCount - lastTestMoveToCommand.timestamp - ObjectCache.gamePing;
                            Console.WriteLine("Extra Delay: " + moveTime);
                            lastTestMoveToCommand.isProcessed = true;

                            sumPingTime += moveTime;
                            testCount += 1;
                            averagePingTime = sumPingTime / testCount;
                            maxPingTime = Math.Max(maxPingTime, moveTime);
                        }
                    }

                }
            }
        }
    }
}
