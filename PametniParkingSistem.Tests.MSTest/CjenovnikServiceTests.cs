using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;
using PametniParkingSistem.Services;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class CjenovnikServiceTests
    {
        private Mock<ICjenovnikRepository> _repozitorij = null!;
        private CjenovnikService _servis = null!;

        [TestInitialize]
        public void PripremiTest()
        {
            _repozitorij = new Mock<ICjenovnikRepository>();
            _servis = new CjenovnikService(_repozitorij.Object);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task GetAllAsync_VracaCjenovnikeIzRepozitorija()
        {
            var cjenovnici = new List<Cjenovnik>
            {
                new Cjenovnik { Id = 1, CijenaPoSatu = 2 },
                new Cjenovnik { Id = 2, CijenaPoSatu = 3 }
            };
            _repozitorij.Setup(r => r.GetAllAsync())
                .ReturnsAsync(cjenovnici);

            var rezultat = await _servis.GetAllAsync();

            Assert.AreSame(cjenovnici, rezultat);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task GetAllAsync_NemaCjenovnika_VracaPraznuListu()
        {
            _repozitorij.Setup(r => r.GetAllAsync())
                .ReturnsAsync(new List<Cjenovnik>());

            var rezultat = await _servis.GetAllAsync();

            Assert.AreEqual(0, rezultat.Count);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task GetByIdAsync_PostojeciId_VracaCjenovnik()
        {
            var cjenovnik = new Cjenovnik { Id = 5, CijenaPoSatu = 2 };
            _repozitorij.Setup(r => r.GetByIdAsync(5))
                .ReturnsAsync(cjenovnik);

            var rezultat = await _servis.GetByIdAsync(5);

            Assert.AreSame(cjenovnik, rezultat);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task GetByIdAsync_NepostojeciId_VracaNull()
        {
            _repozitorij.Setup(r => r.GetByIdAsync(99))
                .ReturnsAsync((Cjenovnik?)null);

            var rezultat = await _servis.GetByIdAsync(99);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task AddAsync_ProsljedjujeCjenovnikRepozitoriju()
        {
            var cjenovnik = new Cjenovnik { Id = 1, CijenaPoSatu = 2 };

            await _servis.AddAsync(cjenovnik);

            _repozitorij.Verify(
                r => r.AddAsync(cjenovnik), Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task UpdateAsync_ProsljedjujeCjenovnikRepozitoriju()
        {
            var cjenovnik = new Cjenovnik { Id = 1, CijenaPoSatu = 4 };

            await _servis.UpdateAsync(cjenovnik);

            _repozitorij.Verify(
                r => r.UpdateAsync(cjenovnik), Times.Once);
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
        public async Task UpdateAsync_GreskaRepozitorija_PropagiraIzuzetak()
        {
            var cjenovnik = new Cjenovnik { Id = 1 };
            var izuzetak = new DbUpdateConcurrencyException();

            _repozitorij.Setup(r => r.UpdateAsync(cjenovnik))
                .ThrowsAsync(izuzetak);

            var rezultat = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => _servis.UpdateAsync(cjenovnik));

            Assert.AreSame(izuzetak, rezultat);
        }
    }
}