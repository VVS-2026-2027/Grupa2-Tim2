using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Data;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;
using PametniParkingSistem.Services;

namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class ObrisiNalogTests
{
    [TestMethod]
    public async Task ObrisiNalogAsync_BezAktivnihRezervacija_BriseNalog()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var context = new ApplicationDbContext(options);
        context.Users.Add(new Korisnik
        {
            Id = "1",
            Ime = "Ajla",
            Prezime = "Test",
            UserName = "user1",
            Email = "user1@test.ba"
        });
        context.Rezervacije.Add(new Rezervacija
        {
            Id = 1,
            KorisnikId = "1",
            VrijemePocetka = new DateTime(2026, 11, 10, 10, 0, 0),
            VrijemeKraja = new DateTime(2026, 11, 10, 12, 0, 0),
            StatusRezervacije = StatusRezervacije.Zavrsena
        });
        await context.SaveChangesAsync();
        var service = new KorisnikService(new KorisnikRepository(context));

        // Act
        await service.ObrisiNalogAsync("1");

        // Assert
        Assert.AreEqual(0, await context.Users.CountAsync());
    }
}
