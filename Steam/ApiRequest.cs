using Steam.Model;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Script.Serialization;

namespace Steam
{
    static class ApiRequest
    {
        const string GetOwnedGamesFormat = "http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=json&include_appinfo=1&include_played_free_games=1";
        const string GetPlayerAchievementsFormat = "http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?key={0}&steamid={1}&appid={2}";
        const string ResolveVanityUrlFormat = "http://api.steampowered.com/ISteamUser/ResolveVanityURL/v0001/?key={0}&vanityurl={1}";
        const string AppDetailsFormat = "http://store.steampowered.com/api/appdetails?appids={0}&cc=ro";

        static JavaScriptSerializer serializer = new JavaScriptSerializer();

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
            var vanity = serializer.Deserialize<VanityUrl>(Execute(string.Format(ResolveVanityUrlFormat, UserData.Key, id)));
            if (vanity.response.success == 1)
                return vanity.response.steamid;
            throw new ApplicationException("Unknown user " + id);
        }

        internal static OwnedGames GetOwnedGames(string steamId)
        {
            return serializer.Deserialize<OwnedGames>(Execute(string.Format(GetOwnedGamesFormat, UserData.Key, steamId)));
        }

        internal static PlayerAchievements GetPlayerAchievements(string steamId, int appId)
        {
            return serializer.Deserialize<PlayerAchievements>(Execute(string.Format(GetPlayerAchievementsFormat, UserData.Key, steamId, appId)));
        }

        internal static AppDetails GetAppDetails(int appId)
        {
            string json = Execute(string.Format(AppDetailsFormat, appId));
            int i = json.IndexOf("{\"type");
            if (i <= 0)
                return null;
            json = json.Substring(i);
            json = json.Substring(0, json.Length - 2);

            return serializer.Deserialize<AppDetails>(json);
        }
    }
}
