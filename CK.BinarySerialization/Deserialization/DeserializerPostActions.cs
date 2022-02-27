using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Deserialization
{
    /// <summary>
    /// Very simple implementation of deferred actions.
    /// </summary>
    public sealed class PostActions
    {
        readonly List<Action> _postDeserializationActions;

        internal PostActions()
        {
            _postDeserializationActions = new List<Action>();
        }

        /// <summary>
        /// Registers an action that will be executed once all objects are deserialized.
        /// </summary>
        /// <param name="a">An action to be registered. Must not be null.</param>
        public void Add( Action a )
        {
            if( a == null ) throw new ArgumentNullException();
            _postDeserializationActions.Add( a );
        }

        /// <summary>
        /// Executes all the actions that have been registered by <see cref="Add(Action)"/>
        /// (typically from deserialization constructors) and clears the list.
        /// If not called explicitly, this is automatically called when the <see cref="IBinaryDeserializer"/>
        /// is disposed.
        /// <para>
        /// When called explicitly, it should be done once a whole object graph has been deserialized: this
        /// can be used when multiple disjoint object graphs are deserialized (since the registered actions are
        /// cleared after their execution).
        /// <para>
        /// Under normal scenario, disposing the deserializer does the job.
        /// </para>
        /// </para>
        /// </summary>
        public void Execute()
        {
            foreach( var action in _postDeserializationActions )
            {
                action();
            }
            _postDeserializationActions.Clear();
        }


    }
}
