using PametniParkingSistem.Models;

namespace PametniParkingSistem.Services.Interfaces
{
    public interface IKaznaService
    {
        double IzracunajKaznu(
            Rezervacija rezervacija,
            DateTime stvarnoVrijemeIzlaska);
    }
}