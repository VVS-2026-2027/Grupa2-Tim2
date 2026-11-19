using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Data;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class CjenovnikRepositoryTests
    {
        private ApplicationDbContext _kontekst = null!;
        private CjenovnikRepository _repozitorij = null!;

        [TestInitialize]
        public void PripremiTest()
        {
            _kontekst = InMemoryBaza.Kreiraj();
            _repozitorij = new CjenovnikRepository(_kontekst);
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
        public async Task GetAllAsync_PraznaBaza_VracaPraznuListu()
        {
            var rezultat = await _repozitorij.GetAllAsync();

            Assert.AreEqual(0, rezultat.Count);
        }

        [TestMethod]
        public async Task GetAllAsync_VracaSveSacuvaneCjenovnike()
        {
            _kontekst.Cjenovnici.AddRange(
                new Cjenovnik
                {
                    Id = 1,
                    CijenaPoSatu = 2,
                    VaziOd = new DateTime(2026, 1, 1),
                    VaziDo = new DateTime(2026, 6, 30)
                },
                new Cjenovnik
                {
                    Id = 2,
                    CijenaPoSatu = 3,
                    VaziOd = new DateTime(2026, 7, 1),
                    VaziDo = new DateTime(2026, 12, 31)
                });
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var rezultat = await _repozitorij.GetAllAsync();

            CollectionAssert.AreEquivalent(
                new[] { 1, 2 },
                rezultat.Select(c => c.Id).ToArray());
        }

        [TestMethod]
        public async Task GetByIdAsync_PostojeciId_VracaOdgovarajuciCjenovnik()
        {
            _kontekst.Cjenovnici.AddRange(
                new Cjenovnik { Id = 1, CijenaPoSatu = 2 },
                new Cjenovnik { Id = 2, CijenaPoSatu = 5 });
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var rezultat = await _repozitorij.GetByIdAsync(2);

            Assert.IsNotNull(rezultat);
            Assert.AreEqual(2, rezultat.Id);
            Assert.AreEqual(5.0, rezultat.CijenaPoSatu);
        }

        [TestMethod]
        public async Task GetByIdAsync_NepostojeciId_VracaNull()
        {
            _kontekst.Cjenovnici.Add(
                new Cjenovnik { Id = 1, CijenaPoSatu = 2 });
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var rezultat = await _repozitorij.GetByIdAsync(99);

            Assert.IsNull(rezultat);
        }

        [TestMethod]
        public async Task AddAsync_SpremaCjenovnikUBazu()
        {
            var cjenovnik = new Cjenovnik
            {
                CijenaPoSatu = 2.5,
                VaziOd = new DateTime(2026, 1, 1),
                VaziDo = new DateTime(2026, 12, 31)
            };

            await _repozitorij.AddAsync(cjenovnik);
            _kontekst.ChangeTracker.Clear();

            var sacuvani = await _kontekst.Cjenovnici
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(cjenovnik.Id, sacuvani.Id);
            Assert.AreEqual(2.5, sacuvani.CijenaPoSatu);
            Assert.AreEqual(new DateTime(2026, 1, 1), sacuvani.VaziOd);
            Assert.AreEqual(new DateTime(2026, 12, 31), sacuvani.VaziDo);
        }

        [TestMethod]
        public async Task UpdateAsync_SpremaIzmijenjeneVrijednosti()
        {
            _kontekst.Cjenovnici.Add(new Cjenovnik
            {
                Id = 1,
                CijenaPoSatu = 2,
                VaziOd = new DateTime(2026, 1, 1),
                VaziDo = new DateTime(2026, 6, 30)
            });
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            var izmijenjeni = new Cjenovnik
            {
                Id = 1,
                CijenaPoSatu = 4,
                VaziOd = new DateTime(2026, 7, 1),
                VaziDo = new DateTime(2026, 12, 31)
            };

            await _repozitorij.UpdateAsync(izmijenjeni);
            _kontekst.ChangeTracker.Clear();

            var sacuvani = await _kontekst.Cjenovnici
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(1, sacuvani.Id);
            Assert.AreEqual(4.0, sacuvani.CijenaPoSatu);
            Assert.AreEqual(new DateTime(2026, 7, 1), sacuvani.VaziOd);
            Assert.AreEqual(new DateTime(2026, 12, 31), sacuvani.VaziDo);
        }

        [TestMethod]
        public async Task DeleteAsync_PostojeciId_BriseSamoOdabraniCjenovnik()
        {
            _kontekst.Cjenovnici.AddRange(
                new Cjenovnik { Id = 1, CijenaPoSatu = 2 },
                new Cjenovnik { Id = 2, CijenaPoSatu = 3 });
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            await _repozitorij.DeleteAsync(1);
            _kontekst.ChangeTracker.Clear();

            var preostali = await _kontekst.Cjenovnici
                .AsNoTracking()
                .ToListAsync();

            Assert.AreEqual(1, preostali.Count);
            Assert.AreEqual(2, preostali[0].Id);
        }

        [TestMethod]
        public async Task DeleteAsync_NepostojeciId_ZadrzavaPostojecePodatke()
        {
            _kontekst.Cjenovnici.Add(
                new Cjenovnik { Id = 1, CijenaPoSatu = 2 });
            await _kontekst.SaveChangesAsync();
            _kontekst.ChangeTracker.Clear();

            await _repozitorij.DeleteAsync(99);
            _kontekst.ChangeTracker.Clear();

            var preostali = await _kontekst.Cjenovnici
                .AsNoTracking()
                .SingleAsync();

            Assert.AreEqual(1, preostali.Id);
            Assert.AreEqual(2.0, preostali.CijenaPoSatu);
        }
    }
}