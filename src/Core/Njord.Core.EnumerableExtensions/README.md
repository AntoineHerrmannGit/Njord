# Njord.Core.EnumerableExtensions

Small, provider-neutral helpers for checking whether an `IEnumerable<T>` is empty, non-empty, or null.

```csharp
IEnumerable<int> values = [1, 2, 3];

var hasValues = values.IsNotEmpty();
var isMissingOrEmpty = ((IEnumerable<int>?)null).IsNullOrEmpty();
var hasNoNegativeValues = values.None(value => value < 0);
```

`IsEmpty` and `IsNotEmpty` reject a null sequence with `ArgumentNullException`. `IsNullOrEmpty` and `IsNotNullOrEmpty` explicitly support nullable sequences. `None(predicate)` returns `true` for an empty sequence and stops at the first matching element. It rejects a null sequence or predicate with `ArgumentNullException`. The checks use LINQ `Any`, so they do not enumerate the whole sequence.
