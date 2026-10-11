# Njord.Core.Serialization

Shared JSON extensions for the Njord solution, implemented with `System.Text.Json`.

```csharp
using Njord.Core.Serialization;

var json = DateTime.UtcNow.Serialize();
var value = json.Déserialize<DateTime>();
```

Enums are written as their names and read from string names; numeric enum values are rejected. Structs and nullable structs use the standard `System.Text.Json` behavior.

`DateTime` values are written in the invariant round-trip ISO 8601 format (`O`). Deserialization accepts standard `System.Text.Json` ISO date/time values, general date/time strings parsed with the invariant and current cultures, and integer Unix timestamps in seconds or milliseconds. Numeric timestamps with an absolute value of at least `100_000_000_000` are interpreted as milliseconds; smaller values are interpreted as seconds. Unix timestamps produce UTC `DateTime` values. Fractional and out-of-range timestamps raise `JsonException`. Time-zone and `DateTimeKind` behavior for textual input follows the parsed representation; ambiguous culture-specific dates cannot be interpreted uniquely without an explicit format or culture.
