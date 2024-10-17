using CurrencyCloud.Entity.Onboarding.Enums;

namespace CurrencyCloud.Entity.Onboarding;

public class Form
{
    /// <summary>
    /// Client's own use, e.g. to track lead source.
    /// </summary>
    public string AffiliateCode { get; set; }
    /// <summary>
    /// Agreed to marketing emails.
    /// </summary>
    public required bool AgreedToMarketingEmails { get; set; }
    /// <summary>
    /// Agreed to terms and conditions.
    /// </summary>
    public required bool AgreedToTermsAndConditions { get; set; }
    /// <summary>
    /// Confirmed authority to act on behalf of company.
    /// </summary>
    public bool AgreedToCorporateAuthority { get; set; }
    /// <summary>
    /// Home country for individual applicant or country of registered address for corporate applicant.
    /// </summary>
    public required string Country { get; set; }
    /// <summary>
    /// The applicant's email address.
    /// </summary>
    public required string Email { get; set; }
    /// <summary>
    /// Type of applicant - individual or corporate.
    /// </summary>
    public required ApplicantsEntityType EntityType { get; set; }
    /// <summary>
    /// How did the applicant hear about us.
    /// </summary>
    public string ReferredBy { get; set; }
    /// <summary>
    /// The applicant's phone number.
    /// </summary>
    public string PhoneNumber { get; set; }
    /// <summary>
    /// The applicant's name.
    /// </summary>
    public string ContactName { get; set; }
}
