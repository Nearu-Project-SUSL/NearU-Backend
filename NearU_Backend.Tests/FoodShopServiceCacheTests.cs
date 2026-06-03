using Microsoft.EntityFrameworkCore;
using Moq;
using NearU_Backend_Revised.Data;
using NearU_Backend_Revised.DTOs.FoodShop;
using NearU_Backend_Revised.Models;
using NearU_Backend_Revised.Repositories.Interfaces;
using NearU_Backend_Revised.Services;
using NearU_Backend_Revised.Services.Interfaces;
using Xunit;

namespace NearU_Backend.Tests
{
    /// <summary>
    /// Tests that FoodShopService correctly applies:
    ///   - Cache-aside pattern (HIT skips repo, MISS populates cache).
    ///   - Approved-owner security filter (cache miss path uses real EF InMemory DB).
    ///   - CUD operations invalidate the cache.
    ///
    /// No real Redis or PostgreSQL required — cache is mocked, DB uses EF InMemory.
    /// </summary>
    public class FoodShopServiceCacheTests : IDisposable
    {
        private readonly Mock<IFoodShopRepository> _repoMock  = new();
        private readonly Mock<IImageService>       _imageMock = new();
        private readonly Mock<ICacheService>       _cacheMock = new();
        private readonly ApplicationDbContext      _dbContext;

        private const string CacheKey = "nearu:foodshops:all";

        public FoodShopServiceCacheTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _dbContext = new ApplicationDbContext(options);
        }

        private FoodShopService BuildService() =>
            new(_repoMock.Object, _imageMock.Object, _cacheMock.Object, _dbContext);

        public void Dispose() => _dbContext.Dispose();

        // ── Cache HIT ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAllShopsAsync_CacheHit_DoesNotCallRepositoryOrDatabase()
        {
            var cached = new List<FoodShopResponse>
            {
                new() { Id = "1", Name = "Shop A" },
                new() { Id = "2", Name = "Shop B" }
            };

            _cacheMock
                .Setup(c => c.GetAsync<List<FoodShopResponse>>(CacheKey))
                .ReturnsAsync(cached);

            var svc = BuildService();
            var result = await svc.GetAllShopsAsync(1, 10, null, null);

            // Repository and DB must NOT be touched when the cache has data
            _repoMock.Verify(r => r.GetAllAsync(), Times.Never);
            Assert.Equal(2, result.TotalCount);
        }

        // ── Cache MISS (with approved-owner filter) ───────────────────────────────────────

        [Fact]
        public async Task GetAllShopsAsync_CacheMiss_AppliesApprovedOwnerFilterAndPopulatesCache()
        {
            // Arrange: no cache hit
            _cacheMock
                .Setup(c => c.GetAsync<List<FoodShopResponse>>(CacheKey))
                .ReturnsAsync((List<FoodShopResponse>?)null);

            // Seed users first (BusinessApplication.User is a required nav property)
            _dbContext.Users.AddRange(
                new User { Id = "owner-approved", Username = "approved", Email = "a@test.com", PasswordHash = "x", Role = "Business", CreatedDate = DateTime.UtcNow.ToString() },
                new User { Id = "owner-pending",  Username = "pending",  Email = "p@test.com", PasswordHash = "x", Role = "Business", CreatedDate = DateTime.UtcNow.ToString() }
            );
            _dbContext.BusinessApplications.AddRange(
                new BusinessApplication { Id = "ba-1", UserId = "owner-approved", Status = "Approved", BusinessType = "Food", BusinessName = "A", OwnerName = "A", Phone = "0", Address = "A", Description = "A" },
                new BusinessApplication { Id = "ba-2", UserId = "owner-pending",  Status = "Pending",  BusinessType = "Food", BusinessName = "B", OwnerName = "B", Phone = "0", Address = "B", Description = "B" }
            );
            await _dbContext.SaveChangesAsync();

            // Repo returns three shops: admin-created, approved-owner, pending-owner
            var dbShops = new List<FoodShop>
            {
                new() { Id = "s1", Name = "Admin Shop",    OwnerId = null,             CreatedAt = DateTime.UtcNow },
                new() { Id = "s2", Name = "Approved Shop", OwnerId = "owner-approved", CreatedAt = DateTime.UtcNow },
                new() { Id = "s3", Name = "Pending Shop",  OwnerId = "owner-pending",  CreatedAt = DateTime.UtcNow },
            };
            _repoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(dbShops);

            _cacheMock
                .Setup(c => c.SetAsync(CacheKey, It.IsAny<List<FoodShopResponse>>(), It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);

            var svc = BuildService();
            var result = await svc.GetAllShopsAsync(1, 10, null, null);

            // Repo called once
            _repoMock.Verify(r => r.GetAllAsync(), Times.Once);

            // Only admin-created + approved-owner shops should be returned (2 of 3)
            Assert.Equal(2, result.TotalCount);
            Assert.DoesNotContain(result.Items, s => s.Name == "Pending Shop");

            // Cache populated with the filtered list
            _cacheMock.Verify(c => c.SetAsync(
                CacheKey,
                It.Is<List<FoodShopResponse>>(l => l.Count == 2),
                It.Is<TimeSpan?>(t => t == TimeSpan.FromMinutes(5))), Times.Once);
        }

        // ── In-memory search filter (cache HIT path) ──────────────────────────────────────

        [Fact]
        public async Task GetAllShopsAsync_WithSearchTerm_FiltersInMemory()
        {
            var cached = new List<FoodShopResponse>
            {
                new() { Id = "1", Name = "Pizza Palace",  Category = "Pizza"   },
                new() { Id = "2", Name = "Burger Barn",   Category = "Burgers" },
                new() { Id = "3", Name = "Pizza Express", Category = "Pizza"   },
            };

            _cacheMock
                .Setup(c => c.GetAsync<List<FoodShopResponse>>(CacheKey))
                .ReturnsAsync(cached);

            var svc = BuildService();
            var result = await svc.GetAllShopsAsync(1, 10, null, "Pizza");

            Assert.Equal(2, result.TotalCount);
            Assert.All(result.Items, s => Assert.Contains("Pizza", s.Name));
            _repoMock.Verify(r => r.GetAllAsync(), Times.Never);
        }

        // ── Create → invalidates cache ────────────────────────────────────────────────────

        [Fact]
        public async Task CreateShopAsync_InvalidatesCacheAfterCreate()
        {
            var dto = new CreateFoodShop { Name = "New Shop", Category = "Other" };
            var created = new FoodShop { Id = "99", Name = "New Shop", Category = "Other", CreatedAt = DateTime.UtcNow };

            _repoMock.Setup(r => r.CreateAsync(It.IsAny<FoodShop>())).ReturnsAsync(created);
            _cacheMock.Setup(c => c.RemoveAsync(CacheKey)).Returns(Task.CompletedTask);

            var svc = BuildService();
            await svc.CreateShopAsync(dto);

            _cacheMock.Verify(c => c.RemoveAsync(CacheKey), Times.Once);
        }

        // ── Delete → invalidates cache ────────────────────────────────────────────────────

        [Fact]
        public async Task DeleteShopAsync_InvalidatesCacheAfterDelete()
        {
            _repoMock.Setup(r => r.DeleteAsync("shop-1")).ReturnsAsync(true);
            _cacheMock.Setup(c => c.RemoveAsync(CacheKey)).Returns(Task.CompletedTask);

            var svc = BuildService();
            var result = await svc.DeleteShopAsync("shop-1");

            Assert.True(result);
            _cacheMock.Verify(c => c.RemoveAsync(CacheKey), Times.Once);
        }
    }
}
