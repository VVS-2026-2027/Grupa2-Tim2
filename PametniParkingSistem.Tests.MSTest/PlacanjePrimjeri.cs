using System.Text.Json;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.ViewModels.Placanje;

namespace PametniParkingSistem.Tests.MSTest
{
    /// <summary>Gotovi primjeri objekata koje koriste testovi plaćanja.</summary>
    public static class PlacanjePrimjeri
    {
        public const string Email = "korisnik@parking.ba";

        public static Rezervacija NovaRezervacija() => new Rezervacija
        {
            Id = 0,
            ParkingMjestoId = 5,
            KorisnikId = "korisnik-1",
            RegistracijskeTablice = "A12-B-345",
            KontaktTelefon = "061123456",
            EmailZaObavijest = Email,
            VrijemePocetka = new DateTime(2030, 5, 10, 10, 0, 0),
            VrijemeKraja = new DateTime(2030, 5, 10, 12, 0, 0),
            UkupnaCijena = 12.5,
            StatusRezervacije = StatusRezervacije.Kreirana
        };

        public static Rezervacija PostojecaRezervacija() => new Rezervacija
        {
            Id = 7,
            ParkingMjestoId = 5,
            KorisnikId = "korisnik-1",
            RegistracijskeTablice = "STARA-111",
            KontaktTelefon = "060000000",
            EmailZaObavijest = "staro@parking.ba",
            VrijemePocetka = new DateTime(2030, 5, 10, 10, 0, 0),
            VrijemeKraja = new DateTime(2030, 5, 10, 11, 0, 0),
            UkupnaCijena = 5,
            StatusRezervacije = StatusRezervacije.Placena
        };

        public static Rezervacija IzmijenjenaRezervacija() => new Rezervacija
        {
            Id = 7,
            ParkingMjestoId = 5,
            KorisnikId = "korisnik-1",
            RegistracijskeTablice = "NOVA-222",
            KontaktTelefon = "061999999",
            EmailZaObavijest = "novo@parking.ba",
            VrijemePocetka = new DateTime(2030, 5, 10, 10, 0, 0),
            VrijemeKraja = new DateTime(2030, 5, 10, 13, 0, 0),
            UkupnaCijena = 15,
            StatusRezervacije = StatusRezervacije.Izmijenjena
        };

        public static ParkingMjesto Mjesto(int zonaId = 1) => new ParkingMjesto
        {
            Id = 5,
            Oznaka = "A-12",
            ParkingZonaId = zonaId,
            Status = StatusMjesta.Slobodno
        };

        public static PlacanjeRezervacijeViewModel ValjanModel(string brojKartice = "1234 5678 9012 3456") =>
            new PlacanjeRezervacijeViewModel
            {
                Iznos = 12.5,
                ImeVlasnikaKartice = "Sarah Test",
                BrojKartice = brojKartice,
                DatumIsteka = "12/99",
                CVV = "123"
            };

        public static string UJson(Rezervacija rezervacija) => JsonSerializer.Serialize(rezervacija);
    }
}
