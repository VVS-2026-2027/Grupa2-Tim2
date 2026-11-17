
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;

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
    }
}
