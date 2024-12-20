using System.Reflection;

namespace ENIX.Extantions
{
    public static class TypeExtantion
    {
        public static bool IsStruct(this Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum;
        }

        public static bool IsClass(this Type type)
        {
            return type.IsClass == true && type.IsGenericType == false
                && type != typeof(string) && type.IsArray == false;
        }

        public static bool IsList(this Type type)
        {
            return type.IsGenericType == true
                && type.GetGenericTypeDefinition() == typeof(List<>);
        }

        public static bool IsDictionary(this Type type)
        {
            return type.IsGenericType == true
                && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        }

        public static FieldInfo[] GetAllFieldsByType(this Type type, BindingFlags flags)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            HashSet<string> addedFieldNames = new HashSet<string>();

            result.AddRange(type.GetFields(flags));

            foreach (FieldInfo fieldInfo in result)
                addedFieldNames.Add(fieldInfo.Name);

            if (type.BaseType != null)
            {
                FieldInfo[] fieldsInfo = GetAllFieldsByType(type.BaseType, flags);

                foreach (FieldInfo field in fieldsInfo)
                {
                    if (addedFieldNames.Contains(field.Name))
                        continue;

                    result.Add(field);
                    addedFieldNames.Add(field.Name);
                }
            }

            return result.ToArray();
        }

        public static PropertyInfo[] GetAllPropertiesByType(this Type type, BindingFlags flags)
        {
            List<PropertyInfo> result = new List<PropertyInfo>();
            HashSet<string> addedPropertyNames = new HashSet<string>();

            result.AddRange(type.GetProperties(flags));

            foreach (PropertyInfo property in result)
            {
                addedPropertyNames.Add(property.Name);
            }

            if (type.BaseType != null)
            {
                PropertyInfo[] propertiesInfo = GetAllPropertiesByType(type.BaseType, flags);

                foreach (PropertyInfo property in propertiesInfo)
                {
                    if (addedPropertyNames.Contains(property.Name))
                        continue;

                    result.Add(property);
                    addedPropertyNames.Add(property.Name);
                }
            }

            return result.ToArray();
        }
    }
}
