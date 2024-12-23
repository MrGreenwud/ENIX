using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;

namespace ENIX
{
    public static class ENIXSerializer
    {
        private static Dictionary<object, string> sm_RegisteredSerializeObjects = new Dictionary<object, string>();
        private static List<string> sm_SerializedObjectsResult = new List<string>();

        private static uint sm_DepthSerialization;

        public static string Tab => ENIXInfo.GetTab(sm_DepthSerialization);

        public static void Serialize(string name, object[] objects, string path)
        {
            sm_RegisteredSerializeObjects.Clear();

            if (Directory.Exists(path) == false)
                Directory.CreateDirectory(path);

            List<string> serializedObjects = Serialize(objects);

            string enix = "#ENIX v1.0";

            foreach (string serializedObject in serializedObjects)
                enix += serializedObject;

            string pathWithFile = $"{path}/{name}.enix";
            File.WriteAllText(pathWithFile, enix);
        }

        public static List<string> Serialize(object[] objects)
        {
            Reset();

            sm_RegisteredSerializeObjects = new Dictionary<object, string>(objects.Length);
            sm_SerializedObjectsResult = new List<string>(objects.Length);

            foreach (object obj in objects)
                SerializeObject(obj);

            return sm_SerializedObjectsResult;
        }

        public static List<string> Serialize(List<object[]> cluster, int objectCount)
        {
            Reset();

            sm_RegisteredSerializeObjects = new Dictionary<object, string>(objectCount);
            sm_SerializedObjectsResult = new List<string>(objectCount);

            for (int i = 0; i < cluster.Count; i++)
                SerializeCluster(cluster[i]);

            return sm_SerializedObjectsResult;
        }

        public static List<string> Serialize(object[] objects, int countInCluster)
        {
            List<object[]> clusters = Clusterize(objects, countInCluster);

            Reset();

            sm_RegisteredSerializeObjects = new Dictionary<object, string>(objects.Length);
            sm_SerializedObjectsResult = new List<string>(objects.Length);

            for (int i = 0; i < clusters.Count; i++)
                SerializeCluster(clusters[i]);

            return sm_SerializedObjectsResult;
        }

        private static void SerializeCluster(object[] objects)
        {
            foreach (object obj in objects)
                SerializeObject(obj);
        }

        public static List<object[]> Clusterize(object[] objects, int countInCluster)
        {
            int clusterCount = (int)MathF.Round((float)objects.Length / countInCluster);

            List<object[]> clusters = new List<object[]>(clusterCount);

            for (int i = 0; i < clusterCount; i++)
            {
                object[] clusterObjects = new object[countInCluster];

                int startPoint = i * countInCluster;

                for (int j = 0; j < countInCluster; j++)
                {
                    if (startPoint + j > objects.Length)
                        break;

                    clusterObjects[j] = objects[startPoint + j];
                }

                clusters.Add(clusterObjects);
            }

            return clusters;
        }

        public static string SerializeObject<T>(T obj) where T : class
        {
            if (obj == null)
                return string.Empty;

            sm_DepthSerialization = 0;
            string serializedObject = string.Empty;

            string guid = Guid.NewGuid().ToString();
            sm_RegisteredSerializeObjects.Add(obj, guid);

            string tab = Tab;

            serializedObject += $"{tab}Object : {obj.GetType()} : {guid}";
            serializedObject += $"{tab}{{";
            sm_DepthSerialization++;

            FieldInfo[] fields = ENIXCash.GetFields(obj.GetType(), BindingFlags.Public
              | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (FieldInfo property in fields)
                serializedObject += SerializeProperty(property, obj);

            PropertyInfo[] properties = ENIXCash.GetProperties(obj.GetType(), BindingFlags.Public
                | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (PropertyInfo property in properties)
                serializedObject += SerializeProperty(property, obj);

            sm_DepthSerialization--;
            serializedObject += $"{tab}}}";

            sm_SerializedObjectsResult.Add(serializedObject);
            return serializedObject;
        }

        public static string SerializeProperty(FieldInfo property, object obj, bool isSerializedProperty = true)
        {
            if (isSerializedProperty && property.IsDefined(typeof(SerializebleProperty)) == false)
                return string.Empty;

            object? value = property.GetValue(obj);
            string name = property.Name;

            return SerializeProperty(value, name, property.FieldType);
        }

        public static string SerializeProperty(PropertyInfo property, object obj, bool isSerializedProperty = true)
        {
            MemberInfo? memberInfo = property.SetMethod;

            if (memberInfo == null)
                return string.Empty;

            if (isSerializedProperty && property.IsDefined(typeof(SerializebleProperty)) == false)
                return string.Empty;

            object? value = property.GetValue(obj);
            string name = property.Name;

            return SerializeProperty(value, name, property.PropertyType);
        }

        public static string SerializeProperty(object? property, string name, Type type)
        {
            if (property == null)
                return string.Empty;

            string serializedProperty = string.Empty;

            if (RegisterCustomSerializer.TryGetPropertySerializeMethod(type, out MethodInfo method))
            {
                object[] args = { property, name, type };
                object? result = method.Invoke(null, args);
                serializedProperty += result.ToString();
            }
            else if (type.IsArray)
            {
                serializedProperty += SerializeArray(property, name);
            }
            else if (type.IsList())
            {
                serializedProperty += SerializeList(property, name, type);
            }
            else if (type.IsDictionary())
            {
                serializedProperty += SerializeDictionary(property, name, type);
            }
            else if (type.IsClass())
            {
                uint death = sm_DepthSerialization;

                if (sm_RegisteredSerializeObjects.ContainsKey(property) == false)
                    SerializeObject(property);

                sm_DepthSerialization = death;

                serializedProperty = $"{Tab}{name} : {sm_RegisteredSerializeObjects[property]}";
            }
            else if (type.IsStruct())
            {
                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                serializedProperty += SerializeStruct(property, name, fields);
            }
            else if (type.IsEnum)
            {
                serializedProperty = SerializeEnum(property, name);
            }
            else
            {
                serializedProperty = $"{Tab}{name} : {property}";
            }

            return serializedProperty;
        }

        public static string SerializeArray(object? property, string name)
        {
            string serializedProperty = string.Empty;

            serializedProperty += $"{Tab}{name}";
            serializedProperty += $"{Tab}{{";
            sm_DepthSerialization++;

            Array? array = (Array?)property;

#if DEBUG
            if (array == null)
                throw new Exception("This object is not an array");
#endif

            Type? elementType = array.GetType().GetElementType();

#if DEBUG
            if (elementType == null)
            {
                throw new Exception("It was not possible to " +
                    "identify the type of this array");
            }
#endif

            for (int i = 0; i < array.Length; i++)
            {
                serializedProperty += SerializeProperty(array.GetValue(i),
                    $"Element{i}", elementType);
            }

            sm_DepthSerialization--;
            serializedProperty += $"{Tab}}}";

            return serializedProperty;
        }

        public static string SerializeDictionary(object? property, string name, Type type)
        {
            string serializedProperty = string.Empty;

            IDictionary? dictionary = property as IDictionary;

#if DEBUG
            if (dictionary == null)
                throw new Exception("This object is not an dictionary");
#endif

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

            return serializedProperty;
        }

        public static string SerializeList(object? property, string name, Type type)
        {
            string serializedProperty = string.Empty;

            IList? list = (IList?)property;

#if DEBUG
            if (list == null)
                throw new Exception("This object is not an list");
#endif

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

            return serializedProperty;
        }

        public static string SerializeEnum(object? property, string name)
        {
            string serializedProperty = $"{Tab}{name} : {(int?)property}";
            return serializedProperty;
        }

        public static string SerializeStruct(object property, string name,
            FieldInfo[] fields, bool isSerializedProperty = true)
        {
            string serializedStruct = string.Empty;

            serializedStruct += $"{Tab}{name}";
            serializedStruct += $"{Tab}{{";
            sm_DepthSerialization++;

            foreach (FieldInfo field in fields)
                serializedStruct += SerializeProperty(field, property, isSerializedProperty);

            sm_DepthSerialization--;
            serializedStruct += $"{Tab}}}";

            return serializedStruct;
        }

        public static void Reset()
        {
            sm_RegisteredSerializeObjects.Clear();
            sm_SerializedObjectsResult.Clear();
            sm_DepthSerialization = 0;
        }
    }
}
