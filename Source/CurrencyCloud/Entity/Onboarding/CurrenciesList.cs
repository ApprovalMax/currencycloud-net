using System;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity.Onboarding;

public sealed class CurrenciesList
{
    public Currency[] Currencies { get; set; }

    public struct Currency
    {
        public Guid Id { get; set; }
        [JsonProperty("iso3")]
        public string Iso3 { get; set; }
        public string Name { get; set; }
        public bool Supported { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}