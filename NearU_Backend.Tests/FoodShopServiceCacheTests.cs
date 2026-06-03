using Moq;
using Xunit;
using NearU_Backend_Revised.DTOs.FoodShop;
using NearU_Backend_Revised.Services;
using NearU_Backend_Revised.Services.Interfaces;
using NearU_Backend_Revised.Repositories.Interfaces;

namespace NearU_Backend.Tests
{
    /// <summary>
    /// Tests that FoodShopService correctly applies the cache-aside pattern:
    ///   - Cache HIT skips the repository.
    ///   - Cache MISS populates the cache and hits the repository exactly once.
    ///   - Create / Update / Delete invalidate the cache.
    ///
    /// No real Redis or database required — everything is mocked.
    /// </summary>
    public class FoodShopServiceCacheTests
    {
        private readonly Mock<IFoodShopRepository> _repoMock   = new();
        private readonly Mock<IImageService>        _imageMock  = new();
        private readonly Mock<ICacheService>        _cacheMock  = new();

        private FoodShopService BuildService() =>
            new FoodShopService(_repoMock.Object, _imageMock.Object, _cacheMock.Object);

        private const string CacheKey = "nearu:foodshops:all";

        // ─── Cache HIT ────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllShopsAsync_CacheHit_DoesNotCallRepository()
        {
            // Arrange
            var cached = new List<FoodShopResponse>
            {
                new() { Id = "1", Name = "Shop A" },
                new() { Id = "2", Name = "Shop B" }
            };

            _cacheMock
                .Setup(c => c.GetAsync<List<FoodShopResponse>>(CacheKey))
                .ReturnsAsync(cached);

            var svc = BuildService();

            // Act
            var result = await svc.GetAllShopsAsync(1, 10, null, null);

            // Assert — repository MUST NOT be called when cache has data
            _repoMock.Verify(r => r.GetAllAsync(), Times.Never);
            Assert.Equal(2, result.TotalCount);
        }

        // ─── Cache MISS ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllShopsAsync_CacheMiss_CallsRepositoryAndPopulatesCache()
        {
            // Arrange
            _cacheMock
                .Setup(c => c.GetAsync<List<FoodShopResponse>>(CacheKey))
                .ReturnsAsync((List<FoodShopResponse>?)null);   // cache miss

            var dbShops = new List<NearU_Backend_Revised.Models.FoodShop>
            {
                new() { Id = "1", Name = "Shop A", CreatedAt = DateTime.UtcNow }
            };

            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(dbShops);

            _cacheMock
                .Setup(c => c.SetAsync(
                    CacheKey,
                    It.IsAny<List<FoodShopResponse>>(),
                    It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);

            var svc = BuildService();

            // Act
            var result = await svc.GetAllShopsAsync(1, 10, null, null);

            // Assert — repository called once, cache populated
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);
            _cacheMock.Verify(c => c.SetAsync(
                CacheKey,
                It.IsAny<List<FoodShopResponse>>(),
                It.Is<TimeSpan?>(t => t == TimeSpan.FromMinutes(5))), Times.Once);

            Assert.Equal(1, result.TotalCount);
        }

        // ─── Create → invalidates cache ───────────────────────────────────────────────────

        [Fact]
        public async Task CreateShopAsync_InvalidatesCacheAfterCreate()
        {
            // Arrange
            var dto = new CreateFoodShop
            {
                Name = "New Shop",
                Category = "Other"
            };

            var created = new NearU_Backend_Revised.Models.FoodShop
            {
                Id = "99",
                Name = "New Shop",
                Category = "Other",
                CreatedAt = DateTime.UtcNow
            };

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<NearU_Backend_Revised.Models.FoodShop>()))
                     .ReturnsAsync(created);

            _cacheMock
                .Setup(c => c.RemoveAsync(CacheKey))
                .Returns(Task.CompletedTask);

            var svc = BuildService();

            // Act
            await svc.CreateShopAsync(dto);

            // Assert — cache must be invalidated after write
            _cacheMock.Verify(c => c.RemoveAsync(CacheKey), Times.Once);
        }

        // ─── Delete → invalidates cache ───────────────────────────────────────────────────

        [Fact]
        public async Task DeleteShopAsync_InvalidatesCacheAfterDelete()
        {
            // Arrange
            _repoMock.Setup(r => r.DeleteAsync("shop-1")).ReturnsAsync(true);
            _cacheMock.Setup(c => c.RemoveAsync(CacheKey)).Returns(Task.CompletedTask);

            var svc = BuildService();

            // Act
            var result = await svc.DeleteShopAsync("shop-1");

            // Assert
            Assert.True(result);
            _cacheMock.Verify(c => c.RemoveAsync(CacheKey), Times.Once);
        }

        // ─── In-memory search filter (uses cached data) ───────────────────────────────────

        [Fact]
        public async Task GetAllShopsAsync_WithSearchTerm_FiltersInMemory()
        {
            var cached = new List<FoodShopResponse>
            {
                new() { Id = "1", Name = "Pizza Palace",   Category = "Pizza" },
                new() { Id = "2", Name = "Burger Barn",    Category = "Burgers" },
                new() { Id = "3", Name = "Pizza Express",  Category = "Pizza" },
            };

            _cacheMock
                .Setup(c => c.GetAsync<List<FoodShopResponse>>(CacheKey))
                .ReturnsAsync(cached);

            var svc = BuildService();

            var result = await svc.GetAllShopsAsync(1, 10, null, "Pizza");

            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, s => Assert.Contains("Pizza", s.Name));
        }
    }
}
