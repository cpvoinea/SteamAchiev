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

        static int Percent(int count, int total)
        {
            return (int)Math.Round(count * 100.0 / total, 0);
        }

        static string ReadFile(string path)
        {
            if (!File.Exists(path))
                return "";
            using (StreamReader sr = new StreamReader(path))
            {
                string result = sr.ReadToEnd();
                sr.Close();
                return result;
            }
        }

        static void Main(string[] args)
        {
            string steamId = SteamId;
            string dataPath = "";
            bool load = false;
            int threshold = 0;
            #region read params
            // steam id
            if (args == null || args.Length < 1)
            {
                Console.Write("Steam ID (leave empty to use {0}'s ID): ", KeyDomain);
                string sid = Console.ReadLine();
                if (!string.IsNullOrEmpty(sid))
                    steamId = sid;
            }
            else if (!string.IsNullOrEmpty(args[0]))
                steamId = args[0];

            // load & load path
            if (args == null || args.Length < 2)
            {
                Console.Write("Data folder path (leave empty to read from Steam Web API): ");
                dataPath = Console.ReadLine();
            }
            else
                dataPath = args[1];
            load = !string.IsNullOrEmpty(dataPath);

            // played minutes threshold
            if (args == null || args.Length < 3)
            {
                Console.Write("Played game threshold (minutes, default 0): ");
                string t = Console.ReadLine();
                int.TryParse(t, out threshold);
            }
            else
                int.TryParse(args[2], out threshold);
            #endregion

            StreamWriter logFile = new StreamWriter("log.txt");
            logFile.AutoFlush = true;
            int progress = 0;
            Console.WriteLine("Reading stats...");

            try
            {
                string gamesPath = (string.IsNullOrEmpty(dataPath) ? "" : dataPath + "\\") + "games.txt";
                string gamesReq = string.Format(GetOwnedGamesFormat, steamId);
                string jsonGames = load ? ReadFile(gamesPath) : Execute(gamesReq);
                if (string.IsNullOrEmpty(jsonGames))
                {
                    if (load)
                    {
                        load = false;
                        jsonGames = Execute(gamesReq);
                    }
                    else
                    {
                        Console.WriteLine("Could not read games from Steam API");
                        return;
                    }
                }
                if (!load)
                    using (StreamWriter sw = new StreamWriter(gamesPath))
                    {
                        sw.Write(jsonGames);
                        sw.Close();
                    }

                JObject objGames = (JObject)JObject.Parse(jsonGames)["response"];
                JArray games = (JArray)objGames["games"];
                int count = int.Parse(objGames["game_count"].ToString());
                int notPlayed = 0;
                int playedMinutes = 0;
                int withAchiev = 0;
                int achievCount = 0;
                int achievPercent = 0;
                int achievTotal = 0;
                int achievNotPlayed = 0;
                int achievNone = 0;
                int perfectGames = 0;

                #region calculate
                foreach (JObject g in games)
                {
                    Console.Write("\r{0}%  ", Percent(progress++, count));
                    string id = g["appid"].ToString();
                    int minutes = int.Parse(g["playtime_forever"].ToString());
                    bool gameNotPlayed = minutes <= threshold;

                    if (gameNotPlayed)
                        notPlayed++;
                    else
                        playedMinutes += minutes;

                    string gPath = string.Format("{0}{1}.txt", string.IsNullOrEmpty(dataPath) ? "" : dataPath + "\\", id);
                    string jsonAchiev = load ? ReadFile(gPath) : Execute(string.Format(GetPlayerAchievementsFormat, id, steamId));
                    if (string.IsNullOrEmpty(jsonAchiev))
                        continue;
                    if (!load)
                        using (StreamWriter sw = new StreamWriter(gPath))
                        {
                            sw.Write(jsonAchiev);
                            sw.Close();
                        }

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
                    logFile.WriteLine("{0}: {1}/{2} in {3}m", objAchiev["gameName"], ac, at, minutes);

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
                        int ap = Percent(ac, at);
                        achievCount += ac;
                        achievPercent += ap;
                    }
                }
                #endregion
                Console.WriteLine("\rDone.\r\n");

                int gamesWithAchievements = withAchiev - achievNotPlayed - achievNone;
                string result = string.Format(
                    "Hours played = {0}\r\nGame count = {1}\r\nGames not played = {2}\r\nGames with achievements = {3}, Not played = {4}, No achievements = {5}\r\nPerfect games = {6}\r\nTotal achievement percent = {7} for {8} games\r\nAverage completion rate = {9}\r\nAchievements = {10} / {11} ({12}%)\r\n",
                    (playedMinutes + 30) / 60, count, notPlayed, withAchiev, achievNotPlayed, achievNone, perfectGames, achievPercent, gamesWithAchievements, Percent(achievPercent, gamesWithAchievements * 100), achievCount, achievTotal, Percent(achievCount, achievTotal));
                Console.WriteLine(result);
                logFile.WriteLine(result);
                Console.WriteLine("Stats saved to file log.txt");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                Console.ReadKey(true);
                logFile.Close();
            }
        }
    }
}
