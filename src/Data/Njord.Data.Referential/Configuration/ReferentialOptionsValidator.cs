using Microsoft.Extensions.Options;

namespace Njord.Data.Referential.Configuration;

internal sealed class ReferentialOptionsValidator : IValidateOptions<ReferentialOptions>
{
    #region Options validation

    public ValidateOptionsResult Validate(string? name, ReferentialOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.DataFilePath))
            failures.Add($"{ReferentialOptions.SectionName}:DataFilePath is required.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    #endregion
}
