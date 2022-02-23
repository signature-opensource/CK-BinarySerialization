# CK-BinarySerializer

Yet another serialization library? Unfortunately yes...

## Writing is not Reading

Just like with CQRS, serialization cannot be handled exactly like deserialization. There's always one way to 
serialize an instance of a type: the instance's state must be serialized and the code to serialize an instance
depends solely on the instance's type. 

Deserialization is less obvious: 
 - the serialized type may have been renamed, moved to another namespace or even to another assembly.
 - the serialized instance is an old one: the current shape of its state is not the same as the serialized one. 
   what was a field is now a property, a new Power property exists, the field __age_ that was an integer is now a double.

This library is totally schizophrenic: there are Serializers on one side and Deserializers on another side and they 
are quite different beasts. They, of course, work together and the high level API looks similar but _how they work_ differs. 

## Nullable handling

Nullable value types like `int?` (`Nullable<int>`) are serialized with a marker byte and then the value itself if it is not null. 
Nullable value types are easy: the types are not the same. It's unfortunately much more subtle for reference types: A `User?` is 
exactly of the same type as `User`, the difference is in the way you use it in your code.

The kernel is able to fully support Nullable Reference Type: a `List<User>` will not be serialized the same way as a 
`List<User?>`: a nullable item requires one first extra byte that explicits the `null` value. However, as of today, 
CK.BinarySerialization considers all reference types as being potentially null (this is called the "oblivious nullable context",
with one exception: the key of a `Dictionary<TKey,TValue>` that is assumed to be not nullable.

The plan regarding full NRT support is to:
- Extract the NullableTypeTree from CK.CodeGen.
- Improves it with new features of .net 6 that helps discovering nullabilities of generic parameters.
- Use it here to fully exploit the NRT:
    - When writing a null non nullable reference type, the library will throw a NullReferenceException or an InvalidDataException.
    - The serialized form will then be optimal for non nullable types (no extra null byte required).

## Basic serialization: ICKSimpleBinarySerializable (any type)

## Sharing version: ICKVersionedBinarySerializable (struct & sealed classes only)

## ICKSlicedBinarySerializable (any type)


## Automatic mutations supported

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

## ICKVersionedBinarySerializable: struct from/to sealed class
This simply works for struct to class: each serialized struct becomes a new object.

From class to struct, it's a bit more complicated because of NRT handling: since currently a `List<ThingClass>` is actually
considered as a `List<ThingClass?>`, it will be deserialized as a `List<ThingStruct?>`.


 

