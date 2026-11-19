using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;
using System.Text;
namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class KorisnikControllerTests
{
    private Mock<IKorisnikService> _service = null!;
    private Mock<IRezervacijaService> _reservations = null!;
    private KorisnikController _controller = null!;
    private Mock<UserManager<Korisnik>> _manager = null!;
    private Mock<SignInManager<Korisnik>> _signIn = null!;

    [TestInitialize]
    public void Initialize()
    {
        _service = new Mock<IKorisnikService>();
        _reservations = new Mock<IRezervacijaService>();
        var store = new Mock<IUserStore<Korisnik>>();
        var manager = new Mock<UserManager<Korisnik>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var signIn = new Mock<SignInManager<Korisnik>>(
            manager.Object, new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<Korisnik>>().Object, Options.Create(new IdentityOptions()),
            new Mock<ILogger<SignInManager<Korisnik>>>().Object,
            new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<Korisnik>>().Object);
        _manager = manager;
        _signIn = signIn;
        _controller = new KorisnikController(_service.Object, manager.Object, signIn.Object, _reservations.Object);
        SetPrincipal();
        _controller.TempData = new Mock<ITempDataDictionary>().Object;
    }

    private void SetPrincipal(string? role = null, string? userId = null)
    {
        var claims = new List<Claim>();
        if (role != null) claims.Add(new Claim(ClaimTypes.Role, role));
        if (userId != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")) }
        };
    }

    [TestMethod]
    public async Task Index_WhenUsersExist_ReturnsViewWithUsers()
    {
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Korisnik> { User() });
        var result = await _controller.Index() as ViewResult;
        Assert.AreEqual(1, ((IEnumerable<Korisnik>)result!.Model!).Count());
    }

    [TestMethod]
    public async Task Index_WhenOperatorIsLoggedIn_HidesAdministrators()
    {
        SetPrincipal("Operater");
        _service.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<Korisnik>
        {
            User("1", Uloga.Administrator), User("2", Uloga.RegistrovaniKorisnik)
        });
        var result = await _controller.Index() as ViewResult;
        Assert.AreEqual(1, ((IEnumerable<Korisnik>)result!.Model!).Count());
    }

    [TestMethod]
    public async Task Details_WhenIdIsNull_ReturnsNotFound()
    {
        Assert.IsInstanceOfType(await _controller.Details(null), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Details_WhenUserDoesNotExist_ReturnsNotFound()
    {
        _service.Setup(s => s.GetByIdAsync("404")).ReturnsAsync((Korisnik?)null);
        Assert.IsInstanceOfType(await _controller.Details("404"), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Details_WhenUserExists_ReturnsView()
    {
        _service.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(User());
        Assert.IsInstanceOfType(await _controller.Details("1"), typeof(ViewResult));
    }

    [TestMethod]
    public async Task Details_WhenOperatorRequestsAdministrator_ReturnsForbid()
    {
        SetPrincipal("Operater");
        _service.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(User("1", Uloga.Administrator));
        Assert.IsInstanceOfType(await _controller.Details("1"), typeof(ForbidResult));
    }

    [TestMethod]
    public void Create_WhenRequested_ReturnsView()
    {
        Assert.IsInstanceOfType(_controller.Create(), typeof(ViewResult));
    }

    [TestMethod]
    public async Task Create_WhenRequiredNameIsMissing_ReturnsViewWithError()
    {
        var result = await _controller.Create(new Korisnik { Ime = "", Prezime = "Test", Email = "test@test.ba" });
        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Create_WhenDataIsValid_TrimsAndSavesUser()
    {
        var user = new Korisnik { Ime = " Ajla ", Prezime = " Test ", Email = " test@test.ba " };
        var result = await _controller.Create(user);
        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual("Ajla", user.Ime);
        _service.Verify(s => s.AddAsync(user), Times.Once);
    }

    [TestMethod]
    public async Task Edit_WhenIdIsNull_ReturnsNotFound()
    {
        Assert.IsInstanceOfType(await _controller.Edit((string?)null), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Edit_WhenUserExists_ReturnsView()
    {
        _service.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(User());
        Assert.IsInstanceOfType(await _controller.Edit((string?)"1"), typeof(ViewResult));
    }

    [TestMethod]
    public async Task Edit_WhenUserDoesNotExist_ReturnsNotFound()
    {
        Assert.IsInstanceOfType(await _controller.Edit((string?)"404"), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Edit_WhenOperatorRequestsAdministrator_ReturnsForbid()
    {
        SetPrincipal("Operater");
        _service.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(User("1", Uloga.Administrator));
        Assert.IsInstanceOfType(await _controller.Edit((string?)"1"), typeof(ForbidResult));
    }

    [TestMethod]
    public async Task Edit_WhenStatusChanges_UpdatesUser()
    {
        _service.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(User());
        var result = await _controller.Edit("1", StatusNaloga.Blokiran, null);
        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        _service.Verify(s => s.UpdateAsync(It.Is<Korisnik>(u => u.StatusNaloga == StatusNaloga.Blokiran)), Times.Once);
    }

    [TestMethod]
    public async Task Delete_WhenIdIsNull_ReturnsNotFound()
    {
        Assert.IsInstanceOfType(await _controller.Delete(null), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Delete_WhenUserExists_ReturnsView()
    {
        _service.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(User());
        Assert.IsInstanceOfType(await _controller.Delete("1"), typeof(ViewResult));
    }

    [TestMethod]
    public async Task DeleteConfirmed_WhenCalled_DeletesUser()
    {
        var result = await _controller.DeleteConfirmed("1");
        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        _service.Verify(s => s.DeleteAsync("1"), Times.Once);
    }

    [TestMethod]
    public async Task Profil_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
    {
        Assert.IsInstanceOfType(await _controller.Profil(), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task Profil_WhenUserDoesNotExist_ReturnsNotFound()
    {
        SetPrincipal(userId: "404");
        Assert.IsInstanceOfType(await _controller.Profil(), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task EditProfil_WhenUserIsNotAuthenticated_ReturnsUnauthorized()
    {
        Assert.IsInstanceOfType(await _controller.EditProfil(), typeof(UnauthorizedResult));
    }

    [TestMethod]
    public async Task EditProfil_WhenUserExists_ReturnsView()
    {
        SetPrincipal(userId: "1");
        _service.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(User());
        Assert.IsInstanceOfType(await _controller.EditProfil(), typeof(ViewResult));
    }

    [TestMethod]
    public async Task Blokiraj_WhenUserDoesNotExist_ReturnsNotFound()
    {
        Assert.IsInstanceOfType(await _controller.Blokiraj("404"), typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Blokiraj_WhenUserExists_BlocksUser()
    {
        _service.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(User());
        await _controller.Blokiraj("1");
        _service.Verify(s => s.UpdateAsync(It.Is<Korisnik>(u => u.StatusNaloga == StatusNaloga.Blokiran)), Times.Once);
    }

    [TestMethod]
    public async Task Aktiviraj_WhenUserExists_ActivatesUser()
    {
        _service.Setup(s => s.GetByIdAsync("1")).ReturnsAsync(User());
        await _controller.Aktiviraj("1");
        _service.Verify(s => s.UpdateAsync(It.Is<Korisnik>(u => u.StatusNaloga == StatusNaloga.Aktivan)), Times.Once);
    }

    private static Korisnik User(string id = "1", Uloga role = Uloga.RegistrovaniKorisnik) =>
        new() { Id = id, Ime = "Ajla", Prezime = "Test", Email = "test@test.ba", Uloga = role };

    private void SetExtraUser(string? role = null, string? id = null)
    {
        var claims = new List<Claim>();
        if (role != null) claims.Add(new Claim(ClaimTypes.Role, role));
        if (id != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, id));
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))
            }
        };
    }

    private static Korisnik ExtraUser(Uloga role = Uloga.RegistrovaniKorisnik) => new()
    {
        Id = "1",
        Ime = "Ajla",
        Prezime = "Test",
        Email = "test@example.com",
        Uloga = role
    };

    [TestMethod]
    public async Task Profil_WhenUserExists_ReturnsReservationCount()
    {
        SetExtraUser(id: "1");
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(ExtraUser());
        _reservations.Setup(x => x.GetByKorisnikIdAsync("1"))
            .ReturnsAsync(new List<Rezervacija> { new(), new() });

        var result = await _controller.Profil();

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.AreEqual(2, _controller.ViewBag.BrojRezervacija);
    }

    [TestMethod]
    public async Task EditProfil_WhenUserDoesNotExist_ReturnsNotFound()
    {
        SetExtraUser(id: "1");
        var result = await _controller.EditProfil(ExtraUser(), null);
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [DataTestMethod]
    [DataRow("", "Test", "test@example.com")]
    [DataRow("Ajla", "", "test@example.com")]
    [DataRow("Ajla", "Test", "")]
    public async Task EditProfil_WhenRequiredFieldIsEmpty_ReturnsView(string ime, string prezime, string email)
    {
        SetExtraUser(id: "1");
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(ExtraUser());
        var model = new Korisnik { Ime = ime, Prezime = prezime, Email = email };

        var result = await _controller.EditProfil(model, null);

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsFalse(_controller.ModelState.IsValid);
        _manager.Verify(x => x.UpdateAsync(It.IsAny<Korisnik>()), Times.Never);
    }

    [TestMethod]
    public async Task EditProfil_WhenDataIsValid_UpdatesAndRefreshesSignIn()
    {
        SetExtraUser(id: "1");
        var korisnik = ExtraUser();
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(korisnik);
        _manager.Setup(x => x.UpdateAsync(korisnik)).ReturnsAsync(IdentityResult.Success);
        _signIn.Setup(x => x.RefreshSignInAsync(korisnik)).Returns(Task.CompletedTask);
        var model = new Korisnik { Ime = " Novo ", Prezime = " Prezime ", Email = " novo@example.com ", PhoneNumber = " 061123456 " };

        var result = await _controller.EditProfil(model, null);

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual("Novo", korisnik.Ime);
        Assert.AreEqual("novo@example.com", korisnik.UserName);
        Assert.AreEqual("061123456", korisnik.PhoneNumber);
        _signIn.Verify(x => x.RefreshSignInAsync(korisnik), Times.Once);
    }

    [TestMethod]
    public async Task EditProfil_WhenPhoneIsBlank_SetsPhoneToNull()
    {
        SetExtraUser(id: "1");
        var korisnik = ExtraUser();
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(korisnik);
        _manager.Setup(x => x.UpdateAsync(korisnik)).ReturnsAsync(IdentityResult.Success);
        var model = new Korisnik { Ime = "Ajla", Prezime = "Test", Email = "test@example.com", PhoneNumber = " " };

        await _controller.EditProfil(model, null);

        Assert.IsNull(korisnik.PhoneNumber);
    }

    [DataTestMethod]
    [DataRow("DuplicateEmail", "Korisnik sa ovom email adresom veæ postoji.")]
    [DataRow("PasswordMismatch", "Trenutna lozinka nije ispravna.")]
    [DataRow("UnknownError", "Došlo je do greške. Provjerite unesene podatke i pokušajte ponovo.")]
    public async Task EditProfil_WhenIdentityUpdateFails_AddsTranslatedError(string code, string message)
    {
        SetExtraUser(id: "1");
        var korisnik = ExtraUser();
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(korisnik);
        _manager.Setup(x => x.UpdateAsync(korisnik)).ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = code }));

        var result = await _controller.EditProfil(ExtraUser(), null);

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsTrue(_controller.ModelState.Values.SelectMany(x => x.Errors).Any(x => x.ErrorMessage == message));
    }

    [TestMethod]
    public async Task EditProfil_WhenImageExtensionIsInvalid_ReturnsViewWithoutUpdating()
    {
        SetExtraUser(id: "1");
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(ExtraUser());
        var image = new Mock<IFormFile>();
        image.SetupGet(x => x.Length).Returns(5);
        image.SetupGet(x => x.FileName).Returns("slika.exe");

        var result = await _controller.EditProfil(ExtraUser(), image.Object);

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsFalse(_controller.ModelState.IsValid);
        _manager.Verify(x => x.UpdateAsync(It.IsAny<Korisnik>()), Times.Never);
    }

    [TestMethod]
    public void PromijeniLozinku_WhenRequested_ReturnsView()
    {
        Assert.IsInstanceOfType(_controller.PromijeniLozinku(), typeof(ViewResult));
    }

    [DataTestMethod]
    [DataRow(null, "Nova123!", "Nova123!")]
    [DataRow("Stara123!", null, "Nova123!")]
    [DataRow("Stara123!", "Nova123!", null)]
    [DataRow("Stara123!", "Nova123!", "Druga123!")]
    public async Task PromijeniLozinku_WhenInputIsInvalid_ReturnsView(string? oldPassword, string? newPassword, string? confirmation)
    {
        var result = await _controller.PromijeniLozinku(oldPassword, newPassword, confirmation);
        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsFalse(_controller.ModelState.IsValid);
        _manager.Verify(x => x.ChangePasswordAsync(It.IsAny<Korisnik>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task PromijeniLozinku_WhenUserDoesNotExist_ReturnsNotFound()
    {
        _manager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((Korisnik?)null);
        var result = await _controller.PromijeniLozinku("Stara123!", "Nova123!", "Nova123!");
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task PromijeniLozinku_WhenPasswordIsChanged_RefreshesSignIn()
    {
        var korisnik = ExtraUser();
        _manager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);
        _manager.Setup(x => x.ChangePasswordAsync(korisnik, "Stara123!", "Nova123!"))
            .ReturnsAsync(IdentityResult.Success);
        _signIn.Setup(x => x.RefreshSignInAsync(korisnik)).Returns(Task.CompletedTask);

        var result = await _controller.PromijeniLozinku("Stara123!", "Nova123!", "Nova123!");

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        _signIn.Verify(x => x.RefreshSignInAsync(korisnik), Times.Once);
    }

    [DataTestMethod]
    [DataRow("PasswordMismatch", "Trenutna lozinka nije ispravna.")]
    [DataRow("PasswordTooShort", "Lozinka mora imati najmanje 6 karaktera.")]
    [DataRow("InvalidToken", "Token nije ispravan.")]
    public async Task PromijeniLozinku_WhenIdentityRejectsChange_AddsTranslatedError(string code, string message)
    {
        var korisnik = ExtraUser();
        _manager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(korisnik);
        _manager.Setup(x => x.ChangePasswordAsync(korisnik, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = code }));

        var result = await _controller.PromijeniLozinku("Stara123!", "Nova123!", "Nova123!");

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsTrue(_controller.ModelState.Values.SelectMany(x => x.Errors).Any(x => x.ErrorMessage == message));
    }

    [TestMethod]
    public async Task Edit_WhenAdministratorChangesRole_UpdatesIdentityRoles()
    {
        SetExtraUser(role: "Administrator");
        var korisnik = ExtraUser();
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(korisnik);
        _manager.Setup(x => x.GetRolesAsync(korisnik)).ReturnsAsync(new List<string> { "RegistrovaniKorisnik" });
        _manager.Setup(x => x.RemoveFromRolesAsync(korisnik, It.IsAny<IEnumerable<string>>())).ReturnsAsync(IdentityResult.Success);
        _manager.Setup(x => x.AddToRoleAsync(korisnik, "Operater")).ReturnsAsync(IdentityResult.Success);

        var result = await _controller.Edit("1", StatusNaloga.Aktivan, Uloga.Operater);

        Assert.IsInstanceOfType(result, typeof(RedirectToActionResult));
        Assert.AreEqual(Uloga.Operater, korisnik.Uloga);
        _manager.Verify(x => x.RemoveFromRolesAsync(korisnik, It.IsAny<IEnumerable<string>>()), Times.Once);
        _manager.Verify(x => x.AddToRoleAsync(korisnik, "Operater"), Times.Once);
    }

    [TestMethod]
    public async Task Edit_WhenAdministratorChangesRoleWithoutPreviousRoles_AddsNewRole()
    {
        SetExtraUser(role: "Administrator");
        var korisnik = ExtraUser();
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(korisnik);
        _manager.Setup(x => x.GetRolesAsync(korisnik)).ReturnsAsync(new List<string>());
        _manager.Setup(x => x.AddToRoleAsync(korisnik, "Operater")).ReturnsAsync(IdentityResult.Success);

        await _controller.Edit("1", StatusNaloga.Aktivan, Uloga.Operater);

        _manager.Verify(x => x.RemoveFromRolesAsync(It.IsAny<Korisnik>(), It.IsAny<IEnumerable<string>>()), Times.Never);
        _manager.Verify(x => x.AddToRoleAsync(korisnik, "Operater"), Times.Once);
    }

    [TestMethod]
    public async Task Edit_WhenOperatorChangesRole_DoesNotUpdateIdentityRoles()
    {
        SetExtraUser(role: "Operater");
        var korisnik = ExtraUser();
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(korisnik);

        await _controller.Edit("1", StatusNaloga.Blokiran, Uloga.Administrator);

        Assert.AreEqual(Uloga.RegistrovaniKorisnik, korisnik.Uloga);
        _manager.Verify(x => x.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()), Times.Never);
    }

    [TestMethod]
    public async Task Edit_WhenOperatorEditsAdministrator_ReturnsForbid()
    {
        SetExtraUser(role: "Operater");
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(ExtraUser(Uloga.Administrator));
        var result = await _controller.Edit("1", StatusNaloga.Blokiran, null);
        Assert.IsInstanceOfType(result, typeof(ForbidResult));
        _service.Verify(x => x.UpdateAsync(It.IsAny<Korisnik>()), Times.Never);
    }

    [TestMethod]
    public async Task EditPost_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var result = await _controller.Edit("404", StatusNaloga.Blokiran, null);
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Blokiraj_WhenOperatorTargetsAdministrator_ReturnsForbid()
    {
        SetExtraUser(role: "Operater");
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(ExtraUser(Uloga.Administrator));
        var result = await _controller.Blokiraj("1");
        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task Aktiviraj_WhenOperatorTargetsAdministrator_ReturnsForbid()
    {
        SetExtraUser(role: "Operater");
        _service.Setup(x => x.GetByIdAsync("1")).ReturnsAsync(ExtraUser(Uloga.Administrator));
        var result = await _controller.Aktiviraj("1");
        Assert.IsInstanceOfType(result, typeof(ForbidResult));
    }

    [TestMethod]
    public async Task Aktiviraj_WhenUserDoesNotExist_ReturnsNotFound()
    {
        var result = await _controller.Aktiviraj("404");
        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
    }

    [TestMethod]
    public async Task Edit_WhenUserDoesNotExistOnPost_ReturnsNotFound()
    {
        _service.Setup(s => s.GetByIdAsync("404"))
            .ReturnsAsync((Korisnik?)null);

        var result = await _controller.Edit(
            "404",
            StatusNaloga.Aktivan,
            Uloga.RegistrovaniKorisnik);

        Assert.IsInstanceOfType(result, typeof(NotFoundResult));
        _service.Verify(s => s.UpdateAsync(It.IsAny<Korisnik>()), Times.Never);
    }




    [TestMethod]
    public async Task Edit_WhenAdministratorChangesRole_UpdatesRole()
    {
        SetPrincipal("Administrator");

        var korisnik = User("1", Uloga.RegistrovaniKorisnik);

        _service.Setup(s => s.GetByIdAsync("1"))
            .ReturnsAsync(korisnik);

        _manager.Setup(m => m.GetRolesAsync(korisnik))
            .ReturnsAsync(new List<string> { "RegistrovaniKorisnik" });

        _manager.Setup(m => m.RemoveFromRolesAsync(
            korisnik, It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        _manager.Setup(m => m.AddToRoleAsync(korisnik, "Operater"))
            .ReturnsAsync(IdentityResult.Success);

        var result = await _controller.Edit(
            "1",
            StatusNaloga.Aktivan,
            Uloga.Operater);

        Assert.IsInstanceOfType(
            result, typeof(RedirectToActionResult));

        Assert.AreEqual(Uloga.Operater, korisnik.Uloga);

        _service.Verify(
            s => s.UpdateAsync(korisnik), Times.Once);

        _manager.Verify(
            m => m.RemoveFromRolesAsync(
                korisnik, It.IsAny<IEnumerable<string>>()),
            Times.Once);

        _manager.Verify(
            m => m.AddToRoleAsync(korisnik, "Operater"),
            Times.Once);
    }


    [TestMethod]
    public async Task EditProfil_WhenValidImageIsUploaded_SavesImageAndUpdatesProfile()
    {
        SetPrincipal(userId: "1");

        var korisnik = User("1", Uloga.RegistrovaniKorisnik);

        _service.Setup(s => s.GetByIdAsync("1"))
            .ReturnsAsync(korisnik);

        _manager.Setup(m => m.UpdateAsync(korisnik))
            .ReturnsAsync(IdentityResult.Success);

        _signIn.Setup(s => s.RefreshSignInAsync(korisnik))
            .Returns(Task.CompletedTask);

        var model = new Korisnik
        {
            Ime = "Ajla",
            Prezime = "Test",
            Email = "ajla@test.ba"
        };

        byte[] bytes = Encoding.UTF8.GetBytes("test image content");
        using var stream = new MemoryStream(bytes);

        IFormFile slika = new FormFile(
            stream, 0, bytes.Length, "profilnaSlika", "profil.png");

        string? sacuvanaPutanja = null;

        try
        {
            var result = await _controller.EditProfil(model, slika);
            sacuvanaPutanja = korisnik.ProfilnaSlikaUrl;

            Assert.IsInstanceOfType(
                result, typeof(RedirectToActionResult));

            Assert.IsNotNull(sacuvanaPutanja);
            StringAssert.StartsWith(
                sacuvanaPutanja, "/uploads/profili/");

            var putanjaNaDisku = Path.Combine(
                Directory.GetCurrentDirectory(),
                "wwwroot",
                sacuvanaPutanja.TrimStart('/')
                    .Replace('/', Path.DirectorySeparatorChar));

            Assert.IsTrue(File.Exists(putanjaNaDisku));

            _manager.Verify(
                m => m.UpdateAsync(korisnik), Times.Once);

            _signIn.Verify(
                s => s.RefreshSignInAsync(korisnik), Times.Once);
        }
        finally
        {
            if (!string.IsNullOrEmpty(sacuvanaPutanja))
            {
                var putanja = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    sacuvanaPutanja.TrimStart('/')
                        .Replace('/', Path.DirectorySeparatorChar));

                if (File.Exists(putanja))
                    File.Delete(putanja);
            }
        }
    }
}


