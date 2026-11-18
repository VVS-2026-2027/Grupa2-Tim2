using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Models;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class ParkingZonaModelTests
    {
        [TestMethod]
        public void Konstruktor_NoviObjekat_ImaPodrazumijevaneVrijednosti()
        {
            var zona = new ParkingZona();

            Assert.AreEqual(0, zona.Id);
            Assert.AreEqual(string.Empty, zona.Naziv);
            Assert.AreEqual(string.Empty, zona.Lokacija);
            Assert.AreEqual(string.Empty, zona.Opis);
            Assert.AreEqual(0.0, zona.ProsjecnaOcjena);
            Assert.IsNotNull(zona.ParkingMjesta);
            Assert.AreEqual(0, zona.ParkingMjesta.Count);
        }

        [TestMethod]
        [DataRow(1, "Centar", "Titova 1", "Zona u centru", 4.5)]
        [DataRow(2, "Ilidža", "", "", 0.0)]
        [DataRow(99, "Zona X", "Adresa 12", "Dugačak opis zone", 5.0)]
        public void Svojstva_PostavljanjeVrijednosti_VracajuIsteVrijednosti(
            int id, string naziv, string lokacija, string opis, double ocjena)
        {
            var zona = new ParkingZona
            {
                Id = id,
                Naziv = naziv,
                Lokacija = lokacija,
                Opis = opis,
                ProsjecnaOcjena = ocjena
            };

            Assert.AreEqual(id, zona.Id);
            Assert.AreEqual(naziv, zona.Naziv);
            Assert.AreEqual(lokacija, zona.Lokacija);
            Assert.AreEqual(opis, zona.Opis);
            Assert.AreEqual(ocjena, zona.ProsjecnaOcjena);
        }

        [TestMethod]
        public void ParkingMjesta_DodavanjeMjesta_KolekcijaSadrziMjesto()
        {
            var zona = new ParkingZona();
            var mjesto = new ParkingMjesto { Id = 1 };

            zona.ParkingMjesta.Add(mjesto);

            Assert.AreEqual(1, zona.ParkingMjesta.Count);
            Assert.IsTrue(zona.ParkingMjesta.Contains(mjesto));
        }
    }
}