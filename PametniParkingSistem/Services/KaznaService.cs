
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;

namespace PametniParkingSistem.Services
{
    public class KaznaService : IKaznaService
    {
        public double IzracunajKaznu(
            Rezervacija rezervacija,
            DateTime stvarnoVrijemeIzlaska)
        {
            var krajTolerancije = rezervacija.VrijemeKraja.AddMinutes(10);

            if (stvarnoVrijemeIzlaska <= krajTolerancije)
            {
                return 0;
            }

            throw new NotImplementedException();
        }
    }
}
