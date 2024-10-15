using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CurrencyCloud.Entity.Onboarding.Enums;

[JsonConverter(typeof(StringEnumConverter), typeof(SnakeCaseNamingStrategy))]
public enum LegalEntityType
{
    LimitedLiabilityCompany,
    LimitedLiabilityPartnership,
    CompanyWithNomineeShareholdersOrSharesInBearerForm,
    Other,
    PublicLimitedCompany,
    RegisteredCharity,
    ScottishLimitedPartnership,
    SoleTrader,
    Trust,
    UnincorporatedPartnership,
    UnregisteredCharity
}