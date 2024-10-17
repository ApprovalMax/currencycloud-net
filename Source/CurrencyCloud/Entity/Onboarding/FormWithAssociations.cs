namespace CurrencyCloud.Entity.Onboarding;

public sealed class FormWithAssociations : FormWithIds
{
    public BusinessInformationWithIds BusinessInformation { get; set; }
    public AccountUsageWithIds AccountUsage { get; set; }
    /// <summary>
    /// Information about people associated with the application.
    /// </summary>
    public PersonWithIds[] People { get; set; }
}
