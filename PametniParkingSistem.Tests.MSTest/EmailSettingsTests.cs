using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Settings;

namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class EmailSettingsTests
{
    [TestMethod]
    public void EmailSettings_WhenCreated_HasDefaultValues()
    {
        var settings = new EmailSettings();
        Assert.AreEqual(string.Empty, settings.SmtpServer);
        Assert.AreEqual(0, settings.Port);
        Assert.AreEqual(string.Empty, settings.SenderEmail);
    }

    [TestMethod]
    public void EmailSettings_WhenConfigured_RetainsValues()
    {
        var settings = new EmailSettings
        {
            SmtpServer = "smtp.example.com", Port = 587, SenderName = "Test",
            SenderEmail = "sender@example.com", Password = "example-password"
        };
        Assert.AreEqual("smtp.example.com", settings.SmtpServer);
        Assert.AreEqual(587, settings.Port);
        Assert.AreEqual("Test", settings.SenderName);
        Assert.AreEqual("sender@example.com", settings.SenderEmail);
        Assert.AreEqual("example-password", settings.Password);
    }
}
