using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class KaznaServiceTests
    {
        [TestMethod]
        public void IzracunajKaznu_UnutarTolerancije_VracaNula()
        {
            // Arrange
            var service = new KaznaService();

            var rezervacija = new Rezervacija
            {
                VrijemePocetka = new DateTime(2026, 11, 20, 10, 0, 0),
                VrijemeKraja = new DateTime(2026, 11, 20, 12, 0, 0),
                StatusRezervacije = StatusRezervacije.Aktivna
            };

            var stvarnoVrijemeIzlaska =
                new DateTime(2026, 11, 20, 12, 10, 0);

            // Act
            var rezultat = service.IzracunajKaznu(
                rezervacija,
                stvarnoVrijemeIzlaska);

            // Assert
            Assert.AreEqual(0.0, rezultat);
        }
    }
}