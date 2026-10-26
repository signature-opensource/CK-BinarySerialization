# CK.BinarySerialization

Yet another serialization library? Unfortunately yes...

## What matters: performance, immutability and type mutation
Performance matters and that's why this is all about schema-less, binary serialization. Binary serialization can be
highly efficient at the cost of some complexity and non readability.

Immutability is a very important concept that basically relies on `readonly` fields. This why we only consider here
deserialization constructors since constructors are the only clean way to restore read only fields and properties.

The third and most important aspect is that serialization must not prevent the code base to evolve. The architecture
of this library is primarily driven by this concern. 

## Writing is not Reading

Just like with CQRS, serialization cannot be handled exactly like deserialization. There's always one way to 
serialize an instance of a type: the instance's state must be serialized and the code to serialize an instance
depends solely on the instance's type. 

Deserialization is less obvious: 
 - the serialized type may have been renamed, moved to another namespace or even to another assembly.
 - the serialized instance is an old one: the current shape of its state is not the same as the serialized one. 
   what was a field is now a property, a new Power property exists, the field __age_ that was an integer is now a double.

This library is totally schizophrenic: there are Serializers on one side and Deserializers on another and they 
are quite different beasts. They, of course, work together and the high level API looks similar but _how they work_ differs. 

## Documentation

| Topic | Content |
|-------|---------|
| [Serialization](Serialization/README.md) | `BinarySerializer` and `BinaryDeserializer`, the contexts and shared contexts, known objects, registering drivers and resolvers, the `IBinarySerializer` API over `ICKBinaryWriter`. |
| [Deserialization](Deserialization/README.md) | Deserializing into another type, global mutations (renaming, moving types), local mutations (nullability, numeric and enum conversions, collections), struct to class mutations. |

## Basic serialization: CK.Core ICKSimpleBinarySerializable and ICKVersionedBinarySerializable

CK.Core package can handle "simple serializable".
See [here](https://github.com/signature-opensource/CK-Core/tree/stable/CK.Core/SimpleSerialization).

The BinarySerializer is not required for these 2 kind of serializable objects. Everything is available 
from CK.Core: the `CK.Core.ICKBinaryReader` and `CK.Core.ICKBinaryWriter` interfaces and their 
respective default implementations can be used without the CK.BinarySerialization package.

However, this CK.BinarySerialization package offers a much more comprehensive support of binary serialization
thanks to its resolvers and drivers (and handles automatically the objects that are "simple serializable") 
and thanks to the CK.BinarySerialization.Sliced package that exposes and handles a third marker interface
`ICKSlicedSerializable`.

## General considerations

### Nullable handling is "oblivious"

> Definitions from _Oxford Languages_: 
> __not aware of or concerned about what is happening around one.__
> _"She became absorbed, oblivious to the passage of time"_

Nullable value types like `int?` (`Nullable<int>`) are serialized with a marker byte and then the value itself if it is not null. 
Nullable value types are easy: the types are not the same. It's unfortunately much more subtle for reference types: A `User?` is 
exactly of the same type as `User`, the difference is in the way you use it in your code.

The kernel is able to fully support Nullable Reference Type: a `List<User>` will actually be serialized the same way 
as a `List<User?>`: a reference type instance always require an extra byte that can handle an already deserialized reference
vs. a new (not seen yet) instance. Note that this byte marker is also used for the `null` value for nullable reference type
(and we cannot avoid it). 

CK.BinarySerialization considers all reference types as being potentially null (this is called the "oblivious nullable context".
Full nullable reference type support is possible for properties, fields and regular method parameters, but not
in this API because of the use of generics method parameters for reading and writing).

And even if it was possible, the gain would be marginal: the binary layout of reference types always require
a byte to handle potential references and that byte is also used to denote a null reference: a full support of
NRT will have no impact on the size or the performance.

Its real objective would be safety, being able to throw an `InvalidDataException` instead of letting a null
be read in a "non nullable" reference.

## Requires.

- `CK.Core` for `ICKBinaryWriter` / `ICKBinaryReader` and the two simple serialization contracts, and
  `CSemVer`.
