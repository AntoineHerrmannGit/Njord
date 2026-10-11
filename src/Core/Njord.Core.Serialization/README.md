# Njord.Core.Serialization

This project owns shared, provider-neutral JSON serialization and deserialization behavior used by multiple solution components.

- `IJsonSerializer` defines the application-facing JSON contract.
- `SystemTextJsonSerializer` is the standard implementation, with web-oriented JSON defaults and optional caller-provided `JsonSerializerOptions`.
- `JsonElementReader` provides shared, validated access to required properties and optional primitive values for consumers that parse JSON documents.
- `JsonSerializationException` reports a failed operation and model type without embedding serialized payload data.

Provider-specific response schemas, DTOs, API errors, and business mappings belong in their provider adapter, not in this project. Keep vendor field interpretation and business mapping out of these generic helpers. Register or construct one serializer implementation for consumers and avoid duplicating low-level `System.Text.Json` configuration in feature projects.
