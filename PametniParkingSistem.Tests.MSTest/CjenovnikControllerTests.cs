using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class CjenovnikControllerTests
    {
        private Mock<ICjenovnikService> _servis = null!;
        private CjenovnikController _kontroler = null!;

        [TestInitialize]
        public void PripremiTest()
        {
            _servis = new Mock<ICjenovnikService>();
            _kontroler = new CjenovnikController(_servis.Object);
        }

        [TestCleanup]
        public void OcistiTest()
        {
            _kontroler.Dispose();
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Index_VracaViewSaCjenovnicima()
        {
            var cjenovnici = new List<Cjenovnik>
            {
                new Cjenovnik { Id = 1, CijenaPoSatu = 2 },
                new Cjenovnik { Id = 2, CijenaPoSatu = 3 }
            };
            _servis.Setup(s => s.GetAllAsync()).ReturnsAsync(cjenovnici);

            var rezultat = await _kontroler.Index();

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.AreSame(cjenovnici, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Details_NullId_VracaNotFound()
        {
            var rezultat = await _kontroler.Details(null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Details_NepostojeciId_VracaNotFound()
        {
            _servis.Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((Cjenovnik?)null);

            var rezultat = await _kontroler.Details(99);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Details_PostojeciId_VracaViewSaCjenovnikom()
        {
            var cjenovnik = new Cjenovnik { Id = 1 };
            _servis.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(cjenovnik);

            var rezultat = await _kontroler.Details(1);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.AreSame(cjenovnik, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        public void Create_Get_VracaView()
        {
            var rezultat = _kontroler.Create();

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task Create_ValidanModel_DodajeCjenovnik()
        {
            var cjenovnik = new Cjenovnik { Id = 1, CijenaPoSatu = 2 };

            var rezultat = await _kontroler.Create(cjenovnik);

            _servis.Verify(s => s.AddAsync(cjenovnik), Times.Once);
            Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task Create_NevalidanModel_VracaViewBezDodavanja()
        {
            var cjenovnik = new Cjenovnik();
            _kontroler.ModelState.AddModelError(
                "CijenaPoSatu", "Neispravna vrijednost.");

            var rezultat = await _kontroler.Create(cjenovnik);

            _servis.Verify(s => s.AddAsync(It.IsAny<Cjenovnik>()), Times.Never);
            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.AreSame(cjenovnik, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Get_NullId_VracaNotFound()
        {
            var rezultat = await _kontroler.Edit((int?)null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Get_NepostojeciId_VracaNotFound()
        {
            _servis.Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((Cjenovnik?)null);

            var rezultat = await _kontroler.Edit((int?)99);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Get_PostojeciId_VracaViewSaCjenovnikom()
        {
            var cjenovnik = new Cjenovnik { Id = 1 };
            _servis.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(cjenovnik);

            var rezultat = await _kontroler.Edit((int?)1);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.AreSame(cjenovnik, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task Edit_Post_RazlicitiId_VracaNotFoundBezAzuriranja()
        {
            var cjenovnik = new Cjenovnik { Id = 2 };

            var rezultat = await _kontroler.Edit(1, cjenovnik);

            _servis.Verify(s => s.UpdateAsync(It.IsAny<Cjenovnik>()), Times.Never);
            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task Edit_Post_ValidanModel_AzuriraCjenovnik()
        {
            var cjenovnik = new Cjenovnik { Id = 1, CijenaPoSatu = 4 };

            var rezultat = await _kontroler.Edit(1, cjenovnik);

            _servis.Verify(s => s.UpdateAsync(cjenovnik), Times.Once);
            Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task Edit_Post_NevalidanModel_VracaViewBezAzuriranja()
        {
            var cjenovnik = new Cjenovnik { Id = 1 };
            _kontroler.ModelState.AddModelError(
                "CijenaPoSatu", "Neispravna vrijednost.");

            var rezultat = await _kontroler.Edit(1, cjenovnik);

            _servis.Verify(s => s.UpdateAsync(It.IsAny<Cjenovnik>()), Times.Never);
            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.AreSame(cjenovnik, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_ConcurrencyIzuzetak_CjenovnikNePostoji_VracaNotFound()
        {
            var cjenovnik = new Cjenovnik { Id = 1 };
            _servis.Setup(s => s.UpdateAsync(cjenovnik))
                .ThrowsAsync(new DbUpdateConcurrencyException());
            _servis.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync((Cjenovnik?)null);

            var rezultat = await _kontroler.Edit(1, cjenovnik);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_ConcurrencyIzuzetak_CjenovnikPostoji_PropagiraIzuzetak()
        {
            var cjenovnik = new Cjenovnik { Id = 1 };
            var izuzetak = new DbUpdateConcurrencyException();
            _servis.Setup(s => s.UpdateAsync(cjenovnik)).ThrowsAsync(izuzetak);
            _servis.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(cjenovnik);

            var rezultat = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => _kontroler.Edit(1, cjenovnik));

            Assert.AreSame(izuzetak, rezultat);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Delete_NullId_VracaNotFound()
        {
            var rezultat = await _kontroler.Delete(null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Delete_NepostojeciId_VracaNotFound()
        {
            _servis.Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((Cjenovnik?)null);

            var rezultat = await _kontroler.Delete(99);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Delete_PostojeciId_VracaViewSaCjenovnikom()
        {
            var cjenovnik = new Cjenovnik { Id = 1 };
            _servis.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(cjenovnik);

            var rezultat = await _kontroler.Delete(1);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.AreSame(cjenovnik, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task DeleteConfirmed_BriseCjenovnikIPreusmjerava()
        {
            var rezultat = await _kontroler.DeleteConfirmed(1);

            _servis.Verify(s => s.DeleteAsync(1), Times.Once);
            Assert.IsInstanceOfType(rezultat, typeof(RedirectToActionResult));
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
        }
    }
}