using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class ParkingZonaControllerTests
    {
        private Mock<IParkingZonaService> _serviceMock = null!;
        private ParkingZonaController _kontroler = null!;

        [TestInitialize]
        public void Postavi()
        {
            _serviceMock = new Mock<IParkingZonaService>();
            _kontroler = new ParkingZonaController(_serviceMock.Object);
        }

        // ---------- Index ----------

        [TestMethod]
        public async Task Index_ServisVracaZone_VracaViewSaListomZona()
        {
            var lista = new List<ParkingZona> { new ParkingZona { Id = 1 }, new ParkingZona { Id = 2 } };
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(lista);

            var rezultat = await _kontroler.Index();

            var view = (ViewResult)rezultat;
            Assert.AreSame(lista, view.Model);
        }

        // ---------- Details ----------

        [TestMethod]
        public async Task Details_IdJeNull_VracaNotFound()
        {
            var rezultat = await _kontroler.Details(null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_NepostojeciId_VracaNotFound()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(404)).ReturnsAsync((ParkingZona?)null);

            var rezultat = await _kontroler.Details(404);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_PostojeciId_VracaViewSaZonom()
        {
            var zona = new ParkingZona { Id = 3 };
            _serviceMock.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(zona);

            var rezultat = await _kontroler.Details(3);

            Assert.AreSame(zona, ((ViewResult)rezultat).Model);
        }

        // ---------- Create ----------

        [TestMethod]
        public void Create_Get_VracaView()
        {
            var rezultat = _kontroler.Create();

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Create_Post_ValidanModel_DodajeZonuIPreusmjerava()
        {
            var zona = new ParkingZona { Id = 1, Naziv = "Centar" };

            var rezultat = await _kontroler.Create(zona);

            _serviceMock.Verify(s => s.AddAsync(zona), Times.Once);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        public async Task Create_Post_NevalidanModel_VracaViewBezDodavanja()
        {
            _kontroler.ModelState.AddModelError("Naziv", "Obavezno polje");
            var zona = new ParkingZona();

            var rezultat = await _kontroler.Create(zona);

            _serviceMock.Verify(s => s.AddAsync(It.IsAny<ParkingZona>()), Times.Never);
            Assert.AreSame(zona, ((ViewResult)rezultat).Model);
        }

        // ---------- Edit ----------

        [TestMethod]
        public async Task Edit_Get_IdJeNull_VracaNotFound()
        {
            var rezultat = await _kontroler.Edit((int?)null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Get_NepostojeciId_VracaNotFound()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(404)).ReturnsAsync((ParkingZona?)null);

            var rezultat = await _kontroler.Edit((int?)404);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Get_PostojeciId_VracaViewSaZonom()
        {
            var zona = new ParkingZona { Id = 2 };
            _serviceMock.Setup(s => s.GetByIdAsync(2)).ReturnsAsync(zona);

            var rezultat = await _kontroler.Edit((int?)2);

            Assert.AreSame(zona, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        public async Task Edit_Post_RazlicitIdIModelId_VracaNotFound()
        {
            var rezultat = await _kontroler.Edit(1, new ParkingZona { Id = 2 });

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
            _serviceMock.Verify(s => s.UpdateAsync(It.IsAny<ParkingZona>()), Times.Never);
        }

        [TestMethod]
        public async Task Edit_Post_ValidanModel_AzuriraZonuIPreusmjerava()
        {
            var zona = new ParkingZona { Id = 1 };

            var rezultat = await _kontroler.Edit(1, zona);

            _serviceMock.Verify(s => s.UpdateAsync(zona), Times.Once);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        public async Task Edit_Post_NevalidanModel_VracaViewBezAzuriranja()
        {
            _kontroler.ModelState.AddModelError("Naziv", "Obavezno polje");
            var zona = new ParkingZona { Id = 1 };

            var rezultat = await _kontroler.Edit(1, zona);

            _serviceMock.Verify(s => s.UpdateAsync(It.IsAny<ParkingZona>()), Times.Never);
            Assert.AreSame(zona, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        public async Task Edit_Post_ConcurrencyIzuzetakIZonaNePostoji_VracaNotFound()
        {
            _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<ParkingZona>()))
                        .ThrowsAsync(new DbUpdateConcurrencyException());
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((ParkingZona?)null);

            var rezultat = await _kontroler.Edit(1, new ParkingZona { Id = 1 });

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Post_ConcurrencyIzuzetakIZonaPostoji_PropagiraIzuzetak()
        {
            _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<ParkingZona>()))
                        .ThrowsAsync(new DbUpdateConcurrencyException());
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new ParkingZona { Id = 1 });

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => _kontroler.Edit(1, new ParkingZona { Id = 1 }));
        }

        // ---------- Delete ----------

        [TestMethod]
        public async Task Delete_Get_IdJeNull_VracaNotFound()
        {
            var rezultat = await _kontroler.Delete(null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Delete_Get_NepostojeciId_VracaNotFound()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(404)).ReturnsAsync((ParkingZona?)null);

            var rezultat = await _kontroler.Delete(404);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Delete_Get_PostojeciId_VracaViewSaZonom()
        {
            var zona = new ParkingZona { Id = 4 };
            _serviceMock.Setup(s => s.GetByIdAsync(4)).ReturnsAsync(zona);

            var rezultat = await _kontroler.Delete(4);

            Assert.AreSame(zona, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        public async Task DeleteConfirmed_ValidanId_BriseZonuIPreusmjerava()
        {
            var rezultat = await _kontroler.DeleteConfirmed(7);

            _serviceMock.Verify(s => s.DeleteAsync(7), Times.Once);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        }
    }
}