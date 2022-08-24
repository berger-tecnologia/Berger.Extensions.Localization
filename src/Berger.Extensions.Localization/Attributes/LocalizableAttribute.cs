namespace Berger.Extensions.Localization
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class LocalizedAttribute : Attribute
    {
        public LocalizedAttribute() : base()
        {
        }
    }
}