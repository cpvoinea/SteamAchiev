using Steam.Model;
using System.Net;
using System.Web.Script.Serialization;

namespace Steam
{
    static class ApiRequest
    {
        const string GetOwnedGamesFormat = "http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key={0}&steamid={1}&format=json&include_appinfo=1&include_played_free_games=1";
        const string GetPlayerAchievementsFormat = "http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?key={0}&steamid={1}&appid={2}";

        static JavaScriptSerializer serializer = new JavaScriptSerializer();

        static T Execute<T>(string request)
        {
            using (WebClient client = new WebClient())
                return serializer.Deserialize<T>(client.DownloadString(request));
        }

        internal static OwnedGames GetOwnedGames(string steamId)
        {
            return Execute<OwnedGames>(string.Format(GetOwnedGamesFormat, UserData.Key, steamId));
        }

        internal static PlayerAchievements GetPlayerAchievements(string steamId, int appId)
        {
            try { return Execute<PlayerAchievements>(string.Format(GetPlayerAchievementsFormat, UserData.Key, steamId, appId)); }
            catch { return new PlayerAchievements { playerstats = new PlayerAchievementsStats { success = false } }; }
        }
    }
}
