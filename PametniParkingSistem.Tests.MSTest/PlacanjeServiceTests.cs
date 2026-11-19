using Moq;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;
using PametniParkingSistem.Services;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    [TestCategory("Servis")]
    public class PlacanjeServiceTests
    {
        // Zamjenski objekat (Moq) umjesto prave baze podataka.
        private Mock<IPlacanjeRepository> _repozitorij = null!;
        private PlacanjeService _servis = null!;

        [TestInitialize]
        public void Pripremi()
        {
            _repozitorij = new Mock<IPlacanjeRepository>();
            _servis = new PlacanjeService(_repozitorij.Object);
        }

        [TestCleanup]
        public void Pocisti()
        {
            _repozitorij = null!;
            _servis = null!;
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task GetAllAsync_RepozitorijVracaListu_ServisVracaIstuListu()
        {
            var placanja = new List<Placanje> { new Placanje { Id = 1 }, new Placanje { Id = 2 } };
            _repozitorij.Setup(r => r.GetAllAsync()).ReturnsAsync(placanja);

            var rezultat = await _servis.GetAllAsync();

            Assert.AreSame(placanja, rezultat);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task GetByIdAsync_PostojeciId_VracaPlacanje()
        {
            var placanje = new Placanje { Id = 3 };
            _repozitorij.Setup(r => r.GetByIdAsync(3)).ReturnsAsync(placanje);

            var rezultat = await _servis.GetByIdAsync(3);

            Assert.AreSame(placanje, rezultat);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task GetByIdAsync_NepostojeciId_VracaNull()
        {
            _repozitorij.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Placanje?)null);

            var rezultat = await _servis.GetByIdAsync(99);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task AddAsync_Placanje_PrebacujeGaRepozitoriju()
        {
            var placanje = new Placanje { Id = 5, Iznos = 12.5 };

            await _servis.AddAsync(placanje);

            _repozitorij.Verify(r => r.AddAsync(placanje), Times.Once);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task UpdateAsync_Placanje_PrebacujeGaRepozitoriju()
        {
            var placanje = new Placanje { Id = 5, Iznos = 20 };

            await _servis.UpdateAsync(placanje);

            _repozitorij.Verify(r => r.UpdateAsync(placanje), Times.Once);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task DeleteAsync_Id_PrebacujeIdRepozitoriju()
        {
            await _servis.DeleteAsync(8);

            _repozitorij.Verify(r => r.DeleteAsync(8), Times.Once);
        }

        [TestMethod]
        [TestCategory("Moq")]
        [DataRow(true)]
        [DataRow(false)]
        public async Task PostojiPlacanjeZaRezervacijuAsync_VracaOdgovorRepozitorija(bool odgovorRepozitorija)
        {
            _repozitorij.Setup(r => r.PostojiPlacanjeZaRezervacijuAsync(4)).ReturnsAsync(odgovorRepozitorija);

            var rezultat = await _servis.PostojiPlacanjeZaRezervacijuAsync(4);

            Assert.AreEqual(odgovorRepozitorija, rezultat);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task GetUspjesnoPlacanjeZaRezervacijuAsync_PostojiPlacanje_VracaGa()
        {
            var placanje = new Placanje { Id = 2, StatusPlacanja = StatusPlacanja.Uspjesno, RezervacijaId = 4 };
            _repozitorij.Setup(r => r.GetUspjesnoPlacanjeZaRezervacijuAsync(4)).ReturnsAsync(placanje);

            var rezultat = await _servis.GetUspjesnoPlacanjeZaRezervacijuAsync(4);

            Assert.AreSame(placanje, rezultat);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task GetUspjesnoPlacanjeZaRezervacijuAsync_NemaPlacanja_VracaNull()
        {
            _repozitorij.Setup(r => r.GetUspjesnoPlacanjeZaRezervacijuAsync(It.IsAny<int>()))
                .ReturnsAsync((Placanje?)null);

            var rezultat = await _servis.GetUspjesnoPlacanjeZaRezervacijuAsync(4);

            Assert.IsNull(rezultat);
        }

        // ---------- IZUZECI ----------

        [TestMethod]
        [TestCategory("Izuzetak")]
        public async Task AddAsync_RepozitorijBacaIzuzetak_ServisGaNeGuta()
        {
            _repozitorij.Setup(r => r.AddAsync(It.IsAny<Placanje>()))
                .ThrowsAsync(new InvalidOperationException("Greška baze"));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _servis.AddAsync(new Placanje()));
        }

        [TestMethod]
        [TestCategory("Izuzetak")]
        public async Task GetAllAsync_RepozitorijBacaIzuzetak_ServisGaProsljedjuje()
        {
            _repozitorij.Setup(r => r.GetAllAsync()).ThrowsAsync(new TimeoutException());

            await Assert.ThrowsExactlyAsync<TimeoutException>(() => _servis.GetAllAsync());
        }
    }
}
