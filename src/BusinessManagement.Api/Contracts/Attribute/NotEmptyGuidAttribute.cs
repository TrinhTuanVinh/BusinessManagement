using System.ComponentModel.DataAnnotations;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute() : base("The {0} field is required and must not be an empty GUID.") { }

    public override bool IsValid(object? value) => value is Guid id && id != Guid.Empty;
}