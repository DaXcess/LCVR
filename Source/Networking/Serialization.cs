using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using UnityEngine;

namespace LCVR.Networking;

/// <summary>
/// A special serializer that can be used within the networking system.
///
/// This serializer allows easy serialization from structs and classes to bytes and vice-versa.
/// It supports most basic primitive types (numbers, bools, strings), Vector3's, Quaternions, and any other class
/// annotated with the [Serialize] attribute.
///
/// Arrays of any supported type are also supported (max num of items is 2^31)
/// </summary>
public static class Serialization
{
    private static readonly Dictionary<Type, FieldInfo[]> typeCache = [];
    
    private static IEnumerable<FieldInfo> GetFields(Type type)
    {
        // Try from cache first
        if (typeCache.TryGetValue(type, out var fields))
        {
            if (fields is null)
                throw new ArgumentException(@"Object is not marked serializable");
            
            return fields;
        }
        
        // Check to see if type is marked serializable
        if (type.GetCustomAttribute<SerializeAttribute>() is null && !type.IsEnum)
        {
            typeCache[type] = null;
            throw new ArgumentException(@"Object is not marked serializable");
        }
        
        // List all fields, discard any field with the SkipSerialize attribute
        var allFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(
            field => field.GetCustomAttribute<SkipSerializeAttribute>() is null
        );

        // Cache the fields
        typeCache[type] = allFields.ToArray();
        return typeCache[type];
    }
    
    /// <summary>
    /// Serialize an object to bytes
    /// </summary>
    /// <param name="object">The object to serialize. The underlying type must be annotated with [Serialize]</param>
    /// <returns>A byte representation of the provided object</returns>
    public static byte[] Serialize(object @object)
    {
        var fields = GetFields(@object.GetType());

        using var ms = new MemoryStream();
        using var bw = new BinaryWriter(ms);

        foreach (var field in fields)
        {
            var value = field.GetValue(@object);

            if (value.GetType().IsArray)
            {
                var array = (Array)value;
                bw.Write(array.Length);

                foreach (var arrayEl in array)
                    bw.SerializePrimitive(arrayEl, arrayEl.GetType());
            }
            else
                bw.SerializePrimitive(value, value.GetType());
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Deserialize some data
    /// </summary>
    /// <param name="data">The raw data to deserialize</param>
    /// <typeparam name="T">A type that is annotated with [Serialize]</typeparam>
    public static T Deserialize<T>(byte[] data) where T: new()
    {
        using var ms = new MemoryStream(data);
        using var br = new BinaryReader(ms);

        return (T)DeserializeInternal(br, typeof(T));
    }

    /// <summary>
    /// Deserialize some data
    /// </summary>
    /// <param name="br">A reader to the raw data to deserialize</param>
    /// <typeparam name="T">A type that is annotated with [Serialize]</typeparam>
    public static T Deserialize<T>(BinaryReader br) where T: new()
    {
        return (T)DeserializeInternal(br, typeof(T));
    }

    private static object DeserializeInternal(BinaryReader br, Type type)
    {
        var fields = GetFields(type);
        var result = Activator.CreateInstance(type);

        foreach (var field in fields)
        {
            if (field.FieldType.IsArray)
            {
                var size = br.ReadInt32();
                var targetArray = Array.CreateInstance(field.FieldType.GetElementType()!, size);

                for (var i = 0; i < size; i++)
                    targetArray.SetValue(br.DeserializePrimitive(field.FieldType.GetElementType()!), i);

                field.SetValue(result, targetArray);
            }
            else
                field.SetValue(result, br.DeserializePrimitive(field.FieldType));
        }

        return result;
    }

    private static void SerializePrimitive(this BinaryWriter bw, object value, Type type)
    {
        if (type.IsEnum)
        {
            type = Enum.GetUnderlyingType(type);
            value = Convert.ChangeType(value, type);
        }
        
        switch (value)
        {
            case bool boolValue:
                bw.Write(boolValue);
                return;
            case byte byteValue:
                bw.Write(byteValue);
                return;
            case sbyte sbyteValue:
                bw.Write(sbyteValue);
                return;
            case short shortValue:
                bw.Write(shortValue);
                return;
            case ushort ushortValue:
                bw.Write(ushortValue);
                return;
            case int intValue:
                bw.Write(intValue);
                return;
            case uint uintValue:
                bw.Write(uintValue);
                return;
            case long longValue:
                bw.Write(longValue);
                return;
            case ulong ulongValue:
                bw.Write(ulongValue);
                return;
            case decimal decimalValue:
                bw.Write(decimalValue);
                return;
            case float floatValue:
                bw.Write(floatValue);
                return;
            case double doubleValue:
                bw.Write(doubleValue);
                return;
            case string stringValue:
                bw.Write(stringValue);
                return;
            case Vector3 vector3Value:
                bw.Write(vector3Value.x);
                bw.Write(vector3Value.y);
                bw.Write(vector3Value.z);
                return;
            case Quaternion quaternionValue:
                bw.Write(quaternionValue.x);
                bw.Write(quaternionValue.y);
                bw.Write(quaternionValue.z);
                bw.Write(quaternionValue.w);
                return;
        }
        
        if (type.GetCustomAttribute<SerializeAttribute>() is null)
            throw new ArgumentException(@"Object contains non-serializable fields");

        bw.Write(Serialize(value));
    }

    private static object DeserializePrimitive(this BinaryReader br, [NotNull] Type type)
    {
        if (type.IsEnum)
            type = Enum.GetUnderlyingType(type);
        
        switch (type)
        {
            case not null when type == typeof(bool):
                return br.ReadBoolean();
            case not null when type == typeof(byte):
                return br.ReadByte();
            case not null when type == typeof(sbyte):
                return br.ReadSByte();
            case not null when type == typeof(short):
                return br.ReadInt16();
            case not null when type == typeof(ushort):
                return br.ReadUInt16();
            case not null when type == typeof(int):
                return br.ReadInt32();
            case not null when type == typeof(uint):
                return br.ReadUInt32();
            case not null when type == typeof(long):
                return br.ReadInt64();
            case not null when type == typeof(ulong):
                return br.ReadUInt64();
            case not null when type == typeof(decimal):
                return br.ReadDecimal();
            case not null when type == typeof(float):
                return br.ReadSingle();
            case not null when type == typeof(double):
                return br.ReadDouble();
            case not null when type == typeof(string):
                return br.ReadString();
            case not null when type == typeof(Vector3):
                return new Vector3(br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
            case not null when type == typeof(Quaternion):
                return new Quaternion(br.ReadSingle(), br.ReadSingle(), br.ReadSingle(), br.ReadSingle());
        }
        
        if (type.GetCustomAttribute<SerializeAttribute>() is null)
            throw new ArgumentException(@"Object contains non-serializable fields");

        return DeserializeInternal(br, type);
    }
}

public class SerializeAttribute : Attribute {}
public class SkipSerializeAttribute : Attribute {}
