using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using System.ComponentModel.DataAnnotations;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class RezervacijaICjenovnikModelTests
    {
        [TestMethod]
        public void Cjenovnik_CuvaPostavljeneVrijednosti()
        {
            var pocetak = new DateTime(2026, 1, 1);
            var kraj = new DateTime(2026, 12, 31);

            var cjenovnik = new Cjenovnik
            {
                Id = 7,
                CijenaPoSatu = 2.5,
                VaziOd = pocetak,
                VaziDo = kraj
            };

            Assert.AreEqual(7, cjenovnik.Id);
            Assert.AreEqual(2.5, cjenovnik.CijenaPoSatu);
            Assert.AreEqual(pocetak, cjenovnik.VaziOd);
            Assert.AreEqual(kraj, cjenovnik.VaziDo);
        }

        [TestMethod]
        public void Rezervacija_CuvaPodatkeIVeze()
        {
            var korisnik = new Korisnik { Id = "korisnik-1" };
            var mjesto = new ParkingMjesto { Id = 1 };
            var recenzija = new Recenzija();
            var rezervacija = RezervacijaTestPodaci.Kreiraj();

            rezervacija.StatusRezervacije = StatusRezervacije.Placena;
            rezervacija.Korisnik = korisnik;
            rezervacija.ParkingMjesto = mjesto;
            rezervacija.Recenzija = recenzija;

            Assert.AreEqual(1, rezervacija.Id);
            Assert.AreEqual(
                new DateTime(2026, 1, 1),
                rezervacija.DatumKreiranja);
            Assert.AreEqual(
                new DateTime(2026, 12, 1, 10, 0, 0),
                rezervacija.VrijemePocetka);
            Assert.AreEqual(
                new DateTime(2026, 12, 1, 12, 0, 0),
                rezervacija.VrijemeKraja);
            Assert.AreEqual(
                "A12-B-345",
                rezervacija.RegistracijskeTablice);
            Assert.AreEqual(
                "+38761123456",
                rezervacija.KontaktTelefon);
            Assert.AreEqual(
                "test@example.com",
                rezervacija.EmailZaObavijest);
            Assert.AreEqual(4.0, rezervacija.UkupnaCijena);
            Assert.AreEqual(
                StatusRezervacije.Placena,
                rezervacija.StatusRezervacije);
            Assert.AreEqual("korisnik-1", rezervacija.KorisnikId);
            Assert.AreEqual(1, rezervacija.ParkingMjestoId);
            Assert.AreSame(korisnik, rezervacija.Korisnik);
            Assert.AreSame(mjesto, rezervacija.ParkingMjesto);
            Assert.AreSame(recenzija, rezervacija.Recenzija);
        }

        [TestMethod]
        [DataRow("A12-B-345", "+38761123456", "test@example.com", true)]
        [DataRow("ABCDE", "123456", "test@example.com", true)]
        [DataRow("ABCDEFGHIJKL", "+123456789012345", "test@example.com", true)]
        [DataRow("", "123456", "test@example.com", false)]
        [DataRow("ABCD", "123456", "test@example.com", false)]
        [DataRow("ABCDEFGHIJKLM", "123456", "test@example.com", false)]
        [DataRow("ABCDE", "abc123", "test@example.com", false)]
        [DataRow("ABCDE", "12345", "test@example.com", false)]
        [DataRow("ABCDE", "123456", "neispravan-email", false)]
        [DataRow("ABCDE", "123456", "", false)]
        [TestCategory("DataDriven")]
        public void Rezervacija_ValidacijaKontaktPodataka(
            string tablice,
            string telefon,
            string email,
            bool ocekivanoValidno)
        {
            var rezervacija = RezervacijaTestPodaci.Kreiraj();
            rezervacija.RegistracijskeTablice = tablice;
            rezervacija.KontaktTelefon = telefon;
            rezervacija.EmailZaObavijest = email;

            var greske = new List<ValidationResult>();

            var validno = Validator.TryValidateObject(
                rezervacija,
                new ValidationContext(rezervacija),
                greske,
                validateAllProperties: true);

            Assert.AreEqual(
                ocekivanoValidno,
                validno,
                string.Join(" | ", greske.Select(g => g.ErrorMessage)));
        }
    }
}