using System;
using System.Collections.Generic;

namespace HuePat.Util.IO.Bytes {
    public class CerasSerializer : IByteSerializer {
        public static CerasSerializer ForNetworking() {
            return ForNetworking(new Type[0]);
        }

        public static CerasSerializer ForNetworking(Type knownType) {
            return ForNetworking(new Type[] { knownType });
        }

        public static CerasSerializer ForNetworking(IList<Type> knownTypes) {
            Ceras.SerializerConfig config = new Ceras.SerializerConfig();
            config.Advanced.PersistTypeCache = true;
            config.PreserveReferences = false;
            foreach (Type type in knownTypes) {
                config.KnownTypes.Add(type);
            }
            return new CerasSerializer(config);
        }

        private Ceras.CerasSerializer serializer;
        private Ceras.SerializerConfig serializerConfig;

        public CerasSerializer(
                Ceras.SerializerConfig serializerConfig) {
            this.serializerConfig = serializerConfig;
            serializer = new Ceras.CerasSerializer(serializerConfig);
        }

        public IByteSerializer Clone() {
            return new CerasSerializer(serializerConfig);
        }

        public T Deserialize<T>(byte[] buffer) {
            return serializer.Deserialize<T>(buffer);
        }

        public void Deserialize<T>(ref T @object, byte[] buffer) {
            serializer.Deserialize(ref @object, buffer);
        }

        public byte[] Serialize<T>(T @object) {
            return serializer.Serialize(@object);
        }

        public int Serialize<T>(T @object, ref byte[] buffer) {
            return serializer.Serialize(@object, ref buffer);
        }
    }
}