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
    public class ParkingMjestoRepositoryTests
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

        private async Task UbaciUBazu(params ParkingMjesto[] mjesta)
        {
            using var ctx = NovaKonekcija();
            ctx.ParkingMjesta.AddRange(mjesta);
            await ctx.SaveChangesAsync();
        }

        [TestMethod]
        public async Task GetAllAsync_PraznaBaza_VracaPraznuListu()
        {
            using var ctx = NovaKonekcija();
            var repo = new ParkingMjestoRepository(ctx);

            var rezultat = await repo.GetAllAsync();

            Assert.AreEqual(0, rezultat.Count);
        }

        [TestMethod]
        public async Task GetAllAsync_UBaziDvaMjesta_VracaSvaMjesta()
        {
            await UbaciUBazu(
                new ParkingMjesto { Id = 1, Oznaka = "A1" },
                new ParkingMjesto { Id = 2, Oznaka = "A2" });
            using var ctx = NovaKonekcija();
            var repo = new ParkingMjestoRepository(ctx);

            var rezultat = await repo.GetAllAsync();

            Assert.AreEqual(2, rezultat.Count);
        }

        [TestMethod]
        public async Task GetByIdAsync_PostojeciId_VracaMjesto()
        {
            await UbaciUBazu(new ParkingMjesto { Id = 5, Oznaka = "C5" });
            using var ctx = NovaKonekcija();
            var repo = new ParkingMjestoRepository(ctx);

            var rezultat = await repo.GetByIdAsync(5);

            Assert.IsNotNull(rezultat);
            Assert.AreEqual("C5", rezultat.Oznaka);
        }

        [TestMethod]
        public async Task GetByIdAsync_NepostojeciId_VracaNull()
        {
            using var ctx = NovaKonekcija();
            var repo = new ParkingMjestoRepository(ctx);

            var rezultat = await repo.GetByIdAsync(404);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task AddAsync_NovoMjesto_UpisujeSeUBazu()
        {
            using (var ctx = NovaKonekcija())
            {
                var repo = new ParkingMjestoRepository(ctx);
                await repo.AddAsync(new ParkingMjesto { Id = 1, Oznaka = "NOVO" });
            }

            using var provjera = NovaKonekcija();
            Assert.AreEqual(1, await provjera.ParkingMjesta.CountAsync());
            Assert.AreEqual("NOVO", (await provjera.ParkingMjesta.FindAsync(1))!.Oznaka);
        }

        [TestMethod]
        public async Task UpdateAsync_PostojeceMjesto_MijenjaPodatke()
        {
            await UbaciUBazu(new ParkingMjesto { Id = 1, Oznaka = "STARO", CijenaPoSatu = 1 });

            using (var ctx = NovaKonekcija())
            {
                var repo = new ParkingMjestoRepository(ctx);
                await repo.UpdateAsync(new ParkingMjesto { Id = 1, Oznaka = "NOVO", CijenaPoSatu = 5 });
            }

            using var provjera = NovaKonekcija();
            var mjesto = await provjera.ParkingMjesta.FindAsync(1);
            Assert.AreEqual("NOVO", mjesto!.Oznaka);
            Assert.AreEqual(5, mjesto.CijenaPoSatu);
        }

        [TestMethod]
        public async Task DeleteAsync_PostojeciId_BriseMjestoIzBaze()
        {
            await UbaciUBazu(new ParkingMjesto { Id = 1, Oznaka = "A1" });

            using (var ctx = NovaKonekcija())
            {
                var repo = new ParkingMjestoRepository(ctx);
                await repo.DeleteAsync(1);
            }

            using var provjera = NovaKonekcija();
            Assert.AreEqual(0, await provjera.ParkingMjesta.CountAsync());
        }

        [TestMethod]
        public async Task DeleteAsync_NepostojeciId_NeMijenjaBazu()
        {
            await UbaciUBazu(new ParkingMjesto { Id = 1, Oznaka = "A1" });

            using (var ctx = NovaKonekcija())
            {
                var repo = new ParkingMjestoRepository(ctx);
                await repo.DeleteAsync(404);
            }

            using var provjera = NovaKonekcija();
            Assert.AreEqual(1, await provjera.ParkingMjesta.CountAsync());
        }
    }
}