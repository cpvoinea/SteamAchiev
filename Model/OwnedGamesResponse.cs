namespace Steam.Model
{
    class OwnedGamesResponse
    {
        public int game_count { get; set; }
        public OwnedGamesGame[] games { get; set; }
    }
}
