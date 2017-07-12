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
        }
    }
}