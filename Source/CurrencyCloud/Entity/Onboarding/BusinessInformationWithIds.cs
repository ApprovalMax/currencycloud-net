using System;

namespace CurrencyCloud.Entity.Onboarding;

public sealed class BusinessInformationWithIds : BusinessInformationBase
{
    /// <summary>
    /// Unique ID for the Business Information entity.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Form UUID
    /// </summary>
    public Guid FormId { get; set; }
    /// <summary>
    /// The company's registered address.
    /// </summary>
    public Address RegisteredAddress { get; set; }
    /// <summary>
    /// The company's trading address.
    /// </summary>
    public Address TradingAddress { get; set; }
    /// <summary>
    /// Date the Business Information record was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// Date the Business Information record was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
