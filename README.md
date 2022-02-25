# CK-BinarySerializer

Yet another serialization library? Unfortunately yes...

## What matters: performance, immutability and type mutation
Performance matters and that's why this is all about schema-less, binary serialization. Binary serialization can be
highly efficient at the cost of some complexity and non readability.

Immutability is a very important concept that basically relies on `readonly` fields. This why we only consider here
deserialization constructors since constructors are the only clean way to restore read only fields and properties.

The third and most important aspect is that serialization must not prevent the code base to evolve. The architecture
of this library is primarily driven by this concern. 

(This is not yet finalized.)

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

## Nullable handling

Nullable value types like `int?` (`Nullable<int>`) are serialized with a marker byte and then the value itself if it is not null. 
Nullable value types are easy: the types are not the same. It's unfortunately much more subtle for reference types: A `User?` is 
exactly of the same type as `User`, the difference is in the way you use it in your code.

The kernel is able to fully support Nullable Reference Type: a `List<User>` will actually be serialized the same way 
as a `List<User?>`: a reference type instance always require an extra byte that can handle an already deserialized reference
vs. a new (not seen yet) instance. This byte marker is also used for the `null` value for nullable reference type. 

However, as of today, CK.BinarySerialization considers all reference types as being potentially null (this is called the "oblivious nullable context",
with one exception: the key of a `Dictionary<TKey,TValue>` that is assumed to be not nullable.

The plan regarding full NRT support is to:
- Extract the NullableTypeTree from CK.CodeGen.
- Improves it with new features of .net 6 that helps discovering nullabilities of generic parameters.
- Use it here to fully exploit the NRT:
    - When writing a null non nullable reference type, the library will throw a NullReferenceException or an InvalidDataException.
    - The serialized form will then be optimal for non nullable value types (no extra null byte required).

## Basic serialization: ICKSimpleBinarySerializable (any type)
This is the simplest pattern where versioning must be handled explicitly ant that applies
to any POCO-like type since there is no support for object graphs: the only allowed references
are hierarchical ones and data structures like lists, arrays, sets of any kind, including 
dictionaries must be manually handled.

It may seem cumbersome... It is. However it's rather easy to implement and, when the versioning pattern is 
properly applied, a type can freely mute from version to version.

This relies on the `CK.Core.ICKSimpleBinarySerializable` interface that is defined by CK.Core assembly:
```c#
    /// <summary>
    /// Basic interface for simple binary serialization support.
    /// A deserialization constructor must be implemented (that accepts a <see cref="ICKBinaryReader"/>).
    /// <para>
    /// Simple serialization means that there is no support for object graph (no reference management),
    /// no support for polymorphism (the exact type must be known) and that versions must be manually managed.
    /// </para>
    /// </summary>
    public interface ICKSimpleBinarySerializable
    {
        /// <summary>
        /// Serializes this object into the writer.
        /// There should be a version written first (typically a byte): the deserialization
        /// constructor must read this version first.
        /// </summary>
        /// <param name="w"></param>
        void Write( ICKBinaryWriter w );
    }
``` 
The BinarySerializer is not required for this kind of serializable objects. Everything is available 
from CK.Core: the `CK.Core.ICKBinaryReader` and `CK.Core.ICKBinaryWriter` interfaces and their 
respective default implementations can be used without the CK.BinarySerialization package.

## Sharing version: ICKVersionedBinarySerializable (struct & sealed classes only)
This `CK.BinarySerialization.ICKVersionedBinarySerializable` works like the simple one, except that the
version is kindly handled once for all at the type level (the version is written only once per type even if thousands of
objects are serialized) and the current version is specified by a simple `[SerializationVersion( 42 )]` attribute:
```c#
    /// <summary>
    /// Interface for versioned binary serialization of sealed class or value type.
    /// The version must be defined by a <see cref="SerializationVersionAttribute"/> on the type and
    /// is written once.
    /// <para>
    /// A deserialization constructor must be implemented (that accepts a CK.Core.ICKBinaryReader and a int version).
    /// </para>
    /// <para>
    /// This is for "simple" serialization where "Simple" means that there is no support for object graph (no reference
    /// management).
    /// </para>
    /// </summary>
    public interface ICKVersionedBinarySerializable
    {
        void Write( ICKBinaryWriter w );
    }
``` 
This can only be applied to value types or sealed classes (an `InvalidOperationException` will be raised 
if non sealed class appears in a serialization or deserialization session). Can you see why?

To overcome this limitation, a more complex model is required: this is what the CK.BinarySerialization.Sliced package
brings to the table.

## ICKSlicedBinarySerializable (any type)
The `CK.BinarySerialization.ICKVersionedBinarySerializable` interface is a pure marker interface:
```c#
    /// <summary>
    /// Marker interface for types that can use the "Sliced" serialization. 
    /// </summary>
    public interface ICKSlicedSerializable
    {
    }
```

## Automatic mutations supported

Only a few mutations are currently supported but the objective is be able to transparently handle mutations:
- From nullable to non nullable types and vice versa for value as well as reference types.
- Between `List<T>`, `T[]`, `Queue<T>` (and may be others).
- Between Tuple and ValueTuple.
- etc.

### Enum
Enums can change their underlying type freely:
```c#
public enum Status : byte { ... }
```
Can become:
```c#
public enum Status : ushort { ... }
```
As long as the old integral type can be converted at runtime to the new one, this is transparent. At runtime means that 
the actual values are converted, regardless of the underlying type wideness. The below mutation will work:
```c#
public enum Status : long { None = 0, On = 1, Off = 2, White = 4, OutOfRange = -5, OutOfOrder = 3712 }
```
Can become:
```c#
public enum Status : short { None = 0, On = 1, Off = 2, White = 4, OutOfRange = -5, OutOfOrder = 3712 }
```
Since all values fit in a `short` (`Int16`), everything's fine. 

The risk here is to downsize the underlying type, removing or changing the values that don't fit in the new one, forgetting
that you did this and reloading an old serialized stream that contains these out of range values: an `OverflowException` will
be raised.

## ICKVersionedBinarySerializable: struct to sealed class
This simply works for struct to class: each serialized struct becomes a new object.

The opposite is currently not supported.


 

