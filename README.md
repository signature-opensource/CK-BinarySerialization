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

## High level API: Serializer, Deserializer, Context and SharedContext

//TODO

## Nullable handling is currently partial

Nullable value types like `int?` (`Nullable<int>`) are serialized with a marker byte and then the value itself if it is not null. 
Nullable value types are easy: the types are not the same. It's unfortunately much more subtle for reference types: A `User?` is 
exactly of the same type as `User`, the difference is in the way you use it in your code.

The kernel is able to fully support Nullable Reference Type: a `List<User>` will actually be serialized the same way 
as a `List<User?>`: a reference type instance always require an extra byte that can handle an already deserialized reference
vs. a new (not seen yet) instance. Note that this byte marker is also used for the `null` value for nullable reference type
(and we cannot avoid it). 

As of today, CK.BinarySerialization considers all reference types as being potentially null (this is called the "oblivious nullable context",
with one exception: the key of a `Dictionary<TKey,TValue>` that is assumed to be not nullable.

Since the binary layout of reference types always require a byte to handle potential references and that nullable value types are not the 
same as their regular type, full support of NRT will have no real impact on the size or the performance... Its real objective
is related to mutation support. Current partial NRT support makes today mutation from class to struct to actually be class to nullable
struct mutation. This is discussed in more details below.

The plan regarding full NRT support is to:
- Extract the NullableTypeTree from CK.CodeGen.
- Improves it with new features of .net 6 that helps discovering nullabilities of generic parameters.
- Use it here to fully exploit the NRT.

## IBinarySerializer and ICKBinaryWriter, IBinaryDeserializer and ICKBinaryReader

The `ICKBinaryWriter` and `ICKBinaryReader` are defined and implemented by `CKBinaryWriter`
and `CKBinaryReader` in CK.Core. They specialize the .Net System.IO.BinaryReader/Writer classes
and provides an enriched API that reads/writes basic types like `Guid` or `DateTimeOffset` and support
nullable value types once for all.

Those are basic APIs. The CK.BinarySerialization.IBinarySerializer/Deserializer supports objects
serialization (object reference tracking, struct/class neutrality and versioning) but relies
on the basic Reader/Writer (and expose them).

![IBinarySerializer and its Writer](Doc/IBinarySerializer.png)

Recommended conventions are:
- Serializer is **s**: `IBinarySerializer s`
- Writer is **w**: `ICKBinaryWriter w`
- Deserializer is **d**: `IBinaryDeserializer d`
- Reader is **r**: `ICKBinaryReader r`

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
This `CK.Core.ICKVersionedBinarySerializable` works like the simple one, except that the
version is kindly handled once for all at the type level (the version is written only once per type even if thousands of
objects are serialized) and the current version is specified by the `[SerializationVersion( 42 )]` attribute:
```c#
    /// <summary>
    /// Interface for versioned binary serialization that uses an externally 
    /// stored or known version number. This should be used only on sealed classes or value types 
    /// (since inheritance or any other traits or composite objects will have to share the same version).
    /// <para>
    /// The version must be defined by a <see cref="SerializationVersionAttribute"/> on the type and
    /// should be written once for all instances of the type.
    /// </para>
    /// <para>
    /// A deserialization constructor must be implemented (that accepts a <see cref="ICKBinaryReader"/> and a int version).
    /// </para>
    /// <para>
    /// This is for "simple object" serialization where "simple" means that there is no support for object graph (no reference
    /// management).
    /// </para>
    /// </summary>
    public interface ICKVersionedBinarySerializable
    {
        /// <summary>
        /// Must write the binary layout only, without the version number that must be handled externally.
        /// This binary layout will be read by a deserialization constructor that takes a <see cref="ICKBinaryReader"/> 
        /// and a int version.
        /// </summary>
        /// <param name="w">The writer.</param>
        void Write( ICKBinaryWriter w );
    }
``` 
This can only be applied to value types or sealed classes (an `InvalidOperationException` will be raised 
if non sealed class appears in a serialization or deserialization session).

To overcome this limitation, a more complex model is required: this is what the CK.BinarySerialization.Sliced package
brings to the table.

## ICKSlicedBinarySerializable (any type)
The `CK.BinarySerialization.ICKSlicedSerializable` interface is a pure marker interface:
```c#
    /// <summary>
    /// Marker interface for types that can use the "Sliced" serialization. 
    /// </summary>
    public interface ICKSlicedSerializable
    {
    }
```

This interface implies that the type must support the SerializationVersion attribute, a deserialization constructor, a `public static Write`
method (and, if the class is not sealed, a special empty deserialization constructor to be called by specialized types). 
Below is a typical base class implementation (`IsDestroyed` property is discussed below):

```c#
[SerializationVersion(0)]
public class Person : ICKSlicedSerializable, IDestroyable
{
    // ...

    protected Person( Sliced _ ) { }

    public Person( IBinaryDeserializer d, ITypeReadInfo info )
    {
        IsDestroyed = d.Reader.ReadBoolean();
        Name = d.Reader.ReadNullableString();
        if( !IsDestroyed )
        {
            Friends = d.ReadObject<List<Person>>();
            Town = d.ReadObject<Town>();
        }
    }

    public static void Write( IBinarySerializer s, in Person o )
    {
        s.Writer.Write( o.IsDestroyed );
        s.Writer.WriteNullableString( o.Name );
        if( !o.IsDestroyed )
        {
            s.WriteObject( o.Friends );
            s.WriteObject( o.Town );
        }
    }
}
```
The base class **must** be marked with `ICKSlicedSerializable`:

Below is a non sealed specialization of this base class:
```c#
[SerializationVersion(0)]
public class Employee : Person
{
    // ...

    protected Employee( Sliced _ ) : base( _ ) { }

    public Employee( IBinaryDeserializer d, ITypeReadInfo info )
        : base( Sliced.Instance )
    {
        BestFriend = d.ReadNullableObject<Employee>();
        EmployeeNumber = d.Reader.ReadInt32();
        Garage = d.ReadObject<Garage>();
    }

    public static void Write( IBinarySerializer s, in Employee o )
    {
        s.WriteNullableObject( o.BestFriend );
        s.Writer.Write( o.EmployeeNumber );
        s.WriteObject( o.Garage );
    }
}
```

The `IDestroyable` interface is a minimalist interface:

```c#
    /// <summary>
    /// Optional interface that exposes a <see cref="IsDestroyed"/> property that can be implemented 
    /// by reference types that have a "alive" semantics (they may be <see cref="IDisposable"/> but this 
    /// is not required).
    /// <para>
    /// <see cref="IBinarySerializer.OnDestroyedObject"/> event is raised whenever a destroyed object
    /// is written: this supports tracking of "dead" objects in serialized graphs.
    /// </para>
    /// <para>
    /// When used with "sliced serializable", this must be implemented at the root of the serializable
    /// hierarchy and automatically skips calls to specialized Write methods and deserialization constructors.
    /// </para>
    /// <para>
    /// Only reference types are supported: implementing this interface on value type is ignored.
    /// </para>
    /// </summary>
    public interface IDestroyable
    {
        /// <summary>
        /// Gets whether this object has been disposed.
        /// </summary>
        bool IsDestroyed { get; }
    }
```
As the comment states, a destroyed instance is "optimized" by the serializer since only the root Write/Deserialization
constructor is called, specialized ones are skipped (this is why `Employee` doesn't need to handle it). 

## Automatic mutations supported

Only a few mutations are currently supported, they are detailed below.

However the objective is be able to transparently handle mutations:
- From non nullable to nullable types for value as well as reference types: this should be handled automatically 
  and is always safe.
- From nullable to non nullable types for value as well as reference types: this MAY be handled automatically 
  but we are still thinking about it since this not "safe by design": some serialized data that __happens__ to have 
no null values will work whereas another one will fail miserably or worst(?), for value types, default values will 
"magically" appear in place of their previous null.
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

**Important:** 
Underlying type mutation will work ONLY when using `IBinarySerializer.WriteValue<T>(in T)` and 
`IBinaryDeserializer.ReadValue<T>()`.
Using the `ICKBinaryWriter.WriteEnum<T>(T)` and `ICKBinaryReader.ReadEnum<T>` CANNOT handle such migrations, but they 
can be done easily:
```c#
    /// Status was a long in version 1, we are now in version 2 and this is now a short.
    if( info.Version < 2 )
    {
        MyStatus = (Status)(short)d.Reader.ReadInt64();
    }
    else
    {
        MyStatus = d.Reader.ReadEnum<Status>();
    }
```

## General support of struct to class and class to (nullable!) struct mutations

This simply works for struct to class: each serialized struct becomes a new object. The 2 possible 
base classes for reference type ([`ReferenceTypeDeserializer<T>`](CK.BinarySerialization/Deserialization/Deserializer/ReferenceTypeDeserializer.cs) 
and [`SimpleReferenceTypeDeserializer<T>`](CK.BinarySerialization/Deserialization/Deserializer/SimpleReferenceTypeDeserializer.cs)) directly 
supports this mutation.

Transforming a class into a struct is more complex because a serialized reference type is written 
only once (subsequent references are written as simple numbers). The efficient value type deserializer 
[`ValueTypeDeserializer<T>`](CK.BinarySerialization/Deserialization/Deserializer/ValueTypeDeserializer.cs) is not able to handle
this mutation. When a serialized stream that has been written with classes must be read back, 
the [`ValueTypeDeserializerWithRef<T>`](CK.BinarySerialization/Deserialization/Deserializer/ValueTypeDeserializerWithRef.cs)
must be used.

Another aspect to consider is, because of the current partial NRT support, that a class is nullable by default ("oblivious context"). So 
we are stuck today to "class to nullable struct" mutation: a `List<Car>` (where `Car` is a class) must be a `List<Car?>` when `Car` 
becomes a struct since we don't currently analyze the actual type "in depth".

And the cherry on the cake of class to struct mutation complexity is when the serialized class has been chosen to break a 
too deep recursion: 
- its data has not been written at its first occurrence but later (when the stack is emptied)...
- so we cannot read its value right now for its first need (and memorize it for its subsequent occurrences)...
- so there's at least one value property that is "unitialized" in the graph...
- so the whole graph is de facto invalid!

This is the very reason of the potential second pass on the stream: if we have no luck and a class that is being transformed into a struct 
has been chosen to break the serializer's recursion, we forget the whole graph at the end and read it again... except that during the first
pass these problematic values have been enqueued in a special queue and the second pass dequeues these values at their first occurrences:
the final second graph is valid.  

The 3 types of serializations handle these mutations automatically: special deserialization drivers are synthesized when 
such mutation are detected.
 