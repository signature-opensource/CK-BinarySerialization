using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Tests.Samples
{
    [SerializationVersion(0)]
    public class Person : ICKSlicedSerializable
    {
        public Person()
        {
            Friends = new List<Person>();
        }

        public string? Name { get; set; }

        public List<Person> Friends { get; }

        #region Serialization

#pragma warning disable CS8618 
        protected Person( Sliced _ ) { }
#pragma warning restore CS8618

        public Person( IBinaryDeserializer d, ITypeReadInfo info )
        {
            Name = d.Reader.ReadNullableString();
            Friends = d.ReadObject<List<Person>>();
        }

        public static void Write( IBinarySerializer s, in Person o )
        {
            s.Writer.WriteNullableString( o.Name );
            s.WriteObject( o.Friends );
        }

        #endregion
    }
}
