namespace CurrencyCloud.Entity.Onboarding;

public sealed class Person : PersonBase
{
    /// <summary>
    /// Personal address.
    /// </summary>
    public required Address PersonalAddressAttributes { get; set; }
}