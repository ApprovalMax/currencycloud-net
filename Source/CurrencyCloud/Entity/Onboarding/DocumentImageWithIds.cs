using System;
using CurrencyCloud.Entity.Onboarding.Enums;

namespace CurrencyCloud.Entity.Onboarding;

public sealed class DocumentImageWithIds
{
    /// <summary>
    /// Document image UUID.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Image of which document side.
    /// </summary>
    public required ImageSide Side { get; set; } 
    /// <summary>
    /// Date the document image entity was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// Date the document image entity was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}