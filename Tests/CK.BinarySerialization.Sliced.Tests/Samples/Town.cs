using CK.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples;

[SerializationVersion( 0 )]
public sealed partial class Town : ICKSlicedSerializable
{
    readonly List<Person> _persons;
    readonly List<Garage> _garages;
    readonly List<Car> _cars;

    public Town( string name )
    {
        Name = name;
        _persons = new List<Person>();
        _garages = new List<Garage>();
        _cars = new List<Car>();
        CityCar = new Car( "The city car!", DateTime.UtcNow );
    }

    public string Name { get; }

    public Car CityCar { get; set; }

    public IReadOnlyList<Person> Persons => _persons;

    public IReadOnlyList<Garage> Garages => _garages;

    public IReadOnlyList<Car> Cars => _cars;

    public IEnumerable<Employee> CurrenlyReparing => _garages.SelectMany( g => g.Employees ).Where( e => e.CurrentCar != null );

    public class Statistics
    {
        public int PersonCount;
        public int PurePersonCount;
        public int GarageCount;
        public int CarCount;
        public int CarUnderReparationCount;
        public int UnownedCarCount;
        public int CustomerCount;
        public int EmployeeCount;
        public int PureEmployeeCount;
        public int ManagerCount;

        public Statistics( Town t )
        {
            PersonCount = t._persons.Count;
            PurePersonCount = t._persons.Where( p => p.GetType() == typeof( Person ) ).Count();
            GarageCount = t._garages.Count;
            CarCount = t._cars.Count;
            CustomerCount = t._persons.OfType<Customer>().Count();
            ManagerCount = t._persons.OfType<Manager>().Count();
            EmployeeCount = t._persons.OfType<Employee>().Count();
            PureEmployeeCount = t._persons.Where( p => p.GetType() == typeof( Employee ) ).Count();
            Debug.Assert( PersonCount == CustomerCount + EmployeeCount + PurePersonCount );
            CarUnderReparationCount = t.CurrenlyReparing.Count();
            var ownedCars = t._persons.OfType<Customer>().Where( c => c.Car != null );
            UnownedCarCount = CarCount - ownedCars.Count();
        }

        public override string ToString()
        {
            return $"PersonCount: {PersonCount}, PurePersonCount: {PurePersonCount}, GarageCount: {GarageCount}, " +
                   $"CarCount: {CarCount}, CarUnderReparationCount: {CarUnderReparationCount}, UnownedCarCount: {UnownedCarCount}, " +
                   $"CustomerCount: {CustomerCount}, EmployeeCount: {EmployeeCount}, PureEmployeeCount: {PureEmployeeCount}, ManagerCount: {ManagerCount}";
        }

        public override bool Equals( object? obj ) => obj?.ToString() == ToString();

        public override int GetHashCode() => HashCode.Combine( PersonCount,
                                                               PurePersonCount,
                                                               GarageCount,
                                                               CarCount,
                                                               CarUnderReparationCount,
                                                               UnownedCarCount,
                                                               CustomerCount,
                                                               HashCode.Combine( EmployeeCount,
                                                                                 PureEmployeeCount,
                                                                                 ManagerCount ) );
    }

    public Statistics Stats => new Statistics( this );

    public Car AddCar( string model, DateTime buildDate )
    {
        var c = new Car( model, buildDate );
        _cars.Add( c );
        return c;
    }

    internal void OnNewPerson( Person e )
    {
        _persons.Add( e );
    }
    
    internal void OnDestroying( Person e )
    {
        _persons.Remove( e );
    }
    
    internal void OnNewGarage( Garage g )
    {
        _garages.Add( g );
    }

    #region Serialization

    public Town( IBinaryDeserializer d, ITypeReadInfo info )
    {
        d.DebugCheckSentinel();
        Name = d.Reader.ReadString();
        CityCar = d.ReadObject<Car>();
        _garages = d.ReadObject<List<Garage>>();
        _persons = d.ReadObject<List<Person>>();
        _cars = d.ReadObject<List<Car>>();
    }

    public static void Write( IBinarySerializer s, in Town o )
    {
        s.DebugWriteSentinel();
        s.Writer.Write( o.Name );
        s.WriteObject( o.CityCar );
        s.WriteObject( o._garages );
        s.WriteObject( o._persons );
        s.WriteObject( o._cars );
    }

    #endregion
}
