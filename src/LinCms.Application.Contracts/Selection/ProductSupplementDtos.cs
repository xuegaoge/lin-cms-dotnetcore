using System;
using System.Collections.Generic;

namespace LinCms.Application.Contracts.Selection
{
    public class ProductKeywordDto
    {
        public long ProductId { get; set; }
        public string Keyword { get; set; }
        public string Type { get; set; }
        public int SearchVolume { get; set; }
        public int CompetitorCount { get; set; }
        public decimal SPR { get; set; }
        public decimal BidPrice { get; set; }
        public string CompetitionLevel { get; set; }
        public decimal OpportunityScore { get; set; }
        public string Priority { get; set; }
        public int? CurrentRank { get; set; }
        public int? TargetRank { get; set; }
    }

    public class ProductTrendDto
    {
        public long ProductId { get; set; }
        public string MetricName { get; set; }
        public decimal Month1 { get; set; }
        public decimal Month2 { get; set; }
        public decimal Month3 { get; set; }
        public decimal Month4 { get; set; }
        public decimal Month5 { get; set; }
        public decimal Month6 { get; set; }
        public decimal Month7 { get; set; }
        public decimal Month8 { get; set; }
        public decimal Month9 { get; set; }
        public decimal Month10 { get; set; }
        public decimal Month11 { get; set; }
        public decimal Month12 { get; set; }
        public decimal YearMean { get; set; }
        public string Trend { get; set; }
        public decimal SeasonalityIndex { get; set; }
    }
}
