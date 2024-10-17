using System;
using Newtonsoft.Json;

namespace CurrencyCloud.Entity.Onboarding;

public sealed class CountriesList
{
    public Country[] Countries { get; set; }

    public struct Country
    {
        public Guid Id { get; set; }
        [JsonProperty("iso2")]
        public string Iso2 { get; set; }
        [JsonProperty("iso3")]
        public string Iso3 { get; set; }
        public string Name { get; set; }
        public string DialCode { get; set; }
        public bool EeaCountry { get; set; }
        public bool PermittedJurisdiction { get; set; }
        public string[] DocumentTypes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
