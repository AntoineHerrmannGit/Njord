using Microsoft.Extensions.Options;
using Njord.Core.Retry;

namespace Njord.DataProviders.YahooFinance.Configuration;

internal sealed class YahooFinanceDataProviderOptionsValidator : IValidateOptions<YahooFinanceDataProviderOptions>
{
    #region Options validation

    public ValidateOptionsResult Validate(string? name, YahooFinanceDataProviderOptions options)
    {
        var failures = new List<string>();
        if (options.BaseAddress is null ||
            !options.BaseAddress.IsAbsoluteUri ||
            options.BaseAddress.Scheme != Uri.UriSchemeHttps ||
            !string.IsNullOrEmpty(options.BaseAddress.UserInfo) ||
            options.BaseAddress.AbsolutePath != "/" ||
            !string.IsNullOrEmpty(options.BaseAddress.Query) ||
            !string.IsNullOrEmpty(options.BaseAddress.Fragment))
            failures.Add($"{YahooFinanceDataProviderOptions.SectionName}:BaseAddress must be an HTTPS origin ending with '/'.");

        if (options.RequestTimeout <= TimeSpan.Zero)
            failures.Add($"{YahooFinanceDataProviderOptions.SectionName}:RequestTimeout must be positive.");

        if (options.MaximumBarsPerQuery is < 1 or > 1_000_000)
            failures.Add($"{YahooFinanceDataProviderOptions.SectionName}:MaximumBarsPerQuery must be between one and one million.");

        if (options.Retry is null)
            failures.Add($"{YahooFinanceDataProviderOptions.SectionName}:Retry is required.");
        else
        {
            var retryValidation = new RetryPolicyOptionsValidator().Validate(
                $"{YahooFinanceDataProviderOptions.SectionName}:Retry",
                options.Retry);
            if (retryValidation.Failed)
                failures.AddRange(retryValidation.Failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    #endregion
}
