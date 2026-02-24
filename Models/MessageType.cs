namespace DouyinLiveReceiver.Models
{
    /// <summary>
    /// 消息类型枚举
    /// </summary>
    public enum MessageType
    {
        All,
        Chat,
        Gift,
        Like,
        Member,
        Social,
        Stats,
        Fansclub,
        Emoji,
        Room,
        RoomStats,
        Rank,
        Control,
        StreamAdaptation
    }

    /// <summary>
    /// 消息类型筛选器
    /// </summary>
    public class MessageTypeFilter
    {
        public MessageType Type { get; set; }
        public string Name { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    /// <summary>
    /// 消息类型辅助类，提供类型解析、图标和颜色映射
    /// </summary>
    public static class MessageTypeHelper
    {
        public static MessageType Parse(string type)
        {
            return type?.ToLower() switch
            {
                "chat" => MessageType.Chat,
                "gift" => MessageType.Gift,
                "like" => MessageType.Like,
                "member" => MessageType.Member,
                "social" => MessageType.Social,
                "stats" => MessageType.Stats,
                "fansclub" => MessageType.Fansclub,
                "emoji" => MessageType.Emoji,
                "room" => MessageType.Room,
                "room_stats" => MessageType.RoomStats,
                "rank" => MessageType.Rank,
                "control" => MessageType.Control,
                "stream_adaptation" => MessageType.StreamAdaptation,
                _ => MessageType.Chat
            };
        }

        public static string GetIcon(MessageType type)
        {
            return type switch
            {
                MessageType.Chat => "💬",
                MessageType.Gift => "🎁",
                MessageType.Like => "❤️",
                MessageType.Member => "🚪",
                MessageType.Social => "⭐",
                MessageType.Stats => "📊",
                MessageType.Fansclub => "💜",
                MessageType.Emoji => "😀",
                MessageType.Room => "🏠",
                MessageType.RoomStats => "📈",
                MessageType.Rank => "🏆",
                MessageType.Control => "⚠️",
                MessageType.StreamAdaptation => "📡",
                _ => "📌"
            };
        }

        public static string GetColor(MessageType type)
        {
            return type switch
            {
                MessageType.Chat => "#2196F3",
                MessageType.Gift => "#E91E63",
                MessageType.Like => "#F44336",
                MessageType.Member => "#4CAF50",
                MessageType.Social => "#FF9800",
                MessageType.Stats => "#9C27B0",
                MessageType.Fansclub => "#673AB7",
                MessageType.Emoji => "#00BCD4",
                MessageType.Room => "#607D8B",
                MessageType.RoomStats => "#795548",
                MessageType.Rank => "#FFC107",
                MessageType.Control => "#F44336",
                MessageType.StreamAdaptation => "#009688",
                _ => "#333333"
            };
        }
    }
}
