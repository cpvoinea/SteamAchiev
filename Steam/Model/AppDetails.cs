namespace Steam.Model
{
    class AppDetails
    {
        public string type { get; set; }
        public AppDetailsPrice price_overview { get; set; }
        public AppDetailsMetacritic metacritic { get; set; }
        public AppDetailsRecommendations recommendations { get; set; }
        public AppDetailsRelease release_date { get; set; }
    }
}
