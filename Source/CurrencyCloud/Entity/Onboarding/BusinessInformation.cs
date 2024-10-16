namespace CurrencyCloud.Entity.Onboarding;

public sealed class BusinessInformation : BusinessInformationBase
{
    /// <summary>
    /// Registered address of the company.
    /// </summary>
    public Address RegisteredAddressAttributes { get; set; }
    /// <summary>
    /// Trading address of the company.
    /// </summary>
    public Address TradingAddressAttributes { get; set; }
}
