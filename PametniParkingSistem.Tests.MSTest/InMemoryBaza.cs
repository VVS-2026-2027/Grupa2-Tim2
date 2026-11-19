using Microsoft.EntityFrameworkCore;
using PametniParkingSistem.Data;

namespace PametniParkingSistem.Tests.MSTest
{
    /// <summary>
    /// Zamjenska (in-memory) baza podataka. Prava SQL Server baza se u testovima ne koristi.
    /// Svaki poziv pravi novu, praznu bazu sa jedinstvenim imenom, pa su testovi međusobno neovisni.
    /// </summary>
    public static class InMemoryBaza
    {
        public static ApplicationDbContext Kreiraj()
        {
            var opcije = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(opcije);
        }
    }
}
