namespace CurrencyCloud.Entity.Onboarding;

public sealed class Address
{
    public string ApartmentNumber { get; set; }
    public string BuildingName { get; set; }
    public string BuildingNumber { get; set; }
    public required string City { get; set; }
    public required string Country { get; set; }
    public required string PostalCode { get; set; }
    public string StateOrProvince { get; set; }
    public required string Street { get; set; }
}
