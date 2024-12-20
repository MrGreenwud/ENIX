namespace ENIX
{
    // *.enix

    public static class ENIXFile
    {
        public static string GetTab(uint depth)
        {
            string tab = "\n";

            for (int i = 0; i < depth; i++)
                tab += "\t";

            return tab;
        }

        public static Dictionary<Type, List<object>> FilterObjectsByType(object[] objects, 
            Type[] typeFilter, bool isConsiderBasicTypes = false)
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
    }
}