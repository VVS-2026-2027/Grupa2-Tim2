using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    [TestCategory("Model")]
    public class PlacanjeModelTests
    {
        private Placanje _placanje = null!;

        [TestInitialize]
        public void Pripremi()
        {
            _placanje = new Placanje();
        }

        [TestMethod]
        public void NovoPlacanje_TekstualnaPoljaSuPrazna()
        {
            Assert.AreEqual(string.Empty, _placanje.ImeVlasnikaKartice);
            Assert.AreEqual(string.Empty, _placanje.BrojKarticeMaskiran);
            Assert.AreEqual(string.Empty, _placanje.TransakcijskiBroj);
        }

        [TestMethod]
        public void NovoPlacanje_RezervacijaJeNull()
        {
            Assert.IsNull(_placanje.Rezervacija);
        }

        [TestMethod]
        public void NovoPlacanje_StatusJeNaCekanju()
        {
            // NaCekanju je prva vrijednost enuma, pa je to podrazumijevani status.
            Assert.AreEqual(StatusPlacanja.NaCekanju, _placanje.StatusPlacanja);
        }

        [TestMethod]
        public void Svojstva_PostavljenaVrijednost_SeVraca()
        {
            _placanje.Iznos = 19.99;
            _placanje.RezervacijaId = 4;
            _placanje.StatusPlacanja = StatusPlacanja.Uspjesno;

            Assert.AreEqual(19.99, _placanje.Iznos);
            Assert.AreEqual(4, _placanje.RezervacijaId);
            Assert.AreEqual(StatusPlacanja.Uspjesno, _placanje.StatusPlacanja);
        }
    }
}
