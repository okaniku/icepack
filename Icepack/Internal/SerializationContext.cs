﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Icepack
{
    internal class SerializationContext
    {
        public Dictionary<Type, TypeMetadata> Types { get; }
        public List<TypeMetadata> TypesInOrder { get; }
        public Dictionary<object, ObjectMetadata> Objects { get; }
        public List<ObjectMetadata> ObjectsInOrder { get; }

        private uint largestInstanceId;
        private uint largestTypeId;
        private readonly TypeRegistry typeRegistry;

        public SerializationContext(TypeRegistry typeRegistry)
        {
            Objects = new Dictionary<object, ObjectMetadata>();
            ObjectsInOrder = new List<ObjectMetadata>();
            Types = new Dictionary<Type, TypeMetadata>();
            TypesInOrder = new List<TypeMetadata>();

            largestInstanceId = 0;
            largestTypeId = 0;
            this.typeRegistry = typeRegistry;
        }

        public uint RegisterObject(object obj)
        {
            uint newId = ++largestInstanceId;
            Type type = obj.GetType();
            if (type.IsSubclassOf(typeof(Type)))
                type = typeof(Type);
            TypeMetadata typeMetadata = GetTypeMetadata(type);

            int length = 0;
            switch (typeMetadata.Category)
            {
                case TypeCategory.Immutable:
                    break;
                case TypeCategory.Array:
                    length = ((Array)obj).Length;
                    break;
                case TypeCategory.List:
                    length = ((IList)obj).Count;
                    break;
                case TypeCategory.HashSet:
                    length = 0;
                    foreach (object item in (IEnumerable)obj)
                        length++;
                    break;
                case TypeCategory.Dictionary:
                    length = ((IDictionary)obj).Count;
                    break;
                case TypeCategory.Struct:
                case TypeCategory.Class:
                case TypeCategory.Enum:
                case TypeCategory.Type:
                    break;
                default:
                    throw new IcepackException($"Invalid category ID: {typeMetadata.Category}");
            }

            ObjectMetadata objMetadata = new(newId, typeMetadata, length, obj);
            Objects.Add(obj, objMetadata);
            ObjectsInOrder.Add(objMetadata);
            
            return newId;
        }

        /// <summary> Retrieves the metadata for a type. </summary>
        /// <param name="type"> The type to retrieve metadata for. </param>
        /// <returns> The metadata for the type. </returns>
        /// <remarks> This method lazy-registers types that have the <see cref="SerializableObjectAttribute"/> attribute. </remarks>
        public TypeMetadata GetTypeMetadata(Type type)
        {
            if (!Types.ContainsKey(type))
            {
                // If this is an enum, we want the underlying type to exist ahead of the enum type
                TypeMetadata enumUnderlyingTypeMetadata = null;
                if (type.IsEnum)
                    enumUnderlyingTypeMetadata = GetTypeMetadata(type.GetEnumUnderlyingType());

                TypeMetadata registeredTypeMetadata = typeRegistry.GetTypeMetadata(type);
                if (registeredTypeMetadata == null)
                    throw new IcepackException($"Type {type} is not registered for serialization!");

                TypeMetadata newTypeMetadata = new(registeredTypeMetadata, ++largestTypeId, enumUnderlyingTypeMetadata);
                Types.Add(type, newTypeMetadata);
                TypesInOrder.Add(newTypeMetadata);
                return newTypeMetadata;
            }

            return Types[type];
        }
    }
}
