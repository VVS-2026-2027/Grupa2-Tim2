using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Services;
using PametniParkingSistem.Settings;

namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class EmailSenderServiceTests
{
    [TestMethod]
    public async Task SendEmailAsync_WhenSenderAddressIsInvalid_ThrowsFormatException()
    {
        var service = new EmailSenderService(Options.Create(new EmailSettings
        {
            SenderEmail = "neispravno", SenderName = "Test",
            SmtpServer = "localhost", Port = 25
        }));
        await Assert.ThrowsExactlyAsync<FormatException>(
            () => service.SendEmailAsync("test@example.com", "Naslov", "Poruka"));
    }

    [TestMethod]
    public async Task SendEmailAsync_WhenRecipientAddressIsInvalid_ThrowsFormatException()
    {
        var service = new EmailSenderService(Options.Create(new EmailSettings
        {
            SenderEmail = "sender@example.com", SenderName = "Test",
            SmtpServer = "localhost", Port = 25
        }));
        await Assert.ThrowsExactlyAsync<FormatException>(
            () => service.SendEmailAsync("neispravno", "Naslov", "Poruka"));
    }

    [TestMethod]
    public async Task SendEmailAsync_WhenSmtpServerIsUnavailable_ThrowsException()
    {
        var service = new EmailSenderService(
            Options.Create(new EmailSettings
            {
                SenderEmail = "sender@example.com",
                SenderName = "Test",
                Password = "test123",
                SmtpServer = "127.0.0.1",
                Port = 1
            }));

        await Assert.ThrowsExactlyAsync<
            System.Net.Mail.SmtpException>(
            () => service.SendEmailAsync(
                "recipient@example.com",
                "Test naslov",
                "<h1>Test poruka</h1>"
            ));
    }

}
