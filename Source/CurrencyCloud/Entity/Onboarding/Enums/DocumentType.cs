using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace CurrencyCloud.Entity.Onboarding.Enums;

[JsonConverter(typeof(StringEnumConverter), typeof(SnakeCaseNamingStrategy))]
public enum DocumentType
{
    BankStatement,
    BiometricStateId,
    DrivingLicence,
    Mortgage,
    Passport,
    ProofOfBusinessActivity,
    ProofOfBusinessIdentity,
    ProofOfDirectors,
    ProofOfOwners,
    ProofOfSourceOfFunds,
    ResidenceId,
    StateId,
    Supporting,
    Unknown,
    UtilityBill,
    Visa
}
