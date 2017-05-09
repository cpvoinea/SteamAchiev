namespace Steam.Model
{
    class PlayerAchievementsStats
    {
        public string steamID { get; set; }
        public string gameName { get; set; }
        public PlayerAchievementsAchievement[] achievements { get; set; }
        public bool success { get; set; }
    }
}
