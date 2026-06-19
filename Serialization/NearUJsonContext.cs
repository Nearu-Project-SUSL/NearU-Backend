using System.Text.Json.Serialization;
using NearU_Backend_Revised.DTOs.Accommodation;
using NearU_Backend_Revised.DTOs.Cache;
using NearU_Backend_Revised.DTOs.FoodShop;
using NearU_Backend_Revised.DTOs.GiftProduct;
using NearU_Backend_Revised.DTOs.GiftShop;
using NearU_Backend_Revised.DTOs.Job;

namespace NearU_Backend_Revised.Serialization
{
    /// <summary>
    /// Source-generated <see cref="JsonSerializerContext"/> for every type stored in Redis.
    ///
    /// AOT SAFETY: All serialisation inside <see cref="Services.CacheService"/> goes through
    /// this context, eliminating reflection-based Deserialize&lt;T&gt; calls that fail under
    /// Native AOT / ILLink trimming.
    ///
    /// To add a new cached type:
    ///   1. Add a [JsonSerializable(typeof(YourDto))] attribute below.
    ///   2. Add a JsonTypeInfo&lt;YourDto&gt; property to ICacheService overloads if needed.
    /// </summary>
    [JsonSerializable(typeof(List<FoodShopResponse>))]
    [JsonSerializable(typeof(FoodShopResponse))]
    [JsonSerializable(typeof(List<AccommodationResponse>))]
    [JsonSerializable(typeof(AccommodationResponse))]
    [JsonSerializable(typeof(List<GiftShopResponseDto>))]
    [JsonSerializable(typeof(GiftShopResponseDto))]
    [JsonSerializable(typeof(List<GiftProductResponseDto>))]
    [JsonSerializable(typeof(GiftProductResponseDto))]
    [JsonSerializable(typeof(PagedJobResponse))]
    [JsonSerializable(typeof(List<JobResponse>))]
    [JsonSerializable(typeof(JobResponse))]
    [JsonSerializable(typeof(PostedByInfo))]
    [JsonSerializable(typeof(CachedRiderStatus))]
    [JsonSerializable(typeof(string))]              // sentinel values (blacklist, etc.)
    [JsonSerializable(typeof(HashSet<string>))]     // per-user active-JTI Set
    [JsonSourceGenerationOptions(
        PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false)]
    public partial class NearUJsonContext : JsonSerializerContext
    {
    }
}
