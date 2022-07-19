﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Icepack
{
    /// <summary> Maintains a collection of type metadata used for serialization/deserialization operations. </summary>
    internal sealed class TypeRegistry
    {
        /// <summary> Maps a type to metadata about the type. </summary>
        private readonly Dictionary<Type, TypeMetadata> types;

        /// <summary> Creates a new type registry. </summary>
        public TypeRegistry()
        {
            types = new Dictionary<Type, TypeMetadata>();
        }

        /// <summary> Registers a type as serializable. </summary>
        /// <param name="type"> The type to register. </param>
        /// <remarks> This is used to allow types in other assemblies to be serialized, or for arrays and concrete generic classes. </remarks>
        public TypeMetadata RegisterType(Type type)
        {
            if (types.ContainsKey(type))
                return types[type];

            var newTypeMetadata = new TypeMetadata(type, this);
            types.Add(type, newTypeMetadata);

            return newTypeMetadata;
        }

        /// <summary>
        /// Called during type registration and serialization. Retrieves the metadata for a type and
        /// lazy-registers types that have the <see cref="SerializableObjectAttribute"/> attribute.
        /// </summary>
        /// <param name="type"> The type to retrieve metadata for. </param>
        /// <returns> The metadata for the type. </returns>
        public TypeMetadata GetTypeMetadata(Type type)
        {
            // Treat all type values as instances of Type, for simplicity.
            if (type.IsSubclassOf(typeof(Type)))
                return types[typeof(Type)];

            TypeMetadata typeMetadata;
            if (types.TryGetValue(type, out typeMetadata))
                return typeMetadata;

            // Register arrays, lists, hashsets, and dictionaries by default
            if (type.IsArray)
                GetTypeMetadata(type.GetElementType());
            else if (type.IsGenericType)
            {
                Type genericTypeDef = type.GetGenericTypeDefinition();
                if (genericTypeDef == typeof(List<>) || genericTypeDef == typeof(HashSet<>))
                {
                    Type[] genericArgs = type.GetGenericArguments();
                    GetTypeMetadata(genericArgs[0]);
                }
                else if (genericTypeDef == typeof(Dictionary<,>))
                {
                    Type[] genericArgs = type.GetGenericArguments();
                    GetTypeMetadata(genericArgs[0]);
                    GetTypeMetadata(genericArgs[1]);
                }
                else
                {
                    object[] attributes = type.GetCustomAttributes(typeof(SerializableObjectAttribute), true);
                    if (attributes.Length == 0)
                        throw new IcepackException($"Type {type} is not registered for serialization!");
                }
            }
            else
            {
                object[] attributes = type.GetCustomAttributes(typeof(SerializableObjectAttribute), true);
                if (attributes.Length == 0)
                    throw new IcepackException($"Type {type} is not registered for serialization!");
            }

            var newTypeMetadata = new TypeMetadata(type, this);
            types.Add(type, newTypeMetadata);
            return newTypeMetadata;
        }

        /// <summary>
        /// Called during deserialization to match a type name to a registered type. It lazy-registers types that
        /// have the <see cref="SerializableObjectAttribute"/> attribute.
        /// </summary>
        /// <param name="name"> The assembly qualified name of the type. </param>
        /// <returns> Metadata for the specified type. </returns>
        public TypeMetadata GetTypeMetadata(string name)
        {
            Type type = Type.GetType(name);
            if (type == null)
                return null;

            return GetTypeMetadata(type);
        }
    }
}
