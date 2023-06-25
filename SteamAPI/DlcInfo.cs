namespace SteamAPI
{
    public class DLCItem
    {
        public string type { get; set; }
        public string name { get; set; }
        public int id { get; set; }
        public string tiny_image { get; set; }
        public string metascore { get; set; }
        public Dictionary<string, bool> platforms { get; set; }
        public bool streamingvideo { get; set; }
        public string controller_support { get; set; }
    }

    public class DLCList
    {
        public int total { get; set; }
        public List<DLCItem> items { get; set; }
    }
}
