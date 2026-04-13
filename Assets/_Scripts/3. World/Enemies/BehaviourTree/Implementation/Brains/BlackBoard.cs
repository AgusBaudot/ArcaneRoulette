using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace World 
{
    
    [SerializeField]
    public readonly struct BlackboardKey : IEquatable<BlackboardKey> 
    {
        readonly string name;
        readonly int hashedKey;

        public BlackboardKey (string name) 
        {
            this.name = name;
            hashedKey = name.ComputeFNV1aHash();
        }

        public bool Equals(BlackboardKey other) => hashedKey == other.hashedKey;

        public override bool Equals(object obj) => obj is BlackboardKey other && Equals(other);
        public override int GetHashCode() => hashedKey;
        public override string ToString() => name;

        public static bool operator ==(BlackboardKey lhs, BlackboardKey rhs) => lhs.hashedKey == rhs.hashedKey;
        public static bool operator !=(BlackboardKey lhs, BlackboardKey rhs) => !(lhs == rhs);
    }

    public class BlackboardEntry<T> 
    {
        public BlackboardKey Key { get; }
        public T Value { get; }
        public Type ValueType { get; }

        public BlackboardEntry (BlackboardKey key, T value) 
        {
            Key = key; 
            Value = value;
            ValueType = typeof (T);
        }

        public override bool Equals(object obj) => obj is BlackboardEntry<T> other && other.Key.Equals(Key);
        public override int GetHashCode() => Key.GetHashCode(); 
    }

    public class Blackboard
    {
        Dictionary<string, BlackboardKey> keyRegistry = new();
        Dictionary<BlackboardKey, object> entries = new();

        public void debug() 
        {
            foreach (var entry in entries) 
            {
                var entryType = entry.Value.GetType();

                if(entryType.IsGenericType && entryType.GetGenericTypeDefinition() == typeof(BlackboardEntry<>)) 
                {
                    var valueProperty = entryType.GetProperty("Value");
                    if (valueProperty == null) continue;
                    var value = valueProperty.GetValue(entry.Value);
                    Debug.Log($"Key: {entry.Key}, Value: {value}");
                }
            }
        }

        public bool TryGetValue<T>(BlackboardKey key, out T value) 
        {
            if(entries.TryGetValue(key, out var entry) && entry is BlackboardEntry<T> castedEntry)
            {
                value = castedEntry.Value;
                return true;
            }
            value = default;
            return false;
        }

        public void SetValue<T>(BlackboardKey key, T value) 
        {
            entries[key] = new BlackboardEntry<T>(key, value);
        }

        public BlackboardKey GetOrRegisterKey(string KeyName) 
        {
            if (KeyName == null)
                throw new ArgumentNullException(nameof(KeyName));

            if(!keyRegistry.TryGetValue(KeyName, out BlackboardKey key)) 
            {
                key = new BlackboardKey(KeyName);
                keyRegistry[KeyName] = key;
            }
            return key;
        }

        public bool ContainsKey(BlackboardKey key) => entries.ContainsKey(key);

        public void Remove(BlackboardKey key) => entries.Remove(key);
    }

    [CreateAssetMenu(fileName = "New Blackboard Data", menuName = "Blackboard/Blackboard Data")]
    public class BlackboardData : ScriptableObject 
    {
        public List<BlackboardEntryData> entries = new();

        public void SetValuesOnBlackboard(Blackboard blackboard) 
        {
             foreach (var entry in entries) 
            {
                entry.SetValueOnBlackboard(blackboard);
            }
        }
    }

    [SerializeField]
    public class BlackboardEntryData : ISerializationCallbackReceiver 
    {
        public string keyName;
        public AnyValue.ValueType valueType;
        public AnyValue value;

        public void SetValueOnBlackboard (Blackboard blackboard) 
        {
            var key = blackboard.GetOrRegisterKey(keyName);
            setValueDispatchTable[value.type](blackboard, key, value);
        }

        static Dictionary<AnyValue.ValueType, Action<Blackboard, BlackboardKey, AnyValue>> setValueDispatchTable = new()
        {
            { AnyValue.ValueType.Int, (blackboard, key, anyValue) => blackboard.SetValue<int>(key, anyValue) },
            { AnyValue.ValueType.Float, (blackboard, key, anyValue) => blackboard.SetValue<float>(key, anyValue) },
            { AnyValue.ValueType.Bool, (blackboard, key, anyValue) => blackboard.SetValue<bool>(key, anyValue) },
            { AnyValue.ValueType.String, (blackboard, key, anyValue) => blackboard.SetValue<string>(key, anyValue) },
            { AnyValue.ValueType.Vector3, (blackboard, key, anyValue) => blackboard.SetValue<Vector3>(key, anyValue) },
        };

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() => value.type = valueType;
    }

    [SerializeField]
    public struct AnyValue
    {
        public enum ValueType { Int, Float, Bool, String, Vector3}
        public ValueType type;

        public int intValue;
        public float floatValue;
        public bool boolValue;
        public string stringValue;
        public Vector3 vector3Value;

        public static implicit operator int(AnyValue value) => value.ConvertValue<int>();
        public static implicit operator float(AnyValue value) => value.ConvertValue<float>();
        public static implicit operator bool(AnyValue value) => value.ConvertValue<bool>();
        public static implicit operator string(AnyValue value) => value.ConvertValue<string>();
        public static implicit operator Vector3(AnyValue value) => value.ConvertValue<Vector3>();

        T ConvertValue<T>() 
        {
            return type switch
            {
                ValueType.Int => AsInt<T>(intValue),
                ValueType.Float => AsFloat<T>(floatValue),
                ValueType.Bool => AsBool<T>(boolValue),
                ValueType.String => (T)(object)stringValue,
                ValueType.Vector3 => AsVector3<T>(vector3Value),
                _ => throw new NotSupportedException($"Not supported value type: {typeof(T)}")
            };
        }

        T AsBool<T>(bool value) => typeof(T) == typeof(bool) && value is T correctType ? correctType : default;
        T AsInt<T>(int value) => typeof(T) == typeof(int) && value is T correctType ? correctType : default;
        T AsFloat<T>(float value) => typeof(T) == typeof(float) && value is T correctType ? correctType : default;
        T AsVector3<T>(Vector3 value) => typeof(T) == typeof(Vector3) && value is T correctType ? correctType : default;
    }

    public static class StringExtensions 
    {
        public static int ComputeFNV1aHash(this string str)
        {
            uint hash = 2166136261;
            foreach (char c in str)
            {
                hash = (hash ^ c) * 16777619;
            }
            return unchecked((int)hash);
        }
    }

}
