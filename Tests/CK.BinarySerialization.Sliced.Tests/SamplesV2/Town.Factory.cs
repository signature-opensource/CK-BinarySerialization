using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.SamplesV2;

partial class Town
{
    public static Town CreateTown( int? randomSeed = null )
    {
        var rnd = randomSeed.HasValue ? new Random( randomSeed.Value ) : new Random();
        var town = new Town( $"Random{randomSeed}" );
        int nbGarage = rnd.Next( 5 ) + 1;
        int nbCustomer = rnd.Next( 100 ) + 1;
        for( int i = 0; i < nbGarage; i++ )
        {
            CreateGarage( rnd, town );
        }
        int iCar = 0;
        // 90% of the customers have a Car.
        // Out of them, 50% are in a garage.
        for( int i = 0; i < nbCustomer; i++ )
        {
            var c = new Customer( town ) { Name = $"Customer n°{i}" };
            if( rnd.Next( 100 ) <= 90 )
            {
                var car = town.AddCar( $"n°{++iCar}", DateTime.UtcNow );
                c.Car = car;
                if( rnd.Next( 100 ) <= 50 )
                {
                    var g = town.Garages[rnd.Next( town.Garages.Count )];
                    var e = g.Employees[rnd.Next( g.Employees.Count )];
                    e.CurrentCar = car;
                    c.Contact = e;
                }
            }
        }
        foreach( var p in town.Persons ) BindFriends( rnd, p );
        // More cars
        int nbCar = rnd.Next( 10 ) + 10;
        for( int i = 0; i < nbCar; i++ )
        {
            town.AddCar( $"n°{++iCar}", DateTime.UtcNow );
        }
        return town;
    }

    static void CreateGarage( Random rnd, Town town )
    {
        var g = new Garage( town );
        int nbManager = rnd.Next( 10 ) + 1;
        for( int i = 0; i < nbManager; i++ )
        {
            new Manager( g ) { Name = $"Manager n°{i}", EmployeeNumber = i, Rank = rnd.Next( 10 ) };
        }
        int nbEmployee = rnd.Next( 100 ) + 1;
        for( int i = 0; i < nbEmployee; i++ )
        {
            new Employee( g ) { Name = $"Employee n°{i}", EmployeeNumber = i };
        }
    }

    static void BindFriends( Random rnd, Person e )
    {
        int nbFriends = rnd.Next( e.Town.Persons.Count );
        for( int i = 0; i < nbFriends; i++ )
        {
            var p = e.Town.Persons[rnd.Next( e.Town.Persons.Count )];
            if( p != e && !e.Friends.Contains( p ) )
            {
                e.Friends.Add( p );
            }
        }
    }
}
