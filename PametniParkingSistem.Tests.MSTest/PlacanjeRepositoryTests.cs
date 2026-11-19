using PametniParkingSistem.Data;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;

namespace PametniParkingSistem.Tests.MSTest
{
    /// <summary>
    /// Repozitorij se testira nad zamjenskom (in-memory) bazom, prava baza nije aktivna.
    /// </summary>
    [TestClass]
    [TestCategory("Repozitorij")]
    public class PlacanjeRepositoryTests
    {
        private ApplicationDbContext _kontekst = null!;
        private PlacanjeRepository _repozitorij = null!;

        [TestInitialize]
        public void Pripremi()
        {
            _kontekst = InMemoryBaza.Kreiraj();
            _repozitorij = new PlacanjeRepository(_kontekst);
        }

        [TestCleanup]
        public void Pocisti()
        {
            _kontekst.Dispose();
        }

        private void DodajRezervaciju(int id)
        {
            _kontekst.Rezervacije.Add(new Rezervacija { Id = id, KorisnikId = "k1" });
            _kontekst.SaveChanges();
        }

        private Placanje DodajPlacanje(int id, int rezervacijaId, StatusPlacanja status)
        {
            var placanje = new Placanje { Id = id, RezervacijaId = rezervacijaId, StatusPlacanja = status, Iznos = 10 };
            _kontekst.Placanja.Add(placanje);
            _kontekst.SaveChanges();
            return placanje;
        }

        [TestMethod]
        public async Task GetAllAsync_PraznaBaza_VracaPraznuListu()
        {
            var rezultat = await _repozitorij.GetAllAsync();

            Assert.AreEqual(0, rezultat.Count);
        }

        [TestMethod]
        public async Task GetAllAsync_ViseZapisa_VracaSvaPlacanja()
        {
            DodajRezervaciju(1);
            DodajPlacanje(1, 1, StatusPlacanja.Uspjesno);
            DodajPlacanje(2, 1, StatusPlacanja.Neuspjesno);

            var rezultat = await _repozitorij.GetAllAsync();

            Assert.AreEqual(2, rezultat.Count);
        }

        [TestMethod]
        public async Task GetAllAsync_UkljucujeRezervaciju()
        {
            DodajRezervaciju(1);
            DodajPlacanje(1, 1, StatusPlacanja.Uspjesno);

            var rezultat = await _repozitorij.GetAllAsync();

            Assert.IsNotNull(rezultat.Single().Rezervacija);
        }

        [TestMethod]
        public async Task GetByIdAsync_PostojeciId_VracaPlacanjeSaRezervacijom()
        {
            DodajRezervaciju(1);
            DodajPlacanje(5, 1, StatusPlacanja.Uspjesno);

            var rezultat = await _repozitorij.GetByIdAsync(5);

            Assert.AreEqual(5, rezultat!.Id);
            Assert.IsNotNull(rezultat.Rezervacija);
        }

        [TestMethod]
        public async Task GetByIdAsync_NepostojeciId_VracaNull()
        {
            var rezultat = await _repozitorij.GetByIdAsync(404);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task AddAsync_NovoPlacanje_SnimaSeUBazu()
        {
            await _repozitorij.AddAsync(new Placanje { Id = 1, Iznos = 25, RezervacijaId = 1 });

            Assert.AreEqual(1, _kontekst.Placanja.Count());
        }

        [TestMethod]
        public async Task UpdateAsync_IzmijenjenIznos_SnimaSeUBazu()
        {
            var placanje = DodajPlacanje(1, 1, StatusPlacanja.NaCekanju);

            placanje.Iznos = 99;
            await _repozitorij.UpdateAsync(placanje);

            Assert.AreEqual(99, _kontekst.Placanja.Single().Iznos);
        }

        [TestMethod]
        public async Task DeleteAsync_PostojeciId_BrisePlacanje()
        {
            DodajPlacanje(1, 1, StatusPlacanja.Uspjesno);

            await _repozitorij.DeleteAsync(1);

            Assert.AreEqual(0, _kontekst.Placanja.Count());
        }

        [TestMethod]
        public async Task DeleteAsync_NepostojeciId_NeBrisePostojeca()
        {
            DodajPlacanje(1, 1, StatusPlacanja.Uspjesno);

            await _repozitorij.DeleteAsync(404);

            Assert.AreEqual(1, _kontekst.Placanja.Count());
        }

        [TestMethod]
        [DataRow(StatusPlacanja.Uspjesno, true)]
        [DataRow(StatusPlacanja.Neuspjesno, false)]
        [DataRow(StatusPlacanja.NaCekanju, false)]
        [DataRow(StatusPlacanja.Refundirano, false)]
        public async Task PostojiPlacanjeZaRezervacijuAsync_ZavisiOdStatusa(StatusPlacanja status, bool ocekivano)
        {
            DodajPlacanje(1, 3, status);

            var rezultat = await _repozitorij.PostojiPlacanjeZaRezervacijuAsync(3);

            Assert.AreEqual(ocekivano, rezultat);
        }

        [TestMethod]
        public async Task PostojiPlacanjeZaRezervacijuAsync_DrugaRezervacija_VracaFalse()
        {
            DodajPlacanje(1, 3, StatusPlacanja.Uspjesno);

            var rezultat = await _repozitorij.PostojiPlacanjeZaRezervacijuAsync(4);

            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        public async Task GetUspjesnoPlacanjeZaRezervacijuAsync_ImaUspjesnoPlacanje_VracaGa()
        {
            DodajPlacanje(1, 3, StatusPlacanja.Neuspjesno);
            DodajPlacanje(2, 3, StatusPlacanja.Uspjesno);

            var rezultat = await _repozitorij.GetUspjesnoPlacanjeZaRezervacijuAsync(3);

            Assert.AreEqual(2, rezultat!.Id);
        }

        [TestMethod]
        public async Task GetUspjesnoPlacanjeZaRezervacijuAsync_SamoNeuspjesno_VracaNull()
        {
            DodajPlacanje(1, 3, StatusPlacanja.Neuspjesno);

            var rezultat = await _repozitorij.GetUspjesnoPlacanjeZaRezervacijuAsync(3);

            Assert.IsNull(rezultat);
        }
    }
}
