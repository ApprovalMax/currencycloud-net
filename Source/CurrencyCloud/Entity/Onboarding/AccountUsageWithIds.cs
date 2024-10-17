using System;

namespace CurrencyCloud.Entity.Onboarding;

public sealed class AccountUsageWithIds : AccountUsage
{
    /// <summary>
    /// UUID for the Account Usage entity.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Form UUID
    /// </summary>
    public Guid FormId { get; set; }
    /// <summary>
    /// Date the Account Usage record created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// Date the Account Usage record was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
