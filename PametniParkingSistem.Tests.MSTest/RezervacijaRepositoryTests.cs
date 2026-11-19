using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Data;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class RezervacijaRepositoryTests
    {
        private ApplicationDbContext _kontekst = null!;
        private RezervacijaRepository _repozitorij = null!;

        [TestInitialize]
        public void PripremiTest()
        {
            _kontekst = InMemoryBaza.Kreiraj();
            _repozitorij = new RezervacijaRepository(_kontekst);

            _kontekst.Users.AddRange(
                new Korisnik
                {
                    Id = "korisnik-1",
                    Ime = "Samra",
                    Prezime = "Test"
                },
                new Korisnik
                {
                    Id = "korisnik-2",
                    Ime = "Drugi",
                    Prezime = "Korisnik"
                });

            _kontekst.ParkingZone.Add(new ParkingZona
            {
                Id = 1,
                Naziv = "Centar"
            });

            _kontekst.ParkingMjesta.AddRange(
                new ParkingMjesto
                {
                    Id = 1,
                    Oznaka = "A1",
                    ParkingZonaId = 1
                },
                new ParkingMjesto
                {
                    Id = 2,
                    Oznaka = "A2",
                    ParkingZonaId = 1
                });

            _kontekst.SaveChanges();
            _kontekst.ChangeTracker.Clear();
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
        public async Task GetAllAsync_NemaRezervacija_VracaPraznuListu()
        {
            var rezultat = await _repozitorij.GetAllAsync();

            Assert.AreEqual(0, rezultat.Count);
        }

        [TestMethod]
        public async Task GetAllAsync_SortiraOdNajnovijeRezervacije()
        {
            var starija = RezervacijaTestPodaci.Kreiraj(1);
            var novija = RezervacijaTestPodaci.Kreiraj(2);
            novija.DatumKreiranja = starija.DatumKreiranja.AddDays(1);

            _kontekst.Rezervacije.AddRange(starija, novija);
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var rezultat = await _repozitorij.GetAllAsync();

            CollectionAssert.AreEqual(
                new[] { 2, 1 },
                rezultat.Select(r => r.Id).ToArray());
        }

        [TestMethod]
        public async Task GetByIdAsync_PostojeciId_UcitavaRezervacijuIVeze()
        {
            _kontekst.Rezervacije.Add(
                RezervacijaTestPodaci.Kreiraj());
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var rezultat = await _repozitorij.GetByIdAsync(1);

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(1, rezultat.Id);
            Assert.IsNotNull(rezultat.Korisnik);
            Assert.AreEqual("korisnik-1", rezultat.Korisnik.Id);
            Assert.IsNotNull(rezultat.ParkingMjesto);
            Assert.AreEqual(1, rezultat.ParkingMjesto.Id);
        }

        [TestMethod]
        public async Task GetByIdAsync_NepostojeciId_VracaNull()
        {
            var rezultat = await _repozitorij.GetByIdAsync(99);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task GetByKorisnikIdAsync_FiltriraISortiraRezervacije()
        {
            var starija = RezervacijaTestPodaci.Kreiraj(1);
            var novija = RezervacijaTestPodaci.Kreiraj(2);
            novija.DatumKreiranja = starija.DatumKreiranja.AddDays(1);

            var tudja = RezervacijaTestPodaci.Kreiraj(
                3, korisnikId: "korisnik-2");

            _kontekst.Rezervacije.AddRange(starija, novija, tudja);
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var rezultat = await _repozitorij.GetByKorisnikIdAsync(
                "korisnik-1");

            CollectionAssert.AreEqual(
                new[] { 2, 1 },
                rezultat.Select(r => r.Id).ToArray());
        }

        [TestMethod]
        public async Task GetByKorisnikIdAsync_NemaRezervacija_VracaPraznuListu()
        {
            _kontekst.Rezervacije.Add(
                RezervacijaTestPodaci.Kreiraj());
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var rezultat = await _repozitorij.GetByKorisnikIdAsync(
                "korisnik-2");

            Assert.AreEqual(0, rezultat.Count);
        }

        [TestMethod]
        public async Task AddAsync_SpremaRezervaciju()
        {
            var rezervacija = RezervacijaTestPodaci.Kreiraj();

            await _repozitorij.AddAsync(rezervacija);
            _kontekst.ChangeTracker.Clear();

            var sacuvana = await _kontekst.Rezervacije
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(rezervacija.Id, sacuvana.Id);
            Assert.AreEqual(rezervacija.KorisnikId, sacuvana.KorisnikId);
            Assert.AreEqual(
                rezervacija.ParkingMjestoId, sacuvana.ParkingMjestoId);
            Assert.AreEqual(
                rezervacija.VrijemePocetka, sacuvana.VrijemePocetka);
            Assert.AreEqual(
                rezervacija.VrijemeKraja, sacuvana.VrijemeKraja);
            Assert.AreEqual(
                rezervacija.RegistracijskeTablice,
                sacuvana.RegistracijskeTablice);
        }

        [TestMethod]
        public async Task UpdateAsync_SpremaIzmjeneRezervacije()
        {
            _kontekst.Rezervacije.Add(
                RezervacijaTestPodaci.Kreiraj());
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var izmijenjena = RezervacijaTestPodaci.Kreiraj();
            izmijenjena.KontaktTelefon = "+38762111222";
            izmijenjena.UkupnaCijena = 8;
            izmijenjena.StatusRezervacije = StatusRezervacije.Placena;

            await _repozitorij.UpdateAsync(izmijenjena);
            _kontekst.ChangeTracker.Clear();

            var sacuvana = await _kontekst.Rezervacije
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual("+38762111222", sacuvana.KontaktTelefon);
            Assert.AreEqual(8.0, sacuvana.UkupnaCijena);
            Assert.AreEqual(
                StatusRezervacije.Placena, sacuvana.StatusRezervacije);
        }

        [TestMethod]
        public async Task DeleteAsync_BriseSamoOdabranuRezervaciju()
        {
            _kontekst.Rezervacije.AddRange(
                RezervacijaTestPodaci.Kreiraj(1),
                RezervacijaTestPodaci.Kreiraj(2));
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            await _repozitorij.DeleteAsync(1);
            _kontekst.ChangeTracker.Clear();

            var preostala = await _kontekst.Rezervacije
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(2, preostala.Id);
        }

        [TestMethod]
        public async Task DeleteAsync_NepostojeciId_ZadrzavaPodatke()
        {
            _kontekst.Rezervacije.Add(
                RezervacijaTestPodaci.Kreiraj());
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            await _repozitorij.DeleteAsync(99);
            _kontekst.ChangeTracker.Clear();

            var preostala = await _kontekst.Rezervacije
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(1, preostala.Id);
        }

        [TestMethod]
        [DataRow(-60, 0, 1, StatusRezervacije.Kreirana, false)]
        [DataRow(120, 180, 1, StatusRezervacije.Kreirana, false)]
        [DataRow(30, 90, 1, StatusRezervacije.Kreirana, true)]
        [DataRow(-60, 180, 1, StatusRezervacije.Kreirana, true)]
        [DataRow(0, 120, 1, StatusRezervacije.Kreirana, true)]
        [DataRow(30, 150, 1, StatusRezervacije.Kreirana, true)]
        [DataRow(30, 90, 2, StatusRezervacije.Kreirana, false)]
        [DataRow(30, 90, 1, StatusRezervacije.Otkazana, false)]
        [TestCategory("DataDriven")]
        public async Task PostojiPreklapanjeTerminaAsync_ProvjeravaGraniceIMjesto(
            int pocetakUMinutama,
            int krajUMinutama,
            int trazenoMjestoId,
            StatusRezervacije statusPostojece,
            bool ocekivanoPreklapanje)
        {
            var rezervacija = RezervacijaTestPodaci.Kreiraj();
            rezervacija.StatusRezervacije = statusPostojece;

            _kontekst.Rezervacije.Add(rezervacija);
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var rezultat = await _repozitorij.PostojiPreklapanjeTerminaAsync(
                trazenoMjestoId,
                rezervacija.VrijemePocetka.AddMinutes(pocetakUMinutama),
                rezervacija.VrijemePocetka.AddMinutes(krajUMinutama));

            Assert.AreEqual(ocekivanoPreklapanje, rezultat);
        }

        [TestMethod]
        [DataRow(StatusRezervacije.Kreirana, -24, true)]
        [DataRow(StatusRezervacije.Aktivna, -24, true)]
        [DataRow(StatusRezervacije.Otkazana, -24, false)]
        [DataRow(StatusRezervacije.Zavrsena, -24, false)]
        [DataRow(StatusRezervacije.Aktivna, 24, false)]
        [TestCategory("DataDriven")]
        public async Task GetIstekleAktivneRezervacijeAsync_FiltriraPoVremenuIStatusu(
            StatusRezervacije status,
            int pomakKrajaUSatima,
            bool trebaBitiURezultatu)
        {
            var sada = DateTime.Now;
            var rezervacija = RezervacijaTestPodaci.Kreiraj();
            rezervacija.StatusRezervacije = status;
            rezervacija.VrijemeKraja = sada.AddHours(pomakKrajaUSatima);
            rezervacija.VrijemePocetka =
                rezervacija.VrijemeKraja.AddHours(-2);

            _kontekst.Rezervacije.Add(rezervacija);
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var rezultat =
                await _repozitorij.GetIstekleAktivneRezervacijeAsync();

            Assert.AreEqual(
                trebaBitiURezultatu,
                rezultat.Any(r => r.Id == rezervacija.Id));
        }
    }
}