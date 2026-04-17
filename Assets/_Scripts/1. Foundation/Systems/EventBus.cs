using System;
using System.Collections.Generic;
using UnityEngine;

namespace Foundation
{
    public static class EventBus
    { 
        private static readonly Dictionary<Type, Delegate> _handlers = new();

        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            _handlers[type] = _handlers.TryGetValue(type, out var existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        public static void Unsubscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var existing))
            {
                var updated = Delegate.Remove(existing, handler);
                if (updated == null) _handlers.Remove(type);
                else _handlers[type] = updated;
            }
        }
        
        public static void Publish<T>(T evt)
        {
            if (!_handlers.TryGetValue(typeof(T), out var handler))
                return;
            
            //Unpack the multicast delegate into individual subscribers
            Delegate[] invocationList = handler.GetInvocationList();

            foreach (Delegate d in invocationList)
            {
                var action =  (Action<T>)d;
                
                //Check if the delegate's target is a Unity object that has been destroyed.
                //Unity's overloaded '==' operator accurately checks the underlying native C++ object.
                if (action.Target is UnityEngine.Object unityTarget && unityTarget == null)
                {
#if UNITY_EDITOR
                    //Log a helpful warning identifying exactly what leaked
                    Type targetType = action.Target.GetType();
                    Debug.LogWarning($"[EventBus] Dead delegate detected for event {typeof(T).Name}! "+
                                     $"A subscriber of type {targetType.Name} was destroyed without unsubscribing. Auto-cleaning.");
#endif
                    //Automatically clean up the leak
                    Unsubscribe(action);
                }
                else
                {
                    //Target is valid static (Target is null), or a non-Unity C# object. Safe to invoke.
                    action.Invoke(evt);
                }
            }
        }
        
        //Called by GameStateManager
        public static void Clear()
        {
            _handlers.Clear();
        }
    }
}