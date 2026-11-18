using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using System;
using System.Linq;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class KriterijPretrageModelTests
    {
        [TestMethod]
        public void Konstruktor_NoviObjekat_ImaPodrazumijevaneVrijednosti()
        {
            var kriterij = new KriterijPretrage();

            Assert.AreEqual(0, kriterij.Id);
            Assert.AreEqual(string.Empty, kriterij.Zona);
            Assert.IsFalse(kriterij.Natkriveno);
            Assert.AreEqual(0.0, kriterij.MinCijena);
            Assert.AreEqual(0.0, kriterij.MaxCijena);
        }

        [TestMethod]
        [DataRow(1, "Centar", true, 0.0, 5.0)]
        [DataRow(2, "Ilidža", false, 1.5, 10.25)]
        [DataRow(3, "", false, 0.0, 0.0)]
        [DataRow(99, "Zona X", true, 100.0, 1000.5)]
        public void Svojstva_PostavljanjeVrijednosti_VracajuIsteVrijednosti(
            int id, string zona, bool natkriveno, double minCijena, double maxCijena)
        {
            var kriterij = new KriterijPretrage
            {
                Id = id,
                Zona = zona,
                Natkriveno = natkriveno,
                MinCijena = minCijena,
                MaxCijena = maxCijena
            };

            Assert.AreEqual(id, kriterij.Id);
            Assert.AreEqual(zona, kriterij.Zona);
            Assert.AreEqual(natkriveno, kriterij.Natkriveno);
            Assert.AreEqual(minCijena, kriterij.MinCijena);
            Assert.AreEqual(maxCijena, kriterij.MaxCijena);
        }

        [TestMethod]
        public void Svojstva_PostavljanjeEnuma_VracajuIsteVrijednosti()
        {
            var tip = Enum.GetValues<TipMjesta>().Last();
            var kriterij = new KriterijPretrage
            {
                Status = StatusMjesta.VanFunkcije,
                TipMjesta = tip
            };

            Assert.AreEqual(StatusMjesta.VanFunkcije, kriterij.Status);
            Assert.AreEqual(tip, kriterij.TipMjesta);
        }

        [TestMethod]
        public void Svojstva_NovaVrijednostPrepisujeStaru_VracaNovuVrijednost()
        {
            var kriterij = new KriterijPretrage { Zona = "Stara" };

            kriterij.Zona = "Nova";

            Assert.AreEqual("Nova", kriterij.Zona);
        }
    }
}