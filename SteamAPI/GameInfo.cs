using Newtonsoft.Json;
using System.Collections.Generic;

namespace app
{
    public class GameInfo
    {
        public int AppId { get; set; }
        public string? Name { get; set; }
        public string? Developer { get; set; }
        public string? Publisher { get; set; }
        public string? ScoreRank { get; set; }
        public float Positive { get; set; }
        public float Negative { get; set; }
        public int Userscore { get; set; }
        public string? Owners { get; set; }
        public float AverageForever { get; set; }
        public int Average2Weeks { get; set; }
        public float MedianForever { get; set; }
        public int Median2Weeks { get; set; }
        public string? Price { get; set; }
        public string? InitialPrice { get; set; }
        public string? Discount { get; set; }
        public int Ccu { get; set; }
    }
}