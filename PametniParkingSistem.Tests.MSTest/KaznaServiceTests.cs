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

        [TestMethod]
        [DataRow(11, 2.0)]
        [DataRow(60, 2.0)]
        [DataRow(61, 4.0)]
        [DataRow(120, 4.0)]
        [DataRow(121, 6.0)]
        public void IzracunajKaznu_PrekoracenjeVanTolerancije_NaplacujeSvakiZapocetiSat(
    int minutaPrekoracenja, double ocekivanaKazna)
        {
            // Arrange
            var service = new KaznaService();

            var rezervacija = new Rezervacija
            {
                VrijemePocetka = new DateTime(2026, 11, 20, 10, 0, 0),
                VrijemeKraja = new DateTime(2026, 11, 20, 12, 0, 0),
                StatusRezervacije = StatusRezervacije.Aktivna
            };

            var stvarnoVrijemeIzlaska = rezervacija.VrijemeKraja.AddMinutes(minutaPrekoracenja);

            // Act
            var rezultat = service.IzracunajKaznu(rezervacija, stvarnoVrijemeIzlaska);

            // Assert
            Assert.AreEqual(ocekivanaKazna, rezultat);
        }

        [TestMethod]
        [DataRow(26 * 60)]
        [DataRow(48 * 60)]
        [DataRow(100 * 60)]
        public void IzracunajKaznu_PrekoracenjeIznadGranice_KaznaJeOgranicenaNa50(int minutaPrekoracenja)
        {
            // Arrange
            var service = new KaznaService();
            var rezervacija = new Rezervacija
            {
                VrijemePocetka = new DateTime(2026, 11, 20, 10, 0, 0),
                VrijemeKraja = new DateTime(2026, 11, 20, 12, 0, 0),
                StatusRezervacije = StatusRezervacije.Aktivna
            };
            var stvarnoVrijemeIzlaska = rezervacija.VrijemeKraja.AddMinutes(minutaPrekoracenja);

            // Act
            var rezultat = service.IzracunajKaznu(rezervacija, stvarnoVrijemeIzlaska);

            // Assert
            Assert.AreEqual(50.0, rezultat);
        }

        [TestMethod]
        [DataRow(StatusRezervacije.Otkazana)]
        [DataRow(StatusRezervacije.Kreirana)]
        public void IzracunajKaznu_OtkazanaIliNezapocetaRezervacija_VracaNula(
            StatusRezervacije status)
        {
            // Arrange
            var service = new KaznaService();

            var rezervacija = new Rezervacija
            {
                VrijemePocetka = new DateTime(2026, 11, 20, 10, 0, 0),
                VrijemeKraja = new DateTime(2026, 11, 20, 12, 0, 0),
                StatusRezervacije = status
            };

            var stvarnoVrijemeIzlaska =
                new DateTime(2026, 11, 20, 15, 0, 0);

            // Act
            var rezultat = service.IzracunajKaznu(
                rezervacija,
                stvarnoVrijemeIzlaska);

            // Assert
            Assert.AreEqual(0.0, rezultat);
        }

        [TestMethod]
        [DataRow(-1)]
        [DataRow(-60)]
        public void IzracunajKaznu_KrajPrijePocetka_BacaArgumentException(
    int pomakKrajaUMinutama)
        {
            // Arrange
            var service = new KaznaService();
            var pocetak = new DateTime(2026, 11, 20, 10, 0, 0);

            var rezervacija = new Rezervacija
            {
                VrijemePocetka = pocetak,
                VrijemeKraja = pocetak.AddMinutes(pomakKrajaUMinutama),
                StatusRezervacije = StatusRezervacije.Aktivna
            };

            var stvarnoVrijemeIzlaska = pocetak.AddHours(1);

            // Act i Assert
            Assert.Throws<ArgumentException>(() =>
                service.IzracunajKaznu(
                    rezervacija,
                    stvarnoVrijemeIzlaska));
        }

    }
}