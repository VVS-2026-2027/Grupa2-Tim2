using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class RecenzijaControllerTests
    {
        private Mock<IRecenzijaService> _serviceMock = null!;
        private Mock<IRezervacijaService> _rezervacijaServiceMock = null!;
        private Mock<UserManager<Korisnik>> _userManagerMock = null!;
        private RecenzijaController _controller = null!;

        private Korisnik _korisnik = null!;
        private Rezervacija _rezervacija = null!;

        [TestInitialize]
        public void Setup()
        {
            _serviceMock = new Mock<IRecenzijaService>();
            _rezervacijaServiceMock = new Mock<IRezervacijaService>();

            var store = new Mock<IUserStore<Korisnik>>();

            _userManagerMock = new Mock<UserManager<Korisnik>>(
                store.Object,
                null!, null!, null!, null!,
                null!, null!, null!, null!);

            _korisnik = new Korisnik
            {
                Id = "korisnik1",
                UserName = "testkorisnik",
                Email = "test@test.com",
                Ime = "Test",
                Prezime = "Korisnik"
            };

            _rezervacija = new Rezervacija
            {
                Id = 10,
                KorisnikId = _korisnik.Id,
                StatusRezervacije = StatusRezervacije.Zavrsena
            };

            _userManagerMock
                .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(_korisnik);

            _controller = new RecenzijaController(
                _serviceMock.Object,
                _rezervacijaServiceMock.Object,
                _userManagerMock.Object);

            PostaviKorisnika();
        }

        private void PostaviKorisnika(params string[] uloge)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, _korisnik.Id)
            };

            foreach (var uloga in uloge)
            {
                claims.Add(new Claim(ClaimTypes.Role, uloga));
            }

            var identity = new ClaimsIdentity(
                claims,
                "TestAuthentication");

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(identity)
            };

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };

            _controller.TempData = new TempDataDictionary(
                httpContext,
                Mock.Of<ITempDataProvider>());
        }

        private Recenzija KreirajRecenziju()
        {
            return new Recenzija
            {
                Id = 1,
                Ocjena = 5,
                Komentar = "Odlična usluga",
                Datum = DateTime.Today,
                Obrisan = false,
                KorisnikId = _korisnik.Id,
                RezervacijaId = _rezervacija.Id
            };
        }

        // INDEKS

        [TestMethod]
        public async Task Indeks_KadaRecenzijePostoje_VracaSveRecenzije()
        {
            var recenzije = new List<Recenzija>
            {
                KreirajRecenziju()
            };

            _serviceMock
                .Setup(s => s.GetAllAsync())
                .ReturnsAsync(recenzije);

            var rezultat = await _controller.Index();

            var view = rezultat as ViewResult;

            Assert.IsNotNull(view);
            Assert.AreSame(recenzije, view.Model);
        }

        // DETALJI

        [TestMethod]
        public async Task Detalji_KadaJeIdNull_VracaRezultatNijePronadjen()
        {
            var rezultat = await _controller.Details(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Detalji_KadaRecenzijaNePostoji_VracaRezultatNijePronadjen()
        {
            _serviceMock
                .Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((Recenzija?)null);

            var rezultat = await _controller.Details(99);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Detalji_KadaRecenzijaPostoji_VracaPostojecuRecenziju()
        {
            var recenzija = KreirajRecenziju();

            _serviceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(recenzija);

            var rezultat = await _controller.Details(1);

            var view = rezultat as ViewResult;

            Assert.IsNotNull(view);
            Assert.AreSame(recenzija, view.Model);
        }

        // PRIKAZ OBRASCA ZA KREIRANJE

        [TestMethod]
        public async Task KreirajGet_KadaKorisnikNePostoji_VracaNeovlastenRezultat()
        {
            _userManagerMock
                .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((Korisnik?)null);

            var rezultat = await _controller.Create(10);

            Assert.IsInstanceOfType<UnauthorizedResult>(rezultat);
        }

        [TestMethod]
        public async Task KreirajGet_KadaRezervacijaNePostoji_VracaRezultatNijePronadjen()
        {
            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((Rezervacija?)null);

            var rezultat = await _controller.Create(99);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task KreirajGet_KadaRezervacijaNePripadaKorisniku_VracaZabranjenPristup()
        {
            _rezervacija.KorisnikId = "drugiKorisnik";

            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(_rezervacija);

            var rezultat = await _controller.Create(10);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        [TestMethod]
        public async Task KreirajGet_KadaRezervacijaNijeZavrsena_PreusmjeravaNaDetaljeRezervacije()
        {
            _rezervacija.StatusRezervacije = (StatusRezervacije)999;

            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(_rezervacija);

            var rezultat = await _controller.Create(10);

            var redirect = rezultat as RedirectToActionResult;

            Assert.IsNotNull(redirect);
            Assert.AreEqual("Details", redirect.ActionName);
            Assert.AreEqual("Rezervacija", redirect.ControllerName);
        }

        [TestMethod]
        public async Task KreirajGet_KadaRecenzijaVecPostoji_PreusmjeravaNaDetaljeRecenzije()
        {
            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(_rezervacija);

            _serviceMock
                .Setup(s => s.ExistsForRezervacijaAsync(10))
                .ReturnsAsync(true);

            var rezultat = await _controller.Create(10);

            var redirect = rezultat as RedirectToActionResult;

            Assert.IsNotNull(redirect);
            Assert.AreEqual("Details", redirect.ActionName);
        }

        [TestMethod]
        public async Task KreirajGet_KadaRecenzijaNePostoji_VracaObrazacSaNovomRecenzijom()
        {
            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(_rezervacija);

            _serviceMock
                .Setup(s => s.ExistsForRezervacijaAsync(10))
                .ReturnsAsync(false);

            var rezultat = await _controller.Create(10);

            var view = rezultat as ViewResult;

            Assert.IsNotNull(view);

            var recenzija = view.Model as Recenzija;

            Assert.IsNotNull(recenzija);
            Assert.AreEqual(10, recenzija.RezervacijaId);
            Assert.AreEqual(5, recenzija.Ocjena);
            Assert.AreEqual(10, _controller.ViewBag.RezervacijaId);
        }

        // SLANJE OBRASCA ZA KREIRANJE

        [TestMethod]
        public async Task KreirajPost_KadaKorisnikNePostoji_VracaNeovlastenRezultat()
        {
            _userManagerMock
                .Setup(u => u.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((Korisnik?)null);

            var rezultat = await _controller.Create(10, KreirajRecenziju());

            Assert.IsInstanceOfType<UnauthorizedResult>(rezultat);
        }

        [TestMethod]
        public async Task KreirajPost_KadaRezervacijaNePostoji_VracaRezultatNijePronadjen()
        {
            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((Rezervacija?)null);

            var rezultat = await _controller.Create(99, KreirajRecenziju());

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task KreirajPost_KadaRezervacijaNePripadaKorisniku_VracaZabranjenPristup()
        {
            _rezervacija.KorisnikId = "drugiKorisnik";

            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(_rezervacija);

            var rezultat = await _controller.Create(10, KreirajRecenziju());

            Assert.IsInstanceOfType<ForbidResult>(rezultat);
        }

        [TestMethod]
        public async Task KreirajPost_KadaRezervacijaNijeZavrsena_PreusmjeravaBezDodavanjaRecenzije()
        {
            _rezervacija.StatusRezervacije = (StatusRezervacije)999;

            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(_rezervacija);

            var rezultat = await _controller.Create(10, KreirajRecenziju());

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
            _serviceMock.Verify(
                s => s.AddAsync(It.IsAny<Recenzija>()),
                Times.Never);
        }

        [TestMethod]
        public async Task KreirajPost_KadaRecenzijaVecPostoji_PreusmjeravaBezDodavanjaRecenzije()
        {
            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(_rezervacija);

            _serviceMock
                .Setup(s => s.ExistsForRezervacijaAsync(10))
                .ReturnsAsync(true);

            var rezultat = await _controller.Create(10, KreirajRecenziju());

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);

            _serviceMock.Verify(
                s => s.AddAsync(It.IsAny<Recenzija>()),
                Times.Never);
        }

        [TestMethod]
        public async Task KreirajPost_KadaModelNijeIspravan_VracaObrazac()
        {
            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(_rezervacija);

            _serviceMock
                .Setup(s => s.ExistsForRezervacijaAsync(10))
                .ReturnsAsync(false);

            _controller.ModelState.AddModelError(
                "Ocjena",
                "Neispravna ocjena");

            var recenzija = KreirajRecenziju();

            var rezultat = await _controller.Create(10, recenzija);

            var view = rezultat as ViewResult;

            Assert.IsNotNull(view);
            Assert.AreSame(recenzija, view.Model);
        }

        [TestMethod]
        public async Task KreirajPost_KadaJeRezervacijaZavrsena_DodajeRecenziju()
        {
            _rezervacijaServiceMock
                .Setup(s => s.GetByIdAsync(10))
                .ReturnsAsync(_rezervacija);

            _serviceMock
                .Setup(s => s.ExistsForRezervacijaAsync(10))
                .ReturnsAsync(false);

            var recenzija = KreirajRecenziju();

            var prijeTesta = DateTime.Now;

            var rezultat = await _controller.Create(10, recenzija);

            var poslijeTesta = DateTime.Now;

            _serviceMock.Verify(
                s => s.AddAsync(recenzija),
                Times.Once);

            Assert.AreEqual(_korisnik.Id, recenzija.KorisnikId);
            Assert.AreEqual(10, recenzija.RezervacijaId);
            Assert.IsFalse(recenzija.Obrisan);
            Assert.IsTrue(
                recenzija.Datum >= prijeTesta &&
                recenzija.Datum <= poslijeTesta);

            var redirect = rezultat as RedirectToActionResult;

            Assert.IsNotNull(redirect);
            Assert.AreEqual("Details", redirect.ActionName);
        }

        // PRIKAZ OBRASCA ZA BRISANJE

        [TestMethod]
        public async Task Obrisi_KadaJeIdNull_VracaRezultatNijePronadjen()
        {
            var rezultat = await _controller.Delete(null);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Obrisi_KadaRecenzijaNePostoji_VracaRezultatNijePronadjen()
        {
            _serviceMock
                .Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((Recenzija?)null);

            var rezultat = await _controller.Delete(99);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task Obrisi_KadaRecenzijaPostoji_VracaObrazacZaBrisanje()
        {
            var recenzija = KreirajRecenziju();

            _serviceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(recenzija);

            var rezultat = await _controller.Delete(1);

            var view = rezultat as ViewResult;

            Assert.IsNotNull(view);
            Assert.AreSame(recenzija, view.Model);
        }

        // POTVRDA BRISANJA

        [TestMethod]
        public async Task PotvrdiBrisanje_KadaRecenzijaNePostoji_VracaRezultatNijePronadjen()
        {
            _serviceMock
                .Setup(s => s.GetByIdAsync(99))
                .ReturnsAsync((Recenzija?)null);

            var rezultat = await _controller.DeleteConfirmed(99);

            Assert.IsInstanceOfType<NotFoundResult>(rezultat);
        }

        [TestMethod]
        public async Task PotvrdiBrisanje_KadaKorisnikNemaPotrebnuUlogu_VracaZabranjenPristup()
        {
            _serviceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(KreirajRecenziju());

            PostaviKorisnika();

            var rezultat = await _controller.DeleteConfirmed(1);

            Assert.IsInstanceOfType<ForbidResult>(rezultat);

            _serviceMock.Verify(
                s => s.DeleteAsync(It.IsAny<int>()),
                Times.Never);
        }

        [TestMethod]
        public async Task PotvrdiBrisanje_KadaJeKorisnikAdministrator_BriseRecenziju()
        {
            _serviceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(KreirajRecenziju());

            PostaviKorisnika("Administrator");

            var rezultat = await _controller.DeleteConfirmed(1);

            _serviceMock.Verify(
                s => s.DeleteAsync(1),
                Times.Once);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        }

        [TestMethod]
        public async Task PotvrdiBrisanje_KadaJeKorisnikOperater_BriseRecenziju()
        {
            _serviceMock
                .Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(KreirajRecenziju());

            PostaviKorisnika("Operater");

            var rezultat = await _controller.DeleteConfirmed(1);

            _serviceMock.Verify(
                s => s.DeleteAsync(1),
                Times.Once);

            Assert.IsInstanceOfType<RedirectToActionResult>(rezultat);
        }
    }
}