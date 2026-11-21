using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;

namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class BrisanjeNalogaControllerTests
{
    private Mock<IKorisnikService> _korisnikService = null!;
    private Mock<SignInManager<Korisnik>> _signInManager = null!;
    private KorisnikController _controller = null!;

    [TestInitialize]
    public void Pripremi()
    {
        _korisnikService = new Mock<IKorisnikService>();

        var store = new Mock<IUserStore<Korisnik>>();

        var userManager = new Mock<UserManager<Korisnik>>(
            store.Object,
            null!, null!, null!, null!,
            null!, null!, null!, null!);

        _signInManager = new Mock<SignInManager<Korisnik>>(
            userManager.Object,
            new Mock<IHttpContextAccessor>().Object,
            new Mock<IUserClaimsPrincipalFactory<Korisnik>>().Object,
            Options.Create(new IdentityOptions()),
            new Mock<ILogger<SignInManager<Korisnik>>>().Object,
            new Mock<IAuthenticationSchemeProvider>().Object,
            new Mock<IUserConfirmation<Korisnik>>().Object);

        _korisnikService
            .Setup(s => s.ObrisiNalogAsync("samra-test"))
            .Returns(Task.CompletedTask);

        _signInManager
            .Setup(s => s.SignOutAsync())
            .Returns(Task.CompletedTask);

        _controller = new KorisnikController(
            _korisnikService.Object,
            userManager.Object,
            _signInManager.Object,
            new Mock<IRezervacijaService>().Object);

        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    "samra-test")
            },
            "Test");

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };

        _controller.TempData = new TempDataDictionary(
            httpContext,
            new Mock<ITempDataProvider>().Object);
    }

    [TestMethod]
    public async Task ObrisiNalog_UspjesnoBrisanje_OdjavljujeKorisnika()
    {
        // Act
        await _controller.ObrisiNalog();

        // Assert
        _korisnikService.Verify(
            s => s.ObrisiNalogAsync("samra-test"),
            Times.Once);

        _signInManager.Verify(
            s => s.SignOutAsync(),
            Times.Once);
    }

    [TestMethod]
    public async Task ObrisiNalog_UspjesnoBrisanje_PreusmjeravaNaPocetnu()
    {
        // Act
        var rezultat = await _controller.ObrisiNalog();

        // Assert
        Assert.IsInstanceOfType(
            rezultat,
            typeof(RedirectToActionResult));

        var preusmjeravanje = (RedirectToActionResult)rezultat;

        Assert.AreEqual("Index", preusmjeravanje.ActionName);
        Assert.AreEqual("Home", preusmjeravanje.ControllerName);
    }

    [TestMethod]
    public async Task ObrisiNalog_UspjesnoBrisanje_PostavljaPorukuUspjeha()
    {
        // Act
        await _controller.ObrisiNalog();

        // Assert
        Assert.AreEqual(
            "Vaš nalog je uspješno obrisan.",
            _controller.TempData["Success"]);
    }
}