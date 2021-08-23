namespace Steam
{
    class Game
    {
        internal int Id { get; private set; }
        internal string Name { get; private set; }
        internal string Type { get; private set; }
        internal int? Price { get; private set; }
        internal int? Metacritic { get; private set; }
        internal int? Recommendations { get; private set; }
        internal string Year { get; private set; }
        internal int? Playtime { get; private set; }
        internal int? AchievCount { get; private set; }
        internal int? AchievDone { get; private set; }
        internal int AchievPercent => AchievDone > 0 && AchievCount > 0 ? AchievDone.Value * 100 / AchievCount.Value : 0;
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

        internal void SetAchiev(int? count, int? done)
        {
            AchievCount = count;
            AchievDone = done;
        }

        internal void SetDetails(string type, int? price, int? metacritic, int? recommendations, string year)
        {
            Type = type;
            Price = price;
            Metacritic = metacritic;
            Recommendations = recommendations;
            Year = year;
        }

        public override string ToString()
        {
            return string.Join(',', new object[] { Id, Name, Type, Price, Metacritic, Recommendations, Year, Playtime, AchievCount, AchievDone, AchievPercent, Icon, Logo });
        }

        static readonly string[] CsvHeaderValues = { "Id", "Name", "Type", "Price", "Metacritic", "Recommendations", "Year", "Playtime", "AchievCount", "AchievDone", "AchievPercent", "Icon", "Logo" };
        internal static string CsvHeader => string.Join(',', CsvHeaderValues);
        internal static int CsvHeaderLength => CsvHeaderValues.Length;
    }
}
