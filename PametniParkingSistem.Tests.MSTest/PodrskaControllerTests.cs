using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Data;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;

namespace PametniParkingSistem.Tests.MSTest
{
    /// <summary>
    /// PodrskaController radi direktno s ApplicationDbContext-om, pa se koristi zamjenska (in-memory) baza.
    /// UserManager se zamjenjuje Moq objektom.
    /// </summary>
    [TestClass]
    [TestCategory("Kontroler")]
    public class PodrskaControllerTests
    {
        private ApplicationDbContext _kontekst = null!;
        private Mock<UserManager<Korisnik>> _userManager = null!;
        private PodrskaController _kontroler = null!;

        [TestInitialize]
        public void Pripremi()
        {
            _kontekst = InMemoryBaza.Kreiraj();
            _userManager = new Mock<UserManager<Korisnik>>(
                Mock.Of<IUserStore<Korisnik>>(), null, null, null, null, null, null, null, null);

            _kontroler = new PodrskaController(_kontekst, _userManager.Object)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };

            PrijaviKao("korisnik-1", "RegistrovaniKorisnik");
        }

        [TestCleanup]
        public void Pocisti()
        {
            _kontroler.Dispose();
            _kontekst.Dispose();
        }

        // ---------- pomoćne metode ----------

        private void PrijaviKao(string idKorisnika, params string[] uloge)
        {
            var tvrdnje = uloge.Select(u => new Claim(ClaimTypes.Role, u)).ToList();
            var principal = new ClaimsPrincipal(new ClaimsIdentity(tvrdnje, "TestAuth"));
            _kontroler.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
                .ReturnsAsync(new Korisnik { Id = idKorisnika, UserName = idKorisnika });
        }

        private void PrijaviNepostojecegKorisnika(params string[] uloge)
        {
            PrijaviKao("nebitno", uloge);
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((Korisnik?)null);
        }

        private void DodajKorisnike(params string[] ids)
        {
            foreach (var id in ids)
                _kontekst.Users.Add(new Korisnik { Id = id, UserName = id });
            _kontekst.SaveChanges();
        }

        private PodrskaZahtjev DodajZahtjev(string korisnikId, StatusPodrske status = StatusPodrske.Otvoren, int dan = 1)
        {
            var zahtjev = new PodrskaZahtjev
            {
                Naslov = "Naslov " + dan,
                Opis = "Opis",
                KorisnikId = korisnikId,
                Status = status,
                DatumKreiranja = new DateTime(2030, 1, dan)
            };
            _kontekst.PodrskaZahtjevi.Add(zahtjev);
            _kontekst.SaveChanges();
            return zahtjev;
        }

        // Četiri zahtjeva: 2x Otvoren, 1x UObradi, 1x Rijesen (dva korisnika).
        private void PripremiCetiriZahtjeva()
        {
            DodajKorisnike("korisnik-1", "korisnik-2");
            DodajZahtjev("korisnik-1", StatusPodrske.Otvoren, 1);
            DodajZahtjev("korisnik-1", StatusPodrske.UObradi, 2);
            DodajZahtjev("korisnik-2", StatusPodrske.Otvoren, 3);
            DodajZahtjev("korisnik-2", StatusPodrske.Rijesen, 4);
        }

        private static PodrskaZahtjev NoviZahtjevSaForme() => new PodrskaZahtjev
        {
            Naslov = "Ne mogu platiti",
            Opis = "Kartica se odbija.",
            Kategorija = KategorijaPodrske.Placanje,
            Prioritet = PrioritetPodrske.Visok
        };

        // =====================================================================
        //  Atributi
        // =====================================================================

        [TestMethod]
        public void Kontroler_ImaAtributAuthorize()
        {
            var atributi = typeof(PodrskaController).GetCustomAttributes(typeof(AuthorizeAttribute), true);

            Assert.AreEqual(1, atributi.Length);
        }

        // =====================================================================
        //  Index
        // =====================================================================

        [TestMethod]
        public async Task Index_Administrator_VidiSveZahtjeve()
        {
            PripremiCetiriZahtjeva();
            PrijaviKao("admin-1", "Administrator");

            var rezultat = await _kontroler.Index(null) as ViewResult;

            Assert.AreEqual(4, ((List<PodrskaZahtjev>)rezultat!.Model!).Count);
        }

        [TestMethod]
        public async Task Index_Operater_VidiSveZahtjeve()
        {
            PripremiCetiriZahtjeva();
            PrijaviKao("operater-1", "Operater");

            var rezultat = await _kontroler.Index(null) as ViewResult;

            Assert.AreEqual(4, ((List<PodrskaZahtjev>)rezultat!.Model!).Count);
        }

        [TestMethod]
        public async Task Index_ObicanKorisnik_VidiSamoSvojeZahtjeve()
        {
            PripremiCetiriZahtjeva();
            PrijaviKao("korisnik-1", "RegistrovaniKorisnik");

            var rezultat = await _kontroler.Index(null) as ViewResult;

            var model = (List<PodrskaZahtjev>)rezultat!.Model!;
            Assert.AreEqual(2, model.Count);
            Assert.IsTrue(model.All(z => z.KorisnikId == "korisnik-1"));
        }

        [TestMethod]
        public async Task Index_Zahtjevi_SortiraniSuOdNajnovijegPremaNajstarijem()
        {
            PripremiCetiriZahtjeva();
            PrijaviKao("admin-1", "Administrator");

            var rezultat = await _kontroler.Index(null) as ViewResult;

            var datumi = ((List<PodrskaZahtjev>)rezultat!.Model!).Select(z => z.DatumKreiranja.Day).ToArray();
            CollectionAssert.AreEqual(new[] { 4, 3, 2, 1 }, datumi);
        }

        [TestMethod]
        public async Task Index_KorisnikNePostoji_VracaUnauthorized()
        {
            PrijaviNepostojecegKorisnika("RegistrovaniKorisnik");

            var rezultat = await _kontroler.Index(null);

            Assert.IsInstanceOfType(rezultat, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public async Task Index_Filter_PostavljaViewBagStatus()
        {
            PrijaviKao("admin-1", "Administrator");

            await _kontroler.Index("UObradi");

            Assert.AreEqual("UObradi", (string)_kontroler.ViewBag.Status);
        }

        // ---------- DATA-DRIVEN (JSON) ----------

        [TestMethod]
        [Priority(1)]
        [TestCategory("DataDriven-JSON")]
        [Description("Status filtera i očekivani broj zahtjeva čitaju se iz TestData/podrska_filter_status.json")]
        [DynamicData(nameof(UcitavacTestnihPodataka.SlucajeviFilteraIzJsona),
            typeof(UcitavacTestnihPodataka))]
        public async Task Index_FilterPoStatusu_VracaOcekivaniBrojZahtjeva(string opis, string? status, int ocekivanoBroj)
        {
            PripremiCetiriZahtjeva();
            PrijaviKao("admin-1", "Administrator");

            var rezultat = await _kontroler.Index(status) as ViewResult;

            Assert.AreEqual(ocekivanoBroj, ((List<PodrskaZahtjev>)rezultat!.Model!).Count, opis);
        }

        [TestMethod]
        public async Task Index_ObicanKorisnikSaFilterom_VidiSamoSvojeZahtjevePoStatusu()
        {
            PripremiCetiriZahtjeva();
            PrijaviKao("korisnik-1", "RegistrovaniKorisnik");

            var rezultat = await _kontroler.Index("Otvoren") as ViewResult;

            var model = (List<PodrskaZahtjev>)rezultat!.Model!;
            Assert.AreEqual(1, model.Count);
        }

        // =====================================================================
        //  Create
        // =====================================================================

        [TestMethod]
        public void CreateGet_VracaViewSaPodrazumijevanimVrijednostima()
        {
            var rezultat = _kontroler.Create() as ViewResult;

            var model = (PodrskaZahtjev)rezultat!.Model!;
            Assert.AreEqual(PrioritetPodrske.Srednji, model.Prioritet);
            Assert.AreEqual(KategorijaPodrske.Ostalo, model.Kategorija);
        }

        [TestMethod]
        public async Task CreatePost_KorisnikNePostoji_VracaUnauthorized()
        {
            PrijaviNepostojecegKorisnika();

            var rezultat = await _kontroler.Create(NoviZahtjevSaForme());

            Assert.IsInstanceOfType(rezultat, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public async Task CreatePost_NeispravanModelState_VracaViewIBezSnimanja()
        {
            var zahtjev = NoviZahtjevSaForme();
            _kontroler.ModelState.AddModelError("Naslov", "Naslov je obavezan.");

            var rezultat = await _kontroler.Create(zahtjev) as ViewResult;

            Assert.AreSame(zahtjev, rezultat!.Model);
            Assert.AreEqual(0, _kontekst.PodrskaZahtjevi.Count());
        }

        [TestMethod]
        public async Task CreatePost_ValjanZahtjev_SnimaSeSaPodacimaKorisnika()
        {
            PrijaviKao("korisnik-1", "RegistrovaniKorisnik");

            await _kontroler.Create(NoviZahtjevSaForme());

            var snimljen = _kontekst.PodrskaZahtjevi.Single();
            Assert.AreEqual("korisnik-1", snimljen.KorisnikId);
            Assert.AreEqual(StatusPodrske.Otvoren, snimljen.Status);
            Assert.IsNull(snimljen.Odgovor);
            Assert.IsNull(snimljen.DatumOdgovora);
        }

        [TestMethod]
        public async Task CreatePost_ValjanZahtjev_PreusmjeravaNaIndexIPostavljaPoruku()
        {
            var rezultat = await _kontroler.Create(NoviZahtjevSaForme()) as RedirectToActionResult;

            Assert.AreEqual("Index", rezultat!.ActionName);
            Assert.AreEqual("Zahtjev za podršku je uspješno poslan.", _kontroler.TempData["Success"]);
        }

        [TestMethod]
        public async Task CreatePost_ZahtjevSaPostavljenimOdgovorom_OdgovorSePonistava()
        {
            var zahtjev = NoviZahtjevSaForme();
            zahtjev.Odgovor = "Lažan odgovor";
            zahtjev.DatumOdgovora = new DateTime(2030, 1, 1);

            await _kontroler.Create(zahtjev);

            var snimljen = _kontekst.PodrskaZahtjevi.Single();
            Assert.IsNull(snimljen.Odgovor);
            Assert.IsNull(snimljen.DatumOdgovora);
        }

        // =====================================================================
        //  Details
        // =====================================================================

        [TestMethod]
        public async Task Details_IdJeNull_VracaNotFound()
        {
            var rezultat = await _kontroler.Details(null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_ZahtjevNePostoji_VracaNotFound()
        {
            var rezultat = await _kontroler.Details(404);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_Vlasnik_VidiSvojZahtjev()
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");
            PrijaviKao("korisnik-1", "RegistrovaniKorisnik");

            var rezultat = await _kontroler.Details(zahtjev.Id) as ViewResult;

            Assert.AreEqual(zahtjev.Id, ((PodrskaZahtjev)rezultat!.Model!).Id);
        }

        [TestMethod]
        public async Task Details_DrugiKorisnik_VracaForbid()
        {
            DodajKorisnike("korisnik-1", "korisnik-2");
            var zahtjev = DodajZahtjev("korisnik-1");
            PrijaviKao("korisnik-2", "RegistrovaniKorisnik");

            var rezultat = await _kontroler.Details(zahtjev.Id);

            Assert.IsInstanceOfType(rezultat, typeof(ForbidResult));
        }

        [TestMethod]
        public async Task Details_KorisnikNePostoji_VracaForbid()
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");
            PrijaviNepostojecegKorisnika("RegistrovaniKorisnik");

            var rezultat = await _kontroler.Details(zahtjev.Id);

            Assert.IsInstanceOfType(rezultat, typeof(ForbidResult));
        }

        [TestMethod]
        [DataRow("Administrator")]
        [DataRow("Operater")]
        public async Task Details_AdministratorIliOperater_VidiTudZahtjev(string uloga)
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");
            PrijaviKao("osoblje-1", uloga);

            var rezultat = await _kontroler.Details(zahtjev.Id);

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        }

        // =====================================================================
        //  Odgovori (GET)
        // =====================================================================

        [TestMethod]
        public async Task OdgovoriGet_IdJeNull_VracaNotFound()
        {
            var rezultat = await _kontroler.Odgovori((int?)null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task OdgovoriGet_ZahtjevNePostoji_VracaNotFound()
        {
            var rezultat = await _kontroler.Odgovori((int?)404);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task OdgovoriGet_ZahtjevPostoji_VracaViewSaZahtjevom()
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");

            var rezultat = await _kontroler.Odgovori((int?)zahtjev.Id) as ViewResult;

            Assert.AreEqual(zahtjev.Id, ((PodrskaZahtjev)rezultat!.Model!).Id);
        }

        // =====================================================================
        //  Odgovori (POST)
        // =====================================================================

        [TestMethod]
        public async Task OdgovoriPost_ZahtjevNePostoji_VracaNotFound()
        {
            var rezultat = await _kontroler.Odgovori(404, "Odgovor", StatusPodrske.Rijesen);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow(default(string))]
        public async Task OdgovoriPost_PrazanOdgovor_PostavljaGreskuIVracaNaFormu(string? odgovor)
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");

            var rezultat = await _kontroler.Odgovori(zahtjev.Id, odgovor!, StatusPodrske.Rijesen) as RedirectToActionResult;

            Assert.AreEqual("Odgovori", rezultat!.ActionName);
            Assert.AreEqual("Odgovor ne može biti prazan.", _kontroler.TempData["Error"]);
        }

        [TestMethod]
        public async Task OdgovoriPost_PrazanOdgovor_NeMijenjaZahtjev()
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");

            await _kontroler.Odgovori(zahtjev.Id, "  ", StatusPodrske.Rijesen);

            var izBaze = _kontekst.PodrskaZahtjevi.Single();
            Assert.AreEqual(StatusPodrske.Otvoren, izBaze.Status);
            Assert.IsNull(izBaze.Odgovor);
        }

        [TestMethod]
        public async Task OdgovoriPost_ValjanOdgovor_SnimaOdgovorStatusIDatum()
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");

            await _kontroler.Odgovori(zahtjev.Id, "  Problem je riješen.  ", StatusPodrske.Rijesen);

            var izBaze = _kontekst.PodrskaZahtjevi.Single();
            Assert.AreEqual("Problem je riješen.", izBaze.Odgovor);
            Assert.AreEqual(StatusPodrske.Rijesen, izBaze.Status);
            Assert.IsNotNull(izBaze.DatumOdgovora);
        }

        [TestMethod]
        public async Task OdgovoriPost_ValjanOdgovor_PreusmjeravaNaIndexSaStatusomFiltera()
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");

            var rezultat = await _kontroler.Odgovori(zahtjev.Id, "Odgovor", StatusPodrske.UObradi) as RedirectToActionResult;

            Assert.AreEqual("Index", rezultat!.ActionName);
            Assert.AreEqual("UObradi", rezultat.RouteValues!["status"]);
        }

        [TestMethod]
        [DataRow(StatusPodrske.Otvoren, "Zahtjev je ažuriran. Novi status: Otvoren.")]
        [DataRow(StatusPodrske.UObradi, "Zahtjev je ažuriran. Novi status: U obradi.")]
        [DataRow(StatusPodrske.Rijesen, "Zahtjev je ažuriran. Novi status: Riješen.")]
        [DataRow(StatusPodrske.Zatvoren, "Zahtjev je ažuriran. Novi status: Zatvoren.")]
        [DataRow((StatusPodrske)99, "Zahtjev je ažuriran. Novi status: 99.")]
        public async Task OdgovoriPost_PorukaSadrziFormatiranStatus(StatusPodrske status, string ocekivanaPoruka)
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");

            await _kontroler.Odgovori(zahtjev.Id, "Odgovor", status);

            Assert.AreEqual(ocekivanaPoruka, _kontroler.TempData["Success"]);
        }

        // =====================================================================
        //  Obrisi
        // =====================================================================

        [TestMethod]
        public async Task Obrisi_ZahtjevNePostoji_VracaNotFound()
        {
            var rezultat = await _kontroler.Obrisi(404);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Obrisi_ZahtjevPostoji_BriseGaIzBaze()
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");

            await _kontroler.Obrisi(zahtjev.Id);

            Assert.AreEqual(0, _kontekst.PodrskaZahtjevi.Count());
        }

        [TestMethod]
        public async Task Obrisi_ZahtjevPostoji_PreusmjeravaNaIndexIPostavljaPoruku()
        {
            DodajKorisnike("korisnik-1");
            var zahtjev = DodajZahtjev("korisnik-1");

            var rezultat = await _kontroler.Obrisi(zahtjev.Id) as RedirectToActionResult;

            Assert.AreEqual("Index", rezultat!.ActionName);
            Assert.AreEqual("Zahtjev za podršku je uspješno obrisan.", _kontroler.TempData["Success"]);
        }

        [TestMethod]
        public async Task Obrisi_ViseZahtjeva_BriseSamoTrazeni()
        {
            PripremiCetiriZahtjeva();
            var id = _kontekst.PodrskaZahtjevi.First().Id;

            await _kontroler.Obrisi(id);

            Assert.AreEqual(3, _kontekst.PodrskaZahtjevi.Count());
        }

        // =====================================================================
        //  Atributi autorizacije po akcijama
        // =====================================================================

        [TestMethod]
        [DataRow(nameof(PodrskaController.Obrisi), "Administrator")]
        [DataRow(nameof(PodrskaController.Odgovori), "Administrator,Operater")]
        public void Akcija_ImaOgranicenjeNaOdredjeneUloge(string imeAkcije, string ocekivaneUloge)
        {
            var atributi = typeof(PodrskaController).GetMethods()
                .Where(m => m.Name == imeAkcije)
                .SelectMany(m => m.GetCustomAttributes(typeof(AuthorizeAttribute), false))
                .Cast<AuthorizeAttribute>();

            Assert.IsTrue(atributi.Any());
            Assert.IsTrue(atributi.All(a => a.Roles == ocekivaneUloge));
        }
    }
}
