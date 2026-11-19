
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
        public void PostaviPrimalac_IspravnaEmailAdresa_VracaPostavljenuVrijednost()
        {
            var obavijest = new EmailObavijest();

            obavijest.Primalac = "test@test.com";

            Assert.AreEqual("test@test.com", obavijest.Primalac);
        }

        [TestMethod]
        public void PostaviNaslov_IspravanNaslov_VracaPostavljenuVrijednost()
        {
            var obavijest = new EmailObavijest();

            obavijest.Naslov = "Potvrda rezervacije";

            Assert.AreEqual("Potvrda rezervacije", obavijest.Naslov);
        }

        [TestMethod]
        public void PostaviSadrzaj_IspravanSadrzaj_VracaPostavljenuVrijednost()
        {
            var obavijest = new EmailObavijest();

            obavijest.Sadrzaj = "Rezervacija je uspješno kreirana.";

            Assert.AreEqual(
                "Rezervacija je uspješno kreirana.",
                obavijest.Sadrzaj);
        }

        [TestMethod]
        public void PostaviDatumSlanja_IspravanDatum_VracaPostavljeniDatum()
        {
            var obavijest = new EmailObavijest();
            var datum = new DateTime(2026, 11, 17);

            obavijest.DatumSlanja = datum;

            Assert.AreEqual(datum, obavijest.DatumSlanja);
        }

        [TestMethod]
        public void PostaviTipObavijesti_TipPodsjetnik_VracaPostavljeniTip()
        {
            var obavijest = new EmailObavijest();

            obavijest.TipObavijesti = TipObavijesti.Podsjetnik;

            Assert.AreEqual(
                TipObavijesti.Podsjetnik,
                obavijest.TipObavijesti);
        }

        [TestMethod]
        public void PostaviStatusEmaila_StatusPoslan_VracaPostavljeniStatus()
        {
            var obavijest = new EmailObavijest();

            obavijest.StatusEmaila = StatusEmaila.Poslan;

            Assert.AreEqual(
                StatusEmaila.Poslan,
                obavijest.StatusEmaila);
        }

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

        [TestMethod]
        [DynamicData(nameof(CsvPodaci))]
        public void KreirajEmailObavijest_PodaciIzCSV_IspravnoPostavljaSvaPolja(
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

        [TestMethod]
        [DynamicData(nameof(XmlPodaci))]
        public void KreirajEmailObavijest_PodaciIzXML_IspravnoPostavljaSvaPolja(
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

        [TestMethod]
        public void CsvPodaci_KadaSeUcitaDatoteka_VracaIspravnePodatke()
        {
            var podaci = CsvPodaci.ToList();

            Assert.IsTrue(podaci.Count > 0);

            foreach (var red in podaci)
            {
                Assert.AreEqual(5, red.Length);

                foreach (var vrijednost in red)
                {
                    Assert.IsInstanceOfType(vrijednost, typeof(string));
                }
            }
        }

        [TestMethod]
        public void XmlPodaci_KadaSeUcitaDatoteka_VracaIspravnePodatke()
        {
            var podaci = XmlPodaci.ToList();

            Assert.IsTrue(podaci.Count > 0);

            foreach (var red in podaci)
            {
                Assert.AreEqual(5, red.Length);

                foreach (var vrijednost in red)
                {
                    Assert.IsInstanceOfType(vrijednost, typeof(string));
                }
            }
        }

    }
}
