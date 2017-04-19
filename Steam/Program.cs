using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;

namespace Steam
{
    class Program
    {
        const string SteamId = "76561198101756699";
        const string KeyDomain = "cpvoinea";
        const string Key = "3D093A3884CF5458408AC3A504965900";

        const string GetOwnedGamesFormat = "http://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/?key=3D093A3884CF5458408AC3A504965900&steamid={0}&format=json";
        const string GetPlayerAchievementsFormat = "http://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v0001/?appid={0}&key=3D093A3884CF5458408AC3A504965900&steamid={1}";

        static string Execute(string request)
        {
            using (WebClient client = new WebClient())
            {
                try { return client.DownloadString(request); }
                catch { return ""; }
            }
        }

        static void Main(string[] args)
        {
            int progress = 0;
            try
            {
                string gamesReq = string.Format(GetOwnedGamesFormat, SteamId);
                string jsonGames = Execute(gamesReq);

                JObject objGames = (JObject)JObject.Parse(jsonGames)["response"];
                JArray games = (JArray)objGames["games"];
                int count = int.Parse(objGames["game_count"].ToString());
                int notPlayed = 0;
                int playedMinutes = 0;
                int withAchiev = 0;
                int achievCount = 0;
                double achievPercent = 0;
                int achievTotal = 0;
                int achievNotPlayed = 0;
                int achievNone = 0;
                int perfectGames = 0;

                #region calculate
                foreach (JObject g in games)
                {
                    Console.Write("\r{0}%  ", progress++ * 100 / count);
                    string id = g["appid"].ToString();
                    int minutes = int.Parse(g["playtime_forever"].ToString());
                    bool gameNotPlayed = minutes == 0;

                    if (gameNotPlayed)
                        notPlayed++;
                    else
                        playedMinutes += minutes;

                    string jsonAchiev = Execute(string.Format(GetPlayerAchievementsFormat, id, SteamId));
                    if (string.IsNullOrEmpty(jsonAchiev))
                        continue;

                    JObject objAchiev = (JObject)JObject.Parse(jsonAchiev)["playerstats"];
                    if (objAchiev == null || !bool.Parse(objAchiev["success"].ToString()))
                        continue;
                    JArray achievements = (JArray)objAchiev["achievements"];
                    if (achievements == null)
                        continue;

                    int at = achievements.Count;
                    achievTotal += at;
                    withAchiev++;
                    if (gameNotPlayed)
                    {
                        achievNotPlayed++;
                        continue;
                    }

                    int ac = 0;
                    foreach (JObject a in achievements)
                    {
                        if (int.Parse(a["achieved"].ToString()) > 0)
                            ac++;
                    }

                    if (ac == 0)
                        achievNone++;
                    else if (ac == at)
                    {
                        achievCount += at;
                        achievPercent += 100;
                        perfectGames++;
                    }
                    else
                    {
                        achievCount += ac;
                        achievPercent += ac * 100.0 / at;
                    }
                }
                #endregion
                Console.WriteLine("\rDone.\r\n");

                int gamesWithAchievements = withAchiev - achievNotPlayed - achievNone;
                string result = string.Format(
                    "Hours played = {0}\r\nGame count = {1}\r\nGames not played = {2}\r\nGames with achievements = {3}, Not played = {4}, No achievements = {5}\r\nPerfect games = {6}\r\nTotal achievement percent = {7} for {8} games\r\nAverage completion rate = {9}\r\nAchievements = {10} / {11}\r\n",
                    (playedMinutes + 30) / 60, count, notPlayed, withAchiev, achievNotPlayed, achievNone, perfectGames, achievPercent, gamesWithAchievements, achievPercent / gamesWithAchievements, achievCount, achievTotal);
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.ReadKey(true);
            }
        }
    }
}
