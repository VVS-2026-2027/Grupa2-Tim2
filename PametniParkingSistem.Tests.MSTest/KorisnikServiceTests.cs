using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PametniParkingSistem.Models;
using PametniParkingSistem.Repositories;
using PametniParkingSistem.Services;

namespace PametniParkingSistem.Tests.MSTest;

[TestClass]
public class KorisnikServiceTests
{
    private Mock<IKorisnikRepository> _repository = null!;
    private KorisnikService _service = null!;

    [TestInitialize]
    public void Initialize()
    {
        _repository = new Mock<IKorisnikRepository>();
        _service = new KorisnikService(_repository.Object);
    }

    [TestMethod]
    public async Task GetAllAsync_WhenUsersExist_ReturnsUsers()
    {
        var users = new List<Korisnik> { new() { Id = "1", Ime = "Ajla" } };
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
        var result = await _service.GetAllAsync();
        CollectionAssert.AreEqual(users, result);
    }

    [TestMethod]
    public async Task GetAllAsync_WhenRepositoryIsEmpty_ReturnsEmptyList()
    {
        _repository.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Korisnik>());
        var result = await _service.GetAllAsync();
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenUserExists_ReturnsUser()
    {
        var user = new Korisnik { Id = "1" };
        _repository.Setup(r => r.GetByIdAsync("1")).ReturnsAsync(user);
        var result = await _service.GetByIdAsync("1");
        Assert.AreSame(user, result);
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenUserDoesNotExist_ReturnsNull()
    {
        _repository.Setup(r => r.GetByIdAsync("404")).ReturnsAsync((Korisnik?)null);
        var result = await _service.GetByIdAsync("404");
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task AddAsync_WhenCalled_DelegatesToRepository()
    {
        var user = new Korisnik { Id = "1" };
        await _service.AddAsync(user);
        _repository.Verify(r => r.AddAsync(user), Times.Once);
    }

    [TestMethod]
    public async Task UpdateAsync_WhenCalled_DelegatesToRepository()
    {
        var user = new Korisnik { Id = "1" };
        await _service.UpdateAsync(user);
        _repository.Verify(r => r.UpdateAsync(user), Times.Once);
    }

    [TestMethod]
    public async Task DeleteAsync_WhenCalled_DelegatesToRepository()
    {
        await _service.DeleteAsync("1");
        _repository.Verify(r => r.DeleteAsync("1"), Times.Once);
    }

    [TestMethod]
    public async Task GetAllAsync_WhenRepositoryThrows_PropagatesException()
    {
        _repository.Setup(r => r.GetAllAsync()).ThrowsAsync(new InvalidOperationException());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _service.GetAllAsync());
    }

    [TestMethod]
    public async Task GetByIdAsync_WhenRepositoryThrows_PropagatesException()
    {
        _repository.Setup(r => r.GetByIdAsync("1")).ThrowsAsync(new InvalidOperationException());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _service.GetByIdAsync("1"));
    }

    [TestMethod]
    public async Task AddAsync_WhenRepositoryThrows_PropagatesException()
    {
        var user = new Korisnik();
        _repository.Setup(r => r.AddAsync(user)).ThrowsAsync(new InvalidOperationException());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _service.AddAsync(user));
    }

    [TestMethod]
    public async Task UpdateAsync_WhenRepositoryThrows_PropagatesException()
    {
        var user = new Korisnik();
        _repository.Setup(r => r.UpdateAsync(user)).ThrowsAsync(new InvalidOperationException());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _service.UpdateAsync(user));
    }

    [TestMethod]
    public async Task DeleteAsync_WhenRepositoryThrows_PropagatesException()
    {
        _repository.Setup(r => r.DeleteAsync("1")).ThrowsAsync(new InvalidOperationException());
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _service.DeleteAsync("1"));
    }
}
