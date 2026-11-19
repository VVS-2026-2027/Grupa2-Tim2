using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;
using PametniParkingSistem.Services;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class RezervacijaServiceTests
    {
        private Mock<IRezervacijaRepository> _repozitorij = null!;
        private RezervacijaService _servis = null!;

        [TestInitialize]
        public void PripremiTest()
        {
            _repozitorij = new Mock<IRezervacijaRepository>();
            _servis = new RezervacijaService(_repozitorij.Object);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task GetAllAsync_VracaRezervacijeIzRepozitorija()
        {
            var rezervacije = new List<Rezervacija>
            {
                new Rezervacija { Id = 1 },
                new Rezervacija { Id = 2 }
            };
            _repozitorij.Setup(r => r.GetAllAsync())
                .ReturnsAsync(rezervacije);

            var rezultat = await _servis.GetAllAsync();

            Assert.AreSame(rezervacije, rezultat);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task GetByIdAsync_PostojeciId_VracaRezervaciju()
        {
            var rezervacija = new Rezervacija { Id = 5 };
            _repozitorij.Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(rezervacija);

            var rezultat = await _servis.GetByIdAsync(5);

            Assert.AreSame(rezervacija, rezultat);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task GetByIdAsync_NepostojeciId_VracaNull()
        {
            _repozitorij.Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((Rezervacija?)null);

            var rezultat = await _servis.GetByIdAsync(99);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task AddAsync_ProsljedjujeRezervacijuRepozitoriju()
        {
            var rezervacija = new Rezervacija { Id = 1 };

            await _servis.AddAsync(rezervacija);

            _repozitorij.Verify(
                r => r.AddAsync(rezervacija), Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task UpdateAsync_ProsljedjujeRezervacijuRepozitoriju()
        {
            var rezervacija = new Rezervacija { Id = 1 };

            await _servis.UpdateAsync(rezervacija);

            _repozitorij.Verify(
                r => r.UpdateAsync(rezervacija), Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task DeleteAsync_ProsljedjujeIdRepozitoriju()
        {
            await _servis.DeleteAsync(7);

            _repozitorij.Verify(
                r => r.DeleteAsync(7), Times.Once);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task ProvjeriDostupnostAsync_NemaPreklapanja_VracaTrue()
        {
            var pocetak = new DateTime(2026, 12, 1, 10, 0, 0);
            var kraj = pocetak.AddHours(2);

            _repozitorij.Setup(r =>
                r.PostojiPreklapanjeTerminaAsync(3, pocetak, kraj))
                .ReturnsAsync(false);

            var rezultat = await _servis.ProvjeriDostupnostAsync(
                3, pocetak, kraj);

            Assert.IsTrue(rezultat);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task ProvjeriDostupnostAsync_PostojiPreklapanje_VracaFalse()
        {
            var pocetak = new DateTime(2026, 12, 1, 10, 0, 0);
            var kraj = pocetak.AddHours(2);

            _repozitorij.Setup(r =>
                r.PostojiPreklapanjeTerminaAsync(3, pocetak, kraj))
                .ReturnsAsync(true);

            var rezultat = await _servis.ProvjeriDostupnostAsync(
                3, pocetak, kraj);

            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        [DataRow(0, 2.0, 0.0)]
        [DataRow(-60, 2.0, 0.0)]
        [DataRow(1, 2.0, 2.0)]
        [DataRow(60, 2.0, 2.0)]
        [DataRow(61, 2.0, 4.0)]
        [DataRow(120, 2.5, 5.0)]
        [DataRow(90, 0.0, 0.0)]
        [TestCategory("DataDriven")]
        public void IzracunajCijenu_RazlicitaTrajanja_VracaOcekivanuCijenu(
            int trajanjeUMinutama,
            double cijenaPoSatu,
            double ocekivanaCijena)
        {
            var pocetak = new DateTime(2026, 12, 1, 10, 0, 0);
            var kraj = pocetak.AddMinutes(trajanjeUMinutama);

            var rezultat = _servis.IzracunajCijenu(
                pocetak, kraj, cijenaPoSatu);

            Assert.AreEqual(ocekivanaCijena, rezultat, 0.000001);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task GetByKorisnikIdAsync_VracaRezervacijeKorisnika()
        {
            var rezervacije = new List<Rezervacija>
            {
                new Rezervacija { Id = 1, KorisnikId = "korisnik-1" },
                new Rezervacija { Id = 2, KorisnikId = "korisnik-1" }
            };
            _repozitorij.Setup(r => r.GetByKorisnikIdAsync("korisnik-1"))
                .ReturnsAsync(rezervacije);

            var rezultat = await _servis.GetByKorisnikIdAsync("korisnik-1");

            Assert.AreSame(rezervacije, rezultat);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task GetIstekleAktivneRezervacijeAsync_VracaRezultatRepozitorija()
        {
            var rezervacije = new List<Rezervacija>
            {
                new Rezervacija { Id = 1 }
            };
            _repozitorij.Setup(r => r.GetIstekleAktivneRezervacijeAsync())
                .ReturnsAsync(rezervacije);

            var rezultat = await _servis.GetIstekleAktivneRezervacijeAsync();

            Assert.AreSame(rezervacije, rezultat);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task UpdateAsync_GreskaRepozitorija_PropagiraIzuzetak()
        {
            var rezervacija = new Rezervacija { Id = 1 };
            var izuzetak = new DbUpdateConcurrencyException();

            _repozitorij.Setup(r => r.UpdateAsync(rezervacija))
                .ThrowsAsync(izuzetak);

            var rezultat = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => _servis.UpdateAsync(rezervacija));

            Assert.AreSame(izuzetak, rezultat);
        }
    }
}