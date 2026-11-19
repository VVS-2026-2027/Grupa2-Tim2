
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.ViewModels.Account;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class AccountControllerTests
{
    private Mock<UserManager<Korisnik>> _users = null!;
    private Mock<SignInManager<Korisnik>> _signIn = null!;
    private AccountController _controller = null!;

    [TestInitialize]
    public void Initialize()
    {
        var store = new Mock<IUserStore<Korisnik>>();
        _users = new Mock<UserManager<Korisnik>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        var context = new Mock<IHttpContextAccessor>();
        var claims = new Mock<IUserClaimsPrincipalFactory<Korisnik>>();

        _signIn = new Mock<SignInManager<Korisnik>>(
            _users.Object, context.Object, claims.Object, Options.Create(new IdentityOptions()),
            new Mock<ILogger<SignInManager<Korisnik>>>().Object,
            new Mock<Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<Korisnik>>().Object);

        _controller = new AccountController(_users.Object, _signIn.Object);
    }

    [TestMethod]
    public void Register_WhenRequested_ReturnsView()
    {
        Assert.IsInstanceOfType(_controller.Register(), typeof(ViewResult));
    }

    [TestMethod]
    public void Login_WhenRequested_ReturnsView()
    {
        Assert.IsInstanceOfType(_controller.Login(), typeof(ViewResult));
    }

    [TestMethod]
    public void AccessDenied_WhenRequested_ReturnsView()
    {
        Assert.IsInstanceOfType(_controller.AccessDenied(), typeof(ViewResult));
    }

    [TestMethod]
    public async Task Register_WhenModelStateIsInvalid_ReturnsView()
    {
        _controller.ModelState.AddModelError("Email", "Neispravan");
        var result = await _controller.Register(new RegisterViewModel());
        Assert.IsInstanceOfType(result, typeof(ViewResult));
    }

    [TestMethod]
    public async Task Register_WhenIdentityRejectsUser_AddsModelError()
    {
        _users.Setup(u => u.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "DuplicateEmail" }));

        var result = await _controller.Register(ValidRegister());

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Register_WhenUserAndRoleAreCreated_RedirectsHome()
    {
        _users.Setup(u => u.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _users.Setup(u => u.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _signIn.Setup(s => s.SignInAsync(It.IsAny<Korisnik>(), false, null))
            .Returns(Task.CompletedTask);

        var result = await _controller.Register(ValidRegister()) as RedirectToActionResult;

        Assert.AreEqual("Home", result?.ControllerName);
    }

    [TestMethod]
    public async Task Register_WhenRoleAssignmentFails_AddsModelError()
    {
        _users.Setup(u => u.CreateAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _users.Setup(u => u.AddToRoleAsync(It.IsAny<Korisnik>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Code = "InvalidRoleName" }));

        var result = await _controller.Register(ValidRegister());

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Login_WhenModelStateIsInvalid_ReturnsView()
    {
        _controller.ModelState.AddModelError("Email", "Neispravan");
        Assert.IsInstanceOfType(await _controller.Login(ValidLogin()), typeof(ViewResult));
    }

    [TestMethod]
    public async Task Login_WhenUserDoesNotExist_ReturnsViewWithError()
    {
        _users.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((Korisnik?)null);

        var result = await _controller.Login(ValidLogin());

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Login_WhenAccountIsBlocked_DoesNotAttemptSignIn()
    {
        _users.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new Korisnik { StatusNaloga = StatusNaloga.Blokiran });

        await _controller.Login(ValidLogin());

        _signIn.Verify(s => s.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false), Times.Never);
    }

    [DataTestMethod]
    [DataRow("Administrator", "Korisnik")]
    [DataRow("Operater", "ParkingMjesto")]
    [DataRow("RegistrovaniKorisnik", "Home")]
    public async Task Login_WhenCredentialsAreCorrect_RedirectsByRole(string role, string expectedController)
    {
        _users.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new Korisnik
            {
                StatusNaloga = StatusNaloga.Aktivan,
                Uloga = Enum.Parse<Uloga>(role)
            });

        _signIn.Setup(s => s.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
            .ReturnsAsync(SignInResult.Success);

        var result = await _controller.Login(ValidLogin()) as RedirectToActionResult;

        Assert.AreEqual(expectedController, result?.ControllerName);
    }

    [TestMethod]
    public async Task Login_WhenPasswordIsWrong_ReturnsViewWithError()
    {
        SetupActiveUser();

        _signIn.Setup(s => s.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
            .ReturnsAsync(SignInResult.Failed);

        Assert.IsInstanceOfType(await _controller.Login(ValidLogin()), typeof(ViewResult));
        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Login_WhenAccountIsLockedOut_ReturnsViewWithError()
    {
        SetupActiveUser();

        _signIn.Setup(s => s.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
            .ReturnsAsync(SignInResult.LockedOut);

        await _controller.Login(ValidLogin());

        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Login_WhenSignInIsNotAllowed_ReturnsViewWithError()
    {
        SetupActiveUser();

        _signIn.Setup(s => s.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
            .ReturnsAsync(SignInResult.NotAllowed);

        await _controller.Login(ValidLogin());

        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Login_WhenTwoFactorIsRequired_ReturnsViewWithError()
    {
        SetupActiveUser();

        _signIn.Setup(s => s.PasswordSignInAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(), false))
            .ReturnsAsync(SignInResult.TwoFactorRequired);

        await _controller.Login(ValidLogin());

        Assert.IsFalse(_controller.ModelState.IsValid);
    }

    [TestMethod]
    public async Task Logout_WhenCalled_SignsOutAndRedirectsHome()
    {
        _signIn.Setup(s => s.SignOutAsync()).Returns(Task.CompletedTask);

        var result = await _controller.Logout() as RedirectToActionResult;

        Assert.AreEqual("Home", result?.ControllerName);
        _signIn.Verify(s => s.SignOutAsync(), Times.Once);
    }

    [DataTestMethod]
    [DataRow("DefaultError", "Došlo je do greške. Pokušajte ponovo.")]
    [DataRow("ConcurrencyFailure", "Došlo je do konflikta pri spremanju podataka. Pokušajte ponovo.")]
    [DataRow("PasswordMismatch", "Lozinka nije ispravna.")]
    [DataRow("InvalidToken", "Token nije ispravan.")]
    [DataRow("LoginAlreadyAssociated", "Ova prijava je već povezana sa drugim korisnikom.")]
    [DataRow("InvalidUserName", "Korisničko ime nije ispravno.")]
    [DataRow("InvalidEmail", "Email adresa nije ispravna.")]
    [DataRow("DuplicateUserName", "Korisnik sa ovom email adresom već postoji.")]
    [DataRow("DuplicateEmail", "Korisnik sa ovom email adresom već postoji.")]
    [DataRow("InvalidRoleName", "Naziv uloge nije ispravan.")]
    [DataRow("DuplicateRoleName", "Ova uloga već postoji.")]
    [DataRow("UserAlreadyHasPassword", "Korisnik već ima postavljenu lozinku.")]
    [DataRow("UserLockoutNotEnabled", "Zaključavanje korisničkog naloga nije omogućeno.")]
    [DataRow("UserAlreadyInRole", "Korisnik već ima ovu ulogu.")]
    [DataRow("UserNotInRole", "Korisnik nema ovu ulogu.")]
    [DataRow("PasswordTooShort", "Lozinka mora imati najmanje 6 karaktera.")]
    [DataRow("PasswordRequiresNonAlphanumeric", "Lozinka mora sadržavati barem jedan specijalni znak.")]
    [DataRow("PasswordRequiresDigit", "Lozinka mora sadržavati barem jedan broj.")]
    [DataRow("PasswordRequiresLower", "Lozinka mora sadržavati barem jedno malo slovo.")]
    [DataRow("PasswordRequiresUpper", "Lozinka mora sadržavati barem jedno veliko slovo.")]
    [DataRow("UnknownError", "Došlo je do greške. Provjerite unesene podatke i pokušajte ponovo.")]
    public async Task Register_WhenIdentityReturnsError_TranslatesErrorToBosnian(
        string errorCode,
        string expectedMessage)
    {
        _users.Setup(u => u.CreateAsync(
                It.IsAny<Korisnik>(),
                It.IsAny<string>()))
            .ReturnsAsync(
                IdentityResult.Failed(
                    new IdentityError
                    {
                        Code = errorCode,
                        Description = "Originalna Identity greška"
                    }));

        var result = await _controller.Register(ValidRegister());

        Assert.IsInstanceOfType(result, typeof(ViewResult));
        Assert.IsFalse(_controller.ModelState.IsValid);

        var errors = _controller.ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();

        CollectionAssert.Contains(
            errors,
            expectedMessage,
            $"Očekivana poruka: '{expectedMessage}'. " +
            $"Stvarne poruke: '{string.Join(" | ", errors)}'");

        _users.Verify(u => u.CreateAsync(
            It.IsAny<Korisnik>(),
            It.IsAny<string>()), Times.Once);
    }

    private void SetupActiveUser() =>
        _users.Setup(u => u.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new Korisnik { StatusNaloga = StatusNaloga.Aktivan });

    private static LoginViewModel ValidLogin() =>
        new() { Email = "test@test.ba", Lozinka = "Sifra123!" };

    private static RegisterViewModel ValidRegister() =>
        new()
        {
            Ime = "Ajla",
            Prezime = "Test",
            Email = "test@test.ba",
            Lozinka = "Sifra123!",
            PotvrdiLozinku = "Sifra123!"
        };
}
