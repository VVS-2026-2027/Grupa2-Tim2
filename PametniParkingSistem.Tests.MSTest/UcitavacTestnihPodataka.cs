using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;

namespace PametniParkingSistem.Tests.MSTest
{
    /// <summary>
    /// Čita pohranjene podatke za data-driven testove (CSV, XML, JSON).
    /// Sama logika čitanja je ovdje, da testne metode ostanu bez programske logike.
    /// </summary>
    public static class UcitavacTestnihPodataka
    {
        private static string Putanja(string imeFajla) =>
            Path.Combine(AppContext.BaseDirectory, "TestData", imeFajla);

        public static string PutanjaKarticaCsv => Putanja("kartice_validacija.csv");

        // CSV: podaci kartice -> da li je model valjan
        public static IEnumerable<object[]> PodaciKarticaIzCsv()
        {
            foreach (var linija in File.ReadAllLines(PutanjaKarticaCsv).Skip(1))
            {
                var k = linija.Split(',');
                yield return new object[] { k[0], k[1], k[2], k[3], bool.Parse(k[4]) };
            }
        }

        // XML: lista plaćanja -> očekivana ukupna zarada
        public static IEnumerable<object[]> SlucajeviZaradeIzXml()
        {
            var dokument = XDocument.Load(Putanja("ukupna_zarada.xml"));

            foreach (var slucaj in dokument.Root!.Elements("slucaj"))
            {
                var placanja = slucaj.Elements("placanje")
                    .Select(p => new Placanje
                    {
                        StatusPlacanja = Enum.Parse<StatusPlacanja>((string)p.Attribute("status")!),
                        Iznos = double.Parse((string)p.Attribute("iznos")!, CultureInfo.InvariantCulture)
                    })
                    .ToList();

                var ocekivano = double.Parse((string)slucaj.Attribute("ocekivano")!, CultureInfo.InvariantCulture);

                yield return new object[] { (string)slucaj.Attribute("opis")!, placanja, ocekivano };
            }
        }

        // JSON: status filtera -> očekivani broj zahtjeva
        public static IEnumerable<object?[]> SlucajeviFilteraIzJsona()
        {
            var json = File.ReadAllText(Putanja("podrska_filter_status.json"));
            var slucajevi = JsonSerializer.Deserialize<List<SlucajFiltera>>(
                json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            foreach (var s in slucajevi)
                yield return new object?[] { s.Opis, s.Status, s.OcekivanoBroj };
        }

        private sealed class SlucajFiltera
        {
            public string Opis { get; set; } = string.Empty;
            public string? Status { get; set; }
            public int OcekivanoBroj { get; set; }
        }
    }
}
