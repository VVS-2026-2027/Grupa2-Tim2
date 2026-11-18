using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using PametniParkingSistem.Data;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class RecenzijaRepositoryTests
    {
        private ApplicationDbContext KreirajContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private void DodajPovezanePodatke(
            ApplicationDbContext context,
            params int[] rezervacijaIds)
        {
            var korisnik = new Korisnik
            {
                Id = "korisnik1",
                UserName = "testkorisnik",
                Email = "test@test.com",
                Ime = "Test",
                Prezime = "Korisnik"
            };

            context.Users.Add(korisnik);

            foreach (var id in rezervacijaIds)
            {
                var parkingMjesto = new ParkingMjesto
                {
                    Id = id,
                    Oznaka = $"P-{id}",
                    ParkingZonaId = 1
                };

                var rezervacija = new Rezervacija
                {
                    Id = id,
                    KorisnikId = korisnik.Id,
                    ParkingMjestoId = parkingMjesto.Id,
                    Korisnik = korisnik,
                    ParkingMjesto = parkingMjesto,
                    RegistracijskeTablice = "A12-B-345",
                    KontaktTelefon = "061123456",
                    EmailZaObavijest = "test@test.com",
                    DatumKreiranja = DateTime.Today,
                    VrijemePocetka = DateTime.Today,
                    VrijemeKraja = DateTime.Today.AddHours(1)
                };

                context.ParkingMjesta.Add(parkingMjesto);
                context.Rezervacije.Add(rezervacija);
            }
        }

        private Recenzija KreirajRecenziju(
            int id,
            int rezervacijaId,
            int ocjena,
            DateTime datum,
            bool obrisan = false)
        {
            return new Recenzija
            {
                Id = id,
                RezervacijaId = rezervacijaId,
                Ocjena = ocjena,
                Komentar = "Odlična usluga",
                Datum = datum,
                Obrisan = obrisan,
                KorisnikId = "korisnik1"
            };
        }

        [TestMethod]
        public async Task DohvatiSveAsync_VracaSamoNeobrisaneRecenzijeSortiranePoDatumu()
        {
            using var context = KreirajContext();

            DodajPovezanePodatke(context, 10, 11, 12);

            context.Recenzije.AddRange(
                KreirajRecenziju(1, 10, 5, new DateTime(2026, 1, 1)),
                KreirajRecenziju(2, 11, 4, new DateTime(2026, 3, 1)),
                KreirajRecenziju(3, 12, 2, new DateTime(2026, 2, 1), true)
            );

            await context.SaveChangesAsync();

            var repository = new RecenzijaRepository(context);

            var rezultat = await repository.GetAllAsync();

            Assert.AreEqual(2, rezultat.Count);
            Assert.AreEqual(2, rezultat[0].Id);
            Assert.AreEqual(1, rezultat[1].Id);
        }

        [TestMethod]
        public async Task DohvatiPoIdAsync_KadaNeobrisanaRecenzijaPostoji_VracaRecenziju()
        {
            using var context = KreirajContext();

            DodajPovezanePodatke(context, 10);

            var recenzija = KreirajRecenziju(
                1, 10, 5, DateTime.Today);

            context.Recenzije.Add(recenzija);
            await context.SaveChangesAsync();

            var repository = new RecenzijaRepository(context);

            var rezultat = await repository.GetByIdAsync(1);

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(1, rezultat.Id);
        }

        [TestMethod]
        public async Task DohvatiPoIdAsync_KadaJeRecenzijaObrisana_VracaNull()
        {
            using var context = KreirajContext();

            DodajPovezanePodatke(context, 10);

            context.Recenzije.Add(
                KreirajRecenziju(1, 10, 5, DateTime.Today, true));

            await context.SaveChangesAsync();

            var repository = new RecenzijaRepository(context);

            var rezultat = await repository.GetByIdAsync(1);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task DohvatiPoIdRezervacijeAsync_KadaRecenzijaPostoji_VracaRecenzijuZaRezervaciju()
        {
            using var context = KreirajContext();

            DodajPovezanePodatke(context, 10);

            context.Recenzije.Add(
                KreirajRecenziju(1, 10, 5, DateTime.Today));

            await context.SaveChangesAsync();

            var repository = new RecenzijaRepository(context);

            var rezultat = await repository.GetByRezervacijaIdAsync(10);

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(10, rezultat.RezervacijaId);
        }

        [TestMethod]
        public async Task ProvjeriPostojanjeZaRezervacijuAsync_KadaRecenzijaPostoji_VracaTacno()
        {
            using var context = KreirajContext();

            context.Recenzije.Add(
                KreirajRecenziju(1, 10, 5, DateTime.Today));

            await context.SaveChangesAsync();

            var repository = new RecenzijaRepository(context);

            var rezultat = await repository.ExistsForRezervacijaAsync(10);

            Assert.IsTrue(rezultat);
        }

        [TestMethod]
        public async Task ProvjeriPostojanjeZaRezervacijuAsync_KadaRecenzijaNePostoji_VracaNetacno()
        {
            using var context = KreirajContext();

            var repository = new RecenzijaRepository(context);

            var rezultat = await repository.ExistsForRezervacijaAsync(99);

            Assert.IsFalse(rezultat);
        }

        [TestMethod]
        public async Task DodajAsync_KadaJeRecenzijaProslijedjena_DodajeRecenzijuUBazu()
        {
            using var context = KreirajContext();

            var repository = new RecenzijaRepository(context);
            var recenzija = KreirajRecenziju(
                1, 10, 5, DateTime.Today);

            await repository.AddAsync(recenzija);

            Assert.AreEqual(1, await context.Recenzije.CountAsync());
        }

        [TestMethod]
        public async Task AzurirajAsync_KadaRecenzijaPostoji_AzuriraRecenziju()
        {
            using var context = KreirajContext();

            var recenzija = KreirajRecenziju(
                1, 10, 3, DateTime.Today);

            context.Recenzije.Add(recenzija);
            await context.SaveChangesAsync();

            recenzija.Ocjena = 5;
            recenzija.Komentar = "Odlično";

            var repository = new RecenzijaRepository(context);

            await repository.UpdateAsync(recenzija);

            var rezultat = await context.Recenzije.FindAsync(1);

            Assert.AreEqual(5, rezultat!.Ocjena);
            Assert.AreEqual("Odlično", rezultat.Komentar);
        }

        [TestMethod]
        public async Task ObrisiAsync_KadaRecenzijaPostoji_MekoBriseRecenziju()
        {
            using var context = KreirajContext();

            context.Recenzije.Add(
                KreirajRecenziju(1, 10, 5, DateTime.Today));

            await context.SaveChangesAsync();

            var repository = new RecenzijaRepository(context);

            await repository.DeleteAsync(1);

            var rezultat = await context.Recenzije.FindAsync(1);

            Assert.IsNotNull(rezultat);
            Assert.IsTrue(rezultat.Obrisan);
        }

        [TestMethod]
        public async Task ObrisiAsync_KadaRecenzijaNePostoji_BazaOstajeNepromijenjena()
        {
            using var context = KreirajContext();

            var repository = new RecenzijaRepository(context);

            await repository.DeleteAsync(99);

            Assert.AreEqual(0, await context.Recenzije.CountAsync());
        }
    }
}