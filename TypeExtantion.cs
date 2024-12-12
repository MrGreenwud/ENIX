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
    }
}
