using System;
using CurrencyCloud.Entity.Onboarding.Enums;

namespace CurrencyCloud.Entity.Onboarding;

public abstract class PersonBase
{
    /// <summary>
    /// List of IDs for any uploaded supporting documents.
    /// </summary>
    public Guid[] DocumentIds { get; set; }
    /// <summary>
    /// The person's role(s).
    /// </summary>
    public required Role[] Roles { get; set; }
    /// <summary>
    /// First name
    /// </summary>
    public required string FirstName { get; set; }
    /// <summary>
    /// Middle name
    /// </summary>
    public string MiddleName { get; set; }
    /// <summary>
    /// Surname
    /// </summary>
    public required string LastName { get; set; }
    /// <summary>
    /// Date of Birth
    /// </summary>
    public DateTimeOffset Dob { get; set; }
    /// <summary>
    /// Phone number
    /// </summary>
    public string PhoneNumber { get; set; }
    /// <summary>
    /// Email address
    /// </summary>
    public string Email { get; set; }
    /// <summary>
    /// Percentage of company owned.
    /// </summary>
    public decimal OwnershipPercentage { get; set; }
    /// <summary>
    /// Social security number if home_country=US.
    /// </summary>
    public string IdNumber { get; set; }
    /// <summary>
    /// Country of permanent address.
    /// </summary>
    public required string HomeCountry { get; set; }
    /// <summary>
    /// Passport number
    /// </summary>
    public string PassportNumber { get; set; }
    /// <summary>
    /// Country code for where the passport was issued.
    /// </summary>
    public string PassportCountryOfIssue { get; set; }
}