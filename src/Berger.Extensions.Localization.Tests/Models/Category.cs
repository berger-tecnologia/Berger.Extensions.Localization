namespace Berger.Extensions.Localization.Tests.Models
{
    public class Category : ILocalizable
    {
        [Localized]
        public string Name { get; set; }
        [Localized]
        public string Description { get; set; }
    }
}