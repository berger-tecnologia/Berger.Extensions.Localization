using System.Reflection;

namespace Berger.Extensions.Localization
{
    public static class Reflection
    {
        public static T CloneObject<T>(this T objSource) where T : class
        {
            //Get the type of source object and create a new instance of that type
            var typeSource = objSource.GetType();

            T target = Activator.CreateInstance(typeSource) as T;

            PropertyInfo[] propertyInfo = typeSource.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (PropertyInfo property in propertyInfo)
            {
                if (property.CanWrite)
                {
                    if (property.PropertyType.IsValueType || property.PropertyType.IsEnum || property.PropertyType.Equals(typeof(System.String)))
                    {
                        property.SetValue(target, property.GetValue(objSource, null), null);
                    }
                    else
                    {
                        object objPropertyValue = property.GetValue(objSource, null);

                        if (objPropertyValue == null)
                            property.SetValue(target, null, null);
                        else
                            property.SetValue(target, objPropertyValue.CloneObject(), null);
                    }
                }
            }

            return target;
        }
    }
}