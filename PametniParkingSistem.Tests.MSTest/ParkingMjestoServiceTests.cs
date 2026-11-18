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
    public class ParkingMjestoServiceTests
    {
        private Mock<IParkingMjestoRepository> _repoMock = null!;
        private ParkingMjestoService _service = null!;

        [TestInitialize]
        public void Postavi()
        {
            _repoMock = new Mock<IParkingMjestoRepository>();
            _service = new ParkingMjestoService(_repoMock.Object);
        }

        [TestMethod]
        public async Task GetAllAsync_RepozitorijVracaListu_VracaIstuListu()
        {
            var lista = new List<ParkingMjesto> { new ParkingMjesto { Id = 1 }, new ParkingMjesto { Id = 2 } };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(lista);

            var rezultat = await _service.GetAllAsync();

            Assert.AreSame(lista, rezultat);
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
        }

        [TestMethod]
        [DataRow(1)]
        [DataRow(25)]
        [DataRow(int.MaxValue)]
        public async Task GetByIdAsync_PostojeciId_VracaMjestoIzRepozitorija(int id)
        {
            var mjesto = new ParkingMjesto { Id = id };
            _repoMock.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(mjesto);

            var rezultat = await _service.GetByIdAsync(id);

            Assert.AreSame(mjesto, rezultat);
        }

        [TestMethod]
        public async Task GetByIdAsync_NepostojeciId_VracaNull()
        {
            _repoMock.Setup(r => r.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((ParkingMjesto?)null);

            var rezultat = await _service.GetByIdAsync(404);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task AddAsync_ValidnoMjesto_PozivaRepozitorijJednom()
        {
            var mjesto = new ParkingMjesto { Id = 1 };

            await _service.AddAsync(mjesto);

            _repoMock.Verify(r => r.AddAsync(mjesto), Times.Once);
        }

        [TestMethod]
        public async Task UpdateAsync_ValidnoMjesto_PozivaRepozitorijJednom()
        {
            var mjesto = new ParkingMjesto { Id = 1 };

            await _service.UpdateAsync(mjesto);

            _repoMock.Verify(r => r.UpdateAsync(mjesto), Times.Once);
        }

        [TestMethod]
        public async Task DeleteAsync_ValidanId_PozivaRepozitorijJednom()
        {
            await _service.DeleteAsync(7);

            _repoMock.Verify(r => r.DeleteAsync(7), Times.Once);
        }

        [TestMethod]
        public async Task AddAsync_RepozitorijBacaIzuzetak_IzuzetakSePropagira()
        {
            _repoMock.Setup(r => r.AddAsync(It.IsAny<ParkingMjesto>()))
                     .ThrowsAsync(new InvalidOperationException());

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _service.AddAsync(new ParkingMjesto()));
        }

        [TestMethod]
        public async Task UpdateAsync_RepozitorijBacaIzuzetak_IzuzetakSePropagira()
        {
            _repoMock.Setup(r => r.UpdateAsync(It.IsAny<ParkingMjesto>()))
                     .ThrowsAsync(new DbUpdateConcurrencyException());

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => _service.UpdateAsync(new ParkingMjesto()));
        }

        [TestMethod]
        public async Task DeleteAsync_RepozitorijBacaIzuzetak_IzuzetakSePropagira()
        {
            _repoMock.Setup(r => r.DeleteAsync(It.IsAny<int>()))
                     .ThrowsAsync(new InvalidOperationException());

            await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteAsync(1));
        }

        [TestMethod]
        public async Task GetAllAsync_RepozitorijBacaIzuzetak_IzuzetakSePropagira()
        {
            _repoMock.Setup(r => r.GetAllAsync()).ThrowsAsync(new DbUpdateException());

            await Assert.ThrowsAsync<DbUpdateException>(() => _service.GetAllAsync());
        }
    }
    }
