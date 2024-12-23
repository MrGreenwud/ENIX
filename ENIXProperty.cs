using System;
using System.Reflection;

namespace ENIX
{
    public class ENIXProperty
    {
        private FieldInfo? m_Field;
        private PropertyInfo? m_Property;
        private Type? m_Type;

        public Type Type
        {
            get
            {
                if (m_Type == null)
                    m_Type = m_Field != null ? m_Field.FieldType : m_Property.PropertyType;

                return m_Type;
            }
        }

        public ENIXProperty(FieldInfo? field)
        {
            m_Field = field;
        }

        public ENIXProperty(PropertyInfo? property)
        {
            m_Property = property;
        }

        public void SetValue(object? obj, object value)
        {
            if (m_Field != null)
                m_Field.SetValue(obj, value);
            else if (m_Property != null)
                m_Property.SetValue(obj, value);
        }
    }
}
