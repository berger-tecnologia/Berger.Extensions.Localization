using System.Linq;
using NUnit.Framework;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using Berger.Extensions.Localization.Tests.Models;

namespace Berger.Extensions.Localization.Tests
{
    public class Tests
    {
        private Product Product = new();
        private readonly HashSet<string> Optional = new(new[] { "es" });
        private readonly HashSet<string> Required = new(new[] { "en", "pt" });

        private readonly string ProductName = $"{{\"en\":\"Car\",\"pt\":\"Carro\"}}";
        private readonly string ProductDescription = $"{{\"en\":\"This is a product description.\",\"pt\":\"Isso é uma descrição de produto.\"}}";

        private readonly string CategoryName = $"{{\"en\":\"Cars\",\"pt\":\"Carros\"}}";
        private readonly string CategoryDescription = $"{{\"en\":\"This is a category description.\",\"pt\":\"Isso é uma descrição de categoria.\"}}";

        [SetUp]
        public void Setup()
        {
            Localizer.Configure(Required, Optional);

            var culture = CultureInfo.GetCultureInfo("en");

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            Product = new Product()
            {
                Name = ProductName,
                Description = ProductDescription,
                Category = new Category()
                {
                    Name = CategoryName,
                    Description = CategoryDescription
                }
            };
        }

        [Test]
        [Order(0)]
        public void ShouldBeConfigured()
        {
            Assert.IsTrue(Localizer.IsConfigured);
        }

        [Test]
        [Order(1)]
        public void ShouldGetRequiredLanguages()
        {
            Assert.IsTrue(!Localizer.Required.Except(Required).Any());
        }

        [Test]
        [Order(2)]
        public void ShouldGetOptionalLanguages()
        {
            Assert.IsTrue(!Localizer.Optional.Except(Optional).Any());
        }

        [Test]
        [Order(3)]
        public void ShouldGetAllLanguages()
        {
            var expected = Required.Union(Optional);

            Assert.IsTrue(!Localizer.Supported.Except(expected).Any());
        }

        [Test]
        [Order(4)]
        public void ShouldTranslateProductInEnglish()
        {
            Product.Localize(Depth.Deep);

            Assert.IsTrue(Product.Name == "Car");
        }

        [Test]
        [Order(5)]
        public void ShouldTranslateProductInPortuguese()
        {
            Product.Localize("pt", Depth.Deep);

            Assert.IsTrue(Product.Name == "Carro");
        }

        [Test]
        [Order(6)]
        public void ShouldNotTranslateProductInPortuguese()
        {
            Product.Localize(Depth.Deep);

            Assert.IsFalse(Product.Name == "Carro");
        }

        [Test]
        [Order(7)]
        public void ShouldSerialize()
        {
            var expected = $"{{\"en\":\"Car\",\"pt\":\"Carro\",\"es\":\"Coche\"}}";

            var dictionary = new Dictionary<string, string>()
            {
                { "en", "Car" },
                { "pt", "Carro" },
                { "es", "Coche" },
            };

            var result = Localizer.Serialize(dictionary);

            Assert.IsTrue(result == expected);
        }

        [Test]
        [Order(8)]
        public void ShouldDeserialize()
        {
            var json = $"{{\"en\":\"Car\",\"pt\":\"Carro\",\"es\":\"Coche\"}}";

            var expected = new Dictionary<string, string>()
            {
                { "en", "Car" },
                { "pt", "Carro" },
                { "es", "Coche" },
            };

            var result = Localizer.Deserialize(json);

            Assert.IsTrue(expected.Count == result.Count && !expected.Except(result).Any());
        }
    }
}