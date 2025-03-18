using NUnit.Framework;
using CK.Core;
using System.Collections.Generic;
using static CK.Testing.MonitorTestHelper;

namespace CK.BinarySerialization.Tests;

[TestFixture]
public class SpecializedCollectionTests
{
    [SerializationVersion( 0 )]
    sealed class SpecHashSet<T> : HashSet<T>, ICKSlicedSerializable where T : class
    {
        public SpecHashSet()
        {
        }

        public SpecHashSet( IBinaryDeserializer d, ITypeReadInfo info )
        {
            int c = d.Reader.ReadInt32();
            while( --c >= 0 )
            {
                Add( d.ReadObject<T>() );
            }
        }

        public static void Write( IBinarySerializer s, in SpecHashSet<T> o )
        {
            s.Writer.Write( o.Count );
            foreach( var e in o )
            {
                s.WriteObject( e );
            }
        }

    }

    [Test]
    public void a_specialized_HashSet()
    {
        var o = new SpecHashSet<string>() { "a", "b", "c" };
        var backO = TestHelper.SaveAndLoadObject( o );
        backO.ShouldBe( o );
    }

}
