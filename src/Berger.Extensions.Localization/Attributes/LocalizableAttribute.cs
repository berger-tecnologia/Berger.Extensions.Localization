namespace Berger.Extensions.Localization.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class LocalizedAttribute : Attribute
    {
        public LocalizedAttribute() : base()
        {
            var a = 1;
        }
    }
}
