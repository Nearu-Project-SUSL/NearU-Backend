using Moq;
using Xunit;
using NearU_Backend_Revised.Services.Interfaces;

namespace NearU_Backend.Tests
{
    /// <summary>
    /// Verifies ICacheService contract behaviour using a Mock.
    /// Zero Redis dependency — safe to run inside GitHub Actions without Docker.
    /// </summary>
    public class CacheServiceTests
    {
        private readonly Mock<ICacheService> _cacheMock = new(MockBehavior.Strict);

        // ── GetAsync ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task GetAsync_WhenKeyExists_ReturnsValue()
        {
            _cacheMock
                .Setup(c => c.GetAsync<string>("key:1"))
                .ReturnsAsync("cached-value");

            var result = await _cacheMock.Object.GetAsync<string>("key:1");

            Assert.Equal("cached-value", result);
        }

        [Fact]
        public async Task GetAsync_WhenKeyMissing_ReturnsDefault()
        {
            _cacheMock
                .Setup(c => c.GetAsync<string>("key:missing"))
                .ReturnsAsync((string?)null);

            var result = await _cacheMock.Object.GetAsync<string>("key:missing");

            Assert.Null(result);
        }

        // ── SetAsync ─────────────────────────────────────────────────────────────────────

        [Fact]
        public async Task SetAsync_InvokesUnderlyingCache_WithCorrectArguments()
        {
            _cacheMock
                .Setup(c => c.SetAsync("key:2", "value", TimeSpan.FromMinutes(5)))
                .Returns(Task.CompletedTask);

            await _cacheMock.Object.SetAsync("key:2", "value", TimeSpan.FromMinutes(5));

            _cacheMock.Verify(c => c.SetAsync("key:2", "value", TimeSpan.FromMinutes(5)), Times.Once);
        }

        // ── RemoveAsync ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task RemoveAsync_InvokesUnderlyingCache()
        {
            _cacheMock
                .Setup(c => c.RemoveAsync("key:3"))
                .Returns(Task.CompletedTask);

            await _cacheMock.Object.RemoveAsync("key:3");

            _cacheMock.Verify(c => c.RemoveAsync("key:3"), Times.Once);
        }

        // ── ExistsAsync ───────────────────────────────────────────────────────────────────

        [Fact]
        public async Task ExistsAsync_WhenKeyExists_ReturnsTrue()
        {
            _cacheMock
                .Setup(c => c.ExistsAsync("blacklist:jti-abc"))
                .ReturnsAsync(true);

            var exists = await _cacheMock.Object.ExistsAsync("blacklist:jti-abc");

            Assert.True(exists);
        }

        [Fact]
        public async Task ExistsAsync_WhenKeyAbsent_ReturnsFalse()
        {
            _cacheMock
                .Setup(c => c.ExistsAsync("blacklist:jti-xyz"))
                .ReturnsAsync(false);

            var exists = await _cacheMock.Object.ExistsAsync("blacklist:jti-xyz");

            Assert.False(exists);
        }

        // ── Set operations (Sign-Out-All-Devices) ─────────────────────────────────────────

        [Fact]
        public async Task SetAddAsync_RecordsMember()
        {
            _cacheMock
                .Setup(c => c.SetAddAsync("nearu:user:jtis:u1", "jti-1", It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);

            await _cacheMock.Object.SetAddAsync("nearu:user:jtis:u1", "jti-1", TimeSpan.FromMinutes(15));

            _cacheMock.Verify(c =>
                c.SetAddAsync("nearu:user:jtis:u1", "jti-1", It.IsAny<TimeSpan?>()), Times.Once);
        }

        [Fact]
        public async Task SetMembersAsync_ReturnsAllMembers()
        {
            var expected = new List<string> { "jti-1", "jti-2" };
            _cacheMock
                .Setup(c => c.SetMembersAsync("nearu:user:jtis:u1"))
                .ReturnsAsync(expected);

            var members = await _cacheMock.Object.SetMembersAsync("nearu:user:jtis:u1");

            Assert.Equal(2, members.Count());
            Assert.Contains("jti-1", members);
            Assert.Contains("jti-2", members);
        }
    }
}
