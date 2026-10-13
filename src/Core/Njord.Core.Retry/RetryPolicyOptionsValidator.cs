using Microsoft.Extensions.Options;

namespace Njord.Core.Retry;

/// <summary>Validates configuration for the shared asynchronous retry policy.</summary>
public sealed class RetryPolicyOptionsValidator : IValidateOptions<RetryPolicyOptions>
{
    #region Options validation

    public ValidateOptionsResult Validate(string? name, RetryPolicyOptions options)
    {
        var failures = new List<string>();

        if (options.MaxRetryAttempts is < 0 or > 10)
            failures.Add("MaxRetryAttempts must be between zero and ten.");

        if (!Enum.IsDefined(options.Pattern))
            failures.Add("Pattern must be Exponential or Linear.");

        if (options.BaseDelay <= TimeSpan.Zero)
            failures.Add("BaseDelay must be positive.");

        if (options.MaximumDelay <= TimeSpan.Zero || options.MaximumDelay < options.BaseDelay)
            failures.Add("MaximumDelay must be positive and at least BaseDelay.");

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    #endregion
}
