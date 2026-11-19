using System.ComponentModel.DataAnnotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Models;
using PametniParkingSistem.ViewModels.Account;

namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class ModelValidationTests
{
    private static bool IsValid(object model) =>
        Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);

    [TestMethod]
    public void Korisnik_WhenRequiredNamesAreMissing_IsInvalid()
    {
        var model = new Korisnik { Ime = "", Prezime = "" };
        Assert.IsFalse(IsValid(model));
    }

    [TestMethod]
    public void Korisnik_WhenNamesArePresent_IsValid()
    {
        var model = new Korisnik { Ime = "Ajla", Prezime = "Test" };
        Assert.IsTrue(IsValid(model));
    }

    [TestMethod]
    public void LoginViewModel_WhenEmailIsInvalid_IsInvalid()
    {
        var model = new LoginViewModel { Email = "pogresno", Lozinka = "Sifra123!" };
        Assert.IsFalse(IsValid(model));
    }

    [TestMethod]
    public void LoginViewModel_WhenPasswordIsMissing_IsInvalid()
    {
        var model = new LoginViewModel { Email = "test@test.ba", Lozinka = "" };
        Assert.IsFalse(IsValid(model));
    }

    [TestMethod]
    public void LoginViewModel_WhenValuesAreValid_IsValid()
    {
        var model = new LoginViewModel { Email = "test@test.ba", Lozinka = "Sifra123!" };
        Assert.IsTrue(IsValid(model));
    }

    [TestMethod]
    public void RegisterViewModel_WhenPasswordsDiffer_IsInvalid()
    {
        var model = ValidRegistration();
        model.PotvrdiLozinku = "Druga123!";
        Assert.IsFalse(IsValid(model));
    }

    [TestMethod]
    public void RegisterViewModel_WhenEmailIsInvalid_IsInvalid()
    {
        var model = ValidRegistration();
        model.Email = "pogresno";
        Assert.IsFalse(IsValid(model));
    }

    [TestMethod]
    public void RegisterViewModel_WhenRequiredNameIsMissing_IsInvalid()
    {
        var model = ValidRegistration();
        model.Ime = "";
        Assert.IsFalse(IsValid(model));
    }

    [TestMethod]
    public void RegisterViewModel_WhenValuesAreValid_IsValid()
    {
        Assert.IsTrue(IsValid(ValidRegistration()));
    }

    private static RegisterViewModel ValidRegistration() => new()
    {
        Ime = "Ajla", Prezime = "Test", Email = "test@test.ba",
        Lozinka = "Sifra123!", PotvrdiLozinku = "Sifra123!"
    };
}
