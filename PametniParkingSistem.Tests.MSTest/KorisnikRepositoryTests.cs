using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Data;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;
using PametniParkingSistem.Enums;

namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class KorisnikRepositoryTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new ApplicationDbContext(options);
    }

    private static Korisnik CreateUser(string id = "1") =>
        new() { Id = id, Ime = "Ajla", Prezime = "Test", UserName = $"user{id}", Email = $"user{id}@test.ba" };

    [TestMethod]
    public async Task AddAsync_WhenUserIsValid_PersistsUser()
    {
        using var context = CreateContext();
        var repository = new KorisnikRepository(context);
        await repository.AddAsync(CreateUser());
        Assert.AreEqual(1, await context.Users.CountAsync());
    }

    [TestMethod]
    public async Task GetAllAsync_WhenUsersExist_ReturnsAllUsers()
    {
        using var context = CreateContext();
        context.Users.AddRange(CreateUser("1"), CreateUser("2"));
        await context.SaveChangesAsync();
        var result = await new KorisnikRepository(context).GetAllAsync();
        Assert.AreEqual(2, result.Count);
    }

    [TestMethod]
    public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
    {
        using var context = CreateContext();
        var result = await new KorisnikRepository(context).GetAllAsync();
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUser()
    {
        using var context = CreateContext();
        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();
        var result = await new KorisnikRepository(context).GetByIdAsync("1");
        Assert.AreEqual("Ajla", result?.Ime);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        using var context = CreateContext();
        var result = await new KorisnikRepository(context).GetByIdAsync("404");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task UpdateAsync_WhenUserExists_PersistsChanges()
    {
        using var context = CreateContext();
        var user = CreateUser();
        context.Users.Add(user);
        await context.SaveChangesAsync();
        user.Ime = "Novo";
        await new KorisnikRepository(context).UpdateAsync(user);
        Assert.AreEqual("Novo", (await context.Users.FindAsync("1"))?.Ime);
    }

    [TestMethod]
    public async Task DeleteAsync_WhenUserExists_RemovesUser()
    {
        using var context = CreateContext();
        context.Users.Add(CreateUser());
        await context.SaveChangesAsync();
        await new KorisnikRepository(context).DeleteAsync("1");
        Assert.AreEqual(0, await context.Users.CountAsync());
    }

    //za potrebe implementiranja Funkcionalnosti 2, mijenjamo ovaj test, Task 2 i stanje prije promjene je već zabilježeno na gitu u Task2
    [TestMethod]
    public async Task DeleteAsync_WhenUserDoesNotExist_ThrowsException()
    {
        using var context = CreateContext();
        var repository = new KorisnikRepository(context);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => repository.DeleteAsync("404"));
    }

    [TestMethod]
    public async Task DeleteAsync_WhenUserHasActiveReservations_ThrowsException()
    {
        using var context = CreateContext();
        context.Users.Add(CreateUser());
        context.Rezervacije.Add(new Rezervacija
        {
            Id = 1,
            KorisnikId = "1",
            VrijemePocetka = new DateTime(2026, 11, 20, 10, 0, 0),
            VrijemeKraja = new DateTime(2026, 11, 20, 12, 0, 0),
            StatusRezervacije = StatusRezervacije.Aktivna
        });
        await context.SaveChangesAsync();
        var repository = new KorisnikRepository(context);

        var izuzetak = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => repository.DeleteAsync("1"));

        Assert.AreEqual("Korisnik ima aktivne rezervacije.", izuzetak.Message);
        Assert.AreEqual(1, await context.Users.CountAsync());
    }
}
