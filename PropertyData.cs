using System.Collections;
using System.Reflection;

namespace ENIX
{
    internal struct PropertyData
    {
        private object Owner;
        private object Property;

        public object Key { get; private set; }
        public object Value { get; private set; }

        public Type GetPropertyType() => Property.GetType();

        public PropertyData(object owner, object property, object value, object key = null)
        {
            Owner = owner;
            Property = property;

            Key = key;
            Value = value;
        }

        public void SetValue(object value, object key = null)
        {
            Type propertyType = Property.GetType();

            if (propertyType.IsArray == true)
            {
                Array array = (Array)Property;
                Type elementType = array.GetType().GetElementType();

                if (elementType.IsAssignableFrom(value.GetType()) == false)
                {
                    //Debug.LogWarning("");
                    return;
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (array.GetValue(i) != null)
                        continue;

                    array.SetValue(value, i);
                }
            }
            else if (propertyType.IsGenericType == true)
            {
                if (propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    IDictionary dictionary = (IDictionary)Property;

                    Type[] argsType = dictionary.GetType().GetGenericArguments();
                    Type keyType = argsType[0];
                    Type valueType = argsType[1];

                    if (keyType.IsAssignableFrom(key.GetType()) == false
                        || valueType.IsAssignableFrom(value.GetType()) == false)
                    {
                        throw new Exception();
                    }

                    dictionary.Add(key, value);
                }
                else if (propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    IList list = (IList)Property;
                    Type elementType = list.GetType().GetGenericArguments()[0];

                    if (elementType.IsAssignableFrom(value.GetType()) == false)
                    {
                        throw new Exception();
                    }

                    list.Add(value);
                }
            }
            else if (propertyType == Type.GetType("System.Reflection.RuntimeFieldInfo"))
            {
                FieldInfo field = Property as FieldInfo;

                if (field.FieldType.IsAssignableFrom(value.GetType()) == false)
                {
                    throw new Exception();
                }

                field.SetValue(Owner, value);
            }
        }
    }
}
