
using Microsoft.EntityFrameworkCore;
using PametniParkingSistem.Data;
using PametniParkingSistem.Models;
using PametniParkingSistem.Enums;

namespace PametniParkingSistem.Repositories
{
    public class KorisnikRepository : IKorisnikRepository
    {
        private const string KorisnikNePostojiPoruka = "Korisnik ne postoji.";
        private const string AktivneRezervacijePoruka = "Korisnik ima aktivne rezervacije.";

        private readonly ApplicationDbContext _context;

        public KorisnikRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Korisnik>> GetAllAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<Korisnik?> GetByIdAsync(string id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task AddAsync(Korisnik korisnik)
        {
            await _context.Users.AddAsync(korisnik);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Korisnik korisnik)
        {
            _context.Users.Update(korisnik);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(string id)
        {
            var korisnik = await _context.Users.FindAsync(id);

            if (korisnik == null)
            {
                throw new InvalidOperationException(KorisnikNePostojiPoruka);
            }

            var imaAktivneRezervacije = await _context.Rezervacije
    .AnyAsync(r => r.KorisnikId == id && r.StatusRezervacije == StatusRezervacije.Aktivna);

            if (imaAktivneRezervacije)
            {
                throw new InvalidOperationException(AktivneRezervacijePoruka);
            }

            _context.Users.Remove(korisnik);
            await _context.SaveChangesAsync();
        }
    }
}
