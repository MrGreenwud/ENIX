namespace ENIX
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class SerializebleObject : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class SerializebleProperty : Attribute { }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class CustomSerializer : Attribute { }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CustomPropertySerializerMethod : Attribute
    {
        public Type Type { get; private set; }

        public CustomPropertySerializerMethod(Type type) => Type = type;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class CustomPropertyDeserializerMethod : Attribute
    {
        public Type Type { get; private set; }

        public CustomPropertyDeserializerMethod(Type type) => Type = type;
    }
}
