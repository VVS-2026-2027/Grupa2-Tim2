Izvještaj o statičkoj analizi koda – projekat HealthZone

Projekat: HealthZone (ASP.NET Core MVC, .NET 9)
Repozitorij: Grupa2-Tim2
Datum analize: 17.11.2026.
Autor: [ime i prezime]

1. Cilj i opseg

Cilj je da se nad kodom aplikacije provede automatska statička analiza linterom spomenutim na predavanjima, da se rezultati ocijene i da se uporede sa nalazima ručne inspekcije koda.

Statička analiza je provedena nad cijelim projektom (kontroleri, servisi, repozitoriji, modeli, view-ovi, Program.cs, migracije i generisani Identity dio). Ručna inspekcija je provedena nad modulom Recenzija (poglavlje 5), pa se poređenje sa inspekcijom odnosi na taj modul.

2. Korišteni alat i konfiguracija

Alat: Microsoft Roslyn Analyzers (ugrađeni .NET analizatori, pravila CS, CA i IDE). Na Predavanju 2 navedeni su kao linter za C# koji je sastavni dio kompajlera.

Pokretanje: automatski, preko GitHub Actions workflowa .github/workflows/staticka-analiza.yml, na GitHubovom serveru (bez instalacije na lokalnom računaru). Workflow pokreće dotnet build sa uključenim analizatorima:

dotnet build HealthZone.csproj --no-incremental \
  -p:EnableNETAnalyzers=true -p:AnalysisLevel=latest \
  -p:AnalysisMode=All -p:EnforceCodeStyleInBuild=true

AnalysisMode=All uključuje sva pravila. To je namjerno odabrano da se dobije maksimalan broj upozorenja pa da se zatim ocijeni njihova korisnost. Workflow izvršava i dotnet list package --vulnerable --include-transitive (provjera zavisnosti, SCA, Predavanje 3, tačka 3.6).

Izlazni fajlovi (prilog): analiza_puna.txt (cijeli build izlaz), analiza_recenzija.txt (upozorenja za modul Recenzija), analiza_paketi.txt (ranjivi paketi).

3. Rezultati za cijeli projekat

Build je završen sa 0 grešaka i 1058 upozorenja (prema sažetku builda). U tabelama ispod koristi se 1056 jedinstvenih unosa sa lokacijom u kodu; razlika od 2 je zanemarljiva. Upozorenja su prijavljena u 104 fajla.

3.1 Upozorenja po pravilu (Pareto)
Pravilo	Opis pravila	                              Broj	Udio	  Kumulativno
CA2007	Razmotriti ConfigureAwait na awaited tasku	573	  54,3 %	54,3 %
CA1515	Tipovi mogu biti internal	                  119	  11,3 %	65,5 %
CA5395	Akcija nema eksplicitan HTTP atribut	      60	  5,7 %	  71,2 %
CA2201	System.Exception nije dovoljno specifičan	  59	  5,6 %	  76,8 %
CA1062	Validirati parametar na null	              41	  3,9 %	  80,7 %
CA1848	Koristiti LoggerMessage delegate	          25	  2,4 %	  83,0 %
CA1031	Hvatati specifičniji izuzetak	              21	  2,0 %	  85,0 %
CS8618	Non-nullable svojstvo bez vrijednosti	      20	  1,9 %	  86,9 %
CA1725	Ime parametra ne odgovara interfejsu	      17	  1,6 %	  88,5 %
CA1054	URI parametar tipa string	                  15	  1,4 %	  90,0 %
CA1034	Ugniježđeni javni tipovi	                  14	  1,3 %	  91,3 %
CA1873	Skup argument u log pozivu	                14	  1,3 %	  92,6 %
CA1305	Format zavisan od kulture	                  14	  1,3 %	  93,9 %
CA1307	Poređenje stringova bez StringComparison	  13	  1,2 %	  95,2 %
Ostalo	20 drugih pravila	                          51	  4,8 %	  100 %
Ukupno		                                          1056		

Samo 5 pravila (CA2007, CA1515, CA5395, CA2201, CA1062) stvara 80 % svih upozorenja.

3.2 Upozorenja po slojevima
Sloj	Broj	Udio
Controllers	348	33,0 %
Services	298	28,2 %
Areas/Identity (generisani Microsoftov UI)	273	25,9 %
Repositories	63	6,0 %
Models	36	3,4 %
Program.cs	17	1,6 %
Migrations	9	0,9 %
Views	6	0,6 %
Data	6	0,6 %

Skoro četvrtina upozorenja (273) dolazi iz generisanog Identity koda koji tim nije pisao. Kad se on i migracije izuzmu, ostaje 774 upozorenja u kodu koji je tim napisao, a raspodjela po pravilima ostaje slična (CA2007 čini 54,8 %).

Fajlovi sa najviše upozorenja: TerminController.cs (76), KorisnikController.cs (67), TerminService.cs (53), KorisnikService.cs (38), KorisnikNaListiController.cs (34), NalazController.cs (33), ListaCekanjaController.cs (31), ZahtjevZaOpremuController.cs (30), NalazService.cs (28), MedicinskaUslugaService.cs (27).

3.3 Provjera zavisnosti (SCA)

Alat je prijavio tri tranzitivna paketa sa poznatim ranjivostima:

Paket	Verzija	Ozbiljnost	Advisory
Microsoft.Build	17.10.4	High	GHSA-w3q9-fxm7-j8fq
NuGet.Packaging	6.11.0	Low	GHSA-g4vj-cjjj-v7hg
NuGet.Protocol	6.11.0	Low	GHSA-g4vj-cjjj-v7hg

Direktne reference u HealthZone.csproj nisu ranjive, pa su paketi uvučeni tranzitivno. Vjerovatni uzrok je paket Microsoft.VisualStudio.Web.CodeGeneration.Design, koji služi za scaffolding tokom razvoja i ne treba produkciji (potvrditi komandom dotnet nuget why HealthZone.csproj Microsoft.Build).

4. Komentar analize alata

Procjenjujemo da je od 1056 upozorenja oko 181 (17 %) vrijedno pažnje, a oko 875 (83 %) lažno pozitivno ili nebitno za ovu aplikaciju. Procjena je urađena po kategorijama pravila, ne ručnim pregledom svakog upozorenja. Predavanje 2 upravo navodi lažno pozitivne rezultate kao glavni izazov statičke analize.

4.1 Lažno pozitivni i nebitni rezultati
Pravilo	Broj	Ocjena
CA2007	573	U ASP.NET Core aplikacijama ne postoji SynchronizationContext, pa ConfigureAwait(false) nema efekta. Pravilo je namijenjeno bibliotekama. Lažno pozitivno.
CA1515	119	Kontroleri, modeli i interfejsi se registruju kroz DI i MVC. Pravilo ima smisla za biblioteke, ne za web aplikaciju.
CA5395	60	Većina označenih akcija su čitajuće (GET) bez eksplicitnog [HttpGet]. Ipak, treba ručno provjeriti da nijedna akcija koja mijenja stanje (Edit, Delete, Create) nema [HttpPost] i [ValidateAntiForgeryToken] (CWE-352).
CA1062	41	Većinom lažno pozitivno: parametre popunjava MVC model binder ili DI kontejner.
CA1848, CA1873	39	Optimizacija logiranja (LoggerMessage). Bez uticaja na ispravnost.
CA1054, CA1056, CA1034, CA2227, CA1819	43	Uglavnom iz generisanog Identity koda.
4.2 Nalazi koji zaslužuju pažnju
Pravilo	Broj	Komentar
CA2201	59	Servisi (53) i kontroleri bacaju generički Exception, pa pozivalac ne može razlikovati poslovnu od sistemske greške. Isti obrazac se ponavlja kroz cijeli projekat.
CA1031	21	catch (Exception) u kontrolerima (15) i servisima (6). Zajedno sa CA2201 pokazuje da je obrada grešaka u projektu nedovoljno razrađena.
CS8618	20	Non-nullable svojstva u modelima (npr. ZahtjevZaOpremu: Naziv, Opis) bez inicijalizacije.
CS8600, CS8602, CS8629	7	Moguće null vrijednosti: _Layout.cshtml (linije 2-5), Views/Korisnik/Edit.cshtml:141, NotifikacijaController.cs:136 i :148 (pristup Nullable vrijednosti bez provjere, može dati InvalidOperationException u radu).
CS0472	1	Views/Notifikacija/Index.cshtml:38: poređenje vrijednosti tipa Status sa null je uvijek false. Grana koda se nikad ne izvršava, što je vjerovatna logička greška.
CA1304, CA1305, CA1307, CA1311, CA1862, CA1308	44	Operacije nad stringovima i datumima zavise od kulture (npr. MedicinskaUslugaService.cs:39, 59 pretraga sa ToLower() i Contains; TerminController.cs:395, 418 formatiranje vremena). Rezultat može biti drugačiji zavisno od postavki servera.
CA1725	17	Imena parametara u implementaciji ne odgovaraju interfejsu (npr. r, z umjesto naziva entiteta).
CA1860	2	Any() umjesto Count == 0 (RecenzijaService.cs:79, KorisnikService.cs:53).
Ostalo	10	CA1050 (NotifikacijaController izvan namespace-a, linija 9), CA1849 (app.Run() blokira, Program.cs:146), CA1847, CA1820, CA2263, CA1822, CA1863, CS8981 (migracija modeli sa imenom malim slovima).
4.3 Šta alat nije mogao otkriti

Alat ne razumije poslovnu logiku, arhitekturu ni značenje podataka. Nisu prijavljeni propusti poput curenja podataka o pacijentima, loše provjere uloga, nedostatka logiranja sigurnosnih događaja ili neograničene dužine unosa. Takvi propusti su pronađeni samo ručnom inspekcijom (poglavlje 5). Roslyn također nije alat za otkrivanje hardkodiranih tajni niti za dublju analizu toka podataka (SQL injection, XSS), pa se za to preporučuje SAST alat (poglavlje 6).

5. Detalj: modul Recenzija (poređenje sa inspekcijom)

Za modul Recenzija (RecenzijaController, Recenzija, IRecenzijaService, RecenzijaService, IRecenzijaRepository, RecenzijaRepository) alat je dao 55 upozorenja: CA2007 (31), CA1515 (6), CA2201 (5), CA5395 (4), CA1062 (3), CS8618 (2), CA1725 (2), CA1860 (1), CA1031 (1). Od toga 12 upozorenja (22 %) ukazuje na stvarne nedostatke, što odgovara općoj procjeni iz poglavlja 4.

Inspekcijom tog modula pronađeno je 31 nalaz (25 funkcionalnih i 6 sigurnosnih). Alat je potvrdio 3 od njih (F-11 nullable svojstva, F-20 generički izuzeci i dio S-02 generički catch), dodatno je pronašao 2 nova (F-24 Any() naspram Count, F-25 imena parametara) i ranjive zavisnosti (S-06).

Vrsta nalaza	Inspekcija	Alat
Sintaksni i stilski propusti (nullable, imenovanje, generički izuzeci)	Da	Da
Logičke greške u odnosu na zahtjev (F-14: Status.Aktivan naspram poruke "obavljeni pregled")	Da	Ne
Arhitektura i struktura (F-04, F-17, F-18, F-22)	Da	Ne
Performanse (učitavanje svih zapisa u memoriju, N+1: F-15, F-16)	Da	Ne
Kontrola pristupa (S-01 curenje podataka o pacijentu, S-05 uloga po Specijalizacija != null)	Da	Ne
Logiranje sigurnosnih događaja (S-03), ograničenje dužine unosa (S-04)	Da	Ne
Ranjive zavisnosti (S-06)	Ne	Da
! operator (F-01)	Da	Ne (! namjerno gasi upozorenje o null)
6. Opći zaključak
Projekat se kompajlira bez grešaka, a 1058 upozorenja treba čitati kritički: oko 83 % su lažno pozitivni ili nebitni rezultati (prije svega CA2007 i CA1515), pa broj upozorenja sam po sebi ne govori o kvalitetu koda.
Ponavljajući obrasci upućuju na stvarne probleme koji se rješavaju u cijelom projektu, a ne pojedinačno: generički Exception (59) i generički catch (21), nullable svojstva u modelima (20) i operacije nad stringovima zavisne od kulture (44).
Izdvojena upozorenja zaslužuju ručnu provjeru: Notifikacija/Index.cshtml:38 (uslov uvijek false) i NotifikacijaController.cs:136, 148 (moguć pad zbog null).
Zavisnosti sadrže poznate ranjivosti (Microsoft.Build 17.10.4 High, NuGet.Packaging i NuGet.Protocol 6.11.0 Low). Rješenje je uklanjanje paketa za scaffolding iz produkcijskog projekta ili ažuriranje.
Alat i inspekcija se dopunjuju. Alat brzo pokriva cijeli projekat i hvata sintaksne propuste i ranjive pakete, ali su ozbiljniji nalazi (logika, kontrola pristupa, curenje podataka, performanse) pronađeni samo ručnom inspekcijom.
Preporuke
Zamijeniti throw new Exception(...) vlastitim izuzecima i hvatati specifične tipove, te korisniku prikazivati generičke poruke (CWE-209).
U modelima inicijalizirati ili označiti kao nullable sva svojstva (= string.Empty, required, string?).
Ispraviti Notifikacija/Index.cshtml:38 i dodati provjere null u NotifikacijaController.
U pretragama koristiti StringComparison.OrdinalIgnoreCase ili ToLowerInvariant(), a u formatiranju datuma eksplicitnu kulturu.
Ažurirati ili ukloniti pakete sa ranjivostima (S-06).
Isključiti nebitna pravila u .editorconfig da se smanji šum:
dotnet_diagnostic.CA2007.severity = none
dotnet_diagnostic.CA1515.severity = none
Ručno provjeriti da sve akcije koje mijenjaju stanje imaju [HttpPost] i [ValidateAntiForgeryToken] (upozorenja CA5395).
Zadržati GitHub Actions workflow kao stalnu provjeru pri svakom pull requestu, a za dublju sigurnosnu analizu dodati SAST alat (npr. SonarQube ili CodeQL).
7. Prilozi
analiza_puna.txt: kompletan izlaz builda (cijeli projekat)
analiza_recenzija.txt: upozorenja za modul Recenzija
analiza_paketi.txt: rezultat provjere ranjivih paketa
.github/workflows/staticka-analiza.yml: workflow za automatsko pokretanje analize
