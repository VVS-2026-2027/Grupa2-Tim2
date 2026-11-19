using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using System.Security.Claims;
using System.Text.Json;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class RezervacijaControllerTests
    {
        private RezervacijaControllerOkruzenje _ok = null!;

        [TestInitialize]
        public void PripremiTest()
        {
            _ok = new RezervacijaControllerOkruzenje();
        }

        [TestCleanup]
        public void OcistiTest()
        {
            _ok.Dispose();
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Index_ObicniKorisnik_PreusmjeravaNaMoje()
        {
            var rezultat = await _ok.Kontroler.Index(null);

            Assert.AreEqual(
                "Moje", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        [DataRow(null, 3)]
        [DataRow("Aktivne", 1)]
        [DataRow("Zavrsene", 1)]
        [DataRow("Otkazane", 1)]
        [TestCategory("Stub")]
        public async Task Index_Administrator_FiltriraISortira(
            string? status, int ocekivaniBroj)
        {
            _ok.PostaviKorisnika("Administrator");

            var aktivna = RezervacijaTestPodaci.Kreiraj(1);
            var zavrsena = RezervacijaTestPodaci.Kreiraj(2);
            zavrsena.StatusRezervacije = StatusRezervacije.Zavrsena;
            var otkazana = RezervacijaTestPodaci.Kreiraj(3);
            otkazana.StatusRezervacije = StatusRezervacije.Otkazana;
            otkazana.DatumKreiranja = aktivna.DatumKreiranja.AddDays(1);

            _ok.Servis.Setup(s => s.GetAllAsync())
                .ReturnsAsync(new List<Rezervacija>
                {
                    aktivna, zavrsena, otkazana
                });

            var rezultat = (ViewResult)await _ok.Kontroler.Index(status);
            var model = (List<Rezervacija>)rezultat.Model!;

            Assert.AreEqual(ocekivaniBroj, model.Count);
            Assert.AreEqual(status, rezultat.ViewData["Status"]);
            CollectionAssert.AreEqual(
                model.OrderByDescending(r => r.DatumKreiranja)
                    .Select(r => r.Id).ToArray(),
                model.Select(r => r.Id).ToArray());
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task Index_IsteklaRezervacija_PostavljaStatusZavrsena()
        {
            _ok.PostaviKorisnika("Operater");
            _ok.Servis.Setup(s => s.GetIstekleAktivneRezervacijeAsync())
                .ReturnsAsync(new List<Rezervacija> { _ok.Rezervacija });

            await _ok.Kontroler.Index(null);

            _ok.Servis.Verify(s => s.UpdateAsync(
                It.Is<Rezervacija>(r =>
                    r.Id == 1 &&
                    r.StatusRezervacije == StatusRezervacije.Zavrsena)),
                Times.Once);
        }

        [TestMethod]
        [DataRow("Administrator")]
        [DataRow("Operater")]
        [TestCategory("Stub")]
        public async Task Moje_Osoblje_PreusmjeravaNaIndex(string uloga)
        {
            _ok.PostaviKorisnika(uloga);

            var rezultat = await _ok.Kontroler.Moje(null);

            Assert.AreEqual(
                "Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Moje_NemaKorisnika_VracaUnauthorized()
        {
            _ok.Korisnici.Setup(k =>
                k.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((Korisnik?)null);

            var rezultat = await _ok.Kontroler.Moje(null);

            Assert.IsInstanceOfType(rezultat, typeof(UnauthorizedResult));
        }

        [TestMethod]
        [DataRow(null, 1)]
        [DataRow("Aktivne", 1)]
        [DataRow("Zavrsene", 0)]
        [DataRow("Otkazane", 0)]
        [TestCategory("Stub")]
        public async Task Moje_FiltriraRezervacijeKorisnika(
            string? status, int ocekivaniBroj)
        {
            var rezultat = (ViewResult)await _ok.Kontroler.Moje(status);
            var model = (List<Rezervacija>)rezultat.Model!;

            Assert.AreEqual(ocekivaniBroj, model.Count);
            Assert.AreEqual("Moje rezervacije", rezultat.ViewData["Naslov"]);
        }

        [TestMethod]
        [DataRow("Administrator")]
        [DataRow("Operater")]
        [TestCategory("Stub")]
        public async Task Historija_Osoblje_PreusmjeravaNaIndex(string uloga)
        {
            _ok.PostaviKorisnika(uloga);

            var rezultat = await _ok.Kontroler.Historija(null);

            Assert.AreEqual(
                "Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Historija_NemaKorisnika_VracaUnauthorized()
        {
            _ok.Korisnici.Setup(k =>
                k.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((Korisnik?)null);

            var rezultat = await _ok.Kontroler.Historija(null);

            Assert.IsInstanceOfType(rezultat, typeof(UnauthorizedResult));
        }

        [TestMethod]
        [DataRow(null, 2)]
        [DataRow("Zavrsena", 1)]
        [DataRow("Otkazana", 1)]
        [DataRow("nepoznat-status", 2)]
        [TestCategory("Stub")]
        public async Task Historija_VracaSamoZavrseneIOtkazane(
            string? status, int ocekivaniBroj)
        {
            var zavrsena = RezervacijaTestPodaci.Kreiraj(2);
            zavrsena.StatusRezervacije = StatusRezervacije.Zavrsena;
            var otkazana = RezervacijaTestPodaci.Kreiraj(3);
            otkazana.StatusRezervacije = StatusRezervacije.Otkazana;

            _ok.Servis.Setup(s => s.GetByKorisnikIdAsync("korisnik-1"))
                .ReturnsAsync(new List<Rezervacija>
                {
                    _ok.Rezervacija, zavrsena, otkazana
                });

            var rezultat = (ViewResult)await _ok.Kontroler.Historija(status);
            var model = (List<Rezervacija>)rezultat.Model!;

            Assert.AreEqual(ocekivaniBroj, model.Count);
            Assert.IsFalse(model.Any(r =>
                r.StatusRezervacije == StatusRezervacije.Kreirana));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(99)]
        [TestCategory("Stub")]
        public async Task Details_NeispravanId_VracaNotFound(int? id)
        {
            var rezultat = await _ok.Kontroler.Details(id);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("Administrator")]
        [DataRow("Operater")]
        [TestCategory("Stub")]
        public async Task Details_OvlastenKorisnik_VracaRezervaciju(string? uloga)
        {
            _ok.PostaviKorisnika(uloga);

            var rezultat = (ViewResult)await _ok.Kontroler.Details(1);

            Assert.AreSame(_ok.Rezervacija, rezultat.Model);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Details_TudjaRezervacija_VracaForbid()
        {
            _ok.Rezervacija.KorisnikId = "drugi-korisnik";

            var rezultat = await _ok.Kontroler.Details(1);

            Assert.IsInstanceOfType(rezultat, typeof(ForbidResult));
        }

        [TestMethod]
        [DataRow("Administrator")]
        [DataRow("Operater")]
        [TestCategory("Stub")]
        public async Task Create_Get_Osoblje_PreusmjeravaNaIndex(string uloga)
        {
            _ok.PostaviKorisnika(uloga);

            var rezultat = await _ok.Kontroler.Create(1);

            Assert.AreEqual(
                "Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Create_Get_NepostojeceMjesto_VracaNotFound()
        {
            var rezultat = await _ok.Kontroler.Create(99);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Create_Get_PripremaNovuRezervaciju()
        {
            var rezultat = (ViewResult)await _ok.Kontroler.Create(1);
            var model = (Rezervacija)rezultat.Model!;

            Assert.AreEqual(1, model.ParkingMjestoId);
            Assert.AreEqual("test@example.com", model.EmailZaObavijest);
            Assert.IsTrue(model.VrijemeKraja > model.VrijemePocetka);
            Assert.AreSame(_ok.Mjesto, rezultat.ViewData["ParkingMjesto"]);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Create_Get_ObnavljaSacuvanuRezervaciju()
        {
            _ok.Kontroler.TempData["RezervacijaZaPlacanje"] =
                JsonSerializer.Serialize(_ok.Rezervacija);

            var rezultat = (ViewResult)await _ok.Kontroler.Create(1);
            var model = (Rezervacija)rezultat.Model!;

            Assert.AreEqual(
                _ok.Rezervacija.RegistracijskeTablice,
                model.RegistracijskeTablice);
            Assert.AreEqual(
                _ok.Rezervacija.VrijemePocetka, model.VrijemePocetka);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("null")]
        [DataRow("{\"ParkingMjestoId\":99}")]
        [TestCategory("Stub")]
        public async Task Create_Get_NeprimjenjiviSacuvaniPodaci_PripremaNoviModel(
            string json)
        {
            _ok.Kontroler.TempData["RezervacijaZaPlacanje"] = json;

            var rezultat = (ViewResult)await _ok.Kontroler.Create(1);
            var model = (Rezervacija)rezultat.Model!;

            Assert.AreEqual(1, model.ParkingMjestoId);
            Assert.AreEqual(0, model.Id);
        }

        [TestMethod]
        [DataRow("Administrator")]
        [DataRow("Operater")]
        [TestCategory("Stub")]
        public async Task Create_Post_Osoblje_PreusmjeravaNaIndex(string uloga)
        {
            _ok.PostaviKorisnika(uloga);

            var rezultat = await _ok.Kontroler.Create(_ok.Rezervacija);

            Assert.AreEqual(
                "Index", ((RedirectToActionResult)rezultat).ActionName);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Create_Post_NemaKorisnika_VracaUnauthorized()
        {
            _ok.Korisnici.Setup(k =>
                k.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync((Korisnik?)null);

            var rezultat = await _ok.Kontroler.Create(_ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(UnauthorizedResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Create_Post_NemaMjesta_VracaNotFound()
        {
            _ok.Rezervacija.ParkingMjestoId = 99;

            var rezultat = await _ok.Kontroler.Create(_ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Create_Post_PocetakUProslosti_VracaGresku()
        {
            _ok.Rezervacija.VrijemePocetka = DateTime.Now.AddDays(-1);

            var rezultat = await _ok.Kontroler.Create(_ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.IsTrue(
                _ok.Kontroler.ModelState["VrijemePocetka"]!.Errors.Count > 0);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Create_Post_KrajPrijePocetka_VracaGresku()
        {
            _ok.Rezervacija.VrijemeKraja =
                _ok.Rezervacija.VrijemePocetka.AddHours(-1);

            var rezultat = await _ok.Kontroler.Create(_ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.IsFalse(_ok.Kontroler.ModelState.IsValid);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Create_Post_ZauzetoMjesto_VracaGresku()
        {
            _ok.Servis.Setup(s => s.ProvjeriDostupnostAsync(
                    It.IsAny<int>(),
                    It.IsAny<DateTime>(),
                    It.IsAny<DateTime>()))
                .ReturnsAsync(false);

            var rezultat = await _ok.Kontroler.Create(_ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.IsFalse(_ok.Kontroler.ModelState.IsValid);
            Assert.IsTrue(_ok.Kontroler.TempData.ContainsKey("Error"));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Create_Post_ValidniPodaci_PripremaPlacanjeSaPdv()
        {
            var rezultat = (RedirectToActionResult)
                await _ok.Kontroler.Create(_ok.Rezervacija);

            var json = (string)_ok.Kontroler.TempData["RezervacijaZaPlacanje"]!;
            var sacuvana = JsonSerializer.Deserialize<Rezervacija>(json)!;

            Assert.AreEqual("Create", rezultat.ActionName);
            Assert.AreEqual("Placanje", rezultat.ControllerName);
            Assert.AreEqual(11.7, sacuvana.UkupnaCijena, 0.000001);
            Assert.AreEqual("korisnik-1", sacuvana.KorisnikId);
            Assert.AreEqual(
                StatusRezervacije.Kreirana, sacuvana.StatusRezervacije);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(99)]
        [TestCategory("Stub")]
        public async Task Edit_Get_NeispravanId_VracaNotFound(int? id)
        {
            var rezultat = await _ok.Kontroler.Edit(id);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Get_TudjaRezervacija_VracaForbid()
        {
            _ok.Rezervacija.KorisnikId = "drugi-korisnik";

            var rezultat = await _ok.Kontroler.Edit((int?)1);

            Assert.IsInstanceOfType(rezultat, typeof(ForbidResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Get_Vlasnik_VracaModel()
        {
            var rezultat = (ViewResult)await _ok.Kontroler.Edit((int?)1);

            Assert.AreSame(_ok.Rezervacija, rezultat.Model);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Post_RazlicitId_VracaNotFound()
        {
            var rezultat = await _ok.Kontroler.Edit(99, _ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Post_NemaRezervacije_VracaNotFound()
        {
            _ok.Servis.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync((Rezervacija?)null);

            var rezultat = await _ok.Kontroler.Edit(1, _ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Post_TudjaRezervacija_VracaForbid()
        {
            _ok.Rezervacija.KorisnikId = "drugi-korisnik";

            var rezultat = await _ok.Kontroler.Edit(1, _ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(ForbidResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Post_NemaMjesta_VracaNotFound()
        {
            _ok.Mjesta.Setup(s => s.GetByIdAsync(1))
                .ReturnsAsync((ParkingMjesto?)null);

            var rezultat = await _ok.Kontroler.Edit(1, _ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Post_NeispravanInterval_VracaGresku()
        {
            _ok.Rezervacija.VrijemeKraja = _ok.Rezervacija.VrijemePocetka;

            var rezultat = await _ok.Kontroler.Edit(1, _ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
            Assert.IsFalse(_ok.Kontroler.ModelState.IsValid);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Post_VecaCijena_PripremaDoplatu()
        {
            _ok.Rezervacija.UkupnaCijena = 5;

            var rezultat = (RedirectToActionResult)
                await _ok.Kontroler.Edit(1, _ok.Rezervacija);

            var json = (string)
                _ok.Kontroler.TempData["IzmjenaRezervacijeZaPlacanje"]!;
            var izmjena = JsonSerializer.Deserialize<Rezervacija>(json)!;

            Assert.AreEqual("Placanje", rezultat.ControllerName);
            Assert.AreEqual("Create", rezultat.ActionName);
            Assert.AreEqual(11.7, izmjena.UkupnaCijena, 0.000001);
            Assert.AreEqual(
                StatusRezervacije.Aktivna, izmjena.StatusRezervacije);
            Assert.IsTrue(_ok.Kontroler.TempData.ContainsKey("IznosDoplate"));
        }

        [TestMethod]
        [DataRow(null, "Moje")]
        [DataRow("Administrator", "Index")]
        [DataRow("Operater", "Index")]
        [TestCategory("Mock")]
        public async Task Edit_Post_BezDoplate_SpremaIzmjenu(
            string? uloga, string odrediste)
        {
            _ok.PostaviKorisnika(uloga);

            var rezultat = (RedirectToActionResult)
                await _ok.Kontroler.Edit(1, _ok.Rezervacija);

            _ok.Servis.Verify(s => s.UpdateAsync(
                It.Is<Rezervacija>(r =>
                    r.Id == 1 &&
                    r.StatusRezervacije == StatusRezervacije.Aktivna &&
                    Math.Abs(r.UkupnaCijena - 11.7) < 0.000001)),
                Times.Once);
            Assert.AreEqual(odrediste, rezultat.ActionName);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Post_ConcurrencyIObrisanaRezervacija_VracaNotFound()
        {
            _ok.Servis.SetupSequence(s => s.GetByIdAsync(1))
                .ReturnsAsync(_ok.Rezervacija)
                .ReturnsAsync((Rezervacija?)null);
            _ok.Servis.Setup(s => s.UpdateAsync(It.IsAny<Rezervacija>()))
                .ThrowsAsync(new DbUpdateConcurrencyException());

            var rezultat = await _ok.Kontroler.Edit(1, _ok.Rezervacija);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Edit_Post_ConcurrencyIPostojecaRezervacija_PropagiraIzuzetak()
        {
            var izuzetak = new DbUpdateConcurrencyException();
            _ok.Servis.Setup(s => s.UpdateAsync(It.IsAny<Rezervacija>()))
                .ThrowsAsync(izuzetak);

            var rezultat = await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => _ok.Kontroler.Edit(1, _ok.Rezervacija));

            Assert.AreSame(izuzetak, rezultat);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow(99)]
        [TestCategory("Stub")]
        public async Task Delete_Get_NeispravanId_VracaNotFound(int? id)
        {
            var rezultat = await _ok.Kontroler.Delete(id);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Delete_Get_TudjaRezervacija_VracaForbid()
        {
            _ok.Rezervacija.KorisnikId = "drugi-korisnik";

            var rezultat = await _ok.Kontroler.Delete(1);

            Assert.IsInstanceOfType(rezultat, typeof(ForbidResult));
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task Delete_Get_Vlasnik_VracaModel()
        {
            var rezultat = (ViewResult)await _ok.Kontroler.Delete(1);

            Assert.AreSame(_ok.Rezervacija, rezultat.Model);
        }

        [TestMethod]
        [TestCategory("Stub")]
        public async Task DeleteConfirmed_NemaRezervacije_VracaNotFound()
        {
            var rezultat = await _ok.Kontroler.DeleteConfirmed(99);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task DeleteConfirmed_TudjaRezervacija_NeSpremaIzmjenu()
        {
            _ok.Rezervacija.KorisnikId = "drugi-korisnik";

            var rezultat = await _ok.Kontroler.DeleteConfirmed(1);

            Assert.IsInstanceOfType(rezultat, typeof(ForbidResult));
            _ok.Servis.Verify(s => s.UpdateAsync(
                It.IsAny<Rezervacija>()), Times.Never);
        }

        [TestMethod]
        [DataRow(null, "Moje")]
        [DataRow("Administrator", "Index")]
        [DataRow("Operater", "Index")]
        [TestCategory("Mock")]
        public async Task DeleteConfirmed_OvlastenKorisnik_OtkazujeRezervaciju(
            string? uloga, string odrediste)
        {
            _ok.PostaviKorisnika(uloga);
            _ok.Rezervacija.EmailZaObavijest = "";

            var rezultat = (RedirectToActionResult)
                await _ok.Kontroler.DeleteConfirmed(1);

            _ok.Servis.Verify(s => s.UpdateAsync(
                It.Is<Rezervacija>(r =>
                    r.Id == 1 &&
                    r.StatusRezervacije == StatusRezervacije.Otkazana)),
                Times.Once);
            Assert.AreEqual(odrediste, rezultat.ActionName);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task DeleteConfirmed_PostojeciUspjesniUplatniZapis_RefundiraPlacanje()
        {
            var placanje = new Placanje { Id = 1, RezervacijaId = 1 };
            _ok.Placanja.Setup(s =>
                s.GetUspjesnoPlacanjeZaRezervacijuAsync(1))
                .ReturnsAsync(placanje);
            _ok.Rezervacija.EmailZaObavijest = "";

            await _ok.Kontroler.DeleteConfirmed(1);

            _ok.Placanja.Verify(s => s.UpdateAsync(
                It.Is<Placanje>(p =>
                    p.Id == 1 &&
                    p.StatusPlacanja == StatusPlacanja.Refundirano)),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("Mock")]
        public async Task DeleteConfirmed_PostojiEmail_SaljeObavijest()
        {
            await _ok.Kontroler.DeleteConfirmed(1);

            _ok.Email.Verify(s => s.SendEmailAsync(
                _ok.Rezervacija.EmailZaObavijest,
                It.IsAny<string>(),
                It.Is<string>(tekst =>
                    tekst.Contains("Otkazivanje rezervacije") &&
                    tekst.Contains(_ok.Rezervacija.RegistracijskeTablice))),
                Times.Once);
        }
    }
}