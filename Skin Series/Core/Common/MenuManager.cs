namespace Skin_Series.Core.Common
{
    using System;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;

    internal static class MenuManager
    {
        internal static Menu SkinMenu { get; set; }

        private static readonly List<ExtensionBase> Extensions = new List<ExtensionBase>();

        internal static void LoadMenu()
        {
            SkinMenu = MainMenu.AddMenu("造型皮膚 系列", "Skin_Series.Extensions");

            foreach (var source in Assembly.GetAssembly(typeof(ExtensionBase)).GetTypes()
                .Where(x => x.IsSubclassOf(typeof(ExtensionBase)) && x.IsSealed))
            {
                var property = source.GetProperty("EnabledByDefault");

                bool enabledByDefault;

                if (property == null)
                {
                    enabledByDefault = false;
                }
                else enabledByDefault = (bool)property.GetValue(source);

                var menuItem = SkinMenu.Add(
                    "MenuManager.SkinMenu." + source.Name,
                    new CheckBox("啟動造型 ", enabledByDefault));

                if (menuItem.CurrentValue)
                {
                    if (Extensions.All(x => x.Name != source.Name))
                    {
                        var instance = Activator.CreateInstance(source);

                        Extensions.Add(instance as ExtensionBase);

                        source.GetMethod("Load").Invoke(instance, null);
                    }
                }

                menuItem.OnValueChange += (sender, args) =>
                {
                    if (args.NewValue)
                    {
                        if (Extensions.Any(x => x.Name == source.Name)) return;

                        var instance = Activator.CreateInstance(source);

                        if (instance == null) return;

                        Extensions.Add(instance as ExtensionBase);

                        source.GetMethod("Load").Invoke(instance, null);
                    }
                    else if (Extensions.Any(x => x.Name == source.Name))
                    {
                        var extension = Extensions.FirstOrDefault(x => x.Name == source.Name);

                        if (extension == null) return;

                        extension.Dispose();
                        Extensions.RemoveAll(x => x.Name == source.Name);
                    }
                };
            }

            SkinMenu.AddGroupLabel("---------------------------------------------------------------------------------------------------------------");
            SkinMenu.AddLabel("Support Hero : Ahri Akali Anivia Ashe Caitlyn Ezreal Jhin Jinx Sivir Syndra Vayne Xayah Yasuo");
            SkinMenu.AddSeparator(5);
            SkinMenu.AddLabel("Support Hero : Zed Katarina Amumu Maokai Riven Orianna Rakan Camille Kayn");
            SkinMenu.AddSeparator(5);
            SkinMenu.AddGroupLabel("Skin Series !");
            SkinMenu.AddGroupLabel("=============================================================");
            SkinMenu.AddGroupLabel("造型支持英雄: 阿璃 阿卡莉 艾妮維亞 艾希 凱特琳 卡蜜兒 伊澤瑞爾 燼 茂凱");
            SkinMenu.AddGroupLabel("造型支持英雄: 吉茵珂絲 奧莉安娜 銳空 希維爾 星朵拉 汎 犽宿 剎雅 劫 雷玟");
            SkinMenu.AddGroupLabel("造型支持英雄: 卡特蓮娜 阿姆姆 慨影");
        }
    }
}