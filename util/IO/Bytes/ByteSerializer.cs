using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace HuePat.Util.IO.Bytes {
    public class ByteSerializer: IByteSerializer {
        private class TypeBinder : SerializationBinder {
            private Dictionary<string, Type> typeRegistry;

            public TypeBinder(Dictionary<string, Type> typeRegistry) {
                this.typeRegistry = typeRegistry;
            }

            public TypeBinder Clone() {
                return new TypeBinder(typeRegistry);
            }

            public override Type BindToType(string assemblyName, string typeName) {
                if (!typeRegistry.ContainsKey(typeName)) {
                    throw new ArgumentException(
                        $"Cannot bind type '{typeName}' to any of the configured types ({typeRegistry.Keys.Join()}).");
                }
                return typeRegistry[typeName];
            }
        }

        private static Dictionary<string, Type> CreateTypeRegistry(
                IEnumerable<Type> innerTypes) {
            Dictionary<string, Type> types = new Dictionary<string, Type>();
            foreach (Type type in innerTypes) {
                types.Add(type.FullName, type);
            }
            return types;
        }

        private BinaryFormatter formatter;

        public ByteSerializer(Type type) :
            this(CreateTypeRegistry(new Type[] { type })) {
        }

        public ByteSerializer(IEnumerable<Type> types): 
            this(CreateTypeRegistry(types)) {
        }

        private ByteSerializer(Dictionary<string, Type> typeRegistry) :
            this(new TypeBinder(typeRegistry)) {
        }

        private ByteSerializer(TypeBinder typeBinder) {
            formatter = new BinaryFormatter();
            formatter.Binder = typeBinder;
        }

        public IByteSerializer Clone() {
            return new ByteSerializer(
                (formatter.Binder as TypeBinder).Clone());
        }

        public byte[] Serialize<T>(T @object) {
            if (@object == null) {
                return new byte[0];
            }
            using (MemoryStream memoryStream = new MemoryStream()) {
                formatter.Serialize(memoryStream, @object);
                return memoryStream.ToArray();
            }
        }

        public int Serialize<T>(T @object, ref byte[] buffer) {
            buffer = Serialize(@object);
            return buffer.Length;
        }

        public T Deserialize<T>(byte[] buffer) {
            if (buffer.Length == 0) {
                return default;
            }
            using (MemoryStream memoryStream = new MemoryStream(buffer)) {
                return (T)formatter.Deserialize(memoryStream);
            }
        }

        public void Deserialize<T>(ref T @object, byte[] buffer) {
            @object = Deserialize<T>(buffer);
        }
    }
}