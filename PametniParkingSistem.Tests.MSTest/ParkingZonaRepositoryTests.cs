using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Data;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;
using System;
using System.Threading.Tasks;

namespace PametniParkingSistem.Tests.MSTest
{
    // InMemory baza služi kao zamjenski objekat za pravu bazu
    [TestClass]
    public class ParkingZonaRepositoryTests
    {
        private DbContextOptions<ApplicationDbContext> _options = null!;

        [TestInitialize]
        public void Postavi()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
        }

        private ApplicationDbContext NovaKonekcija() => new ApplicationDbContext(_options);

        private async Task UbaciUBazu(params ParkingZona[] zone)
        {
            using var ctx = NovaKonekcija();
            ctx.ParkingZone.AddRange(zone);
            await ctx.SaveChangesAsync();
        }

        [TestMethod]
        public async Task GetAllAsync_PraznaBaza_VracaPraznuListu()
        {
            using var ctx = NovaKonekcija();
            var repo = new ParkingZonaRepository(ctx);

            var rezultat = await repo.GetAllAsync();

            Assert.AreEqual(0, rezultat.Count);
        }

        [TestMethod]
        public async Task GetAllAsync_UBaziDvijeZone_VracaSveZone()
        {
            await UbaciUBazu(
                new ParkingZona { Id = 1, Naziv = "Centar" },
                new ParkingZona { Id = 2, Naziv = "Ilidža" });
            using var ctx = NovaKonekcija();
            var repo = new ParkingZonaRepository(ctx);

            var rezultat = await repo.GetAllAsync();

            Assert.AreEqual(2, rezultat.Count);
        }

        [TestMethod]
        public async Task GetByIdAsync_PostojeciId_VracaZonu()
        {
            await UbaciUBazu(new ParkingZona { Id = 5, Naziv = "Zona 5" });
            using var ctx = NovaKonekcija();
            var repo = new ParkingZonaRepository(ctx);

            var rezultat = await repo.GetByIdAsync(5);

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("Zona 5", rezultat.Naziv);
        }

        [TestMethod]
        public async Task GetByIdAsync_NepostojeciId_VracaNull()
        {
            using var ctx = NovaKonekcija();
            var repo = new ParkingZonaRepository(ctx);

            var rezultat = await repo.GetByIdAsync(404);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task AddAsync_NovaZona_UpisujeSeUBazu()
        {
            using (var ctx = NovaKonekcija())
            {
                var repo = new ParkingZonaRepository(ctx);
                await repo.AddAsync(new ParkingZona { Id = 1, Naziv = "NOVA" });
            }

            using var provjera = NovaKonekcija();
            Assert.AreEqual(1, await provjera.ParkingZone.CountAsync());
            Assert.AreEqual("NOVA", (await provjera.ParkingZone.FindAsync(1))!.Naziv);
        }

        [TestMethod]
        public async Task UpdateAsync_PostojecaZona_MijenjaPodatke()
        {
            await UbaciUBazu(new ParkingZona { Id = 1, Naziv = "STARA", ProsjecnaOcjena = 1 });

            using (var ctx = NovaKonekcija())
            {
                var repo = new ParkingZonaRepository(ctx);
                await repo.UpdateAsync(new ParkingZona { Id = 1, Naziv = "NOVA", ProsjecnaOcjena = 4.5 });
            }

            using var provjera = NovaKonekcija();
            var zona = await provjera.ParkingZone.FindAsync(1);
            Assert.AreEqual("NOVA", zona!.Naziv);
            Assert.AreEqual(4.5, zona.ProsjecnaOcjena);
        }

        [TestMethod]
        public async Task DeleteAsync_PostojeciId_BriseZonuIzBaze()
        {
            await UbaciUBazu(new ParkingZona { Id = 1, Naziv = "Zona" });

            using (var ctx = NovaKonekcija())
            {
                var repo = new ParkingZonaRepository(ctx);
                await repo.DeleteAsync(1);
            }

            using var provjera = NovaKonekcija();
            Assert.AreEqual(0, await provjera.ParkingZone.CountAsync());
        }

        [TestMethod]
        public async Task DeleteAsync_NepostojeciId_NeMijenjaBazu()
        {
            await UbaciUBazu(new ParkingZona { Id = 1, Naziv = "Zona" });

            using (var ctx = NovaKonekcija())
            {
                var repo = new ParkingZonaRepository(ctx);
                await repo.DeleteAsync(404);
            }

            using var provjera = NovaKonekcija();
            Assert.AreEqual(1, await provjera.ParkingZone.CountAsync());
        }
    }
}