using System.Text.Json;
using System.Text.Json.Serialization;

namespace DouyinLiveReceiver.Models
{
    /// <summary>
    /// 直播消息基类
    /// </summary>
    public class LiveMessage
    {
        [JsonPropertyName("type")]
        public string Type { get; set; }

        [JsonPropertyName("data")]
        public object Data { get; set; }

        [JsonPropertyName("live_id")]
        public string LiveId { get; set; }

        [JsonPropertyName("timestamp")]
        public double Timestamp { get; set; }
    }

    public class ChatData
    {
        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class GiftData
    {
        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("gift_name")]
        public string GiftName { get; set; }

        [JsonPropertyName("gift_count")]
        public int GiftCount { get; set; }
    }

    public class LikeData
    {
        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    public class MemberData
    {
        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }

        [JsonPropertyName("gender")]
        public string Gender { get; set; }
    }

    public class SocialData
    {
        [JsonPropertyName("user_id")]
        public long UserId { get; set; }

        [JsonPropertyName("user_name")]
        public string UserName { get; set; }
    }

    public class StatsData
    {
        [JsonPropertyName("current")]
        public int Current { get; set; }

        [JsonPropertyName("total")]
        public string Total { get; set; } = "";
    }

    public class FansclubData
    {
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }

    public class EmojiData
    {
        [JsonPropertyName("emoji_id")]
        public string EmojiId { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; }

        [JsonPropertyName("common")]
        public string Common { get; set; }

        [JsonPropertyName("default_content")]
        public string DefaultContent { get; set; }
    }

    public class RoomData
    {
        [JsonPropertyName("room_id")]
        public long RoomId { get; set; }
    }

    public class RoomStatsData
    {
        [JsonPropertyName("display_long")]
        public string DisplayLong { get; set; }
    }

    public class RankData
    {
        [JsonPropertyName("ranks_list")]
        public string RanksList { get; set; }
    }

    public class ControlData
    {
        [JsonPropertyName("status")]
        public int Status { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; }
    }

    public class StreamAdaptationData
    {
        [JsonPropertyName("adaptation_type")]
        public int AdaptationType { get; set; }
    }
}
