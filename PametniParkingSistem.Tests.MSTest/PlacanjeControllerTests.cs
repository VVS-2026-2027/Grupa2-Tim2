using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;
using PametniParkingSistem.ViewModels.Placanje;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    [TestCategory("Kontroler")]
    public class PlacanjeControllerTests
    {
        private const string NovaKljuc = "RezervacijaZaPlacanje";
        private const string IzmjenaKljuc = "IzmjenaRezervacijeZaPlacanje";
        private const string DoplataKljuc = "IznosDoplate";

        // Zamjenski objekti (Moq) za module od kojih kontroler zavisi.
        private Mock<IPlacanjeService> _placanjeServis = null!;
        private Mock<IRezervacijaService> _rezervacijaServis = null!;
        private Mock<IParkingMjestoService> _mjestoServis = null!;
        private Mock<IEmailSenderService> _emailServis = null!;
        private PlacanjeController _kontroler = null!;

        private Placanje? _snimljenoPlacanje;
        private (string Email, string Naslov)? _poslanEmail;

        [TestInitialize]
        public void Pripremi()
        {
            _placanjeServis = new Mock<IPlacanjeService>();
            _rezervacijaServis = new Mock<IRezervacijaService>();
            _mjestoServis = new Mock<IParkingMjestoService>();
            _emailServis = new Mock<IEmailSenderService>();

            _snimljenoPlacanje = null;
            _poslanEmail = null;

            _placanjeServis.Setup(s => s.AddAsync(It.IsAny<Placanje>()))
                .Callback<Placanje>(p => _snimljenoPlacanje = p)
                .Returns(Task.CompletedTask);

            _emailServis.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string, string>((email, naslov, _) => _poslanEmail = (email, naslov))
                .Returns(Task.CompletedTask);

            _kontroler = new PlacanjeController(
                _placanjeServis.Object, _rezervacijaServis.Object, _mjestoServis.Object, _emailServis.Object)
            {
                TempData = new TempDataDictionary(new DefaultHttpContext(), Mock.Of<ITempDataProvider>())
            };
        }

        [TestCleanup]
        public void Pocisti()
        {
            _kontroler.Dispose();
        }

        // ---------- pomoćne metode za pripremu TempData (bez logike u testovima) ----------

        private void PostaviNovuRezervaciju(Rezervacija rezervacija) =>
            _kontroler.TempData[NovaKljuc] = PlacanjePrimjeri.UJson(rezervacija);

        private void PostaviIzmjenu(Rezervacija izmjena, double doplata)
        {
            _kontroler.TempData[IzmjenaKljuc] = PlacanjePrimjeri.UJson(izmjena);
            _kontroler.TempData[DoplataKljuc] = doplata;
        }

        private static void ProvjeriPreusmjeravanje(IActionResult rezultat, string akcija, string kontroler)
        {
            var preusmjeravanje = rezultat as RedirectToActionResult;
            Assert.IsNotNull(preusmjeravanje, "Očekivano je preusmjeravanje.");
            Assert.AreEqual(akcija, preusmjeravanje.ActionName);
            Assert.AreEqual(kontroler, preusmjeravanje.ControllerName);
        }

        // =====================================================================
        //  Atribut autorizacije
        // =====================================================================

        [TestMethod]
        public void Kontroler_ImaAtributAuthorize()
        {
            var atributi = typeof(PlacanjeController).GetCustomAttributes(typeof(AuthorizeAttribute), true);

            Assert.AreEqual(1, atributi.Length);
        }

        // =====================================================================
        //  Index
        // =====================================================================

        [TestMethod]
        [TestCategory("Moq")]
        public async Task Index_ServisVracaPlacanja_VracaViewSaPlacanjima()
        {
            var placanja = new List<Placanje> { new Placanje { Id = 1 }, new Placanje { Id = 2 } };
            _placanjeServis.Setup(s => s.GetAllAsync()).ReturnsAsync(placanja);

            var rezultat = await _kontroler.Index() as ViewResult;

            Assert.IsNotNull(rezultat);
            Assert.AreSame(placanja, rezultat.Model);
        }

        // =====================================================================
        //  Create (GET) - nova rezervacija
        // =====================================================================

        [TestMethod]
        public async Task CreateGet_BezPodatakaUTempData_PreusmjeravaNaParkingMjesto()
        {
            var rezultat = await _kontroler.Create();

            ProvjeriPreusmjeravanje(rezultat, "Index", "ParkingMjesto");
        }

        [TestMethod]
        public async Task CreateGet_NovaRezervacijaPrazanJson_PreusmjeravaNaParkingMjesto()
        {
            _kontroler.TempData[NovaKljuc] = string.Empty;

            var rezultat = await _kontroler.Create();

            ProvjeriPreusmjeravanje(rezultat, "Index", "ParkingMjesto");
        }

        [TestMethod]
        public async Task CreateGet_NovaRezervacija_VracaViewSaIznosomRezervacije()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _mjestoServis.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(PlacanjePrimjeri.Mjesto());

            var rezultat = await _kontroler.Create() as ViewResult;

            var model = (PlacanjeRezervacijeViewModel)rezultat!.Model!;
            Assert.AreEqual(12.5, model.Iznos);
        }

        [TestMethod]
        public async Task CreateGet_NovaRezervacija_OznacavaTipPlacanjaNovaRezervacija()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _mjestoServis.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(PlacanjePrimjeri.Mjesto());

            await _kontroler.Create();

            Assert.AreEqual("NovaRezervacija", (string)_kontroler.ViewBag.TipPlacanja);
        }

        [TestMethod]
        public async Task CreateGet_NovaRezervacija_NazivMjestaSadrziZonuIOznaku()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _mjestoServis.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(PlacanjePrimjeri.Mjesto(zonaId: 1));

            await _kontroler.Create();

            Assert.AreEqual("Zona A - A-12", (string)_kontroler.ViewBag.ParkingMjestoNaziv);
        }

        [TestMethod]
        [TestCategory("DataRow")]
        [DataRow(1, "Zona A - A-12")]
        [DataRow(2, "Zona B - A-12")]
        [DataRow(3, "Zona C - A-12")]
        [DataRow(4, "VIP zona - A-12")]
        [DataRow(9, "Zona #9 - A-12")]
        public async Task CreateGet_NovaRezervacija_NazivZoneZavisiOdIdZone(int zonaId, string ocekivaniNaziv)
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _mjestoServis.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(PlacanjePrimjeri.Mjesto(zonaId));

            await _kontroler.Create();

            Assert.AreEqual(ocekivaniNaziv, (string)_kontroler.ViewBag.ParkingMjestoNaziv);
        }

        [TestMethod]
        public async Task CreateGet_NovaRezervacijaMjestoNePostoji_NazivJeMjestoSaIdem()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _mjestoServis.Setup(s => s.GetByIdAsync(5)).ReturnsAsync((ParkingMjesto?)null);

            await _kontroler.Create();

            Assert.AreEqual("Mjesto #5", (string)_kontroler.ViewBag.ParkingMjestoNaziv);
        }

        // =====================================================================
        //  Create (GET) - doplata
        // =====================================================================

        [TestMethod]
        public async Task CreateGet_IzmjenaPrazanJson_PreusmjeravaNaRezervacije()
        {
            _kontroler.TempData[IzmjenaKljuc] = string.Empty;

            var rezultat = await _kontroler.Create();

            ProvjeriPreusmjeravanje(rezultat, "Index", "Rezervacija");
        }

        [TestMethod]
        public async Task CreateGet_IzmjenaJsonNull_PreusmjeravaNaRezervacije()
        {
            _kontroler.TempData[IzmjenaKljuc] = "null";

            var rezultat = await _kontroler.Create();

            ProvjeriPreusmjeravanje(rezultat, "Index", "Rezervacija");
        }

        [TestMethod]
        public async Task CreateGet_Izmjena_VracaViewSaIznosomDoplate()
        {
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);

            var rezultat = await _kontroler.Create() as ViewResult;

            var model = (PlacanjeRezervacijeViewModel)rezultat!.Model!;
            Assert.AreEqual(10.0, model.Iznos);
        }

        [TestMethod]
        public async Task CreateGet_Izmjena_OznacavaTipPlacanjaDoplata()
        {
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);

            await _kontroler.Create();

            Assert.AreEqual("Doplata", (string)_kontroler.ViewBag.TipPlacanja);
        }

        // =====================================================================
        //  Create (POST) - neispravni podaci
        // =====================================================================

        [TestMethod]
        public async Task CreatePost_NeispravanModelState_VracaIstiModelUView()
        {
            var model = PlacanjePrimjeri.ValjanModel();
            _kontroler.ModelState.AddModelError("CVV", "CVV je obavezan.");

            var rezultat = await _kontroler.Create(model) as ViewResult;

            Assert.AreSame(model, rezultat!.Model);
        }

        [TestMethod]
        public async Task CreatePost_NeispravanModelStateNovaRezervacija_PostavljaIznosITipPlacanja()
        {
            var model = PlacanjePrimjeri.ValjanModel();
            model.Iznos = 0;
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _kontroler.ModelState.AddModelError("CVV", "CVV je obavezan.");

            await _kontroler.Create(model);

            Assert.AreEqual(12.5, model.Iznos);
            Assert.AreEqual("NovaRezervacija", (string)_kontroler.ViewBag.TipPlacanja);
        }

        [TestMethod]
        public async Task CreatePost_NeispravanModelStateDoplata_PostavljaIznosDoplateITipPlacanja()
        {
            var model = PlacanjePrimjeri.ValjanModel();
            model.Iznos = 0;
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);
            _kontroler.ModelState.AddModelError("CVV", "CVV je obavezan.");

            await _kontroler.Create(model);

            Assert.AreEqual(10.0, model.Iznos);
            Assert.AreEqual("Doplata", (string)_kontroler.ViewBag.TipPlacanja);
        }

        [TestMethod]
        public async Task CreatePost_NeispravanModelStatePrazanJson_NeMijenjaIznos()
        {
            var model = PlacanjePrimjeri.ValjanModel();
            _kontroler.TempData[NovaKljuc] = string.Empty;
            _kontroler.TempData[IzmjenaKljuc] = string.Empty;
            _kontroler.ModelState.AddModelError("CVV", "CVV je obavezan.");

            await _kontroler.Create(model);

            Assert.AreEqual(12.5, model.Iznos);
        }

        [TestMethod]
        public async Task CreatePost_NeispravanModelStateJsonNull_NeMijenjaIznos()
        {
            var model = PlacanjePrimjeri.ValjanModel();
            _kontroler.TempData[NovaKljuc] = "null";
            _kontroler.TempData[IzmjenaKljuc] = "null";
            _kontroler.ModelState.AddModelError("CVV", "CVV je obavezan.");

            await _kontroler.Create(model);

            Assert.AreEqual(12.5, model.Iznos);
        }

        [TestMethod]
        [DataRow("1234")]
        [DataRow("1234 5678 9")]
        [DataRow("")]
        public async Task CreatePost_PrekratakBrojKartice_DodajeGreskuNaBrojKartice(string brojKartice)
        {
            var model = PlacanjePrimjeri.ValjanModel(brojKartice);

            var rezultat = await _kontroler.Create(model) as ViewResult;

            Assert.AreSame(model, rezultat!.Model);
            Assert.AreEqual("Broj kartice nije ispravan.", _kontroler.ModelState["BrojKartice"]!.Errors.Single().ErrorMessage);
        }

        [TestMethod]
        [DataRow("")]
        [DataRow("1")]
        [DataRow("12")]
        public async Task CreatePost_PrekratakCvv_DodajeGreskuNaCvv(string cvv)
        {
            var model = PlacanjePrimjeri.ValjanModel();
            model.CVV = cvv;

            var rezultat = await _kontroler.Create(model) as ViewResult;

            Assert.AreSame(model, rezultat!.Model);
            Assert.AreEqual("CVV nije ispravan.", _kontroler.ModelState["CVV"]!.Errors.Single().ErrorMessage);
        }

        [TestMethod]
        public async Task CreatePost_NeispravnaKartica_NeSnimaPlacanje()
        {
            var model = PlacanjePrimjeri.ValjanModel("1234");

            await _kontroler.Create(model);

            _placanjeServis.Verify(s => s.AddAsync(It.IsAny<Placanje>()), Times.Never);
        }

        [TestMethod]
        public async Task CreatePost_BezPodatakaUTempData_PreusmjeravaNaParkingMjesto()
        {
            var rezultat = await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            ProvjeriPreusmjeravanje(rezultat, "Index", "ParkingMjesto");
        }

        // =====================================================================
        //  Create (POST) - plaćanje nove rezervacije
        // =====================================================================

        [TestMethod]
        public async Task CreatePost_NovaRezervacijaPrazanJson_PreusmjeravaNaParkingMjesto()
        {
            _kontroler.TempData[NovaKljuc] = string.Empty;

            var rezultat = await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            ProvjeriPreusmjeravanje(rezultat, "Index", "ParkingMjesto");
        }

        [TestMethod]
        public async Task CreatePost_NovaRezervacijaJsonNull_PreusmjeravaNaParkingMjesto()
        {
            _kontroler.TempData[NovaKljuc] = "null";

            var rezultat = await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            ProvjeriPreusmjeravanje(rezultat, "Index", "ParkingMjesto");
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task CreatePost_NovaRezervacija_SnimaRezervacijuSaStatusomAktivna()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            _rezervacijaServis.Verify(s => s.AddAsync(
                It.Is<Rezervacija>(r => r.StatusRezervacije == StatusRezervacije.Aktivna)), Times.Once);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task CreatePost_NovaRezervacija_SnimaUspjesnoPlacanjeSaIznosomRezervacije()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.AreEqual(StatusPlacanja.Uspjesno, _snimljenoPlacanje!.StatusPlacanja);
            Assert.AreEqual(12.5, _snimljenoPlacanje.Iznos);
            Assert.AreEqual("Sarah Test", _snimljenoPlacanje.ImeVlasnikaKartice);
        }

        [TestMethod]
        public async Task CreatePost_NovaRezervacija_PlacanjeSeVezeZaSnimljenuRezervaciju()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            // Zamjenski servis simulira bazu koja dodjeljuje Id novoj rezervaciji.
            _rezervacijaServis.Setup(s => s.AddAsync(It.IsAny<Rezervacija>()))
                .Callback<Rezervacija>(r => r.Id = 42)
                .Returns(Task.CompletedTask);

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.AreEqual(42, _snimljenoPlacanje!.RezervacijaId);
        }

        [TestMethod]
        [TestCategory("DataRow")]
        [DataRow("1234 5678 9012 3456", "**** **** **** 3456")]
        [DataRow("1234567890123456", "**** **** **** 3456")]
        [DataRow("4111 1111 1111 1111", "**** **** **** 1111")]
        [DataRow("5500000000000004", "**** **** **** 0004")]
        public async Task CreatePost_NovaRezervacija_BrojKarticeSeMaskira(string brojKartice, string ocekivanaMaska)
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel(brojKartice));

            Assert.AreEqual(ocekivanaMaska, _snimljenoPlacanje!.BrojKarticeMaskiran);
        }

        [TestMethod]
        public async Task CreatePost_NovaRezervacija_TransakcijskiBrojImaOsamVelikihZnakova()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            StringAssert.Matches(_snimljenoPlacanje!.TransakcijskiBroj, new System.Text.RegularExpressions.Regex("^[0-9A-F]{8}$"));
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task CreatePost_NovaRezervacija_SaljeEmailPotvrdeNaAdresuRezervacije()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.AreEqual(PlacanjePrimjeri.Email, _poslanEmail!.Value.Email);
            Assert.AreEqual("Potvrda rezervacije - Pametni Parking Sistem", _poslanEmail.Value.Naslov);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task CreatePost_NovaRezervacija_EmailSadrziQrKodIOznakuMjesta()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _mjestoServis.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(PlacanjePrimjeri.Mjesto());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            _emailServis.Verify(s => s.SendEmailAsync(
                PlacanjePrimjeri.Email,
                It.IsAny<string>(),
                It.Is<string>(telo => telo.Contains("REZERVACIJA-0-A-12-A12-B-345") && telo.Contains("api.qrserver.com"))),
                Times.Once);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task CreatePost_NovaRezervacija_MjestoPostajeRezervisano()
        {
            var mjesto = PlacanjePrimjeri.Mjesto();
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _mjestoServis.Setup(s => s.GetByIdAsync(5)).ReturnsAsync(mjesto);

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.AreEqual(StatusMjesta.Rezervisano, mjesto.Status);
            _mjestoServis.Verify(s => s.UpdateAsync(mjesto), Times.Once);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task CreatePost_NovaRezervacijaMjestoNePostoji_NeAzuriraMjesto()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _mjestoServis.Setup(s => s.GetByIdAsync(5)).ReturnsAsync((ParkingMjesto?)null);

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            _mjestoServis.Verify(s => s.UpdateAsync(It.IsAny<ParkingMjesto>()), Times.Never);
        }

        [TestMethod]
        public async Task CreatePost_NovaRezervacija_UklanjaPodatkeIzTempDataIPostavljaPoruku()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.IsFalse(_kontroler.TempData.ContainsKey(NovaKljuc));
            Assert.AreEqual("Rezervacija je uspješno plaćena i potvrđena.", _kontroler.TempData["Success"]);
        }

        [TestMethod]
        public async Task CreatePost_NovaRezervacija_PreusmjeravaNaListuRezervacija()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());

            var rezultat = await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            ProvjeriPreusmjeravanje(rezultat, "Index", "Rezervacija");
        }

        // ---------- izuzeci ----------

        [TestMethod]
        [TestCategory("Izuzetak")]
        public async Task CreatePost_SlanjeEmailaNeUspije_IzuzetakSePropagira()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _emailServis.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP nije dostupan"));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _kontroler.Create(PlacanjePrimjeri.ValjanModel()));
        }

        [TestMethod]
        [TestCategory("Izuzetak")]
        public async Task CreatePost_SnimanjeRezervacijeNeUspije_IzuzetakSePropagiraIPlacanjeSeNeSnima()
        {
            PostaviNovuRezervaciju(PlacanjePrimjeri.NovaRezervacija());
            _rezervacijaServis.Setup(s => s.AddAsync(It.IsAny<Rezervacija>()))
                .ThrowsAsync(new InvalidOperationException("Greška baze"));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _kontroler.Create(PlacanjePrimjeri.ValjanModel()));

            _placanjeServis.Verify(s => s.AddAsync(It.IsAny<Placanje>()), Times.Never);
        }

        // =====================================================================
        //  Create (POST) - doplata za izmjenu rezervacije
        // =====================================================================

        [TestMethod]
        public async Task CreatePost_IzmjenaPrazanJson_PreusmjeravaNaRezervacije()
        {
            _kontroler.TempData[IzmjenaKljuc] = string.Empty;

            var rezultat = await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            ProvjeriPreusmjeravanje(rezultat, "Index", "Rezervacija");
        }

        [TestMethod]
        public async Task CreatePost_IzmjenaJsonNull_PreusmjeravaNaRezervacije()
        {
            _kontroler.TempData[IzmjenaKljuc] = "null";

            var rezultat = await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            ProvjeriPreusmjeravanje(rezultat, "Index", "Rezervacija");
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task CreatePost_IzmjenaRezervacijaNePostoji_VracaNotFound()
        {
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);
            _rezervacijaServis.Setup(s => s.GetByIdAsync(7)).ReturnsAsync((Rezervacija?)null);

            var rezultat = await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task CreatePost_Izmjena_PrepisujePodatkePostojeceRezervacije()
        {
            var postojeca = PlacanjePrimjeri.PostojecaRezervacija();
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);
            _rezervacijaServis.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(postojeca);

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.AreEqual("NOVA-222", postojeca.RegistracijskeTablice);
            Assert.AreEqual("novo@parking.ba", postojeca.EmailZaObavijest);
            Assert.AreEqual("061999999", postojeca.KontaktTelefon);
            Assert.AreEqual(15.0, postojeca.UkupnaCijena);
            Assert.AreEqual(new DateTime(2030, 5, 10, 13, 0, 0), postojeca.VrijemeKraja);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task CreatePost_Izmjena_RezervacijaPostajeAktivnaISnimaSe()
        {
            var postojeca = PlacanjePrimjeri.PostojecaRezervacija();
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);
            _rezervacijaServis.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(postojeca);

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.AreEqual(StatusRezervacije.Aktivna, postojeca.StatusRezervacije);
            _rezervacijaServis.Verify(s => s.UpdateAsync(postojeca), Times.Once);
        }

        [TestMethod]
        public async Task CreatePost_Izmjena_PlacanjeImaIznosDoplateAVezanoJeZaRezervaciju()
        {
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);
            _rezervacijaServis.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(PlacanjePrimjeri.PostojecaRezervacija());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.AreEqual(10.0, _snimljenoPlacanje!.Iznos);
            Assert.AreEqual(7, _snimljenoPlacanje.RezervacijaId);
            Assert.AreEqual(StatusPlacanja.Uspjesno, _snimljenoPlacanje.StatusPlacanja);
        }

        [TestMethod]
        [TestCategory("DataRow")]
        [DataRow("1234 5678 9012 3456", "**** **** **** 3456")]
        [DataRow("9999888877776666", "**** **** **** 6666")]
        public async Task CreatePost_Izmjena_BrojKarticeSeMaskira(string brojKartice, string ocekivanaMaska)
        {
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);
            _rezervacijaServis.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(PlacanjePrimjeri.PostojecaRezervacija());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel(brojKartice));

            Assert.AreEqual(ocekivanaMaska, _snimljenoPlacanje!.BrojKarticeMaskiran);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task CreatePost_Izmjena_SaljeEmailPotvrdeDoplateNaNovuAdresu()
        {
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);
            _rezervacijaServis.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(PlacanjePrimjeri.PostojecaRezervacija());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.AreEqual("novo@parking.ba", _poslanEmail!.Value.Email);
            Assert.AreEqual("Potvrda doplate - Pametni Parking Sistem", _poslanEmail.Value.Naslov);
        }

        [TestMethod]
        public async Task CreatePost_Izmjena_UklanjaPodatkeIzTempDataIPostavljaPoruku()
        {
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);
            _rezervacijaServis.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(PlacanjePrimjeri.PostojecaRezervacija());

            await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            Assert.IsFalse(_kontroler.TempData.ContainsKey(IzmjenaKljuc));
            Assert.IsFalse(_kontroler.TempData.ContainsKey(DoplataKljuc));
            Assert.AreEqual("Doplata je uspješno izvršena i rezervacija je ažurirana.", _kontroler.TempData["Success"]);
        }

        [TestMethod]
        public async Task CreatePost_Izmjena_PreusmjeravaNaListuRezervacija()
        {
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);
            _rezervacijaServis.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(PlacanjePrimjeri.PostojecaRezervacija());

            var rezultat = await _kontroler.Create(PlacanjePrimjeri.ValjanModel());

            ProvjeriPreusmjeravanje(rezultat, "Index", "Rezervacija");
        }

        [TestMethod]
        [TestCategory("Izuzetak")]
        public async Task CreatePost_IzmjenaSlanjeEmailaNeUspije_IzuzetakSePropagira()
        {
            PostaviIzmjenu(PlacanjePrimjeri.IzmijenjenaRezervacija(), 10);
            _rezervacijaServis.Setup(s => s.GetByIdAsync(7)).ReturnsAsync(PlacanjePrimjeri.PostojecaRezervacija());
            _emailServis.Setup(s => s.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new InvalidOperationException("SMTP nije dostupan"));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => _kontroler.Create(PlacanjePrimjeri.ValjanModel()));
        }
    }
}
