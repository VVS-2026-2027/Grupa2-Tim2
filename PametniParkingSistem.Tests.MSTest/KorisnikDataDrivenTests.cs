using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PametniParkingSistem.Models;

namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class KorisnikDataDrivenTests
{
    private static string DataPath(string file) =>
        Path.Combine(AppContext.BaseDirectory, "TestData", file);

    public static IEnumerable<object[]> CsvKorisnici()
    {
        return File.ReadAllLines(DataPath("korisnici.csv"))
            .Skip(1)
            .Select(line => line.Split(';'))
            .Select(values => new object[]
            {
                values[0], values[1],
                bool.Parse(values[2])
            });
    }

    public static IEnumerable<object[]> XmlKorisnici()
    {
        return XDocument.Load(DataPath("korisnici.xml"))
            .Root!.Elements("korisnik")
            .Select(node => new object[]
            {
                (string?)node.Attribute("ime") ?? "",
                (string?)node.Attribute("prezime") ?? "",
                bool.Parse((string?)node.Attribute("validan") ?? "false")
            });
    }

    private static bool IsValid(Korisnik model) =>
        Validator.TryValidateObject(model, new ValidationContext(model), new List<ValidationResult>(), true);

    [DataTestMethod]
    [DynamicData(nameof(CsvKorisnici), DynamicDataSourceType.Method)]
    public void Korisnik_WhenCsvDataIsProvided_ValidatesRequiredNames(string ime, string prezime, bool expected)
    {
        var korisnik = new Korisnik { Ime = ime, Prezime = prezime };
        Assert.AreEqual(expected, IsValid(korisnik));
    }

    [DataTestMethod]
    [DynamicData(nameof(XmlKorisnici), DynamicDataSourceType.Method)]
    public void Korisnik_WhenXmlDataIsProvided_ValidatesRequiredNames(string ime, string prezime, bool expected)
    {
        var korisnik = new Korisnik { Ime = ime, Prezime = prezime };
        Assert.AreEqual(expected, IsValid(korisnik));
    }

    [TestMethod]
    public void CsvKorisnici_WhenCalled_ReturnsTestData()
    {
        var podaci = CsvKorisnici().ToList();

        Assert.IsTrue(podaci.Count > 0);

        foreach (var red in podaci)
        {
            Assert.AreEqual(3, red.Length);
            Assert.IsInstanceOfType(red[0], typeof(string));
            Assert.IsInstanceOfType(red[1], typeof(string));
            Assert.IsInstanceOfType(red[2], typeof(bool));
        }
    }

    [TestMethod]
    public void XmlKorisnici_WhenCalled_ReturnsTestData()
    {
        var podaci = XmlKorisnici().ToList();

        Assert.IsTrue(podaci.Count > 0);

        foreach (var red in podaci)
        {
            Assert.AreEqual(3, red.Length);
            Assert.IsInstanceOfType(red[0], typeof(string));
            Assert.IsInstanceOfType(red[1], typeof(string));
            Assert.IsInstanceOfType(red[2], typeof(bool));
        }
    }

}
