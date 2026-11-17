
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class EmailObavijestControllerTests
    {
        private Mock<IEmailObavijestService> _serviceMock = null!;
        private EmailObavijestController _controller = null!;

        [TestInitialize]
        public void Setup()
        {
            _serviceMock = new Mock<IEmailObavijestService>();
            _controller = new EmailObavijestController(_serviceMock.Object);
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
        public async Task Index_VracaSveEmailObavijesti()
        {
            var obavijesti = new List<EmailObavijest>
            {
                KreirajEmailObavijest()
            };

            _serviceMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync(obavijesti);

            var rezultat = await _controller.Index();

            var viewResult = rezultat as ViewResult;

            Assert.IsNotNull(viewResult);
            Assert.AreSame(obavijesti, viewResult.Model);
        }

        [TestMethod]
        public async Task Details_VracaNotFoundAkoJeIdNull()
        {
            var rezultat = await _controller.Details(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_VracaNotFoundAkoObavijestNePostoji()
        {
            _serviceMock
                .Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((EmailObavijest?)null);

            var rezultat = await _controller.Details(99);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Details_VracaPostojecuEmailObavijest()
        {
            var obavijest = KreirajEmailObavijest();

            _serviceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(obavijest);

            var rezultat = await _controller.Details(1);

            var viewResult = rezultat as ViewResult;

            Assert.IsNotNull(viewResult);
            Assert.AreSame(obavijest, viewResult.Model);
        }

        [TestMethod]
        public void Create_VracaPraznuFormu()
        {
            var rezultat = _controller.Create();

            Assert.IsInstanceOfType<ViewResult>(rezultat);
        }

        [TestMethod]
        public async Task CreatePost_DodajeEmailObavijestAkoJeModelValidan()
        {
            var obavijest = KreirajEmailObavijest();

            var rezultat = await _controller.Create(obavijest);

            _serviceMock.Verify(
                s => s.AddAsync(obavijest),
                Times.Once);

            var redirectResult = rezultat as RedirectToActionResult;

            Assert.IsNotNull(redirectResult);
            Assert.AreEqual(nameof(_controller.Index), redirectResult.ActionName);
        }

        [TestMethod]
        public async Task CreatePost_VracaViewAkoModelNijeValidan()
        {
            var obavijest = KreirajEmailObavijest();

            _controller.ModelState.AddModelError(
                "Primalac",
                "Neispravan primalac");

            var rezultat = await _controller.Create(obavijest);

            Assert.IsInstanceOfType<ViewResult>(rezultat);

            _serviceMock.Verify(
                s => s.AddAsync(It.IsAny<EmailObavijest>()),
                Times.Never);
        }

        [TestMethod]
        public async Task EditGet_VracaNotFoundAkoJeIdNull()
        {
            var rezultat = await _controller.Edit(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task EditGet_VracaNotFoundAkoObavijestNePostoji()
        {
            _serviceMock
                .Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((EmailObavijest?)null);

            var rezultat = await _controller.Edit(99);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task EditGet_VracaPostojecuEmailObavijest()
        {
            var obavijest = KreirajEmailObavijest();

            _serviceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(obavijest);

            var rezultat = await _controller.Edit(1);

            var viewResult = rezultat as ViewResult;

            Assert.IsNotNull(viewResult);
            Assert.AreSame(obavijest, viewResult.Model);
        }

        [TestMethod]
        public async Task EditPost_VracaNotFoundAkoSeIdNePodudara()
        {
            var obavijest = KreirajEmailObavijest();

            var rezultat = await _controller.Edit(2, obavijest);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);

            _serviceMock.Verify(
                s => s.UpdateAsync(It.IsAny<EmailObavijest>()),
                Times.Never);
        }

        [TestMethod]
        public async Task EditPost_AzuriraEmailObavijestAkoJeModelValidan()
        {
            var obavijest = KreirajEmailObavijest();

            var rezultat = await _controller.Edit(1, obavijest);

            _serviceMock.Verify(
                s => s.UpdateAsync(obavijest),
                Times.Once);

            var redirectResult = rezultat as RedirectToActionResult;

            Assert.IsNotNull(redirectResult);
            Assert.AreEqual(nameof(_controller.Index), redirectResult.ActionName);
        }

        [TestMethod]
        public async Task EditPost_VracaViewAkoModelNijeValidan()
        {
            var obavijest = KreirajEmailObavijest();

            _controller.ModelState.AddModelError(
                "Naslov",
                "Neispravan naslov");

            var rezultat = await _controller.Edit(1, obavijest);

            Assert.IsInstanceOfType<ViewResult>(rezultat);

            _serviceMock.Verify(
                s => s.UpdateAsync(It.IsAny<EmailObavijest>()),
                Times.Never);
        }

        [TestMethod]
        public async Task EditPost_VracaNotFoundAkoObavijestNePostojiNakonConcurrencyException()
        {
            var obavijest = KreirajEmailObavijest();

            _serviceMock
                .Setup(s => s.UpdateAsync(obavijest))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            _serviceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync((EmailObavijest?)null);

            var rezultat = await _controller.Edit(1, obavijest);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task EditPost_BacaConcurrencyExceptionAkoObavijestPostoji()
        {
            var obavijest = KreirajEmailObavijest();

            _serviceMock
                .Setup(s => s.UpdateAsync(obavijest))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            _serviceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(obavijest);

            await Assert.ThrowsExactlyAsync<DbUpdateConcurrencyException>(
                async () => await _controller.Edit(1, obavijest));
        }

        [TestMethod]
        public async Task DeleteGet_VracaNotFoundAkoJeIdNull()
        {
            var rezultat = await _controller.Delete(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task DeleteGet_VracaNotFoundAkoObavijestNePostoji()
        {
            _serviceMock
                .Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((EmailObavijest?)null);

            var rezultat = await _controller.Delete(99);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task DeleteGet_VracaPostojecuEmailObavijest()
        {
            var obavijest = KreirajEmailObavijest();

            _serviceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(obavijest);

            var rezultat = await _controller.Delete(1);

            var viewResult = rezultat as ViewResult;

            Assert.IsNotNull(viewResult);
            Assert.AreSame(obavijest, viewResult.Model);
        }

        [TestMethod]
        public async Task DeleteConfirmed_BriseEmailObavijest()
        {
            var rezultat = await _controller.DeleteConfirmed(1);

            _serviceMock.Verify(
                s => s.DeleteAsync(1),
                Times.Once);

            var redirectResult = rezultat as RedirectToActionResult;

            Assert.IsNotNull(redirectResult);
            Assert.AreEqual(nameof(_controller.Index), redirectResult.ActionName);
        }
    }
}
