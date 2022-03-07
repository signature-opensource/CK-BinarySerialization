using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    [SerializationVersion(0)]
    public class Person : ICKSlicedSerializable, IDestroyable
    {
        public Person( Town town )
        {
            Town = town;
            town.OnNewPerson( this );
            Friends = new List<Person>();
        }

        public Town Town { get; }

        public string? Name { get; set; }

        public List<Person> Friends { get; }

        public bool IsDestroyed { get; private set; }

        public void Destroy()
        {
            if( !IsDestroyed )
            {
                Town.OnDestroying( this );
                IsDestroyed = true;
            }
        }

        #region Serialization

#pragma warning disable CS8618
        protected Person( Sliced _ ) { }
#pragma warning restore CS8618

        public Person( IBinaryDeserializer d, ITypeReadInfo info )
        {
            IsDestroyed = d.Reader.ReadBoolean();
            Name = d.Reader.ReadNullableString();
            Friends = d.ReadObject<List<Person>>();
            Town = d.ReadObject<Town>();
        }

        public static void Write( IBinarySerializer s, in Person o )
        {
            s.Writer.Write( o.IsDestroyed );
            s.Writer.WriteNullableString( o.Name );
            s.WriteObject( o.Friends );
            s.WriteObject( o.Town );
        }

        #endregion
    }
}
