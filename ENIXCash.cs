using System.Reflection;
using ENIX.Extantions;

namespace ENIX
{
    internal static class ENIXCash
    {
        private static Dictionary<Type, FieldInfo[]> sm_FieldCash = new Dictionary<Type, FieldInfo[]>();
        private static Dictionary<Type, PropertyInfo[]> sm_PropertyCash = new Dictionary<Type, PropertyInfo[]>();

        public static FieldInfo[] GetFields(Type type, BindingFlags flags)
        {
            if (sm_FieldCash.TryGetValue(type, out FieldInfo[]? result) == false)
            {
                CashFields(type, flags);
                return GetFields(type);
            }

            return result;
        }

        public static PropertyInfo[] GetProperties(Type type, BindingFlags flags)
        {
            if (sm_PropertyCash.TryGetValue(type, out PropertyInfo[]? result) == false)
            {
                CashProperties(type, flags);
                return GetProperties(type);
            }

            return GetProperties(type);
        }

        public static void CashFields(Type type, BindingFlags flags)
        {
            FieldInfo[] fields = type.GetAllFieldsByType(flags);
            sm_FieldCash.Add(type, fields);
        }

        public static FieldInfo[] GetFields(Type type)
        {
            if (sm_FieldCash.ContainsKey(type) == false)
                throw new Exception();

            return sm_FieldCash[type];
        }

        public static void CashProperties(Type type, BindingFlags flags)
        {
            PropertyInfo[] properties = type.GetAllPropertiesByType(flags);
            sm_PropertyCash.Add(type, properties);
        }

        public static PropertyInfo[] GetProperties(Type type)
        {
            if (sm_PropertyCash.ContainsKey(type) == false)
                throw new Exception();

            return sm_PropertyCash[type];
        }

        public static void Clear()
        {
            sm_FieldCash.Clear();
            sm_PropertyCash.Clear();
        }
    }
}
