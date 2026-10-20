# Njord Data Referential

`Njord.Data.Referential` owns canonical ticker mappings, provider ticker symbols, Njord asset classes, and inclusive quotation-validity dates. `StartDate` and `EndDate` are `DateTime` values whose calendar dates describe when the ticker was quoted; the time component is ignored. They default to `DateTime.MinValue` and `DateTime.MaxValue`. Periods cannot overlap for the same canonical ticker and provider, but may overlap across different providers.

The configured JSON file is the primary database, not an export or a snapshot. Its root is a plain JSON array of `TickerProviderMapping` records. There is no schema-version field, wrapper document, migration, or automatic legacy-dump import. A missing file is treated as an empty database and is created when the first mapping is saved. Writes replace the file atomically so readers see either the previous or the complete new contents. Use one writer process at a time; simultaneous writers are not coordinated.

## Configuration

Bind the `Referential` section from a local JSON file using `AddNjordReferential(configuration)`:

```json
{
  "Referential": {
    "DataFilePath": "data/referential/instruments.json"
  }
}
```

Relative paths are resolved from the process working directory. The default database path is ignored by Git so local referential data remains local.

## Registration

Register the service in the host. No database server, migration, or separate initializer is required:

```csharp
services.AddNjordReferential(configuration);
```

`IInstrumentReferential` is injectable. Add mappings with quotation date ranges, resolve a canonical ticker for a provider and query period, or enumerate mappings. The referential service applies the ticker/provider rules and coverage checks; its repository owns JSON file access and overlap enforcement. Periods are inclusive, non-overlapping for a ticker/provider pair, and must completely cover a requested range. Missing or ambiguous coverage is an error; there is no ticker-format inference.

```csharp
await referential.AddMappingsAsync(
[
    new TickerProviderMapping
    {
        Ticker = "SPX",
        Provider = "Massive",
        ProviderTicker = "I:SPX",
        AssetClass = NjordAssetClass.Index,
        StartDate = new DateTime(2000, 1, 1),
        EndDate = new DateTime(2030, 12, 31)
    }
]);
```

The database file stores the mappings themselves, with no enclosing metadata or version field. Re-adding an identical mapping is idempotent; overlapping or conflicting validity periods are rejected rather than overwritten. Malformed JSON raises an `InvalidDataException` with the database file path instead of silently starting with an empty database.

## Mapping example

An entry for `SPX` can map to Massive's `I:SPX`, with `AssetClass` set to `Index` and a validity interval. Njord option and futures identifiers such as `STOXX=2026-12-18C700000` are stored as canonical tickers; their Massive ticker strings are supplied explicitly rather than derived from the identifier.

Massive supports only the asset classes its API provides. Other Njord asset classes remain valid referential data but are rejected by the Massive adapter when requested.
