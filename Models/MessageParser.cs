using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DouyinLiveReceiver.Models
{
    /// <summary>
    /// 消息解析器，负责解析和格式化各种类型的直播消息
    /// </summary>
    public static class MessageParser
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static LiveMessage Parse(string json)
        {
            return JsonSerializer.Deserialize<LiveMessage>(json, JsonOptions);
        }

        public static T ParseData<T>(object data)
        {
            if (data is JsonElement element)
            {
                return JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions);
            }
            return default;
        }

        public static string FormatMessage(LiveMessage message)
        {
            if (message?.Data == null) return "";

            var type = message.Type?.ToLower();
            try
            {
                return type switch
                {
                    "chat" => FormatChat(ParseData<ChatData>(message.Data)),
                    "gift" => FormatGift(ParseData<GiftData>(message.Data)),
                    "like" => FormatLike(ParseData<LikeData>(message.Data)),
                    "member" => FormatMember(ParseData<MemberData>(message.Data)),
                    "social" => FormatSocial(ParseData<SocialData>(message.Data)),
                    "stats" => FormatStats(ParseData<StatsData>(message.Data)),
                    "fansclub" => FormatFansclub(ParseData<FansclubData>(message.Data)),
                    "emoji" => FormatEmoji(ParseData<EmojiData>(message.Data)),
                    "room" => FormatRoom(ParseData<RoomData>(message.Data)),
                    "room_stats" => FormatRoomStats(ParseData<RoomStatsData>(message.Data)),
                    "rank" => FormatRank(ParseData<RankData>(message.Data)),
                    "control" => FormatControl(ParseData<ControlData>(message.Data)),
                    "stream_adaptation" => FormatStreamAdaptation(ParseData<StreamAdaptationData>(message.Data)),
                    _ => $"[{message.Type}] {message.Data}"
                };
            }
            catch
            {
                return $"[{message.Type}] {message.Data}";
            }
        }

        private static string FormatChat(ChatData data) => $"{data.UserName}: {data.Content}";
        private static string FormatGift(GiftData data) => $"{data.UserName} 送出 {data.GiftName} x{data.GiftCount}";
        private static string FormatLike(LikeData data) => $"{data.UserName} 点了 {data.Count} 个赞";
        private static string FormatMember(MemberData data) => $"[{data.Gender}] {data.UserName} 进入直播间";
        private static string FormatSocial(SocialData data) => $"{data.UserName} 关注了主播";
        private static string FormatStats(StatsData data)
        {
            return $"当前: {data.Current}, 累计人数: {data.Total}";
        }
        private static string FormatFansclub(FansclubData data) => data.Content ?? "";
        private static string FormatEmoji(EmojiData data) => data.DefaultContent ?? $"表情 {data.EmojiId}";
        private static string FormatRoom(RoomData data) => $"直播间ID: {data.RoomId}";
        private static string FormatRoomStats(RoomStatsData data)
        {
            if (string.IsNullOrWhiteSpace(data.DisplayLong))
                return "";

            try
            {
                var jsonDoc = JsonDocument.Parse(data.DisplayLong);
                var root = jsonDoc.RootElement;

                var current = root.TryGetProperty("current", out var currentElem) ? currentElem.ToString() : "";
                var total = root.TryGetProperty("total", out var totalElem) ? totalElem.ToString() : "";

                return $"当前: {current}, 累计人数: {total}";
            }
            catch
            {
                return data.DisplayLong;
            }
        }

        private static string FormatRank(RankData data)
        {
            if (string.IsNullOrWhiteSpace(data.RanksList))
                return "";

            try
            {
                var names = new List<string>();
                var pattern = @"nick_name='([^']*)'";
                var matches = System.Text.RegularExpressions.Regex.Matches(data.RanksList, pattern);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        names.Add(match.Groups[1].Value);
                    }
                }

                if (names.Count > 0)
                {
                    return $"排行榜: {string.Join(", ", names)}";
                }
                else
                {
                    return "排行榜";
                }
            }
            catch
            {
                return $"排行榜: {data.RanksList.Substring(0, Math.Min(100, data.RanksList.Length))}...";
            }
        }
        private static string FormatControl(ControlData data) => data.Message ?? $"状态: {data.Status}";
        private static string FormatStreamAdaptation(StreamAdaptationData data) => $"流配置: {data.AdaptationType}";
    }
}
