using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    [TestCategory("Kontroler")]
    public class HomeControllerTests
    {
        private Mock<IRecenzijaService> _recenzijaServis = null!;
        private Mock<IRezervacijaService> _rezervacijaServis = null!;
        private Mock<IParkingMjestoService> _mjestoServis = null!;
        private Mock<IKorisnikService> _korisnikServis = null!;
        private Mock<IPlacanjeService> _placanjeServis = null!;
        private Mock<UserManager<Korisnik>> _userManager = null!;
        private HomeController _kontroler = null!;

        [TestInitialize]
        public void Pripremi()
        {
            Activity.Current = null;
            _recenzijaServis = new Mock<IRecenzijaService>();
            _rezervacijaServis = new Mock<IRezervacijaService>();
            _mjestoServis = new Mock<IParkingMjestoService>();
            _korisnikServis = new Mock<IKorisnikService>();
            _placanjeServis = new Mock<IPlacanjeService>();
            _userManager = new Mock<UserManager<Korisnik>>(
                Mock.Of<IUserStore<Korisnik>>(), null, null, null, null, null, null, null, null);

            // Podrazumijevano: sve liste su prazne.
            _recenzijaServis.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Recenzija>());
            _rezervacijaServis.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Rezervacija>());
            _mjestoServis.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<ParkingMjesto>());
            _korisnikServis.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Korisnik>());
            _placanjeServis.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Placanje>());

            _kontroler = new HomeController(
                _recenzijaServis.Object, _rezervacijaServis.Object, _mjestoServis.Object,
                _korisnikServis.Object, _placanjeServis.Object, _userManager.Object);

            PostaviKorisnika(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        [TestCleanup]
        public void Pocisti()
        {
            Activity.Current = null;
            _kontroler.Dispose();
        }

        private void PostaviKorisnika(ClaimsPrincipal korisnik, string traceId = "trace-1")
        {
            _kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = korisnik, TraceIdentifier = traceId }
            };
        }

        private static ClaimsPrincipal Prijavljen(params string[] uloge)
        {
            var tvrdnje = uloge.Select(u => new Claim(ClaimTypes.Role, u)).ToList();
            return new ClaimsPrincipal(new ClaimsIdentity(tvrdnje, "TestAuth"));
        }

        private static Recenzija NovaRecenzija(int ocjena, int dan) =>
            new Recenzija { Ocjena = ocjena, Datum = new DateTime(2030, 1, dan) };

        private static Rezervacija NovaRezervacija(StatusRezervacije status, int dan = 1) =>
            new Rezervacija { StatusRezervacije = status, DatumKreiranja = new DateTime(2030, 1, dan), VrijemePocetka = new DateTime(2030, 2, dan) };

        // =====================================================================
        //  Index: recenzije
        // =====================================================================

        [TestMethod]
        public async Task Index_BezRecenzija_ProsjecnaOcjenaJeNula()
        {
            await _kontroler.Index();

            Assert.AreEqual(0.0, (double)_kontroler.ViewBag.ProsjecnaOcjena);
            Assert.AreEqual(0, (int)_kontroler.ViewBag.BrojRecenzija);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task Index_ViseRecenzija_RacunaProsjecnuOcjenuIBrojRecenzija()
        {
            _recenzijaServis.Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<Recenzija> { NovaRecenzija(5, 1), NovaRecenzija(4, 2), NovaRecenzija(3, 3) });

            await _kontroler.Index();

            Assert.AreEqual(4.0, (double)_kontroler.ViewBag.ProsjecnaOcjena);
            Assert.AreEqual(3, (int)_kontroler.ViewBag.BrojRecenzija);
        }

        [TestMethod]
        public async Task Index_ViseOdTriRecenzije_ZadnjeRecenzijeSuTriNajnovije()
        {
            _recenzijaServis.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Recenzija>
            {
                NovaRecenzija(1, 1), NovaRecenzija(2, 4), NovaRecenzija(3, 2), NovaRecenzija(4, 3)
            });

            await _kontroler.Index();

            var zadnje = (List<Recenzija>)_kontroler.ViewBag.ZadnjeRecenzije;
            CollectionAssert.AreEqual(new[] { 2, 4, 3 }, zadnje.Select(r => r.Ocjena).ToArray());
        }

        // =====================================================================
        //  Index: rezervacije
        // =====================================================================

        [TestMethod]
        public async Task Index_RezervacijePoStatusima_BrojiSvakuVrstu()
        {
            _rezervacijaServis.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Rezervacija>
            {
                NovaRezervacija(StatusRezervacije.Aktivna), NovaRezervacija(StatusRezervacije.Aktivna),
                NovaRezervacija(StatusRezervacije.Zavrsena), NovaRezervacija(StatusRezervacije.Otkazana),
                NovaRezervacija(StatusRezervacije.Kreirana)
            });

            await _kontroler.Index();

            Assert.AreEqual(5, (int)_kontroler.ViewBag.BrojRezervacija);
            Assert.AreEqual(2, (int)_kontroler.ViewBag.AktivneRezervacije);
            Assert.AreEqual(1, (int)_kontroler.ViewBag.ZavrseneRezervacije);
            Assert.AreEqual(1, (int)_kontroler.ViewBag.OtkazaneRezervacije);
        }

        [TestMethod]
        public async Task Index_ViseOdPetRezervacija_ZadnjeRezervacijeSuPetNajnovijih()
        {
            _rezervacijaServis.Setup(s => s.GetAllAsync()).ReturnsAsync(
                Enumerable.Range(1, 7).Select(dan => NovaRezervacija(StatusRezervacije.Aktivna, dan)).ToList());

            await _kontroler.Index();

            var zadnje = (List<Rezervacija>)_kontroler.ViewBag.ZadnjeRezervacije;
            Assert.AreEqual(5, zadnje.Count);
            Assert.AreEqual(new DateTime(2030, 1, 7), zadnje.First().DatumKreiranja);
        }

        // =====================================================================
        //  Index: parking mjesta, korisnici i plaćanja
        // =====================================================================

        [TestMethod]
        public async Task Index_ParkingMjestaIKorisnici_VracaBrojeve()
        {
            _mjestoServis.Setup(s => s.GetAllAsync()).ReturnsAsync(
                new List<ParkingMjesto> { new ParkingMjesto(), new ParkingMjesto(), new ParkingMjesto() });
            _korisnikServis.Setup(s => s.GetAllAsync()).ReturnsAsync(
                new List<Korisnik> { new Korisnik(), new Korisnik() });

            await _kontroler.Index();

            Assert.AreEqual(3, (int)_kontroler.ViewBag.BrojParkingMjesta);
            Assert.AreEqual(2, (int)_kontroler.ViewBag.BrojKorisnika);
        }

        [TestMethod]
        public async Task Index_MjestaVanFunkcije_VracaSamoMjestaVanFunkcije()
        {
            _mjestoServis.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<ParkingMjesto>
            {
                new ParkingMjesto { Oznaka = "A1", Status = StatusMjesta.Slobodno },
                new ParkingMjesto { Oznaka = "A2", Status = StatusMjesta.VanFunkcije },
                new ParkingMjesto { Oznaka = "A3", Status = StatusMjesta.VanFunkcije }
            });

            await _kontroler.Index();

            var vanFunkcije = (List<ParkingMjesto>)_kontroler.ViewBag.MjestaVanFunkcije;
            CollectionAssert.AreEqual(new[] { "A2", "A3" }, vanFunkcije.Select(m => m.Oznaka).ToArray());
        }

        [TestMethod]
        public async Task Index_NoviKorisnici_SortiraniSuPoDatumuRegistracije()
        {
            _korisnikServis.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Korisnik>
            {
                new Korisnik { Ime = "Stari", DatumRegistracije = new DateTime(2030, 1, 1) },
                new Korisnik { Ime = "Novi", DatumRegistracije = new DateTime(2030, 1, 5) }
            });

            await _kontroler.Index();

            var novi = (List<Korisnik>)_kontroler.ViewBag.NoviKorisnici;
            Assert.AreEqual("Novi", novi.First().Ime);
        }

        [TestMethod]
        public async Task Index_ZadnjaPlacanja_SortiranaSuPoDatumuPlacanja()
        {
            _placanjeServis.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Placanje>
            {
                new Placanje { Id = 1, DatumPlacanja = new DateTime(2030, 1, 1) },
                new Placanje { Id = 2, DatumPlacanja = new DateTime(2030, 1, 9) }
            });

            await _kontroler.Index();

            var zadnja = (List<Placanje>)_kontroler.ViewBag.ZadnjaPlacanja;
            Assert.AreEqual(2, zadnja.First().Id);
        }

        // ---------- DATA-DRIVEN (XML) ----------

        [TestMethod]
        [Priority(1)]
        [TestCategory("DataDriven-XML")]
        [Description("Plaćanja i očekivana zarada se čitaju iz TestData/ukupna_zarada.xml")]
        [DynamicData(nameof(UcitavacTestnihPodataka.SlucajeviZaradeIzXml),
            typeof(UcitavacTestnihPodataka))]
        public async Task Index_UkupnaZarada_RacunaSamoUspjesnaPlacanja(
            string opis, List<Placanje> placanja, double ocekivanaZarada)
        {
            _placanjeServis.Setup(s => s.GetAllAsync()).ReturnsAsync(placanja);

            await _kontroler.Index();

            Assert.AreEqual(ocekivanaZarada, (double)_kontroler.ViewBag.UkupnaZarada, 0.001, opis);
        }

        // =====================================================================
        //  Index: podaci prijavljenog korisnika
        // =====================================================================

        [TestMethod]
        [TestCategory("Moq")]
        public async Task Index_PrijavljeniKorisnik_VracaBrojINajblizuAktivnuRezervaciju()
        {
            PostaviKorisnika(Prijavljen("RegistrovaniKorisnik"));
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new Korisnik { Id = "k-1" });
            var kasnija = NovaRezervacija(StatusRezervacije.Aktivna, 9);
            var ranija = NovaRezervacija(StatusRezervacije.Aktivna, 3);
            _rezervacijaServis.Setup(s => s.GetByKorisnikIdAsync("k-1")).ReturnsAsync(new List<Rezervacija>
            {
                kasnija, NovaRezervacija(StatusRezervacije.Otkazana, 1), ranija
            });

            await _kontroler.Index();

            Assert.AreEqual(3, (int)_kontroler.ViewBag.MojeRezervacijeCount);
            Assert.AreSame(ranija, (Rezervacija)_kontroler.ViewBag.MojaAktivnaRezervacija);
        }

        [TestMethod]
        public async Task Index_PrijavljeniKorisnikBezAktivneRezervacije_AktivnaRezervacijaJeNull()
        {
            PostaviKorisnika(Prijavljen("RegistrovaniKorisnik"));
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new Korisnik { Id = "k-1" });
            _rezervacijaServis.Setup(s => s.GetByKorisnikIdAsync("k-1"))
                .ReturnsAsync(new List<Rezervacija> { NovaRezervacija(StatusRezervacije.Zavrsena) });

            await _kontroler.Index();

            Assert.IsNull((object?)_kontroler.ViewBag.MojaAktivnaRezervacija);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task Index_PrijavljenAliKorisnikNePostoji_NePostavljaPodatkeKorisnika()
        {
            PostaviKorisnika(Prijavljen("RegistrovaniKorisnik"));
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((Korisnik?)null);

            await _kontroler.Index();

            Assert.IsNull((object?)_kontroler.ViewBag.MojeRezervacijeCount);
            _rezervacijaServis.Verify(s => s.GetByKorisnikIdAsync(It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [TestCategory("Moq")]
        [DataRow("Administrator")]
        [DataRow("Operater")]
        public async Task Index_AdministratorIliOperater_NeDohvataLicneRezervacije(string uloga)
        {
            PostaviKorisnika(Prijavljen(uloga));

            await _kontroler.Index();

            _rezervacijaServis.Verify(s => s.GetByKorisnikIdAsync(It.IsAny<string>()), Times.Never);
            Assert.IsNull((object?)_kontroler.ViewBag.MojeRezervacijeCount);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task Index_NeprijavljenKorisnik_NeDohvataLicneRezervacije()
        {
            PostaviKorisnika(new ClaimsPrincipal(new ClaimsIdentity()));

            await _kontroler.Index();

            _userManager.Verify(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()), Times.Never);
        }

        [TestMethod]
        [TestCategory("Izuzetak")]
        public async Task Index_ServisBacaIzuzetak_IzuzetakSePropagira()
        {
            _recenzijaServis.Setup(s => s.GetAllAsync()).ThrowsAsync(new InvalidOperationException("Baza nije dostupna"));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _kontroler.Index());
        }

        // =====================================================================
        //  Privacy i Error
        // =====================================================================

        [TestMethod]
        public void Privacy_VracaView()
        {
            var rezultat = _kontroler.Privacy();

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        }

        [TestMethod]
        public void Error_BezAktivnosti_KoristiTraceIdentifier()
        {
            PostaviKorisnika(new ClaimsPrincipal(new ClaimsIdentity()), traceId: "trace-xyz");

            var rezultat = _kontroler.Error() as ViewResult;

            var model = (ErrorViewModel)rezultat!.Model!;
            Assert.AreEqual("trace-xyz", model.RequestId);
        }

        [TestMethod]
        public void Error_SaAktivnoscu_KoristiIdAktivnosti()
        {
            var aktivnost = new Activity("test-aktivnost");
            aktivnost.Start();

            var rezultat = _kontroler.Error() as ViewResult;

            var model = (ErrorViewModel)rezultat!.Model!;
            Assert.AreEqual(aktivnost.Id, model.RequestId);
            aktivnost.Stop();
        }
    }
}
