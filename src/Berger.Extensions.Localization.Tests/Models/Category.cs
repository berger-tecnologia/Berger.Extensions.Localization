using System;

namespace Berger.Extensions.Localization.Tests.Models
{
    public class Category : ILocalizable
    {
        public Guid ID { get; set; }
        [Localized]
        public string Name { get; set; }
        [Localized]
        public string Description { get; set; }
    }
}
