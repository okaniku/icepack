using NUnit.Framework;
using Icepack;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Text;

namespace IcepackTest
{
    public class SerializationTests
    {
        [SerializableObject]
        private class FlatClass
        {
            public int Field1;
            public string Field2;
            public float Field3;
        }

        [SerializableObject]
        private struct SerializableStruct
        {
            public int Field1;
            public int Field2;
        }

        [SerializableObject]
        private class HierarchicalObject
        {
            public int Field1;
            public HierarchicalObject Nested;
        }

        [SerializableObject]
        private class ParentClass
        {
            private int field;

            public ParentClass(int field)
            {
                this.field = field;
            }

            public ParentClass() { }

            public int ParentField
            {
                get { return field; }
            }
        }

        [SerializableObject]
        private class ChildClass : ParentClass
        {
            private int field;

            public ChildClass(int field, int parentField) : base(parentField)
            {
                this.field = field;
            }

            public ChildClass() { }

            public int Field
            {
                get { return field; }
            }
        }

        [SerializableObject]
        private class ObjectWithIgnoredField
        {
            public int Field1 = 0;

            [IgnoreField]
            public int Field2 = 0;
        }

        [SerializableObject]
        private class RegisteredClass
        {
        }

        [SerializableObject]
        private class ObjectWithObjectReferences
        {
            public RegisteredClass Field1;
            public RegisteredClass Field2;
        }

        [SerializableObject]
        private struct StructWithObjectReferences
        {
            public FlatClass Field1;
            public int Field2;
        }

        private enum SerializableEnum
        {
            Option1,
            Option2,
            Option3
        }

        private class UnregisteredClass
        {
        }

        [SerializableObject]
        private class ClassWithIntField
        {
            public int Field1;
        }

        [SerializableObject]
        private class ClassWithObjectField
        {
            public object Field1;
            public object Field2;
            public object Field3;
        }

        [SerializableObject]
        private class ClassWithSerializationHooks : ISerializerListener
        {
            public int Field;

            public void OnBeforeSerialize()
            {
                Field = Field * 2;
            }

            public void OnAfterDeserialize()
            {
                Field = Field + 1;
            }
        }

        private struct StructWithNestedStruct
        {
            public NestedStruct Field;
        }

        private struct NestedStruct
        {
            public int Field;
        }

        [Test]
        public void SerializeFlatObject()
        {
            Serializer serializer = new Serializer();

            FlatClass obj = new FlatClass() { Field1 = 123, Field2 = "asdf", Field3 = 6.78f };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual("asdf", deserializedObj.Field2);
            Assert.AreEqual(6.78f, deserializedObj.Field3);
        }

        [Test]
        public void SerializeFlatObjectWithNullString()
        {
            Serializer serializer = new Serializer();

            FlatClass obj = new FlatClass() { Field1 = 123, Field2 = null, Field3 = 6.78f };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(null, deserializedObj.Field2);
            Assert.AreEqual(6.78f, deserializedObj.Field3);
        }

        [Test]
        public void SerializeHierarchicalObject()
        {
            Serializer serializer = new Serializer();

            HierarchicalObject nestedObj = new HierarchicalObject() { Field1 = 123, Nested = null };
            HierarchicalObject rootObj = new HierarchicalObject() { Field1 = 456, Nested = nestedObj };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(rootObj, stream);
            HierarchicalObject deserializedObj = serializer.Deserialize<HierarchicalObject>(stream);
            stream.Close();

            Assert.AreEqual(456, deserializedObj.Field1);
            Assert.NotNull(deserializedObj.Nested);

            HierarchicalObject deserializedNestedObj = deserializedObj.Nested;

            Assert.AreEqual(123, deserializedNestedObj.Field1);
            Assert.Null(deserializedNestedObj.Nested);
        }

        [Test]
        public void SerializeDerivedObjectWithSameFieldNameAsParent()
        {
            Serializer serializer = new Serializer();

            ChildClass obj = new ChildClass(123, 456);

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            ChildClass deserializedObj = serializer.Deserialize<ChildClass>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field);
            Assert.AreEqual(456, deserializedObj.ParentField);
        }

        [Test]
        public void SerializeObjectWithIgnoredField()
        {
            Serializer serializer = new Serializer();

            ObjectWithIgnoredField obj = new ObjectWithIgnoredField() { Field1 = 123, Field2 = 456 };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            ObjectWithIgnoredField deserializedObj = serializer.Deserialize<ObjectWithIgnoredField>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(0, deserializedObj.Field2);
        }

        [Test]
        public void SerializeObjectReferences()
        {
            Serializer serializer = new Serializer();

            RegisteredClass nestedObj = new RegisteredClass();
            ObjectWithObjectReferences obj = new ObjectWithObjectReferences() { Field1 = nestedObj, Field2 = nestedObj };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            ObjectWithObjectReferences deserializedObj = serializer.Deserialize<ObjectWithObjectReferences>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj.Field1);
            Assert.NotNull(deserializedObj.Field2);
            Assert.AreEqual(deserializedObj.Field1, deserializedObj.Field2);            
        }

        [Test]
        public void SerializeArray()
        {
            Serializer serializer = new Serializer();
            serializer.RegisterType(typeof(int[]));

            int[] array = new int[] { 1, 2, 3 };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(array, stream);
            int[] deserializedArray = serializer.Deserialize<int[]>(stream);
            stream.Close();

            Assert.NotNull(deserializedArray);
            Assert.AreEqual(3, deserializedArray.Length);
            Assert.AreEqual(1, deserializedArray[0]);
            Assert.AreEqual(2, deserializedArray[1]);
            Assert.AreEqual(3, deserializedArray[2]);
        }

        [Test]
        public void SerializeList()
        {
            Serializer serializer = new Serializer();
            serializer.RegisterType(typeof(List<string>));

            List<string> list = new List<string>() { "qwer", "asdf", "zxcv" };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(list, stream);
            List<string> deserializedList = serializer.Deserialize<List<string>>(stream);
            stream.Close();

            Assert.NotNull(deserializedList);
            Assert.AreEqual(3, deserializedList.Count);
            Assert.AreEqual("qwer", deserializedList[0]);
            Assert.AreEqual("asdf", deserializedList[1]);
            Assert.AreEqual("zxcv", deserializedList[2]);
        }

        [Test]
        public void SerializeHashSet()
        {
            Serializer serializer = new Serializer();
            serializer.RegisterType(typeof(HashSet<string>));

            HashSet<string> set = new HashSet<string>() { "qwer", "asdf", "zxcv" };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(set, stream);
            HashSet<string> deserializedSet = serializer.Deserialize<HashSet<string>>(stream);
            stream.Close();

            Assert.NotNull(deserializedSet);
            Assert.AreEqual(3, deserializedSet.Count);
            Assert.True(deserializedSet.Contains("qwer"));
            Assert.True(deserializedSet.Contains("asdf"));
            Assert.True(deserializedSet.Contains("zxcv"));
        }

        [Test]
        public void SerializeDictionary()
        {
            Serializer serializer = new Serializer();
            serializer.RegisterType(typeof(Dictionary<int, string>));

            Dictionary<int, string> dictionary = new Dictionary<int, string>() { { 1, "asdf" }, { 2, "zxcv" } };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(dictionary, stream);
            Dictionary<int, string> deserializedDictionary = serializer.Deserialize<Dictionary<int, string>>(stream);
            stream.Close();

            Assert.NotNull(deserializedDictionary);
            Assert.AreEqual(2, deserializedDictionary.Count);
            Assert.True(deserializedDictionary.ContainsKey(1));
            Assert.True(deserializedDictionary.ContainsKey(2));
            Assert.AreEqual("asdf", deserializedDictionary[1]);
            Assert.AreEqual("zxcv", deserializedDictionary[2]);
        }

        [Test]
        public void SerializeEnum()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(SerializableEnum.Option2, stream);
            SerializableEnum deserializedEnum = serializer.Deserialize<SerializableEnum>(stream);
            stream.Close();

            Assert.AreEqual(SerializableEnum.Option2, deserializedEnum);
        }

        [Test]
        public void SerializeStruct()
        {
            Serializer serializer = new Serializer();

            SerializableStruct s = new SerializableStruct() { Field1 = 123, Field2 = 456 };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(s, stream);
            SerializableStruct deserializedStruct = serializer.Deserialize<SerializableStruct>(stream);
            stream.Close();

            Assert.AreEqual(123, deserializedStruct.Field1);
            Assert.AreEqual(456, deserializedStruct.Field2);
        }

        [Test]
        public void SerializeStructWithObjectReferences()
        {
            Serializer serializer = new Serializer();

            FlatClass nestedObj = new FlatClass() { Field1 = 234, Field2 = "asdf", Field3 = 1.23f };
            StructWithObjectReferences obj = new StructWithObjectReferences() { Field1 = nestedObj, Field2 = 123 };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            StructWithObjectReferences deserializedObj = serializer.Deserialize<StructWithObjectReferences>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj.Field1);
            Assert.AreEqual(234, deserializedObj.Field1.Field1);
            Assert.AreEqual("asdf", deserializedObj.Field1.Field2);
            Assert.AreEqual(1.23f, deserializedObj.Field1.Field3);
            Assert.AreEqual(123, deserializedObj.Field2);
        }

        [Test]
        public void SerializeUnregisteredClass()
        {
            Serializer serializer = new Serializer();
            UnregisteredClass obj = new UnregisteredClass();

            MemoryStream stream = new MemoryStream();
            Assert.Throws<IcepackException>(() => {
                serializer.Serialize(obj, stream);
            });
            stream.Close();
        }

        [Test]
        public void CompatibilityMatch()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(0);            // Number of types
            writer.Write(0);            // Number of objects
            writer.Write(true);         // Root object is value-type
            writer.Write(123);          // Root object
            writer.Close();

            Assert.DoesNotThrow(() => {
                int output = serializer.Deserialize<int>(stream);
            });

            stream.Close();
        }

        [Test]
        public void CompatibilityVersionMismatch()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write((ushort)0);    // Compatibility version
            writer.Write(0);            // Number of types
            writer.Write(0);            // Number of objects
            writer.Write(true);         // Root object is value-type
            writer.Write(123);          // Root object
            writer.Close();

            Assert.Throws<IcepackException>(() => {
                int output = serializer.Deserialize<int>(stream);
            });

            stream.Close();
        }

        [Test]
        public void DeserializeUnregisteredClass()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(1);            // Number of types
            writer.Write(typeof(UnregisteredClass).AssemblyQualifiedName);     // Type name
            writer.Write((byte)5);      // Category: class
            writer.Write(1);            // Size of type
            writer.Write(false);        // Type has no parent
            writer.Write(0);            // Number of fields
            writer.Write(1);            // Number of objects
            writer.Write((uint)1);      // Type ID
            writer.Write(false);        // Root object is reference-type
            writer.Write((uint)1);      // Type ID
            writer.Close();

            Assert.Throws<IcepackException>(() => {
                serializer.Deserialize<UnregisteredClass>(stream);
            });

            stream.Close();
        }

        [Test]
        public void DeserializeRegisteredClassWithoutPriorSerialization()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(1);            // Number of types
            writer.Write(typeof(RegisteredClass).AssemblyQualifiedName);     // Type name
            writer.Write((byte)6);      // Category: class
            writer.Write(1);            // Size of type
            writer.Write(false);        // Type has no parent
            writer.Write(0);            // Number of fields
            writer.Write(1);            // Number of objects
            writer.Write((uint)1);      // Type ID
            writer.Write(false);        // Root object is reference-type
            writer.Write((uint)1);      // Type ID
            writer.Close();

            RegisteredClass deserializedObj = serializer.Deserialize<RegisteredClass>(stream);

            Assert.NotNull(deserializedObj);
        }

        [Test]
        public void DeserializeClassWithAdditionalField()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(1);            // Number of types
            writer.Write(typeof(FlatClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(8);            // Type size
            writer.Write(false);        // Has no parent
            writer.Write(2);            // Number of fields
            writer.Write("Field1");
            writer.Write(4);            // Size of int
            writer.Write("Field3");
            writer.Write(4);            // Size of float
            writer.Write(1);            // Number of objects
            writer.Write((uint)1);      // Type ID of root object
            writer.Write(false);        // Root object is reference-type
            writer.Write((uint)1);      // Type ID of root object
            writer.Write(123);          // Field1
            writer.Write(2.34f);        // Field3
            writer.Close();

            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream);

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual(null, deserializedObj.Field2);
            Assert.AreEqual(2.34f, deserializedObj.Field3);
        }

        [Test]
        public void DeserializeClassWithDeletedReferenceField()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(2);            // Number of types
            writer.Write(typeof(string).AssemblyQualifiedName);
            writer.Write((byte)0);      // Category: string
            writer.Write(typeof(FlatClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(16);           // Type size
            writer.Write(false);        // Has no parent
            writer.Write(4);            // Number of fields
            writer.Write("Field1");
            writer.Write(4);            // Size of int
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field2andHalf");
            writer.Write(4);            // Size of object reference
            writer.Write("Field3");
            writer.Write(4);            // Size of float
            writer.Write(3);            // Number of objects
            writer.Write((uint)2);      // Type ID of root object
            writer.Write((uint)1);      // Type ID of object 2
            writer.Write("asdf");       // String value
            writer.Write((uint)1);      // Type ID of object 3
            writer.Write("some stuff"); // String value
            writer.Write(false);        // Root object is reference-type
            writer.Write((uint)2);      // Type ID of root object
            writer.Write(123);          // Field1
            writer.Write((uint)2);      // Field2
            writer.Write((uint)3);      // Field2andHalf
            writer.Write(2.34f);        // Field3
            writer.Close();

            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream);

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual("asdf", deserializedObj.Field2);
            Assert.AreEqual(2.34f, deserializedObj.Field3);
        }

        [Test]
        public void DeserializeClassWithDeletedStructField()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types
            writer.Write(typeof(string).AssemblyQualifiedName);
            writer.Write((byte)0);      // Category: string
            writer.Write(typeof(SerializableStruct).AssemblyQualifiedName);
            writer.Write((byte)5);      // Category: struct
            writer.Write(12);           // Type size (int + int + 4)
            writer.Write(2);            // 2 fields
            writer.Write("Field1");
            writer.Write(4);            // int
            writer.Write("Field2");
            writer.Write(4);            // int
            writer.Write(typeof(FlatClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(16);           // Type size
            writer.Write(false);        // Has no parent
            writer.Write(4);            // Number of fields
            writer.Write("Field1");
            writer.Write(4);            // Size of int
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field2andHalf");
            writer.Write(12);           // Size of struct
            writer.Write("Field3");
            writer.Write(4);            // Size of float
            writer.Write(2);            // Number of objects
            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)1);      // Type ID of object 2
            writer.Write("asdf");       // String value
            writer.Write(false);        // Root object is reference-type
            writer.Write((uint)3);      // Type ID of root object
            writer.Write(123);          // Field1
            writer.Write((uint)2);      // Field2
            writer.Write((uint)2);      // Field2andHalf type ID
            writer.Write(222);          // struct Field1
            writer.Write(444);          // struct Field2
            writer.Write(2.34f);        // Field3
            writer.Close();

            FlatClass deserializedObj = serializer.Deserialize<FlatClass>(stream);

            Assert.NotNull(deserializedObj);
            Assert.AreEqual(123, deserializedObj.Field1);
            Assert.AreEqual("asdf", deserializedObj.Field2);
            Assert.AreEqual(2.34f, deserializedObj.Field3);
        }

        [Test]
        public void DeserializeClassWithDeletedClassType()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types
            
            writer.Write("MissingClassName");
            writer.Write((byte)6);      // Category: class
            writer.Write(0);            // Type size
            writer.Write(false);        // Has no parent
            writer.Write(0);            // Number of fields

            writer.Write(typeof(RegisteredClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(0);            // Type size
            writer.Write(false);        // Has no parent
            writer.Write(0);            // Number of fields
            
            writer.Write(typeof(ClassWithObjectField).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(12);           // Type size
            writer.Write(false);        // Has no parent
            writer.Write(3);            // Number of fields            
            writer.Write("Field1");
            writer.Write(4);            // Size of object reference
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field3");
            writer.Write(4);            // Size of object reference

            writer.Write(4);            // Number of objects
            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)2);      // Type ID of object 2
            writer.Write((uint)1);      // Type ID of object 3
            writer.Write((uint)2);      // Type ID of object 4

            writer.Write(false);        // Root object is reference-type

            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)2);      // Field1
            writer.Write((uint)3);      // Field2
            writer.Write((uint)4);      // Field3
            
            writer.Write((uint)2);      // Type ID of object 2
            
            writer.Write((uint)1);      // Type ID of object 3
            
            writer.Write((uint)2);      // Type ID of object 4
            writer.Close();

            ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1.GetType() == typeof(RegisteredClass));
            Assert.Null(deserializedObj.Field2);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3.GetType() == typeof(RegisteredClass));
        }

        [Test]
        public void DeserializeClassWithDeletedArrayType()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types

            writer.Write("MissingClassName[]");
            writer.Write((byte)1);      // Category: array
            writer.Write(4);            // Item size

            writer.Write(typeof(ClassWithIntField).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(4);            // Type size
            writer.Write(false);        // Has no parent
            writer.Write(1);            // Number of fields
            writer.Write("Field1");     // Field1
            writer.Write(4);            // Field1 size

            writer.Write(typeof(ClassWithObjectField).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(12);           // Type size
            writer.Write(false);        // Has no parent
            writer.Write(3);            // Number of fields            
            writer.Write("Field1");
            writer.Write(4);            // Size of object reference
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field3");
            writer.Write(4);            // Size of object reference

            writer.Write(4);            // Number of objects
            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)2);      // Type ID of object 2
            writer.Write((uint)1);      // Type ID of object 3
            writer.Write(3);            // Array length
            writer.Write((uint)2);      // Type ID of object 4

            writer.Write(false);        // Root object is reference-type

            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)2);      // Field1
            writer.Write((uint)3);      // Field2
            writer.Write((uint)4);      // Field3

            writer.Write((uint)2);      // Type ID of object 2
            writer.Write(123);          // Field1

            writer.Write(1);            // Array[0]
            writer.Write(2);            // Array[1]
            writer.Write(3);            // Array[2]

            writer.Write((uint)2);      // Type ID of object 4
            writer.Write(456);          // Field1
            writer.Close();

            ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1.GetType() == typeof(ClassWithIntField));
            Assert.AreEqual(123, ((ClassWithIntField)deserializedObj.Field1).Field1);
            Assert.Null(deserializedObj.Field2);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3.GetType() == typeof(ClassWithIntField));
            Assert.AreEqual(456, ((ClassWithIntField)deserializedObj.Field3).Field1);
        }

        [Test]
        public void DeserializeClassWithDeletedDictionaryType()
        {
            Serializer serializer = new Serializer();

            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream, Encoding.Unicode, true);
            writer.Write(Serializer.CompatibilityVersion);
            writer.Write(3);            // Number of types

            writer.Write("MissingClassName<,>");
            writer.Write((byte)4);      // Category: dictionary
            writer.Write(1);            // Key size
            writer.Write(4);            // Item size

            writer.Write(typeof(RegisteredClass).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(0);            // Type size
            writer.Write(false);        // Has no parent
            writer.Write(0);            // Number of fields

            writer.Write(typeof(ClassWithObjectField).AssemblyQualifiedName);
            writer.Write((byte)6);      // Category: class
            writer.Write(12);           // Type size
            writer.Write(false);        // Has no parent
            writer.Write(3);            // Number of fields            
            writer.Write("Field1");
            writer.Write(4);            // Size of object reference
            writer.Write("Field2");
            writer.Write(4);            // Size of object reference
            writer.Write("Field3");
            writer.Write(4);            // Size of object reference

            writer.Write(4);            // Number of objects
            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)2);      // Type ID of object 2
            writer.Write((uint)1);      // Type ID of object 3
            writer.Write(3);            // Dictionary length
            writer.Write((uint)2);      // Type ID of object 4

            writer.Write(false);        // Root object is reference-type

            writer.Write((uint)3);      // Type ID of root object
            writer.Write((uint)2);      // Field1
            writer.Write((uint)3);      // Field2
            writer.Write((uint)4);      // Field3

            writer.Write((uint)2);      // Type ID of object 2
            writer.Write((byte)1);      // Key 0
            writer.Write(2);            // Value 0
            writer.Write((byte)3);      // Key 1
            writer.Write(4);            // Value 1
            writer.Write((byte)5);      // Key 2
            writer.Write(6);            // Value 2

            writer.Write((uint)2);      // Type ID of object 4
            writer.Close();

            ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1.GetType() == typeof(RegisteredClass));
            Assert.Null(deserializedObj.Field2);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3.GetType() == typeof(RegisteredClass));
        }

        [Test]
        public void SerializeReferenceTypeInObjectField()
        {
            Serializer serializer = new Serializer();

            ClassWithObjectField obj = new ClassWithObjectField()
            {
                Field1 = new RegisteredClass(),
                Field2 = new ClassWithIntField() { Field1 = 123 },
                Field3 = new RegisteredClass()
            };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            ClassWithObjectField deserializedObj = serializer.Deserialize<ClassWithObjectField>(stream);
            stream.Close();

            Assert.NotNull(deserializedObj);
            Assert.NotNull(deserializedObj.Field1);
            Assert.IsTrue(deserializedObj.Field1.GetType() == typeof(RegisteredClass));
            Assert.NotNull(deserializedObj.Field2);
            Assert.IsTrue(deserializedObj.Field2.GetType() == typeof(ClassWithIntField));
            Assert.AreEqual(123, ((ClassWithIntField)deserializedObj.Field2).Field1);
            Assert.NotNull(deserializedObj.Field3);
            Assert.IsTrue(deserializedObj.Field3.GetType() == typeof(RegisteredClass));
        }

        [Test]
        public void SerializeClassWithSerializationHooks()
        {
            Serializer serializer = new Serializer();

            ClassWithSerializationHooks obj = new ClassWithSerializationHooks() { Field = 123 };

            MemoryStream stream = new MemoryStream();
            serializer.Serialize(obj, stream);
            ClassWithSerializationHooks deserializedObj = serializer.Deserialize<ClassWithSerializationHooks>(stream);
            stream.Close();

            Assert.AreEqual(247, deserializedObj.Field);
        }

        [Test]
        public void RegisterDependantStructBeforeDependency()
        {
            Serializer serializer = new Serializer();

            Assert.Throws<IcepackException>(() => {
                serializer.RegisterType(typeof(StructWithNestedStruct));
            });
        }

        [Test]
        public void RegisterDependencyStructBeforeDependant()
        {
            Serializer serializer = new Serializer();

            serializer.RegisterType(typeof(NestedStruct));
            serializer.RegisterType(typeof(StructWithNestedStruct));
        }
    }
}