using System;

namespace CurrencyCloud.Entity.Onboarding;

public class FormWithIds : Form
{
    /// <summary>
    /// The form's UUID.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// List of IDs for any uploaded supporting documents.
    /// </summary>
    public Guid[] DetachedDocumentIds { get; set; }
    public string[] ProductsRequired { get; set; }
    public string State { get; set; }
    /// <summary>
    /// Date and time the form was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// Date and time the form was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}