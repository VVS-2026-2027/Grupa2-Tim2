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
    private static ApplicationDbContext NapraviContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options);
    }

    private static Korisnik NapraviKorisnika(string id = "1") =>
        new() { Id = id, Ime = "Ajla", Prezime = "Test", UserName = $"user{id}", Email = $"user{id}@test.ba" };

    [TestMethod]
    public async Task ObrisiNalogAsync_BezAktivnihRezervacija_BriseNalog()
    {
        // Arrange
        using var context = NapraviContext();
        context.Users.Add(NapraviKorisnika());
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

    [TestMethod]
    public async Task ObrisiNalogAsync_CuvaHistorijuIAnonimizujePodatke()
    {
        // Arrange
        using var context = NapraviContext();

        var korisnik = NapraviKorisnika();
        korisnik.PhoneNumber = "061123456";
        context.Users.Add(korisnik);

        context.Rezervacije.Add(new Rezervacija
        {
            Id = 2,
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
        var sacuvanaRezervacija = await context.Rezervacije
            .FirstOrDefaultAsync(r => r.Id == 2);

        Assert.IsNotNull(sacuvanaRezervacija);
        Assert.AreEqual("1", sacuvanaRezervacija.KorisnikId);

        var anonimizovaniKorisnik = await context.Users
            .FirstOrDefaultAsync(k => k.Id == "1");

        Assert.IsNotNull(anonimizovaniKorisnik);
        Assert.AreNotEqual("Ajla", anonimizovaniKorisnik.Ime);
        Assert.AreNotEqual("Test", anonimizovaniKorisnik.Prezime);
        Assert.AreNotEqual("user1@test.ba", anonimizovaniKorisnik.Email);
        Assert.AreNotEqual("061123456", anonimizovaniKorisnik.PhoneNumber);
    }

}