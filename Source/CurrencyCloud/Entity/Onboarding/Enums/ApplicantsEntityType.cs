using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CurrencyCloud.Entity.Onboarding.Enums;

[JsonConverter(typeof(StringEnumConverter), typeof(SnakeCaseNamingStrategy))]
public enum ApplicantsEntityType
{
    Individual,
    Corporate
}