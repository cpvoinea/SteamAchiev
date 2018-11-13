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
        const string GetOwnedGamesFormat = "http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=json&include_appinfo=1&include_played_free_games=1";
        const string GetPlayerAchievementsFormat = "http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?key={0}&steamid={1}&appid={2}";
        const string ResolveVanityUrlFormat = "http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={0}&vanityurl={1}";
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
            var vanity = JsonConvert.DeserializeObject<VanityUrl>(Execute(string.Format(ResolveVanityUrlFormat, SteamKey, id)));
            if (vanity.response.success == 1)
                return vanity.response.steamid;
            throw new ApplicationException("Unknown user " + id);
        }

        internal static OwnedGames GetOwnedGames(string steamId)
        {
            return JsonConvert.DeserializeObject<OwnedGames>(Execute(string.Format(GetOwnedGamesFormat, SteamKey, steamId)));
        }

        internal static PlayerAchievements GetPlayerAchievements(string steamId, int appId)
        {
            return JsonConvert.DeserializeObject<PlayerAchievements>(Execute(string.Format(GetPlayerAchievementsFormat, SteamKey, steamId, appId)));
        }

        internal static AppDetails GetAppDetails(int appId)
        {
            string json = Execute(string.Format(AppDetailsFormat, appId));
            int i = json.IndexOf("{\"type");
            if (i <= 0)
                return null;
            json = json.Substring(i);
            json = json.Substring(0, json.Length - 2);

            return JsonConvert.DeserializeObject<AppDetails>(json);
        }
    }
}
