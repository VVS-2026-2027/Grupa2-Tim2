
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Models;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class RecenzijaModelTests
    {
        [TestMethod]
        public void Korisnik_MozeSePostavitiIDohvatiti()
        {
            var recenzija = new Recenzija();
            var korisnik = new Korisnik();

            recenzija.Korisnik = korisnik;

            Assert.AreSame(korisnik, recenzija.Korisnik);
        }

        [TestMethod]
        public void Rezervacija_MozeSePostavitiIDohvatiti()
        {
            var recenzija = new Recenzija();
            var rezervacija = new Rezervacija();

            recenzija.Rezervacija = rezervacija;

            Assert.AreSame(rezervacija, recenzija.Rezervacija);
        }
    }
}
