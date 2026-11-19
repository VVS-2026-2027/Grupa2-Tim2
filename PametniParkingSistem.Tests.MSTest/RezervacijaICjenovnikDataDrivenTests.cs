using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Data;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class RezervacijaICjenovnikDataDrivenTests
    {
        private ApplicationDbContext _kontekst = null!;

        [ClassInitialize]
        public static void ProvjeriDatoteke(TestContext kontekst)
        {
            Assert.IsTrue(File.Exists(Path.Combine(
                AppContext.BaseDirectory, "TestData", "cjenovnici.csv")),
                "CSV datoteka nije kopirana u izlazni folder.");

            Assert.IsTrue(File.Exists(Path.Combine(
                AppContext.BaseDirectory, "TestData", "cjenovnici.xml")),
                "XML datoteka nije kopirana u izlazni folder.");
        }

        [TestInitialize]
        public void PripremiTest()
        {
            _kontekst = InMemoryBaza.Kreiraj();
        }

        [TestCleanup]
        public void OcistiTest()
        {
            try
            {
                _kontekst.Database.EnsureDeleted();
            }
            finally
            {
                _kontekst.Dispose();
            }
        }

        [TestMethod]
        [DynamicData(
            nameof(CjenovnikIzvoriPodataka.Csv),
            typeof(CjenovnikIzvoriPodataka))]
        [TestCategory("CSV")]
        public async Task DodavanjeCjenovnika_PodaciIzCsv(
            double cijena, DateTime od, DateTime doDatuma)
        {
            var repozitorij = new CjenovnikRepository(_kontekst);
            var cjenovnik = new Cjenovnik
            {
                CijenaPoSatu = cijena,
                VaziOd = od,
                VaziDo = doDatuma
            };

            await repozitorij.AddAsync(cjenovnik);
            _kontekst.ChangeTracker.Clear();

            var sacuvani = await _kontekst.Cjenovnici
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(cijena, sacuvani.CijenaPoSatu);
            Assert.AreEqual(od, sacuvani.VaziOd);
            Assert.AreEqual(doDatuma, sacuvani.VaziDo);
        }

        [TestMethod]
        [DynamicData(
            nameof(CjenovnikIzvoriPodataka.Xml),
            typeof(CjenovnikIzvoriPodataka))]
        [TestCategory("XML")]
        public async Task DodavanjeCjenovnika_PodaciIzXml(
            double cijena, DateTime od, DateTime doDatuma)
        {
            var repozitorij = new CjenovnikRepository(_kontekst);
            var cjenovnik = new Cjenovnik
            {
                CijenaPoSatu = cijena,
                VaziOd = od,
                VaziDo = doDatuma
            };

            await repozitorij.AddAsync(cjenovnik);
            _kontekst.ChangeTracker.Clear();

            var sacuvani = await _kontekst.Cjenovnici
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(cijena, sacuvani.CijenaPoSatu);
            Assert.AreEqual(od, sacuvani.VaziOd);
            Assert.AreEqual(doDatuma, sacuvani.VaziDo);
        }

        [TestMethod]
        [DynamicData(
            nameof(CjenovnikIzvoriPodataka.CjenovniciInline),
            typeof(CjenovnikIzvoriPodataka))]
        [TestCategory("Inline")]
        public async Task DodavanjeCjenovnika_InlinePodaci(
            double cijena, DateTime od, DateTime doDatuma)
        {
            var repozitorij = new CjenovnikRepository(_kontekst);
            var cjenovnik = new Cjenovnik
            {
                CijenaPoSatu = cijena,
                VaziOd = od,
                VaziDo = doDatuma
            };

            await repozitorij.AddAsync(cjenovnik);
            _kontekst.ChangeTracker.Clear();

            var sacuvani = await _kontekst.Cjenovnici
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(cijena, sacuvani.CijenaPoSatu);
            Assert.AreEqual(od, sacuvani.VaziOd);
            Assert.AreEqual(doDatuma, sacuvani.VaziDo);
        }

        [TestMethod]
        [DynamicData(
            nameof(CjenovnikIzvoriPodataka.RezervacijeInline),
            typeof(CjenovnikIzvoriPodataka))]
        [TestCategory("Inline")]
        public async Task DodavanjeRezervacije_InlinePodaci(
            string tablice, string telefon)
        {
            var repozitorij = new RezervacijaRepository(_kontekst);
            var rezervacija = RezervacijaTestPodaci.Kreiraj();
            rezervacija.RegistracijskeTablice = tablice;
            rezervacija.KontaktTelefon = telefon;

            await repozitorij.AddAsync(rezervacija);
            _kontekst.ChangeTracker.Clear();

            var sacuvana = await _kontekst.Rezervacije
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(tablice, sacuvana.RegistracijskeTablice);
            Assert.AreEqual(telefon, sacuvana.KontaktTelefon);
        }
    }
}
