﻿namespace Skin_Series.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using EloBuddy;
    using EloBuddy.SDK.Menu;
    using EloBuddy.SDK.Menu.Values;
    using Common;

    internal sealed class SkinHack : ExtensionBase
    {
        private Menu SkinHackMenu { get; set; }

        public override bool IsEnabled { get; set; }

        public static bool EnabledByDefault { get; set; } = true;

        public override string Name { get; } = "SkinHack";

        public Dictionary<string, byte> Skins { get; private set; }
        public Dictionary<KeyValuePair<Champion, byte>, Dictionary<string, byte>> Chromas { get; private set; }
        public Dictionary<Champion, string> BaseSkinNames { get; private set; }

        public ComboBox SkinId { get; set; }
        public Slider ChromaId { get; set; }

        public byte LoadSkinId { get; private set; }

        public byte CurrentSkin { get; set; }

        public override void Load()
        {
            this.LoadSkinId = (byte)Player.Instance.SkinId;

            this.IsEnabled = true;

            this.BaseSkinNames = new Dictionary<Champion, string>
            {
                [Champion.Ahri] = "Ahri",
                [Champion.Akali] = "Akali",
                [Champion.Anivia] = "Anivia",
                [Champion.Ashe] = "Ashe",
                [Champion.Caitlyn] = "Caitlyn",
                [Champion.Ezreal] = "Ezreal",
                [Champion.Jhin] = "Jhin",
                [Champion.Jinx] = "Jinx",
                [Champion.Sivir] = "Sivir",
                [Champion.Syndra] = "Syndra",
                [Champion.Vayne] = "Vayne",
                [Champion.Xayah] = "Xayah",
                [Champion.Yasuo] = "Yasuo"
            };

            this.Chromas = new Dictionary<KeyValuePair<Champion, byte>, Dictionary<string, byte>>
            {
                {new KeyValuePair<Champion, byte>(Champion.Ezreal, 7), new Dictionary<string, byte>
                    {
                        {"Amethyst", 7},
                        {"Meteorite", 10},
                        {"Obsidian", 11},
                        {"Pearl", 12},
                        {"Rose", 13},
                        {"Quartz", 14},
                        {"Ruby", 15},
                        {"Sandstone", 16},
                        {"Striped", 17}
                    }
                },
                {new KeyValuePair<Champion, byte>(Champion.Caitlyn, 0), new Dictionary<string, byte>
                    {
                        {"Default", 0},
                        {"Pink", 7},
                        {"Green", 8},
                        {"Blue", 9}
                    }
                },
                {new KeyValuePair<Champion, byte>(Champion.Vayne, 3), new Dictionary<string, byte>
                    {
                        {"Default", 3},
                        {"Green", 7},
                        {"Red", 8},
                        {"Silver", 9}
                    }
                },
            };

            var skin = new SkinData(Player.Instance.ChampionName);
            this.Skins = skin.ToDictionary();

            if (!MenuManager.SkinMenu.SubMenus.Any(x => x.UniqueMenuId.Contains("Extensions.SkinHack")))
            {
                if (!MainMenu.IsOpen)
                {
                    this.SkinHackMenu = MenuManager.SkinMenu.AddSubMenu("造型皮膚", "Extension.SkinHack");
                    this.BuildMenu();
                }
                else MainMenu.OnClose += this.MainMenu_OnClose;
            }
            else
            {
                var subMenu = MenuManager.SkinMenu.SubMenus.Find(x => x.UniqueMenuId.Contains("Extension.SkinHack"));

                if (subMenu?["SkinId." + Player.Instance.ChampionName] == null)
                    return;

                this.SkinId = subMenu["SkinId." + Player.Instance.ChampionName].Cast<ComboBox>();
                this.ChromaId = subMenu["ChromaId." + Player.Instance.ChampionName].Cast<Slider>();

                subMenu["SkinId." + Player.Instance.ChampionName].Cast<ComboBox>().OnValueChange += this.SkinId_OnValueChange;
                subMenu["ChromaId." + Player.Instance.ChampionName].Cast<Slider>().OnValueChange += this.ChromaId_OnValueChange;

                this.UpdateChromaSlider(this.SkinId.CurrentValue);

                if (this.HasChromaPack(this.SkinId.CurrentValue))
                {
                    this.ChangeSkin(this.SkinId.CurrentValue, this.ChromaId.CurrentValue);
                }
                else
                {
                    this.ChangeSkin(this.SkinId.CurrentValue);
                }
            }
        }

        private void MainMenu_OnClose(object sender, EventArgs args)
        {
            if (MenuManager.SkinMenu.SubMenus.Any(x => x.UniqueMenuId.Contains("Extension.SkinHack")))
            {
                return;
            }

            this.SkinHackMenu = MenuManager.SkinMenu.AddSubMenu("造型皮膚", "Extension.SkinHack");
            this.BuildMenu();

            MainMenu.OnClose -= this.MainMenu_OnClose;
        }

        private void BuildMenu()
        {
            var skins = this.Skins.Select(x => x.Key)
                .ToList();

            if (!skins.Any())
                return;

            this.SkinHackMenu.AddGroupLabel("造型 皮膚 設置 : ");

            this.SkinId = this.SkinHackMenu.Add("SkinId." + Player.Instance.ChampionName, new ComboBox("造型皮膚 : ", skins));

            if (this.LoadSkinId != 0)
            {
                this.SkinId.CurrentValue = this.LoadSkinId;
            }

            this.SkinHackMenu.AddSeparator(5);

            this.BuildChroma();
        }

        private void BuildChroma()
        {
            this.ChromaId = this.SkinHackMenu.Add("ChromaId." + Player.Instance.ChampionName, new Slider("顏色濃度 : "));
            this.ChromaId.IsVisible = false;
            this.ChromaId.OnValueChange += this.ChromaId_OnValueChange;
            this.SkinId.OnValueChange += this.SkinId_OnValueChange;

            if (this.HasChromaPack(this.SkinId.CurrentValue))
            {
                var dictionary = this.GetChromaList(this.SkinId.CurrentValue);

                if (dictionary == null)
                {
                    this.ChangeSkin(this.SkinId.CurrentValue);

                    return;
                }

                var maxValue = dictionary.Select(x => x.Key).Count();

                this.ChromaId.MaxValue = maxValue - 1;

                this.ChromaId.DisplayName = this.GetChromaName(this.SkinId.CurrentValue, this.ChromaId.CurrentValue);

                this.ChromaId.IsVisible = true;

                if (Player.Instance.SkinId == 0)
                    this.ChangeSkin(this.SkinId.CurrentValue, this.ChromaId.CurrentValue);
            }
            else if (Player.Instance.SkinId == 0)
            {
                this.ChangeSkin(this.SkinId.CurrentValue);
            }
        }

        private void ChromaId_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            var currentId = this.SkinId.CurrentValue;

            this.ChromaId.DisplayName = this.GetChromaName(this.SkinId.CurrentValue, this.ChromaId.CurrentValue);

            this.ChangeSkin(currentId, args.NewValue);
        }

        private void UpdateChromaSlider(int id)
        {
            var dictionary = this.GetChromaList(id);

            if (dictionary == null)
            {
                this.ChromaId.IsVisible = false;
                return;
            }

            var maxValue = dictionary.Select(x => x.Key).Count();

            this.ChromaId.MaxValue = maxValue - 1;

            this.ChromaId.DisplayName = this.GetChromaName(this.SkinId.CurrentValue, this.ChromaId.CurrentValue);

            this.ChromaId.IsVisible = true;
        }

        private void SkinId_OnValueChange(ValueBase<int> sender, ValueBase<int>.ValueChangeArgs args)
        {
            if (this.HasChromaPack(args.NewValue))
            {
                this.UpdateChromaSlider(args.NewValue);

                this.ChangeSkin(args.NewValue, this.ChromaId.CurrentValue);
                return;
            }

            this.ChromaId.IsVisible = false;

            this.ChangeSkin(args.NewValue);
        }

        private bool HasChromaPack(int id)
            => (this.Chromas != null) && this.Chromas.ContainsKey(new KeyValuePair<Champion, byte>(Player.Instance.Hero, (byte)id));

        private string GetChromaName(int id, int chromaId)
        {
            if ((this.Chromas == null) || !this.Chromas.ContainsKey(new KeyValuePair<Champion, byte>(Player.Instance.Hero, (byte)id)))
                return string.Empty;

            var dictionary = this.GetChromaList(id);
            var baseSkinName = this.Skins.FirstOrDefault(x => x.Value == id).Key;

            if (dictionary == null)
                return baseSkinName;

            var chromaIdT = dictionary.ElementAtOrDefault(chromaId).Key;

            return chromaIdT != default(string) ? $"{baseSkinName} : {chromaIdT} chroma" : baseSkinName;
        }

        private Dictionary<string, byte> GetChromaList(int id)
            =>
                !this.HasChromaPack(id)
                    ? null
                    : this.Chromas.FirstOrDefault(x => (x.Key.Key == Player.Instance.Hero) && (x.Key.Value == id)).Value;

        private void ChangeSkin(int id, int? chromaId = null)
        {
            if (!this.IsEnabled)
                return;

            var skins = this.Skins;

            if (skins == null)
            {
                return;
            }

            var skinId = skins.ElementAtOrDefault(id).Value;

            if (chromaId.HasValue && this.HasChromaPack(id))
            {
                var dictionary = this.GetChromaList(id);

                if (dictionary != null)
                {
                    var chromaIdT = dictionary.ElementAtOrDefault(chromaId.Value).Value;

                    if (chromaIdT != 0)
                    {
                        this.SetSkin(chromaIdT);
                        return;
                    }
                }
            }

            this.SetSkin(skinId);

            this.CurrentSkin = skinId;
        }

        private void SetSkin(int id)
        {
            if (Player.Instance.Model.Equals(Player.Instance.BaseSkinName))
            {
                Player.Instance.SetSkinId(id);
            }
            else Player.Instance.SetSkin(this.BaseSkinNames[Player.Instance.Hero], id);
        }

        public override void Dispose()
        {
            this.IsEnabled = false;

            this.SkinId.OnValueChange -= this.SkinId_OnValueChange;
            this.ChromaId.OnValueChange -= this.ChromaId_OnValueChange;

            MainMenu.OnClose -= this.MainMenu_OnClose;

            this.SetSkin(this.LoadSkinId);
        }

        public class SkinData
        {
            public string DDragonVersion { get; private set; }
            public Skins SkinsData { get; private set; }
            public string ChampionName { get; }
            private string Data { get; set; }

            public SkinData(string championName)
            {
                this.ChampionName = championName;

                try
                {
                    Task.Run(() => this.DownloadData()).Wait(1500);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }

            private void DownloadData()
            {
                try
                {
                    using (var webClient = new WebClient())
                    {
                        this.DDragonVersion = (string)JObject.Parse(webClient.DownloadString(new Uri("http://ddragon.leagueoflegends.com/realms/na.json"))).Property("dd");
                        this.Data = webClient.DownloadString($"http://ddragon.leagueoflegends.com/cdn/{DDragonVersion}/data/en_US/champion/{ChampionName}.json");
                    }

                    var parsedObject = JObject.Parse(this.Data);
                    var data = parsedObject["data"][this.ChampionName];

                    this.SkinsData = data.ToObject<Skins>();
                }
                catch (Exception exception)
                {
                    var ex = exception as WebException;

                    Console.WriteLine(ex != null
                                          ? $"Couldn't load skinhack a WebException occured\nStatus : {ex.Status} | Message : {ex.Message}{Environment.NewLine}"
                                          : $"Couldn't load skinhack an exception occured\n{exception}{Environment.NewLine}");
                }
            }

            public Dictionary<string, byte> ToDictionary()
            {
                var output = new Dictionary<string, byte>();

                try
                {
                    foreach (var skin in SkinsData.SkinsInfos)
                    {
                        output[skin.SkinName.ToSkin()] = (byte)skin.SkinId;
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
                return output;
            }

            public class SkinInfo
            {
                [JsonProperty(PropertyName = "id")]
                public string GameSkinId { get; set; }

                [JsonProperty(PropertyName = "num")]
                public int SkinId { get; set; }

                [JsonProperty(PropertyName = "name")]
                public string SkinName { get; set; }

                [JsonProperty(PropertyName = "chromas")]
                public bool HasChromas { get; set; }
            }

            public class Skins
            {
                [JsonProperty(PropertyName = "skins")]
                public SkinInfo[] SkinsInfos { get; set; }
            }
        }

        public class WebService
        {
            public int Timeout { get; set; }

            public WebService(int timeout = 2000)
            {
                this.Timeout = timeout;
            }

            public string SendRequest(Uri uri)
            {
                var request = WebRequest.Create(uri);

                request.Timeout = this.Timeout;

                try
                {
                    using (var result = request.GetResponse())
                    {
                        using (var response = result as HttpWebResponse)
                        {
                            if ((response == null) || (response.StatusCode != HttpStatusCode.OK))
                            {
                                return string.Empty;
                            }

                            using (var stream = response.GetResponseStream())
                            {
                                if (stream == null)
                                    return string.Empty;

                                using (var streamReader = new StreamReader(stream))
                                {
                                    return streamReader.ReadToEnd();
                                }
                            }
                        }
                    }
                }
                catch (WebException ex)
                {
                    Console.WriteLine(
                        $"{ex}\nServer : {uri.OriginalString}\nMessage : {ex.Message} | Status code : {ex.Status}");
                }

                return string.Empty;
            }
        }
    }
}