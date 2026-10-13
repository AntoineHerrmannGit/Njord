using Microsoft.Extensions.Options;

namespace Njord.DataProviders.Massive.Configuration;

internal sealed class MassiveDataProviderOptionsValidator : IValidateOptions<MassiveDataProviderOptions>
{
    #region Options validation

    public ValidateOptionsResult Validate(string? name, MassiveDataProviderOptions options)
    {
        var failures = new List<string>();

        if (options.BaseAddress is null ||
            !options.BaseAddress.IsAbsoluteUri ||
            options.BaseAddress.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(options.BaseAddress.UserInfo) ||
            !options.BaseAddress.AbsoluteUri.EndsWith("/", StringComparison.Ordinal) ||
            !string.IsNullOrEmpty(options.BaseAddress.Query) ||
            !string.IsNullOrEmpty(options.BaseAddress.Fragment))
            failures.Add("DataProviders:Massive:BaseAddress must be an absolute HTTPS address ending with '/'.");

        if (string.IsNullOrWhiteSpace(options.ApiKey))
            failures.Add("DataProviders:Massive:ApiKey must contain the Massive API key.");
        else if (options.ApiKey.Any(char.IsWhiteSpace))
            failures.Add("DataProviders:Massive:ApiKey must not contain whitespace.");

        if (options.RequestTimeout <= TimeSpan.Zero)
            failures.Add("DataProviders:Massive:RequestTimeout must be positive.");

        if (options.CircuitBreakerFailureThreshold <= 0)
            failures.Add("DataProviders:Massive:CircuitBreakerFailureThreshold must be greater than zero.");

        if (options.CircuitBreakerBreakDuration <= TimeSpan.Zero)
            failures.Add("DataProviders:Massive:CircuitBreakerBreakDuration must be positive.");

        if (options.MaximumPagesPerQuery is < 1 or > 10_000)
            failures.Add("DataProviders:Massive:MaximumPagesPerQuery must be between one and ten thousand.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    #endregion
}
