using System.Globalization;
using System.Xml.Linq;

namespace PametniParkingSistem.Tests.MSTest
{
    public static class CjenovnikIzvoriPodataka
    {
        public static IEnumerable<(double Cijena, DateTime Od, DateTime Do)> Csv()
        {
            var putanja = Path.Combine(
                AppContext.BaseDirectory, "TestData", "cjenovnici.csv");

            foreach (var red in File.ReadLines(putanja)
                .Skip(1)
                .Where(red => !string.IsNullOrWhiteSpace(red)))
            {
                var polja = red.Split(',');

                yield return (
                    double.Parse(polja[0], CultureInfo.InvariantCulture),
                    DateTime.ParseExact(
                        polja[1], "yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateTime.ParseExact(
                        polja[2], "yyyy-MM-dd", CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable<(double Cijena, DateTime Od, DateTime Do)> Xml()
        {
            var putanja = Path.Combine(
                AppContext.BaseDirectory, "TestData", "cjenovnici.xml");

            var dokument = XDocument.Load(putanja);

            foreach (var element in dokument.Root!.Elements("Cjenovnik"))
            {
                yield return (
                    double.Parse(
                        element.Element("CijenaPoSatu")!.Value,
                        CultureInfo.InvariantCulture),
                    DateTime.ParseExact(
                        element.Element("VaziOd")!.Value,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture),
                    DateTime.ParseExact(
                        element.Element("VaziDo")!.Value,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture));
            }
        }

        public static IEnumerable<(double Cijena, DateTime Od, DateTime Do)>
            CjenovniciInline()
        {
            yield return (
                2.0,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 6, 30));

            yield return (
                4.5,
                new DateTime(2026, 7, 1),
                new DateTime(2026, 12, 31));
        }

        public static IEnumerable<(string Tablice, string Telefon)>
            RezervacijeInline()
        {
            yield return ("A12-B-345", "+38761123456");
            yield return ("K98-M-765", "+38762111222");
        }
    }
}