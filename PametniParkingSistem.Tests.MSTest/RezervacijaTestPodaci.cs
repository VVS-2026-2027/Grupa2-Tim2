using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;

namespace PametniParkingSistem.Tests.MSTest
{
    public static class RezervacijaTestPodaci
    {
        public static Rezervacija Kreiraj(
            int id = 1,
            int parkingMjestoId = 1,
            string korisnikId = "korisnik-1")
        {
            return new Rezervacija
            {
                Id = id,
                KorisnikId = korisnikId,
                ParkingMjestoId = parkingMjestoId,
                DatumKreiranja = new DateTime(2026, 1, 1),
                VrijemePocetka = new DateTime(2026, 12, 1, 10, 0, 0),
                VrijemeKraja = new DateTime(2026, 12, 1, 12, 0, 0),
                RegistracijskeTablice = "A12-B-345",
                KontaktTelefon = "+38761123456",
                EmailZaObavijest = "test@example.com",
                UkupnaCijena = 4,
                StatusRezervacije = StatusRezervacije.Kreirana
            };
        }
    }
}