
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;
using PametniParkingSistem.Services;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class RecenzijaServiceTests
    {
        private Mock<IRecenzijaRepository> _repositoryMock = null!;
        private RecenzijaService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IRecenzijaRepository>();
            _service = new RecenzijaService(_repositoryMock.Object);
        }

        private Recenzija KreirajRecenziju()
        {
            return new Recenzija
            {
                Id = 1,
                Ocjena = 5,
                Komentar = "Odlična usluga",
                Datum = new DateTime(2026, 11, 17),
                Obrisan = false,
                KorisnikId = "korisnik1",
                RezervacijaId = 10
            };
        }

        [TestMethod]
        public async Task GetAllAsync_VracaSveRecenzije()
        {
            var recenzije = new List<Recenzija> { KreirajRecenziju() };
            _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(recenzije);

            var rezultat = await _service.GetAllAsync();

            Assert.AreEqual(1, rezultat.Count());
            Assert.AreSame(recenzije, rezultat);
        }

        [TestMethod]
        public async Task GetByIdAsync_VracaPostojecuRecenziju()
        {
            var recenzija = KreirajRecenziju();
            _repositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(recenzija);

            var rezultat = await _service.GetByIdAsync(1);

            Assert.AreSame(recenzija, rezultat);
        }

        [TestMethod]
        public async Task GetByIdAsync_VracaNullZaNepostojecuRecenziju()
        {
            _repositoryMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Recenzija?)null);

            var rezultat = await _service.GetByIdAsync(99);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task GetByRezervacijaIdAsync_VracaRecenziju()
        {
            var recenzija = KreirajRecenziju();
            _repositoryMock.Setup(r => r.GetByRezervacijaIdAsync(10)).ReturnsAsync(recenzija);

            var rezultat = await _service.GetByRezervacijaIdAsync(10);

            Assert.AreSame(recenzija, rezultat);
        }

        [TestMethod]
        public async Task GetByRezervacijaIdAsync_VracaNullAkoNePostoji()
        {
            _repositoryMock.Setup(r => r.GetByRezervacijaIdAsync(99)).ReturnsAsync((Recenzija?)null);

            var rezultat = await _service.GetByRezervacijaIdAsync(99);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task ExistsForRezervacijaAsync_VracaTrueAkoPostoji()
        {
            _repositoryMock.Setup(r => r.ExistsForRezervacijaAsync(10)).ReturnsAsync(true);

            var rezultat = await _service.ExistsForRezervacijaAsync(10);

            Assert.IsTrue(rezultat);
        }

        [TestMethod]
        public async Task ExistsForRezervacijaAsync_VracaFalseAkoNePostoji()
        {
            _repositoryMock.Setup(r => r.ExistsForRezervacijaAsync(99)).ReturnsAsync(false);

            var rezultat = await _service.ExistsForRezervacijaAsync(99);

            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        public async Task AddAsync_PozivaRepository()
        {
            var recenzija = KreirajRecenziju();

            await _service.AddAsync(recenzija);

            _repositoryMock.Verify(r => r.AddAsync(recenzija), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_PozivaRepository()
        {
            var recenzija = KreirajRecenziju();

            await _service.UpdateAsync(recenzija);

            _repositoryMock.Verify(r => r.UpdateAsync(recenzija), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAsync_PozivaRepository()
        {
            await _service.DeleteAsync(1);

            _repositoryMock.Verify(r => r.DeleteAsync(1), Times.Once);
        }

    }
}
