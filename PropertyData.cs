using System.Collections;
using System.Reflection;

namespace ENIX
{
    internal struct PropertyData
    {
        private object? Owner;
        private object Property;

        public object? Key { get; private set; }
        public object? Value { get; private set; }

        public Type GetPropertyType() => Property.GetType();

        public PropertyData(object? owner, object property, object? value, object? key = null)
        {
            Owner = owner;
            Property = property;

            Key = key;
            Value = value;
        }

        public void SetValue(object value, object? key = null)
        {
            Type propertyType = Property.GetType();

            if (propertyType.IsArray == true)
            {
                Array array = (Array)Property;
                Type? elementType = array.GetType().GetElementType();

#if DEBUG
                if (elementType == null)
                    throw new InvalidOperationException("The element type from the array was not found");

                if (elementType.IsAssignableFrom(value.GetType()) == false)
                {
                    throw new InvalidOperationException("The type of the array element and the " +
                        "type of the inserted element do not match");
                }
#endif

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
#if DEBUG
                    if (key == null)
                        throw new ArgumentNullException("You cannot set a null key in a dictionary");
#endif

                    IDictionary dictionary = (IDictionary)Property;

                    Type[] argsType = dictionary.GetType().GetGenericArguments();
                    Type keyType = argsType[0];
                    Type valueType = argsType[1];

#if DEBUG
                    if (keyType.IsAssignableFrom(key.GetType()) == false
                        || valueType.IsAssignableFrom(value.GetType()) == false)
                    {
                        throw new InvalidOperationException("The key and/or " +
                            "value type does not match the type in the dictionary");
                    }
#endif

                    dictionary.Add(key, value);
                }
                else if (propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    IList list = (IList)Property;
                    Type elementType = list.GetType().GetGenericArguments()[0];

#if DEBUG
                    if (elementType.IsAssignableFrom(value.GetType()) == false)
                        throw new InvalidOperationException("The value type does not match the list element type");
#endif

                    list.Add(value);
                }
            }
            else if(propertyType == typeof(ENIXProperty))
            {
                ENIXProperty? property = Property as ENIXProperty;

#if DEBUG
                if (property == null)
                    throw new InvalidOperationException("The value type does not match the field/property type");
#endif

                property.SetValue(Owner, value);
            }
        }
    }
}
