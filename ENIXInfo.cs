namespace ENIX
{
    public static class ENIXInfo
    {
        internal static string GetTab(uint depth)
        {
            string tab = "\n";

            for (int i = 0; i < depth; i++)
                tab += "\t";

            return tab;
        }

        public static Type? GetSerializedObjectType(string serializedObject)
        {
            string[] lines = serializedObject.Split('\n');
            string line = lines[1].Trim();

            string[] part = line.Split(":");
            Type? ObjectType = Type.GetType(part[1].Trim());
            return ObjectType;
        }

        public static Dictionary<string, string> GetPropertes(string serializedElement)
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
