using System;

namespace CurrencyCloud.Entity.Onboarding;

public class FormDeletionResult
{
    /// <summary>
    /// Message.
    /// </summary>
    public string Message { get; set; }

    /// <summary>
    /// Form ID.
    /// </summary>
    public Guid FormId { get; set; }
}
