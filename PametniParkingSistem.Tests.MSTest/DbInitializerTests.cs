using Microsoft.AspNetCore.Identity;
using Moq;
using PametniParkingSistem.Data;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;

namespace PametniParkingSistem.Tests.MSTest
{
    /// <summary>
    /// DbInitializer koristi RoleManager i UserManager. Oba se zamjenjuju Moq objektima,
    /// pa baza ni Identity nisu potrebni.
    /// </summary>
    [TestClass]
    [TestCategory("Seed")]
    public class DbInitializerTests
    {
        private const string AdminEmail = "admin@parking.ba";

        private Mock<RoleManager<IdentityRole>> _roleManager = null!;
        private Mock<UserManager<Korisnik>> _userManager = null!;
        private Mock<IServiceProvider> _servisi = null!;
        private Korisnik? _kreiraniAdmin;
        private string? _lozinkaAdmina;

        [TestInitialize]
        public void Pripremi()
        {
            _roleManager = new Mock<RoleManager<IdentityRole>>(
                Mock.Of<IRoleStore<IdentityRole>>(), null, null, null, null);
            _userManager = new Mock<UserManager<Korisnik>>(
                Mock.Of<IUserStore<Korisnik>>(), null, null, null, null, null, null, null, null);

            _kreiraniAdmin = null;
            _lozinkaAdmina = null;

            // Podrazumijevano: nijedna rola ni administrator ne postoje, sve operacije uspijevaju.
            _roleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(false);
            _roleManager.Setup(m => m.CreateAsync(It.IsAny<IdentityRole>())).ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.FindByEmailAsync(AdminEmail)).ReturnsAsync((Korisnik?)null);
            _userManager.Setup(m => m.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
                .Callback<Korisnik, string>((k, lozinka) => { _kreiraniAdmin = k; _lozinkaAdmina = lozinka; })
                .ReturnsAsync(IdentityResult.Success);
            _userManager.Setup(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            _servisi = new Mock<IServiceProvider>();
            _servisi.Setup(s => s.GetService(typeof(RoleManager<IdentityRole>))).Returns(_roleManager.Object);
            _servisi.Setup(s => s.GetService(typeof(UserManager<Korisnik>))).Returns(_userManager.Object);
        }

        [TestCleanup]
        public void Pocisti()
        {
            _servisi = null!;
        }

        // ---------- role ----------

        [TestMethod]
        [TestCategory("Moq")]
        [DataRow("Administrator")]
        [DataRow("Operater")]
        [DataRow("RegistrovaniKorisnik")]
        public async Task SeedRolesAndAdminAsync_RoleNePostoje_KreiraSvakuRolu(string imeRole)
        {
            await DbInitializer.SeedRolesAndAdminAsync(_servisi.Object);

            _roleManager.Verify(m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == imeRole)), Times.Once);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task SeedRolesAndAdminAsync_RoleVecPostoje_NeKreiraNijednu()
        {
            _roleManager.Setup(m => m.RoleExistsAsync(It.IsAny<string>())).ReturnsAsync(true);

            await DbInitializer.SeedRolesAndAdminAsync(_servisi.Object);

            _roleManager.Verify(m => m.CreateAsync(It.IsAny<IdentityRole>()), Times.Never);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task SeedRolesAndAdminAsync_PostojiSamoJednaRola_KreiraSamoPreostale()
        {
            _roleManager.Setup(m => m.RoleExistsAsync("Operater")).ReturnsAsync(true);

            await DbInitializer.SeedRolesAndAdminAsync(_servisi.Object);

            _roleManager.Verify(m => m.CreateAsync(It.IsAny<IdentityRole>()), Times.Exactly(2));
            _roleManager.Verify(m => m.CreateAsync(It.Is<IdentityRole>(r => r.Name == "Operater")), Times.Never);
        }

        // ---------- administrator ----------

        [TestMethod]
        [TestCategory("Moq")]
        public async Task SeedRolesAndAdminAsync_AdministratorNePostoji_KreiraGaSaPodrazumijevanomLozinkom()
        {
            await DbInitializer.SeedRolesAndAdminAsync(_servisi.Object);

            Assert.IsNotNull(_kreiraniAdmin);
            Assert.AreEqual("Admin123!", _lozinkaAdmina);
        }

        [TestMethod]
        public async Task SeedRolesAndAdminAsync_AdministratorNePostoji_KreiraGaSaIspravnimPodacima()
        {
            await DbInitializer.SeedRolesAndAdminAsync(_servisi.Object);

            Assert.AreEqual(AdminEmail, _kreiraniAdmin!.Email);
            Assert.AreEqual(AdminEmail, _kreiraniAdmin.UserName);
            Assert.IsTrue(_kreiraniAdmin.EmailConfirmed);
            Assert.AreEqual(Uloga.Administrator, _kreiraniAdmin.Uloga);
            Assert.AreEqual(StatusNaloga.Aktivan, _kreiraniAdmin.StatusNaloga);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task SeedRolesAndAdminAsync_AdministratorKreiran_DodjeljujeMuRoluAdministrator()
        {
            await DbInitializer.SeedRolesAndAdminAsync(_servisi.Object);

            _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), "Administrator"), Times.Once);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task SeedRolesAndAdminAsync_KreiranjeAdministratoraNeUspije_NeDodjeljujeRolu()
        {
            _userManager.Setup(m => m.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Lozinka je preslaba" }));

            await DbInitializer.SeedRolesAndAdminAsync(_servisi.Object);

            _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
        }

        [TestMethod]
        [TestCategory("Moq")]
        public async Task SeedRolesAndAdminAsync_AdministratorVecPostoji_NeKreiraNovog()
        {
            _userManager.Setup(m => m.FindByEmailAsync(AdminEmail)).ReturnsAsync(new Korisnik { Email = AdminEmail });

            await DbInitializer.SeedRolesAndAdminAsync(_servisi.Object);

            _userManager.Verify(m => m.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
            _userManager.Verify(m => m.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
        }

        // ---------- izuzeci ----------

        [TestMethod]
        [TestCategory("Izuzetak")]
        public async Task SeedRolesAndAdminAsync_RoleManagerNijeRegistrovan_BacaInvalidOperationException()
        {
            var prazniServisi = new Mock<IServiceProvider>();

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => DbInitializer.SeedRolesAndAdminAsync(prazniServisi.Object));
        }

        [TestMethod]
        [TestCategory("Izuzetak")]
        public async Task SeedRolesAndAdminAsync_KreiranjeRoleBacaIzuzetak_IzuzetakSePropagira()
        {
            _roleManager.Setup(m => m.CreateAsync(It.IsAny<IdentityRole>()))
                .ThrowsAsync(new InvalidOperationException("Baza nije dostupna"));

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(
                () => DbInitializer.SeedRolesAndAdminAsync(_servisi.Object));
        }
    }
}
