namespace Steam
{
    class Game
    {
        internal int Id { get; private set; }
        internal string Name { get; private set; }
        internal int Playtime { get; private set; }
        internal int AchievCount { get; private set; }
        internal int AchievDone { get; private set; }
        internal string Icon { get; private set; }
        internal string Logo { get; private set; }

        internal Game(int id, string name, int playtime, string icon, string logo)
        {
            Id = id;
            Name = name;
            Playtime = playtime;
            Icon = icon;
            Logo = logo;
        }

        internal void SetAchiev(int count, int done)
        {
            AchievCount = count;
            AchievDone = done;
        }

        public override string ToString()
        {
            return string.Format("{0},\"{1}\",{2},{3},{4},{5},{6}", Id, Name, Playtime, AchievCount, AchievDone, Icon, Logo);
        }

        internal static string CsvHeader { get { return "Id, Name, Playtime, AchievCount, AchievDone, Icon, Logo"; } }
    }
}
