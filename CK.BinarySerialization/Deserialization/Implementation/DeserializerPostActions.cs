using CK.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CK.BinarySerialization.Deserialization;

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
        Throw.CheckNotNullArgument( a );
        _postDeserializationActions.Add( a );
    }

    internal void Execute()
    {
        foreach( var action in _postDeserializationActions )
        {
            action();
        }
        Clear();
    }

    internal void Clear()
    {
        _postDeserializationActions.Clear();
    }
}
