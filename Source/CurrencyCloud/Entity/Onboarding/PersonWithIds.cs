using System;

namespace CurrencyCloud.Entity.Onboarding;

public sealed class PersonWithIds : PersonBase
{
    /// <summary>
    /// Person UUID.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Form UUID.
    /// </summary>
    public Guid FormId { get; set; }
    /// <summary>
    /// Personal address.
    /// </summary>
    public Address PersonalAddress { get; set; }
    /// <summary>
    /// Date the person entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// Date the person entity was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}