using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    [SerializationVersion( 0 )]
    public sealed class Town : ICKSlicedSerializable
    {
        readonly List<Person> _persons;
        readonly List<Garage> _garages;

        public Town( string name )
        {
            Name = name;
            _persons = new List<Person>();
            _garages = new List<Garage>();
        }

        public string Name { get; }

        public IReadOnlyList<Person> Persons => _persons;

        public IReadOnlyList<Garage> Garages => _garages;

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
            Name = d.Reader.ReadString();
            _garages = d.ReadObject<List<Garage>>();
            _persons = d.ReadObject<List<Person>>();
        }

        public static void Write( IBinarySerializer s, in Town o )
        {
            s.Writer.Write( o.Name );
            s.WriteObject( o._garages );
            s.WriteObject( o._persons );
        }

        #endregion
    }
}
