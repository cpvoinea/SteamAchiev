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

        static JavaScriptSerializer serializer = new JavaScriptSerializer();

        static T Execute<T>(string request)
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

                return serializer.Deserialize<T>(response);
            }
        }

        static VanityUrl ResolveVanityUrl(string url)
        {
            return Execute<VanityUrl>(string.Format(ResolveVanityUrlFormat, UserData.Key, url));
        }

        internal static OwnedGames GetOwnedGames(string id)
        {
            if (id.All(char.IsDigit))
                return Execute<OwnedGames>(string.Format(GetOwnedGamesFormat, UserData.Key, id));
            else
            {
                var vanity = ResolveVanityUrl(id);
                if (vanity.response.success == 1)
                    return Execute<OwnedGames>(string.Format(GetOwnedGamesFormat, UserData.Key, vanity.response.steamid));
                throw new ApplicationException("Unknown user " + id);
            }
        }

        internal static PlayerAchievements GetPlayerAchievements(string steamId, int appId)
        {
            return Execute<PlayerAchievements>(string.Format(GetPlayerAchievementsFormat, UserData.Key, steamId, appId));
        }
    }
}
