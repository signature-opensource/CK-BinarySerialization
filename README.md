# CK-BinarySerialization

[![Licence](https://img.shields.io/github/license/signature-opensource/CK-BinarySerialization.svg)](LICENSE)

Binary serialization that survives type mutation: renaming, moving, and turning a struct into a class
without losing what was already written.

| Package | Description | Latest stable |
|---------|-------------|---------------|
| [CK.BinarySerialization](CK.BinarySerialization/README.md) | The serializer, the deserializer, their contexts and the type mutation machinery. | [![nuget](https://img.shields.io/nuget/v/CK.BinarySerialization.svg?label=CK.BinarySerialization)](https://www.nuget.org/packages/CK.BinarySerialization/) |
| [CK.BinarySerialization.Sliced](CK.BinarySerialization.Sliced/README.md) | One marker interface that makes any type serializable, one slice per level of inheritance. | [![nuget](https://img.shields.io/nuget/v/CK.BinarySerialization.Sliced.svg?label=CK.BinarySerialization.Sliced)](https://www.nuget.org/packages/CK.BinarySerialization.Sliced/) |
| [CK.BinarySerialization.IPoco](CK.BinarySerialization.IPoco/README.md) | Two resolvers that make every `IPoco` serializable, as a length-prefixed JSON payload. | [![nuget](https://img.shields.io/nuget/v/CK.BinarySerialization.IPoco.svg?label=CK.BinarySerialization.IPoco)](https://www.nuget.org/packages/CK.BinarySerialization.IPoco/) |
