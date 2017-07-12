namespace Skin_Series
{
    using System;
    using EloBuddy.SDK.Events;

    public static class Program
    {

        private static void Main(string[] args)
        {
            Loading.OnLoadingComplete += OnLoadingComplete;
        }

        private static void OnLoadingComplete(EventArgs args)
        {
            Core.Boot.LoadBoot();
        }
    }
}