using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using System;
using System.Linq;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class ParkingMjestoModelTests
    {
        [TestMethod]
        public void Konstruktor_NoviObjekat_ImaPodrazumijevaneVrijednosti()
        {
            var mjesto = new ParkingMjesto();

            Assert.AreEqual(string.Empty, mjesto.Oznaka);
            Assert.IsNotNull(mjesto.Rezervacije);
            Assert.AreEqual(0, mjesto.Rezervacije.Count);
            Assert.IsNull(mjesto.ParkingZona);
        }

        [TestMethod]
        [DataRow(1, "A1", true, 10.5, 2.0, 3)]
        [DataRow(2, "B7", false, 0.0, 0.0, 1)]
        [DataRow(99, "", false, 150.25, 1000.5, 8)]
        public void Svojstva_PostavljanjeVrijednosti_VracajuIsteVrijednosti(
            int id, string oznaka, bool natkriveno, double udaljenost, double cijena, int zonaId)
        {
            var mjesto = new ParkingMjesto
            {
                Id = id,
                Oznaka = oznaka,
                Natkriveno = natkriveno,
                UdaljenostOdUlaza = udaljenost,
                CijenaPoSatu = cijena,
                ParkingZonaId = zonaId
            };

            Assert.AreEqual(id, mjesto.Id);
            Assert.AreEqual(oznaka, mjesto.Oznaka);
            Assert.AreEqual(natkriveno, mjesto.Natkriveno);
            Assert.AreEqual(udaljenost, mjesto.UdaljenostOdUlaza);
            Assert.AreEqual(cijena, mjesto.CijenaPoSatu);
            Assert.AreEqual(zonaId, mjesto.ParkingZonaId);
        }

        [TestMethod]
        public void Svojstva_PostavljanjeEnumaIZone_VracajuIsteVrijednosti()
        {
            var zona = new ParkingZona();
            var tip = Enum.GetValues<TipMjesta>().First();
            var mjesto = new ParkingMjesto
            {
                Status = StatusMjesta.VanFunkcije,
                TipMjesta = tip,
                ParkingZona = zona
            };

            Assert.AreEqual(StatusMjesta.VanFunkcije, mjesto.Status);
            Assert.AreEqual(tip, mjesto.TipMjesta);
            Assert.AreSame(zona, mjesto.ParkingZona);
        }
    }
}