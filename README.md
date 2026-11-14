# CK-BinarySerialization

[![Licence](https://img.shields.io/github/license/signature-opensource/CK-BinarySerialization.svg)](LICENSE)

Binary serialization that survives type mutation: renaming, moving, and turning a struct into a class
without losing what was already written.

| Package | Description | Latest stable |
|---------|-------------|---------------|
| [CK.BinarySerialization](CK.BinarySerialization/README.md) | The serializer, the deserializer, their contexts and the type mutation machinery. | [![nuget](https://img.shields.io/nuget/v/CK.BinarySerialization.svg?label=CK.BinarySerialization)](https://www.nuget.org/packages/CK.BinarySerialization/) |
| [CK.BinarySerialization.Sliced](CK.BinarySerialization.Sliced/README.md) | One marker interface that makes any type serializable, one slice per level of inheritance. | [![nuget](https://img.shields.io/nuget/v/CK.BinarySerialization.Sliced.svg?label=CK.BinarySerialization.Sliced)](https://www.nuget.org/packages/CK.BinarySerialization.Sliced/) |

The `IPoco` support, CK.BinarySerialization.IPoco, is in
[CK-StObj](https://github.com/signature-opensource/CK-StObj/blob/master/CK.BinarySerialization.IPoco/README.md), next
to the Poco model it depends on. It is still found by name: referencing its package is enough.
