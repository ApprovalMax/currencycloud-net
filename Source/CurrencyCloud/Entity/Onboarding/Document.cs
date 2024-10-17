using CurrencyCloud.Entity.Onboarding.Enums;

namespace CurrencyCloud.Entity.Onboarding;

public class Document
{
    /// <summary>
    /// The type of document.
    /// </summary>
    public required DocumentType DocumentType { get; set; }
    /// <summary>
    /// Category of identification documentation.
    /// </summary>
    public required DocumentCategory Category { get; set; }
}
