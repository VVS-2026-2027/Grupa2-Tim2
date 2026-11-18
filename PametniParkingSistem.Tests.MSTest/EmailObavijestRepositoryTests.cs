using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.EntityFrameworkCore;
using PametniParkingSistem.Data;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class EmailObavijestRepositoryTests
    {
        private ApplicationDbContext KreirajContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private EmailObavijest KreirajEmailObavijest(int id)
        {
            return new EmailObavijest
            {
                Id = id,
                Primalac = $"korisnik{id}@test.com",
                Naslov = $"Obavijest {id}",
                Sadrzaj = $"Sadrzaj obavijesti {id}",
                DatumSlanja = new DateTime(2026, 11, id),
                TipObavijesti = TipObavijesti.KreiranjeRezervacije,
                StatusEmaila = StatusEmaila.NaCekanju
            };
        }

        [TestMethod]
        public async Task DohvatiSveAsync_KadaObavijestiPostoje_VracaSveObavijesti()
        {
            using var context = KreirajContext();

            context.EmailObavijesti.AddRange(
                KreirajEmailObavijest(1),
                KreirajEmailObavijest(2));

            await context.SaveChangesAsync();

            var repository = new EmailObavijestRepository(context);

            var rezultat = await repository.GetAllAsync();

            Assert.AreEqual(2, rezultat.Count);
        }

        [TestMethod]
        public async Task DohvatiPoIdAsync_KadaObavijestPostoji_VracaObavijest()
        {
            using var context = KreirajContext();

            context.EmailObavijesti.Add(KreirajEmailObavijest(1));
            await context.SaveChangesAsync();

            var repository = new EmailObavijestRepository(context);

            var rezultat = await repository.GetByIdAsync(1);

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(1, rezultat.Id);
            Assert.AreEqual("korisnik1@test.com", rezultat.Primalac);
        }

        [TestMethod]
        public async Task DohvatiPoIdAsync_KadaObavijestNePostoji_VracaNull()
        {
            using var context = KreirajContext();

            var repository = new EmailObavijestRepository(context);

            var rezultat = await repository.GetByIdAsync(99);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task DodajAsync_KadaJeObavijestIspravna_DodajeObavijestUBazu()
        {
            using var context = KreirajContext();

            var repository = new EmailObavijestRepository(context);
            var obavijest = KreirajEmailObavijest(1);

            await repository.AddAsync(obavijest);

            Assert.AreEqual(1, await context.EmailObavijesti.CountAsync());
        }

        [TestMethod]
        public async Task AzurirajAsync_KadaObavijestPostoji_AzuriraObavijest()
        {
            using var context = KreirajContext();

            var obavijest = KreirajEmailObavijest(1);

            context.EmailObavijesti.Add(obavijest);
            await context.SaveChangesAsync();

            obavijest.StatusEmaila = StatusEmaila.Poslan;
            obavijest.Naslov = "Izmijenjena obavijest";

            var repository = new EmailObavijestRepository(context);

            await repository.UpdateAsync(obavijest);

            var rezultat = await context.EmailObavijesti.FindAsync(1);

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(StatusEmaila.Poslan, rezultat.StatusEmaila);
            Assert.AreEqual("Izmijenjena obavijest", rezultat.Naslov);
        }

        [TestMethod]
        public async Task ObrisiAsync_KadaObavijestPostoji_BriseObavijest()
        {
            using var context = KreirajContext();

            context.EmailObavijesti.Add(KreirajEmailObavijest(1));
            await context.SaveChangesAsync();

            var repository = new EmailObavijestRepository(context);

            await repository.DeleteAsync(1);

            Assert.AreEqual(0, await context.EmailObavijesti.CountAsync());
        }

        [TestMethod]
        public async Task ObrisiAsync_KadaObavijestNePostoji_BazaOstajeNepromijenjena()
        {
            using var context = KreirajContext();

            var repository = new EmailObavijestRepository(context);

            await repository.DeleteAsync(99);

            Assert.AreEqual(0, await context.EmailObavijesti.CountAsync());
        }
    }
}