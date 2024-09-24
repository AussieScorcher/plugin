namespace vatACARS.Util
{
    public static class ObjectUtils
    {
        public static T CloneObject<T>(T source) where T : new()
        {
            var type = typeof(T);
            var clone = new T();

            foreach (var property in type.GetProperties())
            {
                if (property.CanWrite)
                {
                    var value = property.GetValue(source);
                    property.SetValue(clone, value);
                }
            }

            return clone;
        }
    }

}
