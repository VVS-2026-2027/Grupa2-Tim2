using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;

namespace PametniParkingSistem.Services
{
    public class KaznaService : IKaznaService
    {
        private const int TolerancijaUMinutama = 10;

        public double IzracunajKaznu(
            Rezervacija rezervacija,
            DateTime stvarnoVrijemeIzlaska)
        {
            var krajTolerancije = rezervacija.VrijemeKraja
                .AddMinutes(TolerancijaUMinutama);

            if (stvarnoVrijemeIzlaska <= krajTolerancije)
            {
                return 0;
            }

            throw new NotImplementedException();
        }
    }
}