using CurrencyCloud.Entity.Onboarding.Enums;

namespace CurrencyCloud.Entity.Onboarding;

public sealed class DocumentImage
{
    /// <summary>
    /// Image encoder used e.g. base64.
    /// </summary>
    public required string Image { get; set; }
    /// <summary>
    /// Image of which document side.
    /// </summary>
    public required ImageSide Side { get; set; } 
}
