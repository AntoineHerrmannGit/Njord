# Njord.Core.Retry

Provides a provider-neutral asynchronous retry policy for operations that can safely be repeated.

`RetryPolicy` retries only exceptions selected by the caller, up to the configured number of retries after the initial attempt. Delays use capped exponential backoff; a caller-provided server `Retry-After` delay takes precedence when it is longer. Both the operation and delay honor the supplied `CancellationToken`.

The policy does not decide which failures are transient or how provider-specific errors are surfaced. Those decisions remain with the calling adapter.
