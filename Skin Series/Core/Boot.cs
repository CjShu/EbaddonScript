namespace Skin_Series.Core
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using EloBuddy;
    using EloBuddy.Sandbox;
    using EloBuddy.SDK;
    using SharpDX;
    using Common;

    // ReSharper disable once InconsistentNaming
    internal class Boot
    {
        public static bool MenuLoaded { get; set; }

        public static void LoadBoot()
        {
            Core.DelayAction(
                () =>
                    {
                        MenuManager.LoadMenu();
                        MenuLoaded = true;
                    },
                250);
        }
    }
}
