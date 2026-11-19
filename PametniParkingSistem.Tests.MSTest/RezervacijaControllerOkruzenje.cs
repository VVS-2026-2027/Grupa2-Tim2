using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;
using System.Security.Claims;

namespace PametniParkingSistem.Tests.MSTest
{
    public sealed class RezervacijaControllerOkruzenje : IDisposable
    {
        public Mock<IRezervacijaService> Servis { get; } = new();
        public Mock<IParkingMjestoService> Mjesta { get; } = new();
        public Mock<IPlacanjeService> Placanja { get; } = new();
        public Mock<IEmailSenderService> Email { get; } = new();
        public Mock<UserManager<Korisnik>> Korisnici { get; }
        public RezervacijaController Kontroler { get; }

        public Rezervacija Rezervacija { get; }
        public ParkingMjesto Mjesto { get; }
        public Korisnik Korisnik { get; }

        public RezervacijaControllerOkruzenje()
        {
            var spremiste = new Mock<IUserStore<Korisnik>>();

            Korisnici = new Mock<UserManager<Korisnik>>(
                spremiste.Object,
                null!, null!, null!, null!,
                null!, null!, null!, null!);

            Korisnik = new Korisnik
            {
                Id = "korisnik-1",
                UserName = "test@example.com",
                Email = "test@example.com"
            };

            Mjesto = new ParkingMjesto
            {
                Id = 1,
                Oznaka = "A1",
                CijenaPoSatu = 5
            };

            Rezervacija = RezervacijaTestPodaci.Kreiraj();
            Rezervacija.VrijemePocetka = DateTime.Now.AddDays(2);
            Rezervacija.VrijemeKraja =
                Rezervacija.VrijemePocetka.AddHours(2);
            Rezervacija.UkupnaCijena = 100;

            Servis.Setup(s => s.GetIstekleAktivneRezervacijeAsync())
                .ReturnsAsync(new List<Rezervacija>());
            Servis.Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<Rezervacija> { Rezervacija });
            Servis.Setup(s => s.GetByKorisnikIdAsync(Korisnik.Id))
                .ReturnsAsync(new List<Rezervacija> { Rezervacija });
            Servis.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync(Rezervacija);
            Servis.Setup(s => s.ProvjeriDostupnostAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(true);
            Servis.Setup(s => s.IzracunajCijenu(
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<double>()))
                .Returns(10);

            Mjesta.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(Mjesto);

            Kontroler = new RezervacijaController(
                Servis.Object,
                Mjesta.Object,
                Korisnici.Object,
                Placanja.Object,
                Email.Object);

            PostaviKorisnika();
        }

        public void PostaviKorisnika(string? uloga = null)
        {
            var tvrdnje = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Korisnik.Id),
                new Claim(ClaimTypes.Name, Korisnik.Email!)
            };

            if (uloga != null)
                tvrdnje.Add(new Claim(ClaimTypes.Role, uloga));

            var kontekst = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(tvrdnje, "Test"))
            };

            Kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = kontekst
            };

            Kontroler.TempData = new TempDataDictionary(
                kontekst, Mock.Of<ITempDataProvider>());

            Korisnici.Setup(k => k.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(Korisnik);
        }

        public void Dispose()
        {
            Kontroler.Dispose();
            Korisnici.Object.Dispose();
        }
    }
}