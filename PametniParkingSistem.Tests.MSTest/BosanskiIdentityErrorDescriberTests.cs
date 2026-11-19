using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Data;

namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class BosanskiIdentityErrorDescriberTests
{
    private readonly BosanskiIdentityErrorDescriber _describer = new();

    [DataTestMethod]
    [DataRow(6, "Lozinka mora imati najmanje 6 karaktera.")]
    [DataRow(8, "Lozinka mora imati najmanje 8 karaktera.")]
    [DataRow(12, "Lozinka mora imati najmanje 12 karaktera.")]
    public void PasswordTooShort_WhenLengthIsProvided_ReturnsBosnianMessage(int length, string expected)
    {
        var result = _describer.PasswordTooShort(length);
        Assert.AreEqual(expected, result.Description);
    }

    [TestMethod]
    public void PasswordRequiresDigit_WhenCalled_ReturnsBosnianMessage()
    {
        Assert.AreEqual("Lozinka mora sadržavati najmanje jednu cifru.", _describer.PasswordRequiresDigit().Description);
    }

    [TestMethod]
    public void PasswordRequiresLower_WhenCalled_ReturnsBosnianMessage()
    {
        Assert.AreEqual("Lozinka mora sadržavati najmanje jedno malo slovo.", _describer.PasswordRequiresLower().Description);
    }

    [TestMethod]
    public void PasswordRequiresUpper_WhenCalled_ReturnsBosnianMessage()
    {
        Assert.AreEqual("Lozinka mora sadržavati najmanje jedno veliko slovo.", _describer.PasswordRequiresUpper().Description);
    }

    [TestMethod]
    public void PasswordRequiresNonAlphanumeric_WhenCalled_ReturnsBosnianMessage()
    {
        Assert.AreEqual("Lozinka mora sadržavati najmanje jedan specijalni znak.", _describer.PasswordRequiresNonAlphanumeric().Description);
    }
}
