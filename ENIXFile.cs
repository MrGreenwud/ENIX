using System.Collections;
using System.Reflection;

using ENIX.Extantions;

namespace ENIX
{
    // *.enix

    public static class ENIXFile
    {
        private static Dictionary<object, string> sm_RegisteredSerializeObjects = new Dictionary<object, string>();

        private static Dictionary<string, object> sm_RegisteredDeserializeObject = new Dictionary<string, object>();
        private static List<PropertyData> sm_RegisteredPropertyRequaredObject = new List<PropertyData>();

        private static Dictionary<string, string> sm_SerializedObjects = new Dictionary<string, string>();

        private static uint sm_DepthSerialization;

        public static string Tab => GetTab(sm_DepthSerialization);

        private static string GetTab(uint depth)
        {
            string tab = "\n";

            for (int i = 0; i < depth; i++)
                tab += "\t";

            return tab;
        }

        public static Dictionary<Type, List<object>> FilterObjectsByType(object[] objects, Type[] typeFilter, bool isConsiderBasicTypes = false)
        {
            Dictionary<Type, List<object>> result = new Dictionary<Type, List<object>>(typeFilter.Length);

            foreach (object obj in objects)
            {
                foreach (Type type in typeFilter)
                {
                    if (isConsiderBasicTypes)
                    {
                        if (type.ToString() == obj.GetType().ToString())
                        {
                            if (result.ContainsKey(type) == false)
                                result.Add(type, new List<object>());

                            result[type].Add(obj);
                        }
                    }
                    else
                    {
                        if (type.IsAssignableFrom(obj.GetType()))
                        {
                            if (result.ContainsKey(type) == false)
                                result.Add(type, new List<object>());

                            result[type].Add(obj);
                        }
                    }
                }
            }

            return result;
        }

        public static object[] Deserialize(string enixFile)
        {
            ResetAll();

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

            return DeserializeObjects(objects.ToArray());
        }

        public static object[] Deserialize(List<string> objects)
        {
            return DeserializeObjects(objects.ToArray());
        }

        public static object[] DeserializeObjects(string[] serializedObjects)
        {
            ResetAll();

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
                object key = null;
                object value = null;

                Type propertyType = data.GetPropertyType();

                if (propertyType.IsGenericType == true && propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
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
            Type ObjectType = Type.GetType(part[1].Trim());
            string guid = part[2].Trim();

            if (ObjectType == null)
            {
                throw new Exception();
            }

            object obj = null;

            try
            {
                obj = Activator.CreateInstance(ObjectType);
            }
            catch (Exception e)
            {
                throw new Exception($"{e} Class with type {ObjectType} not have void construct");
            }

            FieldInfo[] fields = GetAllFieldsByType(ObjectType, BindingFlags.Public
              | BindingFlags.NonPublic | BindingFlags.Instance);

            Dictionary<string, string> serializedPropertes = GetPropertes(serializedObject);

            foreach (string propertyName in serializedPropertes.Keys)
            {
                string serializedProperty = serializedPropertes[propertyName];

                FieldInfo field = GetFieldByName(fields, propertyName);

                if (field == null)
                    continue;

                object value = DeserializeProperty(serializedProperty, field.FieldType);

                if (field.FieldType.IsClass() == true)
                {
                    PropertyData propertyData = new PropertyData(obj, field, value.ToString());
                    sm_RegisteredPropertyRequaredObject.Add(propertyData);
                }
                else
                {
                    field.SetValue(obj, value);
                }
            }

            sm_RegisteredDeserializeObject.Add(guid, obj);
        }

        public static object DeserializeProperty(string serializedProperty, Type propertyType)
        {
            if(RegisterCustomSerializer.TryGetPropertyDeserializeMethod(propertyType, out MethodInfo method))
            {
                object[] args = { serializedProperty };
                object result = method.Invoke(null, args);
                return result;
            }
            else if (propertyType.IsArray)
            {
                Type elementType = propertyType.GetElementType();
                Dictionary<string, string> elements = GetPropertes(serializedProperty);

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
            else if (propertyType.IsGenericType)
            {
                if (propertyType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    Type[] argsType = propertyType.GetGenericArguments();
                    Type keyType = argsType[0];
                    Type valueType = argsType[1];

                    Dictionary<string, string> elements = GetPropertes(serializedProperty);

                    Type dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
                    IDictionary dictionary = (IDictionary)Activator.CreateInstance(dictionaryType);

                    foreach (string elementName in elements.Keys)
                    {
                        Dictionary<string, string> pairs = GetPropertes(elements[elementName]);

                        object key = DeserializeProperty(pairs["Key"], keyType);
                        object value = DeserializeProperty(pairs["Value"], valueType);

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
                else if (propertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type elementType = propertyType.GetGenericArguments()[0];
                    Dictionary<string, string> elements = GetPropertes(serializedProperty);

                    Type listType = typeof(List<>).MakeGenericType(elementType);
                    IList list = (IList)Activator.CreateInstance(listType);

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

                return null;
            }
            else if (propertyType.IsStruct())
            {
                object property = Activator.CreateInstance(propertyType);
                Dictionary<string, string> childPropertes = GetPropertes(serializedProperty);

                foreach (string propertyName in childPropertes.Keys)
                {
                    FieldInfo field = propertyType.GetField(propertyName,
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
            else if (propertyType.IsClass)
            {
                string guid = serializedProperty.Split(":")[1].Trim();
                return guid;
            }
            else if (propertyType.IsEnum)
            {
                int value = int.Parse(serializedProperty.Split(":")[1].Trim());
                return Enum.ToObject(propertyType, value);
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

        private static Dictionary<string, string> GetPropertes(string serializedElement)
        {
            List<string> lines = serializedElement.Split("\n").ToList();

            if (lines[0] == string.Empty)
                lines.Remove(lines[0]);

            uint depthSerialization = 0;

            Dictionary<string, string> serializedPropertes = new Dictionary<string, string>();

            for (int i = 1; i < lines.Count; i++)
            {
                string fixedLine = lines[i].Trim();

                if (fixedLine.Contains("Object") || fixedLine == string.Empty)
                    continue;

                if (fixedLine.Contains("{"))
                {
                    depthSerialization++;
                }
                else if (fixedLine.Contains("}"))
                {
                    depthSerialization--;
                }
                else if (fixedLine.Contains(":"))
                {
                    string[] parts = fixedLine.Split(":");
                    serializedPropertes.Add(parts[0].Trim(), fixedLine);
                }
                else
                {
                    string propertyName = fixedLine;
                    string serializedProperty = string.Empty;

                    uint curentDepthSerialization = depthSerialization;
                    uint depth;
                    uint lineCount = 0;

                    for (int j = i; j < lines.Count; j++)
                    {
                        if (lines[j].Contains("{"))
                        {
                            depth = curentDepthSerialization - depthSerialization;
                            serializedProperty += $"{GetTab(depth)}{lines[j].Trim()}";
                            curentDepthSerialization++;
                        }
                        else if (lines[j].Contains("}"))
                        {
                            curentDepthSerialization--;
                            depth = curentDepthSerialization - depthSerialization;
                            serializedProperty += $"{GetTab(depth)}{lines[j].Trim()}";
                        }
                        else
                        {
                            depth = curentDepthSerialization - depthSerialization;
                            serializedProperty += $"{GetTab(depth)}{lines[j].Trim()}";
                        }

                        if (curentDepthSerialization == depthSerialization
                            && lines[j].Contains("}"))
                        {
                            lineCount = (uint)(j - i);
                            i = j;
                            break;
                        }
                    }

                    serializedPropertes.Add(propertyName, serializedProperty);
                }
            }

            return serializedPropertes;
        }

        public static void Serialize(string name, object[] objects, string path)
        {
            sm_RegisteredSerializeObjects.Clear();

            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);

            List<string> serializedObjects = Serialize(objects);

            string enix = "#ENIX v0.1";

            foreach (string serializedObject in serializedObjects)
                enix += serializedObject;

            string pathWithFile = $"{path}/{name}.enix";
            File.WriteAllText(pathWithFile, enix);
        }

        public static List<string> Serialize(object[] objects)
        {
            ResetAll();

            foreach (object obj in objects)
                SerializeObject(obj);

            return sm_SerializedObjects.Values.ToList();
        }

        public static string SerializeObject<T>(T obj) where T : class
        {
            sm_DepthSerialization = 0;
            string serializedObject = string.Empty;

            SerializebleObject serializebleObject = (SerializebleObject)Attribute.GetCustomAttribute(obj.GetType(), typeof(SerializebleObject));

            if (serializebleObject == null)
            {
                //throw new Exception($"An object with type '{obj.GetType()}' cannot be sterilized. " +
                //    $"Attribute missing {nameof(SerializebleObject)}");
            }

            string guid = Guid.NewGuid().ToString();
            sm_RegisteredSerializeObjects.Add(obj, guid);

            serializedObject += $"{Tab}Object : {obj.GetType()} : {guid}";
            serializedObject += $"{Tab}{{";
            sm_DepthSerialization++;

            FieldInfo[] fields = GetAllFieldsByType(obj.GetType(), BindingFlags.Public
              | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo property in fields)
                serializedObject += SerializeProperty(property, obj);

            PropertyInfo[] properties = obj.GetType().GetProperties(BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
                serializedObject += SerializeProperty(property, obj);

            sm_DepthSerialization--;
            serializedObject += $"{Tab}}}";

            sm_SerializedObjects.Add(guid, serializedObject);
            return serializedObject;
        }

        public static string SerializeProperty(FieldInfo property, object obj, bool isSerializebleProperty = true)
        {
            if (isSerializebleProperty)
            {
                //SerializebleProperty serializebleProperty = property.GetAttribute<SerializebleProperty>();
                SerializebleProperty serializebleProperty = (SerializebleProperty)property.GetCustomAttribute(typeof(SerializebleProperty), false);

                if (serializebleProperty == null)
                    return string.Empty;
            }

            object value = property.GetValue(obj);
            string name = property.Name;

            return SerializeProperty(value, name, property.FieldType);
        }

        public static string SerializeProperty(PropertyInfo property, object obj)
        {
            SerializebleProperty serializebleProperty = (SerializebleProperty)property.GetCustomAttribute(typeof(SerializebleProperty), false);

            if (serializebleProperty == null)
                return string.Empty;

            object value = property.GetValue(obj);
            string name = property.Name;

            return SerializeProperty(value, name, property.PropertyType);
        }

        public static string SerializeProperty(object property, string name, Type type)
        {
            if (property == null)
                return string.Empty;

            string serializedProperty = string.Empty;

            if (RegisterCustomSerializer.TryGetPropertySerializeMethod(type, out MethodInfo method))
            {
                object[] args = { property, name, type };
                object result = method.Invoke(null, args);
                serializedProperty += result.ToString();
            }
            else if (type.IsArray)
            {
                serializedProperty += $"{Tab}{name}";
                serializedProperty += $"{Tab}{{";
                sm_DepthSerialization++;

                Array array = (Array)property;
                Type elementType = array.GetType().GetElementType();

                for (int i = 0; i < array.Length; i++)
                {
                    serializedProperty += SerializeProperty(array.GetValue(i),
                        $"Element{i}", elementType);
                }

                sm_DepthSerialization--;
                serializedProperty += $"{Tab}}}";
            }
            else if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    IDictionary dictionary = (IDictionary)property;

                    Type[] argsType = type.GetGenericArguments();
                    Type keyType = argsType[0];
                    Type valueType = argsType[1];

                    serializedProperty += $"{Tab}{name}";
                    serializedProperty += $"{Tab}{{";
                    sm_DepthSerialization++;

                    uint elementIndex = 0;
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        serializedProperty += $"{Tab}Element{elementIndex}";
                        serializedProperty += $"{Tab}{{";
                        sm_DepthSerialization++;

                        serializedProperty += SerializeProperty(entry.Key, "Key", keyType);
                        serializedProperty += SerializeProperty(entry.Value, "Value", valueType);

                        sm_DepthSerialization--;
                        serializedProperty += $"{Tab}}}";

                        elementIndex++;
                    }

                    sm_DepthSerialization--;
                    serializedProperty += $"{Tab}}}";
                }
                else if (type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    IList list = (IList)property;
                    Type elementType = type.GetGenericArguments()[0];

                    serializedProperty += $"{Tab}{name}";
                    serializedProperty += $"{Tab}{{";
                    sm_DepthSerialization++;

                    uint elementIndex = 0;
                    foreach (object element in list)
                    {
                        serializedProperty += SerializeProperty(element,
                            $"Element{elementIndex}", elementType);

                        elementIndex++;
                    }

                    sm_DepthSerialization--;
                    serializedProperty += $"{Tab}}}";
                }
            }
            else if (type.IsClass && type != typeof(string))
            {
                uint death = sm_DepthSerialization;

                if (sm_RegisteredSerializeObjects.ContainsKey(property) == false)
                    SerializeObject(property);

                sm_DepthSerialization = death;

                serializedProperty = $"{Tab}{name} : {sm_RegisteredSerializeObjects[property]}";
            }
            else if (type.IsStruct())
            {
                SerializebleObject serializebleObject = (SerializebleObject)Attribute.GetCustomAttribute(type, typeof(SerializebleObject));

                if (serializebleObject == null)
                    return string.Empty;

                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                serializedProperty += SerializeStruct(property, name, fields);
            }
            else if (type.IsEnum)
            {
                serializedProperty = $"{Tab}{name} : {(int)property}";
            }
            else
            {
                serializedProperty = $"{Tab}{name} : {property}";
            }

            return serializedProperty;
        }

        public static string SerializeStruct(object property, string name, FieldInfo[] fields, bool isSerializebleProperty = true)
        {
            string serializedStruct = string.Empty;

            serializedStruct += $"{Tab}{name}";
            serializedStruct += $"{Tab}{{";
            sm_DepthSerialization++;

            foreach (FieldInfo field in fields)
                serializedStruct += SerializeProperty(field, property, isSerializebleProperty);

            sm_DepthSerialization--;
            serializedStruct += $"{Tab}}}";

            return serializedStruct;
        }

        public static Type GetSerializedObjectType(string obj)
        {
            string firtLine = obj.Split("\n")[1];

            if (firtLine.Contains("Object") == false)
                throw new ArgumentException();

            string type = firtLine.Split(":")[1].Trim();
            return Type.GetType(type);
        }

        private static void ResetAll()
        {
            sm_DepthSerialization = 0;

            sm_RegisteredDeserializeObject.Clear();
            sm_RegisteredPropertyRequaredObject.Clear();
            sm_RegisteredSerializeObjects.Clear();
            sm_SerializedObjects.Clear();
        }

        public static FieldInfo[] GetAllFieldsByType(Type type, BindingFlags flags)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            HashSet<string> addedFieldNames = new HashSet<string>();

            result.AddRange(type.GetFields(flags));

            foreach (FieldInfo fieldInfo in result)
            {
                addedFieldNames.Add(fieldInfo.Name);
            }

            if (type.BaseType != null)
            {
                FieldInfo[] fieldInfos = GetAllFieldsByType(type.BaseType, flags);

                foreach (FieldInfo field in fieldInfos)
                {
                    if (!addedFieldNames.Contains(field.Name))
                    {
                        result.Add(field);
                        addedFieldNames.Add(field.Name);
                    }
                }
            }

            return result.ToArray();
        }

        public static FieldInfo GetFieldFlattenHierarchy(Type type, string fieldName, BindingFlags flags)
        {
            FieldInfo[] fields = GetAllFieldsByType(type, flags);
            return GetFieldByName(fields, fieldName);
        }

        public static FieldInfo GetFieldByName(FieldInfo[] fields, string fieldName)
        {
            foreach (FieldInfo field in fields)
            {
                if (field.Name == fieldName)
                    return field;
            }

            return null;
        }
    }
}