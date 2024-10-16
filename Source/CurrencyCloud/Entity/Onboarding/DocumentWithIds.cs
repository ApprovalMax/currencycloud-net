using System;

namespace CurrencyCloud.Entity.Onboarding;

public sealed class DocumentWithIds : Document
{
    /// <summary>
    /// Document UUID.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Date the document was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// Date the document was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
