using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Controllers;
using PametniParkingSistem.Enums;
using PametniParkingSistem.Models;
using PametniParkingSistem.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using System.IO;

namespace PametniParkingSistem.Tests.MSTest
{
    [TestClass]
    public class ParkingMjestoControllerTests
    {
        private static readonly TipMjesta Tip1 = Enum.GetValues<TipMjesta>().First();
        private static readonly TipMjesta Tip2 = Enum.GetValues<TipMjesta>().Last();
        private static readonly StatusMjesta Aktivno = Enum.GetValues<StatusMjesta>().First(s => s != StatusMjesta.VanFunkcije);

        private Mock<IParkingMjestoService> _serviceMock = null!;

        [TestInitialize]
        public void Postavi()
        {
            _serviceMock = new Mock<IParkingMjestoService>();
        }

        private ParkingMjestoController NapraviKontroler(string queryString = "")
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Request.QueryString = new QueryString(queryString);

            return new ParkingMjestoController(_serviceMock.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        private static List<ParkingMjesto> TestnaMjesta() => new List<ParkingMjesto>
        {
            new ParkingMjesto { Id = 1, ParkingZonaId = 1, TipMjesta = Tip1, Natkriveno = true,  UdaljenostOdUlaza = 10, CijenaPoSatu = 2, Status = Aktivno },
            new ParkingMjesto { Id = 2, ParkingZonaId = 1, TipMjesta = Tip2, Natkriveno = false, UdaljenostOdUlaza = 5,  CijenaPoSatu = 4, Status = Aktivno },
            new ParkingMjesto { Id = 3, ParkingZonaId = 2, TipMjesta = Tip1, Natkriveno = false, UdaljenostOdUlaza = 20, CijenaPoSatu = 6, Status = Aktivno },
            new ParkingMjesto { Id = 4, ParkingZonaId = 2, TipMjesta = Tip2, Natkriveno = true,  UdaljenostOdUlaza = 1,  CijenaPoSatu = 8, Status = StatusMjesta.VanFunkcije }
        };

        private static List<ParkingMjesto> Model(IActionResult rezultat)
        {
            var view = (ViewResult)rezultat;
            return ((IEnumerable<ParkingMjesto>)view.Model!).ToList();
        }

        public static IEnumerable<object?[]> FilteriIzCsvFajla()
        {
            var linije = File.ReadAllLines(Path.Combine("TestData", "ParkingMjestoFilteri.csv")).Skip(1);

            foreach (var linija in linije)
            {
                var d = linija.Split(',');
                yield return new object?[]
                {
            d[0] == "" ? null : double.Parse(d[0], CultureInfo.InvariantCulture),
            d[1] == "" ? null : double.Parse(d[1], CultureInfo.InvariantCulture),
            d[2] == "" ? null : double.Parse(d[2], CultureInfo.InvariantCulture),
            int.Parse(d[3])
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(FilteriIzCsvFajla))]
        public async Task Index_FilteriIzCsvFajla_VracaOcekivaniBrojMjesta(
            double? minCijena, double? maxCijena, double? maxUdaljenost, int ocekivano)
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(TestnaMjesta());
            var kontroler = NapraviKontroler("?filter=1");

            var rezultat = await kontroler.Index(null, null, null, minCijena, maxCijena, maxUdaljenost);

            Assert.AreEqual(ocekivano, Model(rezultat).Count);
        }

        // ---------- Index ----------

        [TestMethod]
        public async Task Index_BezFiltera_VracaSvaMjestaBezPreporuke()
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(TestnaMjesta());
            var kontroler = NapraviKontroler();

            var rezultat = await kontroler.Index(null, null, null, null, null, null);

            Assert.AreEqual(4, Model(rezultat).Count);
            Assert.IsNull(kontroler.ViewBag.PreporucenoMjestoId);
        }

        [TestMethod]
        public async Task Index_FilterPoZoni_VracaSamoMjestaIzZone()
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(TestnaMjesta());
            var kontroler = NapraviKontroler("?zonaId=1");

            var rezultat = await kontroler.Index(1, null, null, null, null, null);

            CollectionAssert.AreEquivalent(new[] { 1, 2 }, Model(rezultat).Select(m => m.Id).ToArray());
        }

        [TestMethod]
        public async Task Index_FilterPoTipuMjesta_VracaSamoMjestaTogTipa()
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(TestnaMjesta());
            var kontroler = NapraviKontroler("?tipMjesta=1");

            var rezultat = await kontroler.Index(null, Tip1, null, null, null, null);

            CollectionAssert.AreEquivalent(new[] { 1, 3 }, Model(rezultat).Select(m => m.Id).ToArray());
        }

        [TestMethod]
        [DataRow(true, 2)]
        [DataRow(false, 2)]
        public async Task Index_FilterPoNatkrivenosti_VracaOcekivaniBrojMjesta(bool natkriveno, int ocekivano)
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(TestnaMjesta());
            var kontroler = NapraviKontroler("?natkriveno=" + natkriveno);

            var rezultat = await kontroler.Index(null, null, natkriveno, null, null, null);

            Assert.AreEqual(ocekivano, Model(rezultat).Count);
        }

        [TestMethod]
        [DataRow(3.0, null, null, 3)]
        [DataRow(null, 4.0, null, 2)]
        [DataRow(2.0, 6.0, null, 3)]
        [DataRow(null, null, 10.0, 3)]
        [DataRow(null, null, 0.5, 0)]
        [DataRow(100.0, null, null, 0)]
        public async Task Index_FilterPoCijeniIUdaljenosti_VracaOcekivaniBrojMjesta(
            double? minCijena, double? maxCijena, double? maxUdaljenost, int ocekivano)
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(TestnaMjesta());
            var kontroler = NapraviKontroler("?filter=1");

            var rezultat = await kontroler.Index(null, null, null, minCijena, maxCijena, maxUdaljenost);

            Assert.AreEqual(ocekivano, Model(rezultat).Count);
        }

        [TestMethod]
        public async Task Index_PretragaSaViseMjesta_PreporucujeNajblizeMjestoKojeJeUFunkciji()
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(TestnaMjesta());
            var kontroler = NapraviKontroler("?minCijena=0");

            await kontroler.Index(null, null, null, 0, null, null);

            // Mjesto Id=4 je najbliže (1m), ali je van funkcije, pa se preporučuje Id=2 (5m)
            Assert.AreEqual(2, kontroler.ViewBag.PreporucenoMjestoId);
        }

        [TestMethod]
        public async Task Index_JednakeUdaljenosti_PreporucujeJeftinijeMjesto()
        {
            var mjesta = new List<ParkingMjesto>
            {
                new ParkingMjesto { Id = 1, UdaljenostOdUlaza = 5, CijenaPoSatu = 6, Status = Aktivno },
                new ParkingMjesto { Id = 2, UdaljenostOdUlaza = 5, CijenaPoSatu = 3, Status = Aktivno }
            };
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(mjesta);
            var kontroler = NapraviKontroler("?minCijena=0");

            await kontroler.Index(null, null, null, 0, null, null);

            Assert.AreEqual(2, kontroler.ViewBag.PreporucenoMjestoId);
        }

        [TestMethod]
        public async Task Index_SvaMjestaVanFunkcije_NemaPreporuke()
        {
            var mjesta = new List<ParkingMjesto>
            {
                new ParkingMjesto { Id = 1, Status = StatusMjesta.VanFunkcije }
            };
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(mjesta);
            var kontroler = NapraviKontroler("?minCijena=0");

            await kontroler.Index(null, null, null, 0, null, null);

            Assert.IsNull(kontroler.ViewBag.PreporucenoMjestoId);
        }

        [TestMethod]
        public async Task Index_PretragaBezRezultata_NemaPreporuke()
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(TestnaMjesta());
            var kontroler = NapraviKontroler("?zonaId=99");

            await kontroler.Index(99, null, null, null, null, null);

            Assert.IsNull(kontroler.ViewBag.PreporucenoMjestoId);
        }

        [TestMethod]
        [DataRow("?zonaId=1")]
        [DataRow("?tipMjesta=1")]
        [DataRow("?natkriveno=true")]
        [DataRow("?minCijena=1")]
        [DataRow("?maxCijena=1")]
        [DataRow("?maxUdaljenost=1")]
        public async Task Index_BiloKojiKljucUQueryju_SmatraSePretragom(string queryString)
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(TestnaMjesta());
            var kontroler = NapraviKontroler(queryString);

            await kontroler.Index(null, null, null, null, null, null);

            Assert.IsNotNull(kontroler.ViewBag.PreporucenoMjestoId);
        }

        // ---------- Details ----------

        [TestMethod]
        public async Task Details_IdJeNull_VracaNotFound()
        {
            var rezultat = await NapraviKontroler().Details(null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_NepostojeciId_VracaNotFound()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(404)).ReturnsAsync((ParkingMjesto?)null);

            var rezultat = await NapraviKontroler().Details(404);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Details_PostojeciId_VracaViewSaMjestom()
        {
            var mjesto = new ParkingMjesto { Id = 3 };
            _serviceMock.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(mjesto);

            var rezultat = await NapraviKontroler().Details(3);

            var view = (ViewResult)rezultat;
            Assert.AreSame(mjesto, view.Model);
        }

        // ---------- Create ----------

        [TestMethod]
        public void Create_Get_VracaView()
        {
            var rezultat = NapraviKontroler().Create();

            Assert.IsInstanceOfType(rezultat, typeof(ViewResult));
        }

        [TestMethod]
        public async Task Create_Post_ValidanModel_DodajeMjestoIPreusmjerava()
        {
            var kontroler = NapraviKontroler();
            var mjesto = new ParkingMjesto { Id = 1, Oznaka = "A1" };

            var rezultat = await kontroler.Create(mjesto);

            _serviceMock.Verify(s => s.AddAsync(mjesto), Times.Once);
            var redirect = (RedirectToActionResult)rezultat;
            Assert.AreEqual("Index", redirect.ActionName);
            Assert.AreEqual("Parking mjesto je uspješno dodano.", kontroler.TempData["Success"]);
        }

        [TestMethod]
        public async Task Create_Post_NevalidanModel_VracaViewBezDodavanja()
        {
            var kontroler = NapraviKontroler();
            kontroler.ModelState.AddModelError("Oznaka", "Obavezno polje");
            var mjesto = new ParkingMjesto();

            var rezultat = await kontroler.Create(mjesto);

            _serviceMock.Verify(s => s.AddAsync(It.IsAny<ParkingMjesto>()), Times.Never);
            Assert.AreSame(mjesto, ((ViewResult)rezultat).Model);
        }

        // ---------- Edit ----------

        [TestMethod]
        public async Task Edit_Get_IdJeNull_VracaNotFound()
        {
            var rezultat = await NapraviKontroler().Edit((int?)null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Get_NepostojeciId_VracaNotFound()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(404)).ReturnsAsync((ParkingMjesto?)null);

            var rezultat = await NapraviKontroler().Edit((int?)404);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Get_PostojeciId_VracaViewSaMjestom()
        {
            var mjesto = new ParkingMjesto { Id = 2 };
            _serviceMock.Setup(s => s.GetByIdAsync(2)).ReturnsAsync(mjesto);

            var rezultat = await NapraviKontroler().Edit((int?)2);

            Assert.AreSame(mjesto, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        public async Task Edit_Post_RazlicitIdIModelId_VracaNotFound()
        {
            var rezultat = await NapraviKontroler().Edit(1, new ParkingMjesto { Id = 2 });

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
            _serviceMock.Verify(s => s.UpdateAsync(It.IsAny<ParkingMjesto>()), Times.Never);
        }

        [TestMethod]
        public async Task Edit_Post_ValidanModel_AzuriraMjestoIPreusmjerava()
        {
            var kontroler = NapraviKontroler();
            var mjesto = new ParkingMjesto { Id = 1 };

            var rezultat = await kontroler.Edit(1, mjesto);

            _serviceMock.Verify(s => s.UpdateAsync(mjesto), Times.Once);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
            Assert.AreEqual("Parking mjesto je uspješno ažurirano.", kontroler.TempData["Success"]);
        }

        [TestMethod]
        public async Task Edit_Post_NevalidanModel_VracaViewBezAzuriranja()
        {
            var kontroler = NapraviKontroler();
            kontroler.ModelState.AddModelError("Oznaka", "Obavezno polje");
            var mjesto = new ParkingMjesto { Id = 1 };

            var rezultat = await kontroler.Edit(1, mjesto);

            _serviceMock.Verify(s => s.UpdateAsync(It.IsAny<ParkingMjesto>()), Times.Never);
            Assert.AreSame(mjesto, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        public async Task Edit_Post_ConcurrencyIzuzetakIMjestoNePostoji_VracaNotFound()
        {
            _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<ParkingMjesto>()))
                        .ThrowsAsync(new DbUpdateConcurrencyException());
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync((ParkingMjesto?)null);

            var rezultat = await NapraviKontroler().Edit(1, new ParkingMjesto { Id = 1 });

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Edit_Post_ConcurrencyIzuzetakIMjestoPostoji_PropagiraIzuzetak()
        {
            _serviceMock.Setup(s => s.UpdateAsync(It.IsAny<ParkingMjesto>()))
                        .ThrowsAsync(new DbUpdateConcurrencyException());
            _serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(new ParkingMjesto { Id = 1 });

            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(
                () => NapraviKontroler().Edit(1, new ParkingMjesto { Id = 1 }));
        }
        

        // ---------- Delete ----------

        [TestMethod]
        public async Task Delete_Get_IdJeNull_VracaNotFound()
        {
            var rezultat = await NapraviKontroler().Delete(null);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Delete_Get_NepostojeciId_VracaNotFound()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(404)).ReturnsAsync((ParkingMjesto?)null);

            var rezultat = await NapraviKontroler().Delete(404);

            Assert.IsInstanceOfType(rezultat, typeof(NotFoundResult));
        }

        [TestMethod]
        public async Task Delete_Get_PostojeciId_VracaViewSaMjestom()
        {
            var mjesto = new ParkingMjesto { Id = 4 };
            _serviceMock.Setup(s => s.GetByIdAsync(4)).ReturnsAsync(mjesto);

            var rezultat = await NapraviKontroler().Delete(4);

            Assert.AreSame(mjesto, ((ViewResult)rezultat).Model);
        }

        [TestMethod]
        public async Task DeleteConfirmed_ValidanId_BriseMjestoIPreusmjerava()
        {
            var kontroler = NapraviKontroler();

            var rezultat = await kontroler.DeleteConfirmed(7);

            _serviceMock.Verify(s => s.DeleteAsync(7), Times.Once);
            Assert.AreEqual("Index", ((RedirectToActionResult)rezultat).ActionName);
            Assert.AreEqual("Parking mjesto je uspješno obrisano.", kontroler.TempData["Success"]);
        }
    }
}