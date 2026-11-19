using System.Globalization;

namespace PametniParkingSistem.Tests.MSTest
{
    /// <summary>
    /// Zajednička postavka za cijeli testni projekat (postoji samo jednom, dijele je svi članovi tima).
    /// </summary>
    [TestClass]
    public static class PostavkeTestova
    {
        [AssemblyInitialize]
        public static void PostaviInvarijantnuKulturu(TestContext context)
        {
            // Isti format brojeva i datuma na svakom računaru, da testovi ne ovise o jezičkim postavkama.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        }
    }
}
