# CK.BinarySerialization.Sliced

Adds a third marker interface, `ICKSlicedSerializable`, that makes any type serializable - including
inheritance hierarchies, where each level of the hierarchy writes and reads its own slice.

> ℹ️ Read [CK.BinarySerialization](../CK.BinarySerialization/README.md) first: this package plugs into
> its resolvers and drivers.

## A pure marker interface

The `CK.BinarySerialization.ICKSlicedSerializable` interface is a pure marker interface:
```csharp
    /// <summary>
    /// Marker interface for types that can use the "Sliced" serialization. 
    /// </summary>
    public interface ICKSlicedSerializable
    {
    }
```

This interface implies that the type must be decorated with the `SerializationVersion` attribute just like
the `CK.Core.ICKVersionedBinarySerializable` but "Sliced" serialization can be applied to any type
(whereas the `ICKVersionedBinarySerializable` is limited to sealed classes or structs).

Each sliced serializable type must expose a deserialization constructor, and a `public static Write` method
(and, if the class is not sealed, a special empty deserialization constructor to be called by specialized types). 

The "Sliced" serialization supports versioned classes specializations: a base class (abstract or simply not sealed)
can evolve freely **without any impact on its potential specializations**.

Below is a typical base class implementation (`IsDestroyed` property is discussed below):

```csharp
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
```csharp
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

```csharp
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

## Requires.

- `CK.BinarySerialization`, and nothing else: this package adds a marker interface and the driver that
  honours it.
