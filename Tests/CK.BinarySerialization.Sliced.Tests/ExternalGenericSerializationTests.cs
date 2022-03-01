using NUnit.Framework;
using CK.Core;
using System.Collections;
using System.Collections.Generic;
using static CK.Testing.MonitorTestHelper;
using FluentAssertions;

namespace CK.BinarySerialization.Tests
{
    [TestFixture]
    public class ExternalGenericSerializationTests
    {
        [SerializationVersion( 0 )]
        public class OList<T> : ICKSlicedSerializable, IEnumerable<T>
        {
            readonly List<T> _list;

            public OList()
            {
                _list = new List<T>();
            }

            public void Add( T item ) => _list.Add( item );

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
            protected OList( Sliced _ ) { }
#pragma warning restore CS8618 

            OList( IBinaryDeserializer d, ITypeReadInfo info )
            {
                _list = d.ReadObject<List<T>>();
            }

            public static void Write( IBinarySerializer s, in OList<T> o )
            {
                s.WriteObject( o._list );
            }

            public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)_list).GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_list).GetEnumerator();
        }

        [Test]
        public void a_generic_list()
        {
            var o = new OList<int>() { 45, 12, 3712 };

            var backO = TestHelper.SaveAndLoadObject( o );
            backO.Should().BeEquivalentTo( o );
        }
    }
}
