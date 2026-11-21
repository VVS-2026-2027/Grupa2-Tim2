using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;

namespace PametniParkingSistem.Services
{
    public class KaznaService : IKaznaService
    {
        private const int TolerancijaUMinutama = 10;
        private const double CijenaPoZapocetomSatu = 2.0;
        private const double MaksimalnaKazna = 50.0;

        public double IzracunajKaznu(
            Rezervacija rezervacija,
            DateTime stvarnoVrijemeIzlaska)
        {
            ValidirajVremenskiInterval(rezervacija);

            if (RezervacijaBezKazne(rezervacija))
            {
                return 0;
            }

            var krajTolerancije = rezervacija.VrijemeKraja
                .AddMinutes(TolerancijaUMinutama);

            if (stvarnoVrijemeIzlaska <= krajTolerancije)
            {
                return 0;
            }

            var prekoracenje =
                stvarnoVrijemeIzlaska - rezervacija.VrijemeKraja;

            var zapocetiSati = Math.Ceiling(prekoracenje.TotalHours);
            var kazna = zapocetiSati * CijenaPoZapocetomSatu;

            return Math.Min(kazna, MaksimalnaKazna);
        }

        private static void ValidirajVremenskiInterval(
            Rezervacija rezervacija)
        {
            if (rezervacija.VrijemeKraja < rezervacija.VrijemePocetka)
            {
                throw new ArgumentException(
                    "Vrijeme završetka rezervacije ne može biti prije vremena početka.",
                    nameof(rezervacija));
            }
        }

        private static bool RezervacijaBezKazne(Rezervacija rezervacija)
        {
            return rezervacija.StatusRezervacije == StatusRezervacije.Otkazana ||
                   rezervacija.StatusRezervacije == StatusRezervacije.Kreirana;
        }
    }
}