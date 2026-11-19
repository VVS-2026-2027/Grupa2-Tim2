using System.ComponentModel.DataAnnotations;
using PametniParkingSistem.ViewModels.Placanje;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    [TestCategory("ViewModel")]
    public class PlacanjeRezervacijeViewModelTests
    {
        [ClassInitialize]
        public static void ProvjeriDaPostojiCsv(TestContext context)
        {
            // Preduvjet za data-driven testove: CSV fajl mora biti kopiran uz testove.
            Assert.IsTrue(File.Exists(UcitavacTestnihPodataka.PutanjaKarticaCsv),
                "Nedostaje TestData/kartice_validacija.csv");
        }

        private PlacanjeRezervacijeViewModel _model = null!;

        [TestInitialize]
        public void Pripremi()
        {
            _model = PlacanjePrimjeri.ValjanModel();
        }

        [TestCleanup]
        public void Pocisti()
        {
            _model = null!;
        }

        private static List<ValidationResult> Validiraj(PlacanjeRezervacijeViewModel model, out bool valjan)
        {
            var rezultati = new List<ValidationResult>();
            valjan = Validator.TryValidateObject(model, new ValidationContext(model), rezultati, true);
            return rezultati;
        }

        // ---------- DATA-DRIVEN (CSV) ----------

        [TestMethod]
        [Priority(1)]
        [TestCategory("DataDriven-CSV")]
        [Description("Podaci kartice se čitaju iz TestData/kartice_validacija.csv")]
        [DynamicData(nameof(UcitavacTestnihPodataka.PodaciKarticaIzCsv),
            typeof(UcitavacTestnihPodataka))]
        public void Validacija_PodaciKartice_VracaOcekivaniRezultat(
            string ime, string brojKartice, string datumIsteka, string cvv, bool ocekivanoValjano)
        {
            var model = new PlacanjeRezervacijeViewModel
            {
                ImeVlasnikaKartice = ime,
                BrojKartice = brojKartice,
                DatumIsteka = datumIsteka,
                CVV = cvv
            };

            Validiraj(model, out var valjan);

            Assert.AreEqual(ocekivanoValjano, valjan);
        }

        // ---------- OSNOVNE FUNKCIONALNOSTI ----------

        [TestMethod]
        public void Validacija_ValjanModel_NemaGresaka()
        {
            var greske = Validiraj(_model, out var valjan);

            Assert.IsTrue(valjan);
            Assert.AreEqual(0, greske.Count);
        }

        [TestMethod]
        public void Validacija_IstekaoDatum_VracaPorukuODatumuIsteka()
        {
            _model.DatumIsteka = "01/20";

            var greske = Validiraj(_model, out _);

            Assert.AreEqual("Kartica je istekla. Unesite važeći datum isteka.", greske.Single().ErrorMessage);
        }

        [TestMethod]
        public void Validacija_IstekaoDatum_GreskaJeNaPoljuDatumIsteka()
        {
            _model.DatumIsteka = "01/20";

            var greske = Validiraj(_model, out _);

            CollectionAssert.Contains(greske.Single().MemberNames.ToList(), nameof(PlacanjeRezervacijeViewModel.DatumIsteka));
        }

        [TestMethod]
        public void Validacija_NeispravanMjesec_VracaPorukuOMjesecu()
        {
            _model.DatumIsteka = "13/99";

            var greske = Validiraj(_model, out _);

            Assert.AreEqual("Neispravan mjesec isteka kartice.", greske.Single().ErrorMessage);
        }

        [TestMethod]
        public void Validacija_DatumBezKose_VracaPorukuOFormatu()
        {
            _model.DatumIsteka = "1299";

            var greske = Validiraj(_model, out _);

            Assert.AreEqual("Unesite datum isteka u formatu MM/YY.", greske.Single().ErrorMessage);
        }

        // ---------- IZUZECI (neispravan format se hvata i pretvara u poruku) ----------

        [TestMethod]
        public void Validacija_DatumSaSlovima_NeBacaIzuzetakNegoVracaGresku()
        {
            _model.DatumIsteka = "ab/cd";

            var greske = Validiraj(_model, out _);

            Assert.AreEqual("Neispravan format datuma isteka.", greske.Single().ErrorMessage);
        }

        [TestMethod]
        public void Validacija_PrazanDatum_VracaPorukuOObaveznomPolju()
        {
            _model.DatumIsteka = string.Empty;

            var greske = Validiraj(_model, out _);

            Assert.AreEqual("Datum isteka je obavezan.", greske.Single().ErrorMessage);
        }

        [TestMethod]
        public void Validacija_ViseNeispravnihPolja_VracaGresku()
        {
            var model = new PlacanjeRezervacijeViewModel();

            Validiraj(model, out var valjan);

            Assert.IsFalse(valjan);
        }

        [TestMethod]
        public void NoviModel_PodrazumijevaneVrijednosti_SuPrazneIliNula()
        {
            var model = new PlacanjeRezervacijeViewModel();

            Assert.AreEqual(0.0, model.Iznos);
            Assert.AreEqual(string.Empty, model.BrojKartice);
            Assert.AreEqual(string.Empty, model.CVV);
        }
    }
}
