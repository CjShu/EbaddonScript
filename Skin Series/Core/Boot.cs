namespace Skin_Series.Core
{
    using EloBuddy;
    using EloBuddy.SDK;
    using Common;

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

                        MessageManager.PrintMessage(
                            $"<b><font color=\"#C4E1FF\">{Player.Instance.ChampionName}</font></b> &#x6CE8;&#x5165;&#x6210;&#x529F; </font><font color='#AAAAFF'>by CjShu</font>");

                    },
                250);
        }
    }
}