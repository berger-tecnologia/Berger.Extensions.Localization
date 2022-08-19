using Berger.Extensions.Localization.Tests.Models;
using NUnit.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Berger.Extensions.Localization.Tests
{
    public class Tests
    {
        private Product Product = new();
        private HashSet<string> ReqLanguages = new HashSet<string>(new[] { "en", "pt" });
        private HashSet<string> OptLanguages = new HashSet<string>(new[] { "es" });

        private string ProductNamejson = $"{{\"en\":\"Car\",\"pt\":\"Carro\"}}";
        private string ProductDescriptionjson = $"{{\"en\":\"This is a prodcut description!\",\"pt\":\"Isso é uma descrição de produto!\"}}";

        private string CategoryNamejson = $"{{\"en\":\"Cars\",\"pt\":\"Carros\"}}";
        private string CategoryDescriptionjson = $"{{\"en\":\"This is a category description!\",\"pt\":\"Isso é uma descrição de categoria!\"}}";

        [SetUp]
        public void Setup()
        {
            ObjectLocalizer.Configure(ReqLanguages, OptLanguages);

            // Setting the Thread culture to en
            var culture = CultureInfo.GetCultureInfo("en");

            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;

            // Creating the product
            Product = new Product()
            {
                Name = ProductNamejson,
                Description = ProductDescriptionjson,
                Category = new Category()
                {
                    Name = CategoryNamejson,
                    Description = CategoryDescriptionjson
                }
            };
        }

        [Test]
        [Order(0)]
        public void ShouldBeConfigured()
        {
            Assert.IsTrue(ObjectLocalizer.IsConfigured);
        }

        [Test]
        [Order(1)]
        public void ShouldGetRequiredLanguages()
        {
            var expected = ReqLanguages;

            Assert.IsTrue(!ObjectLocalizer.RequiredLanguages.Except(expected).Any());
        }

        [Test]
        [Order(2)]
        public void ShouldGetOptionalLanguages()
        {
            var expected = OptLanguages;

            Assert.IsTrue(!ObjectLocalizer.OptionalLanguages.Except(expected).Any());
        }

        [Test]
        [Order(3)]
        public void ShouldGetAllLanguages()
        {
            var expected = ReqLanguages.Union(OptLanguages);

            Assert.IsTrue(!ObjectLocalizer.SupportedLanguages.Except(expected).Any());
        }

        [Test]
        [Order(4)]
        public void ShouldTranslateProductInEnglish()
        {
            Product.Localize(LocalizationDepth.Deep);

            Assert.IsTrue(Product.Name == "Car");
        }

        [Test]
        [Order(5)]
        public void ShouldTranslateProductInPortuguese()
        {
            Product.Localize("pt", LocalizationDepth.Deep);

            Assert.IsTrue(Product.Name == "Carro");
        }

        [Test]
        [Order(6)]
        public void ShouldNotTranslateProductInPortuguese()
        {
            Product.Localize(LocalizationDepth.Deep);

            Assert.IsFalse(Product.Name == "Carro");
        }

        [Test]
        [Order(7)]
        public void ShouldSerealize()
        {
            var expected = $"{{\"en\":\"Car\",\"pt\":\"Carro\",\"es\":\"Coche\"}}";

            var dictionary = new Dictionary<string, string>()
            {
                { "en", "Car" },
                { "pt", "Carro" },
                { "es", "Coche" },
            };

            var result = ObjectLocalizer.Serialize(dictionary);

            Assert.IsTrue(result == expected);
        }

        [Test]
        [Order(8)]
        public void ShouldDeserealize()
        {
            var json = $"{{\"en\":\"Car\",\"pt\":\"Carro\",\"es\":\"Coche\"}}";

            var expected = new Dictionary<string, string>()
            {
                { "en", "Car" },
                { "pt", "Carro" },
                { "es", "Coche" },
            };

            var result = ObjectLocalizer.Deserialize(json);

            Assert.IsTrue(expected.Count == result.Count && !expected.Except(result).Any());
        }
    }
}