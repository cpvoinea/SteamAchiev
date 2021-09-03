using Newtonsoft.Json;
using Steam.Model;
using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;

namespace Steam
{
    static class ApiRequest
    {
        const string GetOwnedGamesFormat = "http://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={0}&steamid={1}&format=json&include_appinfo=1&include_played_free_games=1";
        const string GetPlayerAchievementsFormat = "http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/?key={0}&steamid={1}&appid={2}";
        const string ResolveVanityUrlFormat = "http://api.steampowered.com/ISteamUser/ResolveVanityURL/v1/?key={0}&vanityurl={1}";
        const string AppDetailsFormat = "http://store.steampowered.com/api/appdetails?appids={0}&cc=ro";

        static string SteamKey { get { return "3D093A3884CF5458408AC3A504965900"; } }

        static string Execute(string request)
        {
            using (WebClient client = new WebClient())
            {
                string response;
                try { response = client.DownloadString(request); }
                catch (WebException ex)
                {
                    using (StreamReader sr = new StreamReader(ex.Response.GetResponseStream()))
                        response = sr.ReadToEnd();
                }

                return response;
            }
        }

        internal static string ResolveVanityUrl(string id)
        {
            if (id.All(char.IsDigit))
                return id;
            var json = Execute(string.Format(ResolveVanityUrlFormat, SteamKey, id));
            var vanity = JsonConvert.DeserializeObject<VanityUrl>(json);
            if (vanity.response.success == 1)
                return vanity.response.steamid;
            throw new ApplicationException("Unknown user " + id);
        }

        internal static OwnedGames GetOwnedGames(string steamId)
        {
            var games = Execute(string.Format(GetOwnedGamesFormat, SteamKey, steamId));
            return JsonConvert.DeserializeObject<OwnedGames>(games);
        }

        internal static PlayerAchievements GetPlayerAchievements(string steamId, int appId)
        {
            var achievements = Execute(string.Format(GetPlayerAchievementsFormat, SteamKey, steamId, appId));
            return JsonConvert.DeserializeObject<PlayerAchievements>(achievements);
        }

        internal static AppDetails GetAppDetails(int appId)
        {
            string json = Execute(string.Format(AppDetailsFormat, appId));
            int i = json.IndexOf("{\"type");
            if (i <= 0)
                return null;
            json = json.Substring(i);
            json = json.Substring(0, json.Length - 2);

            var details = JsonConvert.DeserializeObject<AppDetails>(json);
            return details;
        }

        internal static int? ToInt(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return null;

            int.TryParse(s, out int i);
            return i;
        }
    }
}
