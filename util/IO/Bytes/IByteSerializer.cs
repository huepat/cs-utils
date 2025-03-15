namespace HuePat.Util.IO.Bytes {
    public interface IByteSerializer {
        IByteSerializer Clone();
        byte[] Serialize<T>(T @object);
        int Serialize<T>(T @object, ref byte[] buffer);
        T Deserialize<T>(byte[] buffer);
        void Deserialize<T>(ref T @object, byte[] buffer);
    }
}