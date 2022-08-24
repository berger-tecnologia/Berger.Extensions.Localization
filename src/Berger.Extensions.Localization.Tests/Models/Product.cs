namespace Berger.Extensions.Localization.Tests.Models
{
    public class Product : ILocalizable
    {
        [Localized]
        public string Name { get; set; }

        [Localized]
        public string Description { get; set; }

        public bool IsAvailable { get; set; }
        public Category Category { get; set; }
    }
}