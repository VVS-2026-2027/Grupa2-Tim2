using System.ComponentModel.DataAnnotations;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    [TestCategory("Model")]
    public class PodrskaZahtjevModelTests
    {
        private PodrskaZahtjev _zahtjev = null!;

        [TestInitialize]
        public void Pripremi()
        {
            _zahtjev = new PodrskaZahtjev { Naslov = "Problem s plaćanjem", Opis = "Kartica je odbijena." };
        }

        private static bool Validiraj(PodrskaZahtjev zahtjev)
        {
            return Validator.TryValidateObject(zahtjev, new ValidationContext(zahtjev), new List<ValidationResult>(), true);
        }

        [TestMethod]
        public void NoviZahtjev_PodrazumijevaniStatusJeOtvoren()
        {
            Assert.AreEqual(StatusPodrske.Otvoren, _zahtjev.Status);
        }

        [TestMethod]
        public void NoviZahtjev_DatumKreiranjaJeUpravoPostavljen()
        {
            Assert.IsTrue(_zahtjev.DatumKreiranja <= DateTime.Now);
            Assert.IsTrue(_zahtjev.DatumKreiranja > DateTime.Now.AddMinutes(-1));
        }

        [TestMethod]
        public void NoviZahtjev_OdgovorIDatumOdgovoraSuNull()
        {
            Assert.IsNull(_zahtjev.Odgovor);
            Assert.IsNull(_zahtjev.DatumOdgovora);
        }

        [TestMethod]
        public void Validacija_ValjanZahtjev_JeValjan()
        {
            Assert.IsTrue(Validiraj(_zahtjev));
        }

        [TestMethod]
        [DataRow("", "Opis problema", false, DisplayName = "Prazan naslov")]
        [DataRow("Naslov", "", false, DisplayName = "Prazan opis")]
        [DataRow("Naslov", "Opis", true, DisplayName = "Naslov i opis popunjeni")]
        public void Validacija_ObaveznaPolja_VracaOcekivaniRezultat(string naslov, string opis, bool ocekivano)
        {
            _zahtjev.Naslov = naslov;
            _zahtjev.Opis = opis;

            Assert.AreEqual(ocekivano, Validiraj(_zahtjev));
        }

        [TestMethod]
        [DataRow(100, true, DisplayName = "Naslov od 100 znakova je dozvoljen")]
        [DataRow(101, false, DisplayName = "Naslov od 101 znaka je predug")]
        public void Validacija_DuzinaNaslova_PostujeOgranicenje(int duzina, bool ocekivano)
        {
            _zahtjev.Naslov = new string('a', duzina);

            Assert.AreEqual(ocekivano, Validiraj(_zahtjev));
        }

        [TestMethod]
        [DataRow(1000, true, DisplayName = "Opis od 1000 znakova je dozvoljen")]
        [DataRow(1001, false, DisplayName = "Opis od 1001 znaka je predug")]
        public void Validacija_DuzinaOpisa_PostujeOgranicenje(int duzina, bool ocekivano)
        {
            _zahtjev.Opis = new string('b', duzina);

            Assert.AreEqual(ocekivano, Validiraj(_zahtjev));
        }

        [TestMethod]
        [DataRow(1000, true)]
        [DataRow(1001, false)]
        public void Validacija_DuzinaOdgovora_PostujeOgranicenje(int duzina, bool ocekivano)
        {
            _zahtjev.Odgovor = new string('c', duzina);

            Assert.AreEqual(ocekivano, Validiraj(_zahtjev));
        }
    }
}
