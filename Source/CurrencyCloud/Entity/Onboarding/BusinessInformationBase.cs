using System;
using System.Collections.Generic;
using CurrencyCloud.Entity.Onboarding.Enums;

namespace CurrencyCloud.Entity.Onboarding;

public abstract class BusinessInformationBase
{
    
    /// <summary>
    /// List of IDs for any uploaded supporting documents.
    /// </summary>
    public List<Guid> DocumentIds { get; set; }
    /// <summary>
    /// ISO country code for the country the business is registered in.
    /// </summary>
    public required string BusinessCountry { get; set; }
    /// <summary>
    /// The company name.
    /// </summary>
    public required string CompanyName { get; set; }
    /// <summary>
    /// The company's trading name.
    /// </summary>
    public string TradingName { get; set; }
    /// <summary>
    /// Company's legal entity type.
    /// </summary>
    public required LegalEntityType LegalEntityType { get; set; }
    /// <summary>
    /// The primary sector the company operates in.
    /// </summary>
    public required LegalSector Sector { get; set; }
    /// <summary>
    /// An official number that identifies the company.
    /// </summary>
    public string CompanyRegistrationNumber { get; set; }
    /// <summary>
    /// The company's VAT number or Tax ID number.
    /// </summary>
    public string VatNumber { get; set; }
    /// <summary>
    /// The date the company was incorporated.
    /// </summary>
    public required DateTimeOffset IncorporationDate { get; set; }
    /// <summary>
    /// The company's website URL or other online presence.
    /// </summary>
    public string BusinessWebsite { get; set; }
    /// <summary>
    /// Description of the company's activities.
    /// </summary>
    public string BusinessActivityDescription { get; set; }
}