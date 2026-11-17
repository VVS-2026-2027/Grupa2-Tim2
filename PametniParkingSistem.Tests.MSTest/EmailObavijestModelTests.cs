
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using System.Xml.Linq;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class EmailObavijestModelTests
    {
        [TestMethod]
        public void Primalac_MozeSePostavitiIDohvatiti()
        {
            var obavijest = new EmailObavijest();

            obavijest.Primalac = "test@test.com";

            Assert.AreEqual("test@test.com", obavijest.Primalac);
        }

        [TestMethod]
        public void Naslov_MozeSePostavitiIDohvatiti()
        {
            var obavijest = new EmailObavijest();

            obavijest.Naslov = "Potvrda rezervacije";

            Assert.AreEqual("Potvrda rezervacije", obavijest.Naslov);
        }

        [TestMethod]
        public void Sadrzaj_MozeSePostavitiIDohvatiti()
        {
            var obavijest = new EmailObavijest();

            obavijest.Sadrzaj = "Rezervacija je uspješno kreirana.";

            Assert.AreEqual(
                "Rezervacija je uspješno kreirana.",
                obavijest.Sadrzaj);
        }

        [TestMethod]
        public void DatumSlanja_MozeSePostavitiIDohvatiti()
        {
            var obavijest = new EmailObavijest();
            var datum = new DateTime(2026, 11, 17);

            obavijest.DatumSlanja = datum;

            Assert.AreEqual(datum, obavijest.DatumSlanja);
        }

        [TestMethod]
        public void TipObavijesti_MozeSePostavitiIDohvatiti()
        {
            var obavijest = new EmailObavijest();

            obavijest.TipObavijesti = TipObavijesti.Podsjetnik;

            Assert.AreEqual(
                TipObavijesti.Podsjetnik,
                obavijest.TipObavijesti);
        }

        [TestMethod]
        public void StatusEmaila_MozeSePostavitiIDohvatiti()
        {
            var obavijest = new EmailObavijest();

            obavijest.StatusEmaila = StatusEmaila.Poslan;

            Assert.AreEqual(
                StatusEmaila.Poslan,
                obavijest.StatusEmaila);
        }

        // Ucitavanje podataka iz CSV datoteke
        public static IEnumerable<object[]> CsvPodaci
        {
            get
            {
                string putanja = Path.Combine(
                    AppContext.BaseDirectory,
                    "TestData",
                    "EmailObavijesti.csv");

                return File.ReadAllLines(putanja)
                    .Skip(1)
                    .Where(red => !string.IsNullOrWhiteSpace(red))
                    .Select(red =>
                    {
                        string[] podaci = red.Split(';');

                        return new object[]
                        {
                            podaci[0],
                            podaci[1],
                            podaci[2],
                            podaci[3],
                            podaci[4]
                        };
                    })
                    .ToArray();
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(CsvPodaci), DynamicDataSourceType.Property)]
        public void EmailObavijest_IspravnoPostavljaPodatkeIzCSV(
            string primalac,
            string naslov,
            string sadrzaj,
            string tip,
            string status)
        {
            var obavijest = new EmailObavijest
            {
                Primalac = primalac,
                Naslov = naslov,
                Sadrzaj = sadrzaj,
                TipObavijesti = Enum.Parse<TipObavijesti>(tip),
                StatusEmaila = Enum.Parse<StatusEmaila>(status)
            };

            Assert.AreEqual(primalac, obavijest.Primalac);
            Assert.AreEqual(naslov, obavijest.Naslov);
            Assert.AreEqual(sadrzaj, obavijest.Sadrzaj);
            Assert.AreEqual(Enum.Parse<TipObavijesti>(tip), obavijest.TipObavijesti);
            Assert.AreEqual(Enum.Parse<StatusEmaila>(status), obavijest.StatusEmaila);
        }

        // Ucitavanje podataka iz XML datoteke
        public static IEnumerable<object[]> XmlPodaci
        {
            get
            {
                string putanja = Path.Combine(
                    AppContext.BaseDirectory,
                    "TestData",
                    "EmailObavijesti.xml");

                XDocument dokument = XDocument.Load(putanja);

                return dokument.Descendants("EmailObavijest")
                    .Select(element => new object[]
                    {
                        (string)element.Element("Primalac")!,
                        (string)element.Element("Naslov")!,
                        (string)element.Element("Sadrzaj")!,
                        (string)element.Element("TipObavijesti")!,
                        (string)element.Element("StatusEmaila")!
                    })
                    .ToArray();
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(XmlPodaci), DynamicDataSourceType.Property)]
        public void EmailObavijest_IspravnoPostavljaPodatkeIzXML(
            string primalac,
            string naslov,
            string sadrzaj,
            string tip,
            string status)
        {
            var obavijest = new EmailObavijest
            {
                Primalac = primalac,
                Naslov = naslov,
                Sadrzaj = sadrzaj,
                TipObavijesti = Enum.Parse<TipObavijesti>(tip),
                StatusEmaila = Enum.Parse<StatusEmaila>(status)
            };

            Assert.AreEqual(primalac, obavijest.Primalac);
            Assert.AreEqual(naslov, obavijest.Naslov);
            Assert.AreEqual(sadrzaj, obavijest.Sadrzaj);
            Assert.AreEqual(Enum.Parse<TipObavijesti>(tip), obavijest.TipObavijesti);
            Assert.AreEqual(Enum.Parse<StatusEmaila>(status), obavijest.StatusEmaila);
        }
    }
}
