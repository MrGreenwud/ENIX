using System.Collections;
using System.Reflection;
using ENIX.Extantions;

namespace ENIX
{
    public static class ENIXDeserializer
    {
        private static Dictionary<string, string> sm_SerializedObjects = new Dictionary<string, string>();
        private static Dictionary<string, object> sm_RegisteredDeserializeObject = new Dictionary<string, object>();
        private static List<PropertyData> sm_RegisteredPropertyRequaredObject = new List<PropertyData>();

        private static uint sm_DepthSerialization;

        public static object[] Deserialize(string enixFile)
        {
            Reset();

            string[] lines = enixFile.Split("\n");
            List<string> objects = new List<string>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("Object"))
                {
                    string serializedObject = string.Empty;

                    for (int j = i; j < lines.Length; j++)
                    {
                        serializedObject += "\n";
                        serializedObject += lines[j];

                        if (lines[j].Contains("{"))
                            sm_DepthSerialization++;
                        else if (lines[j].Contains("}"))
                            sm_DepthSerialization--;

                        if (sm_DepthSerialization == 0 && j != i)
                            break;
                    }

                    objects.Add(serializedObject);
                }
            }

            return Deserialize(objects);
        }

        public static object[] Deserialize(List<string> objects)
        {
            return DeserializeObjects(objects.ToArray());
        }

        public static object[] DeserializeObjects(string[] serializedObjects)
        {
            Reset();

            foreach (string obj in serializedObjects)
            {
                string guid = obj.Split("\n")[1].Split(":")[2].Trim();
                sm_SerializedObjects.Add(guid, obj);
            }

            foreach (string guid in sm_SerializedObjects.Keys)
            {
                if (sm_RegisteredDeserializeObject.ContainsKey(guid))
                    continue;

                DeserializeObject(sm_SerializedObjects[guid]);
            }

            foreach (PropertyData data in sm_RegisteredPropertyRequaredObject)
            {
                object? key = null;
                object? value = null;

                Type propertyType = data.GetPropertyType();

                if (propertyType.IsDictionary())
                {
                    Type[] argsType = propertyType.GetGenericArguments();
                    Type keyType = argsType[0];
                    Type valueType = argsType[1];

                    if (keyType.IsClass() == true)
                    {
                        if (sm_RegisteredDeserializeObject.ContainsKey(data.Key.ToString()) == false)
                            continue;

                        key = sm_RegisteredDeserializeObject[data.Key.ToString()];
                    }
                    else
                    {
                        key = data.Key;
                    }

                    if (valueType.IsClass() == true)
                        value = sm_RegisteredDeserializeObject[data.Value.ToString()];
                    else
                        value = data.Value;
                }
                else
                {
                    if (sm_RegisteredDeserializeObject.ContainsKey(data.Value.ToString()) == false)
                        continue;

                    value = sm_RegisteredDeserializeObject[data.Value.ToString()];
                }

                data.SetValue(value, key);
            }

            return sm_RegisteredDeserializeObject.Values.ToArray();
        }

        public static void DeserializeObject(string serializedObject)
        {
            string[] lines = serializedObject.Split('\n');
            string line = lines[1].Trim();

            string[] part = line.Split(":");
            Type? ObjectType = Type.GetType(part[1].Trim());
            string guid = part[2].Trim();

            if (ObjectType == null)
                throw new Exception("");

            object? obj = null;

            try
            {
                obj = Activator.CreateInstance(ObjectType);
            }
            catch (Exception e)
            {
                throw new Exception($"{e} Class with type {ObjectType} not have void construct");
            }

            FieldInfo[] fields = ENIXCash.GetFields(ObjectType, BindingFlags.Public
              | BindingFlags.NonPublic | BindingFlags.Instance);

            PropertyInfo[] properties = ENIXCash.GetProperties(ObjectType, BindingFlags.Public
              | BindingFlags.NonPublic | BindingFlags.Instance);

            Dictionary<string, string> serializedPropertes = ENIXInfo.GetPropertes(serializedObject);

            foreach (string propertyName in serializedPropertes.Keys)
            {
                string serializedProperty = serializedPropertes[propertyName];

                ENIXProperty enixProperty;

                FieldInfo? field = FindFieldByName(fields, propertyName);
                PropertyInfo? property = FindPropertyByName(properties, propertyName);

                if (field != null)
                    enixProperty = new ENIXProperty(field);
                else if (property != null)
                    enixProperty = new ENIXProperty(property);
                else
                    continue;

                object value = DeserializeProperty(serializedProperty, enixProperty.Type);

                if (enixProperty.Type.IsClass() == true)
                {
                    PropertyData propertyData = new PropertyData(obj, enixProperty, value.ToString());
                    sm_RegisteredPropertyRequaredObject.Add(propertyData);
                }
                else
                {
                    enixProperty.SetValue(obj, value);
                }
            }

            sm_RegisteredDeserializeObject.Add(guid, obj);
        }

        public static object DeserializeProperty(string serializedProperty, Type propertyType)
        {
            if (RegisterCustomSerializer.TryGetPropertyDeserializeMethod(propertyType, out MethodInfo method))
            {
                object[] args = { serializedProperty };
                object result = method.Invoke(null, args);
                return result;
            }
            else if (propertyType.IsArray)
            {
                return DeserializeArray(serializedProperty, propertyType);
            }
            else if (propertyType.IsGenericType)
            {
                if (propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                    return DeserializeDictionary(serializedProperty, propertyType);
                else if (propertyType.GetGenericTypeDefinition() == typeof(List<>))
                    return DeserializeList(serializedProperty, propertyType);
            }
            else if (propertyType.IsStruct())
            {
                return DeserializeStruct(serializedProperty, propertyType);
            }
            else if (propertyType.IsClass)
            {
                string guid = serializedProperty.Split(":")[1].Trim();
                return guid;
            }
            else if (propertyType.IsEnum)
            {
                return DeserializeEnum(serializedProperty, propertyType);
            }
            else
            {
                string value = serializedProperty.Split(":")[1].Trim();

                if (propertyType == typeof(float))
                {
                    if (float.TryParse(value, out float outValue) == false)
                        return string.Empty;

                    return outValue;
                }
                else if (propertyType == typeof(int))
                {
                    if (int.TryParse(value, out int outValue) == false)
                        return 0;

                    return outValue;
                }
                else if (propertyType == typeof(string))
                {
                    return value;
                }
                else if (propertyType == typeof(bool))
                {
                    return bool.Parse(value);
                }
            }

            throw new Exception($"Property with type : {propertyType} " +
                $"сan't be deserialize! \n {serializedProperty}");
        }

        public static object DeserializeArray(string serializedProperty, Type propertyType)
        {
            Type? elementType = propertyType.GetElementType();

#if DEBUG
            if (elementType == null)
                throw new Exception();
#endif

            Dictionary<string, string> elements = ENIXInfo.GetPropertes(serializedProperty);

            Array array = Array.CreateInstance(elementType, elements.Count);

            uint iteration = 0;
            foreach (string elementName in elements.Keys)
            {
                object value = DeserializeProperty(elements[elementName], elementType);

                if (elementType.IsClass == true && elementType != typeof(string))
                {
                    PropertyData propertyData = new PropertyData(null, array, value.ToString());
                    sm_RegisteredPropertyRequaredObject.Add(propertyData);
                }
                else
                {
                    array.SetValue(value, iteration);
                }

                iteration++;
            }

            return array;
        }

        public static object DeserializeList(string serializedProperty, Type propertyType)
        {
            Type? elementType = propertyType.GetGenericArguments()[0];

#if DEBUG
            if (elementType == null)
                throw new Exception();
#endif

            Dictionary<string, string> elements = ENIXInfo.GetPropertes(serializedProperty);

            Type listType = typeof(List<>).MakeGenericType(elementType);
            IList? list = (IList?)Activator.CreateInstance(listType);

#if DEBUG
            if (list == null)
                throw new Exception();
#endif

            foreach (string elementName in elements.Keys)
            {
                object value = DeserializeProperty(elements[elementName], elementType);

                if (elementType.IsClass())
                {
                    PropertyData propertyData = new PropertyData(null, list, value.ToString());
                    sm_RegisteredPropertyRequaredObject.Add(propertyData);
                }
                else
                {
                    list.Add(value);
                }
            }

            return list;
        }

        public static object DeserializeDictionary(string serializedProperty, Type propertyType)
        {
            Type[] argsType = propertyType.GetGenericArguments();
            Type keyType = argsType[0];
            Type valueType = argsType[1];

            Dictionary<string, string> elements = ENIXInfo.GetPropertes(serializedProperty);

            Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
            IDictionary? dictionary = (IDictionary?)Activator.CreateInstance(dictionaryType);

#if DEBUG
            if (dictionary == null)
                throw new Exception();
#endif

            foreach (string elementName in elements.Keys)
            {
                Dictionary<string, string> pairs = ENIXInfo.GetPropertes(elements[elementName]);

                object key = DeserializeProperty(pairs["Key"], keyType);
                object? value = DeserializeProperty(pairs["Value"], valueType);

                if (keyType.IsClass() == true || valueType.IsClass() == true)
                {
                    PropertyData data = new PropertyData(null, dictionary, value, key);
                    sm_RegisteredPropertyRequaredObject.Add(data);
                }
                else
                {
                    dictionary.Add(key, value);
                }
            }

            return dictionary;
        }

        public static object DeserializeStruct(string serializedProperty, Type propertyType)
        {
            object? property = Activator.CreateInstance(propertyType);

#if DEBUG
            if (property == null)
                throw new Exception();
#endif

            Dictionary<string, string> childPropertes = ENIXInfo.GetPropertes(serializedProperty);

            foreach (string propertyName in childPropertes.Keys)
            {
                FieldInfo? field = propertyType.GetField(propertyName,
                    BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

                object value = DeserializeProperty(childPropertes[propertyName], field.FieldType);

                if (field.FieldType.IsClass() == true)
                {
                    PropertyData propertyData = new PropertyData(property, field, value.ToString());
                    sm_RegisteredPropertyRequaredObject.Add(propertyData);
                }
                else
                {
                    field.SetValue(property, value);
                }
            }

            return property;
        }

        public static object DeserializeEnum(string serializedProperty, Type propertyType)
        {
            int value = int.Parse(serializedProperty.Split(":")[1].Trim());
            return Enum.ToObject(propertyType, value);
        }

        private static FieldInfo? FindFieldByName(FieldInfo[] fields, string fieldName)
        {
            foreach (FieldInfo field in fields)
            {
                if (field.Name == fieldName)
                    return field;
            }

            return null;
        }

        private static PropertyInfo? FindPropertyByName(PropertyInfo[] properties, string fieldName)
        {
            foreach (PropertyInfo property in properties)
            {
                if (property.Name == fieldName)
                    return property;
            }

            return null;
        }

        private static void Reset()
        {
            sm_SerializedObjects.Clear();
            sm_RegisteredDeserializeObject.Clear();
            sm_RegisteredPropertyRequaredObject.Clear();
            sm_DepthSerialization = 0;
        }
    }
}
