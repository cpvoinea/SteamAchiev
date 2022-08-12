namespace Steam
{
    class Game
    {
        internal int Id { get; private set; }
        internal string Name { get; private set; }
        internal string Type { get; private set; }
        internal bool IsFree { get; private set; }
        internal int? InitialPrice { get; private set; }
        internal int? FinalPrice { get; private set; }
        internal int? Metacritic { get; private set; }
        internal int? Recommendations { get; private set; }
        internal int? Year { get; private set; }
        internal int? Playtime { get; private set; }
        internal int? AchievCount { get; private set; }
        internal int? AchievDone { get; private set; }
        internal bool AchievError { get; set; }
        internal string Icon { get; private set; }
        internal string Logo { get; private set; }
        internal bool HasStats { get; private set; }
        internal int AchievPercent => AchievDone > 0 && AchievCount > 0 ? AchievDone.Value * 100 / AchievCount.Value : 0;

        internal Game(int id, string name, int playtime, string icon, string logo, bool hasStats)
        {
            Id = id;
            Name = name;
            Playtime = playtime;
            Icon = icon;
            Logo = logo;
            HasStats = hasStats;
        }

        internal void SetAchiev(int? count, int? done)
        {
            AchievCount = count;
            AchievDone = done;
        }

        internal void SetDetails(string type, bool isFree, int? initialPrice, int? finalPrice, int? metacritic, int? recommendations, int? year)
        {
            Type = type;
            IsFree = isFree;
            InitialPrice = initialPrice;
            FinalPrice = finalPrice;
            Metacritic = metacritic;
            Recommendations = recommendations;
            Year = year;
        }

        static readonly string[] CsvHeaderValues = { "Id", "Name", "Type", "IsFree", "InitialPrice", "FinalPrice", "Metacritic", "Recommendations", "Year", "Playtime", "AchievCount", "AchievDone", "AchievPercent", "Icon", "Logo" };
        internal static string CsvHeader => string.Join(",", CsvHeaderValues);
        internal static int CsvHeaderLength => CsvHeaderValues.Length;

        public override string ToString()
        {
            return string.Join(",", new string[] { Id.ToString(), $"\"{Name}\"", Type, IsFree.ToString(), InitialPrice.ToString(), FinalPrice.ToString(), Metacritic.ToString(), Recommendations.ToString(), Year.ToString(), Playtime.ToString(), AchievCount.ToString(), AchievDone.ToString(), AchievPercent.ToString(), Icon, Logo });
        }

        internal static Game FromString(string s)
        {
            string[] vals = s.Split(',');
            int l = vals.Length;
            string name = vals[1];
            for (int i = 2; i < l - CsvHeaderLength + 2; i++)
                name += ", " + vals[i];
            name = name.Trim('"');

            var g = new Game(vals[0].ToInt().Value, name, vals[l - 6].ToInt().Value, vals[l - 2], vals[l - 1], int.TryParse(vals[l - 5], out int a) && a > 0);
            g.SetAchiev(vals[l - 5].ToInt(), vals[l - 4].ToInt());
            g.SetDetails(vals[l - 13], bool.Parse(vals[l - 12]), vals[l - 11].ToInt(), vals[l - 10].ToInt(), vals[l - 9].ToInt(), vals[l - 8].ToInt(), vals[l - 7].ToInt());

            return g;
        }
    }
}
