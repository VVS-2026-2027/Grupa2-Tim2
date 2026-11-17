
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;
using PametniParkingSistem.Services;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class EmailObavijestServiceTests
    {
        private Mock<IEmailObavijestRepository> _repositoryMock = null!;
        private EmailObavijestService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _repositoryMock = new Mock<IEmailObavijestRepository>();
            _service = new EmailObavijestService(_repositoryMock.Object);
        }

        private EmailObavijest KreirajEmailObavijest()
        {
            return new EmailObavijest
            {
                Id = 1,
                Primalac = "test@test.com",
                Naslov = "Potvrda rezervacije",
                Sadrzaj = "Vaša rezervacija je uspješno kreirana.",
                DatumSlanja = new DateTime(2026, 11, 17),
                TipObavijesti = TipObavijesti.KreiranjeRezervacije,
                StatusEmaila = StatusEmaila.Poslan
            };
        }

        [TestMethod]
        public async Task GetAllAsync_VracaSveEmailObavijesti()
        {
            var obavijesti = new List<EmailObavijest>
            {
                KreirajEmailObavijest(),
                new EmailObavijest
                {
                    Id = 2,
                    Primalac = "korisnik2@test.com",
                    Naslov = "Podsjetnik",
                    Sadrzaj = "Podsjetnik za rezervaciju.",
                    DatumSlanja = new DateTime(2026, 11, 18),
                    TipObavijesti = TipObavijesti.Podsjetnik,
                    StatusEmaila = StatusEmaila.NaCekanju
                }
            };

            _repositoryMock
                .Setup(r => r.GetAllAsync())
                .ReturnsAsync(obavijesti);

            var rezultat = await _service.GetAllAsync();

            Assert.AreEqual(2, rezultat.Count);
            Assert.AreSame(obavijesti, rezultat);
        }

        [TestMethod]
        public async Task GetByIdAsync_VracaPostojecuEmailObavijest()
        {
            var obavijest = KreirajEmailObavijest();

            _repositoryMock
                .Setup(r => r.GetByIdAsync(1))
                .ReturnsAsync(obavijest);

            var rezultat = await _service.GetByIdAsync(1);

            Assert.AreSame(obavijest, rezultat);
        }

        [TestMethod]
        public async Task GetByIdAsync_VracaNullZaNepostojecuEmailObavijest()
        {
            _repositoryMock
                .Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((EmailObavijest?)null);

            var rezultat = await _service.GetByIdAsync(99);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task AddAsync_PozivaRepository()
        {
            var obavijest = KreirajEmailObavijest();

            await _service.AddAsync(obavijest);

            _repositoryMock.Verify(
                r => r.AddAsync(obavijest),
                Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_PozivaRepository()
        {
            var obavijest = KreirajEmailObavijest();

            await _service.UpdateAsync(obavijest);

            _repositoryMock.Verify(
                r => r.UpdateAsync(obavijest),
                Times.Once);
        }

        [TestMethod]
        public async Task DeleteAsync_PozivaRepository()
        {
            await _service.DeleteAsync(1);

            _repositoryMock.Verify(
                r => r.DeleteAsync(1),
                Times.Once);
        }
    }
}
