using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;
using PametniParkingSistem.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class ParkingZonaServiceTests
    {
        private Mock<IParkingZonaRepository> _repoMock = null!;
        private ParkingZonaService _service = null!;

        [TestInitialize]
        public void Postavi()
        {
            _repoMock = new Mock<IParkingZonaRepository>();
            _service = new ParkingZonaService(_repoMock.Object);
        }

        [TestMethod]
        public async Task GetAllAsync_RepozitorijVracaListu_VracaIstuListu()
        {
            var lista = new List<ParkingZona> { new ParkingZona { Id = 1 }, new ParkingZona { Id = 2 } };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(lista);

            var rezultat = await _service.GetAllAsync();

            Assert.AreSame(lista, rezultat);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(25)]
        [DataRow(int.MaxValue)]
        public async Task GetByIdAsync_PostojeciId_VracaZonuIzRepozitorija(int id)
        {
            var zona = new ParkingZona { Id = id };
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(zona);

            var rezultat = await _service.GetByIdAsync(id);

            Assert.AreSame(zona, rezultat);
        }

        [TestMethod]
        public async Task GetByIdAsync_NepostojeciId_VracaNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ParkingZona?)null);

            var rezultat = await _service.GetByIdAsync(404);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task AddAsync_ValidnaZona_PozivaRepozitorijJednom()
        {
            var zona = new ParkingZona { Id = 1 };

            await _service.AddAsync(zona);

            _repoMock.Verify(r => r.AddAsync(zona), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_ValidnaZona_PozivaRepozitorijJednom()
        {
            var zona = new ParkingZona { Id = 1 };

            await _service.UpdateAsync(zona);

            _repoMock.Verify(r => r.UpdateAsync(zona), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAsync_ValidanId_PozivaRepozitorijJednom()
        {
            await _service.DeleteAsync(7);

            _repoMock.Verify(r => r.DeleteAsync(7), Times.Once);
        }

        [TestMethod]
        public async Task GetAllAsync_RepozitorijBacaIzuzetak_IzuzetakSePropagira()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new DbUpdateException());

            await Assert.ThrowsAsync<DbUpdateException>(() => _service.GetAllAsync());
        }

        [TestMethod]
        public async Task AddAsync_RepozitorijBacaIzuzetak_IzuzetakSePropagira()
        {
            _repoMock.Setup(r => r.AddAsync(It.IsAny<ParkingZona>()))
                     .ThrowsAsync(new InvalidOperationException());

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.AddAsync(new ParkingZona()));
        }

        [TestMethod]
        public async Task UpdateAsync_RepozitorijBacaIzuzetak_IzuzetakSePropagira()
        {
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ParkingZona>()))
                     .ThrowsAsync(new DbUpdateConcurrencyException());

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => _service.UpdateAsync(new ParkingZona()));
        }

        [TestMethod]
        public async Task DeleteAsync_RepozitorijBacaIzuzetak_IzuzetakSePropagira()
        {
            _repoMock.Setup(r => r.DeleteAsync(It.IsAny<int>()))
                     .ThrowsAsync(new InvalidOperationException());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1));
        }
    }
}