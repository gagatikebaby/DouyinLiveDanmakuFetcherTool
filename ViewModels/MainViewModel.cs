using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using DouyinLiveReceiver.Models;
using DouyinLiveReceiver.Services;

namespace DouyinLiveReceiver.ViewModels
{
    public class MessageListItem
    {
        public MessageType Type { get; set; }
        public string Icon { get; set; } = "";
        public string Color { get; set; } = "";
        public string DisplayText { get; set; } = "";
        public string Time { get; set; } = "";
        public LiveMessage? RawMessage { get; set; }
    }

    /// <summary>
    /// 主视图模型，管理应用的核心业务逻辑
    /// </summary>
    public class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly UdpReceiverService _receiverService;
        private readonly PythonRunnerService _runnerService;

        private string _statusText = "未连接";
        private string _liveId = "";
        private int _messageCount;
        private bool _isListening;
        private MessageType _selectedFilterType = MessageType.All;
        private readonly HashSet<string> _processedMessageIds = new HashSet<string>();
        private readonly Dictionary<string, DateTime> _recentMessageHashes = new Dictionary<string, DateTime>();
        private MessageListItem? _selectedMessage;
        private bool _pauseUpdate = false;
        private int _cachedMessagesCount = 0;

        private ObservableCollection<MessageListItem> _allMessages { get; } = new ObservableCollection<MessageListItem>();

        public ObservableCollection<MessageListItem> Messages { get; } = new ObservableCollection<MessageListItem>();

        public ObservableCollection<MessageTypeFilter> FilterTypes { get; } = new ObservableCollection<MessageTypeFilter>();

        public ObservableCollection<string> RunnerLogs { get; } = new ObservableCollection<string>();

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string LiveId
        {
            get => _liveId;
            set
            {
                if (SetProperty(ref _liveId, value))
                {
                    StartCommand.RaiseCanExecuteChanged();
                }
            }
        }

        public int MessageCount
        {
            get => _messageCount;
            set => SetProperty(ref _messageCount, value);
        }

        public bool IsListening
        {
            get => _isListening;
            set
            {
                SetProperty(ref _isListening, value);
                StartCommand.RaiseCanExecuteChanged();
                StopCommand.RaiseCanExecuteChanged();
            }
        }

        public MessageTypeFilter SelectedFilter
        {
            get => FilterTypes.FirstOrDefault(f => f.Type == _selectedFilterType) ?? FilterTypes[0];
            set
            {
                if (value != null && value.Type != _selectedFilterType)
                {
                    _selectedFilterType = value.Type;
                    OnPropertyChanged(nameof(SelectedFilter));
                    ApplyFilter();
                }
            }
        }

        public RelayCommand StartCommand { get; }
        public RelayCommand StopCommand { get; }
        public RelayCommand ClearCommand { get; }
        public RelayCommand ClearLogsCommand { get; }
        public RelayCommand CopyMessageCommand { get; }
        public RelayCommand TogglePauseCommand { get; }

        public MessageListItem? SelectedMessage
        {
            get => _selectedMessage;
            set
            {
                if (SetProperty(ref _selectedMessage, value))
                {
                    CopyMessageCommand?.RaiseCanExecuteChanged();
                    System.Diagnostics.Debug.WriteLine($"[SelectedMessage] 更新: {value?.DisplayText ?? "null"}");
                }
            }
        }

        public MainViewModel()
        {
            _receiverService = new UdpReceiverService(9999);

            _receiverService.OnMessageReceived -= OnMessageReceived;
            _receiverService.OnMessageReceived += OnMessageReceived;

            _receiverService.OnError -= OnError;
            _receiverService.OnError += OnError;

            _receiverService.OnStarted -= () =>
            {
                StatusText = "UDP监听已启动";
            };
            _receiverService.OnStarted += () =>
            {
                StatusText = "UDP监听已启动";
            };

            _receiverService.OnStopped -= () =>
            {
                StatusText = $"UDP监听已停止 (共接收 {_receiverService.ReceivedMessageCount} 条消息)";
            };
            _receiverService.OnStopped += () =>
            {
                StatusText = $"UDP监听已停止 (共接收 {_receiverService.ReceivedMessageCount} 条消息)";
            };

            _runnerService = new PythonRunnerService();
            _runnerService.OnOutputReceived += OnRunnerOutput;
            _runnerService.OnErrorReceived += OnRunnerError;
            _runnerService.OnProcessExited += OnProcessExited;

            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            _runnerService.Mode = RunMode.Exe;
            _runnerService.ExePath = Path.Combine(appDir, "Python_Cli.exe");

            InitializeFilters();

            StartCommand = new RelayCommand(
                _ => StartFetching(),
                _ => !IsListening && !string.IsNullOrWhiteSpace(LiveId)
            );
            StopCommand = new RelayCommand(
                _ => StopFetching(),
                _ => IsListening
            );
            ClearCommand = new RelayCommand(_ => ClearMessages());
            ClearLogsCommand = new RelayCommand(_ => RunnerLogs.Clear());
            CopyMessageCommand = new RelayCommand(
                param => CopyMessage(param as MessageListItem)
            );
            TogglePauseCommand = new RelayCommand(_ => TogglePause());
        }

        public bool PauseUpdate
        {
            get => _pauseUpdate;
            private set
            {
                if (SetProperty(ref _pauseUpdate, value))
                {
                    if (_pauseUpdate)
                    {
                        _cachedMessagesCount = 0;
                        StatusText = "UI更新：已暂停 (已缓存 0 条)";
                    }
                    else
                    {
                        StatusText = $"UI更新：已恢复 (显示 {_cachedMessagesCount} 条缓存消息)";
                        RefreshMessagesDisplay();
                    }
                }
            }
        }

        private void InitializeFilters()
        {
            FilterTypes.Clear();
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.All, Name = "全部", Icon = "📋" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Chat, Name = "弹幕", Icon = "💬" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Gift, Name = "礼物", Icon = "🎁" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Like, Name = "点赞", Icon = "❤️" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Member, Name = "进场", Icon = "🚪" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Social, Name = "关注", Icon = "⭐" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Stats, Name = "统计", Icon = "📊" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Fansclub, Name = "粉丝团", Icon = "💜" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Emoji, Name = "表情", Icon = "😀" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Room, Name = "房间", Icon = "🏠" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.RoomStats, Name = "房间统计", Icon = "📈" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Rank, Name = "榜单", Icon = "🏆" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.Control, Name = "控制", Icon = "⚠️" });
            FilterTypes.Add(new MessageTypeFilter { Type = MessageType.StreamAdaptation, Name = "流适配", Icon = "📡" });

            SelectedFilter = FilterTypes[0];
        }

        private void ApplyFilter()
        {
            RefreshMessagesDisplay();
        }

        private void StartFetching()
        {
            if (string.IsNullOrWhiteSpace(LiveId))
            {
                MessageBox.Show("请输入直播间ID", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _receiverService.Start();
            StatusText = "UDP监听已启动...";

            if (_runnerService.Start(LiveId))
            {
                IsListening = true;
                StatusText = $"正在监听直播间: {LiveId}";
            }
            else
            {
                _receiverService.Stop();
                StatusText = "启动失败";
            }
        }

        private void StopFetching()
        {
            _runnerService.Stop();
            _receiverService.Stop();
            IsListening = false;
            StatusText = "已停止";
        }

        private void OnMessageReceived(LiveMessage message)
        {
            var messageLiveId = message.LiveId ?? "";

            if (!string.IsNullOrEmpty(_liveId) && messageLiveId != _liveId)
            {
                System.Diagnostics.Debug.WriteLine($"[过滤] 跳过其他直播间消息: {messageLiveId}");
                return;
            }

            var dataString = message.Data?.ToString() ?? "";
            var dataHash = dataString.GetHashCode();
            var preciseTimestamp = Math.Floor(message.Timestamp * 1000) / 1000;
            var messageId = $"{message.Type}_{_liveId}_{preciseTimestamp}_{dataHash}";

            var messageHash = $"{message.Type}_{_liveId}_{dataString.GetHashCode()}";

            System.Diagnostics.Debug.WriteLine($"[消息] ID: {messageId}, 哈希: {messageHash}, 原始时间戳: {message.Timestamp}, 精确时间戳: {preciseTimestamp}");

            Application.Current?.Dispatcher.Invoke(() =>
            {
                lock (_recentMessageHashes)
                {
                    var expiredKeys = _recentMessageHashes.Where(kvp => 
                        (DateTime.Now - kvp.Value).TotalSeconds > 5).Select(kvp => kvp.Key).ToList();
                    
                    foreach (var key in expiredKeys)
                    {
                        _recentMessageHashes.Remove(key);
                    }
                }
                
                bool isRecentDuplicate = false;
                lock (_recentMessageHashes)
                {
                    if (_recentMessageHashes.ContainsKey(messageHash))
                    {
                        isRecentDuplicate = true;
                        System.Diagnostics.Debug.WriteLine($"[时间窗口去重] 跳过5秒内重复消息，哈希: {messageHash}");
                    }
                    else
                    {
                        _recentMessageHashes[messageHash] = DateTime.Now;
                    }
                }
                
                if (isRecentDuplicate) return;

                bool isDuplicate;
                lock (_processedMessageIds)
                {
                    isDuplicate = _processedMessageIds.Contains(messageId);
                    if (!isDuplicate)
                    {
                        _processedMessageIds.Add(messageId);
                    }
                }

                if (isDuplicate)
                {
                    System.Diagnostics.Debug.WriteLine($"[ID去重] 跳过重复消息: {messageId}");
                    return;
                }

                var item = CreateMessageItem(message);

                _allMessages.Insert(0, item);

                while (_allMessages.Count > 1000)
                {
                    var removed = _allMessages.LastOrDefault();
                    if (removed != null)
                    {
                        _allMessages.Remove(removed);
                    }
                }

                lock (_processedMessageIds)
                {
                    if (_processedMessageIds.Count > 2000)
                    {
                        var recentIds = _processedMessageIds.TakeLast(1000).ToHashSet();
                        _processedMessageIds.Clear();
                        foreach (var id in recentIds)
                        {
                            _processedMessageIds.Add(id);
                        }
                    }
                }

                UpdateMessagesDisplay(item);
            });
        }

        private void RefreshMessagesDisplay()
        {
            var currentSelected = _selectedMessage;

            var filteredMessages = _selectedFilterType == MessageType.All
                ? _allMessages
                : _allMessages.Where(m => m.Type == _selectedFilterType);

            Messages.Clear();
            foreach (var msg in filteredMessages)
            {
                Messages.Add(msg);
            }

            MessageCount = Messages.Count;

            if (currentSelected != null && filteredMessages.Contains(currentSelected))
            {
                SelectedMessage = currentSelected;
            }
            else
            {
                SelectedMessage = null;
            }
        }

        private void UpdateMessagesDisplay(MessageListItem newItem)
        {
            if (_pauseUpdate)
            {
                _cachedMessagesCount++;
                StatusText = $"UI更新：已暂停 (已缓存 {_cachedMessagesCount} 条)";
                return;
            }

            var shouldAdd = _selectedFilterType == MessageType.All || newItem.Type == _selectedFilterType;

            if (shouldAdd)
            {
                Messages.Insert(0, newItem);
                MessageCount = Messages.Count;

                while (Messages.Count > 1000)
                {
                    Messages.RemoveAt(Messages.Count - 1);
                }
            }
        }

        private MessageListItem CreateMessageItem(LiveMessage message)
        {
            var msgType = MessageTypeHelper.Parse(message.Type);
            var time = DateTime.Now.ToString("HH:mm:ss");

            return new MessageListItem
            {
                Type = msgType,
                Icon = MessageTypeHelper.GetIcon(msgType),
                Color = MessageTypeHelper.GetColor(msgType),
                DisplayText = MessageParser.FormatMessage(message),
                Time = time,
                RawMessage = message
            };
        }

        private void ClearMessages()
        {
            _allMessages.Clear();
            Messages.Clear();
            lock (_processedMessageIds)
            {
                _processedMessageIds.Clear();
            }
            lock (_recentMessageHashes)
            {
                _recentMessageHashes.Clear();
            }
            MessageCount = 0;
        }

        private void OnRunnerOutput(string output)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                RunnerLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] {output}");
                while (RunnerLogs.Count > 100)
                {
                    RunnerLogs.RemoveAt(RunnerLogs.Count - 1);
                }
            });
        }

        private void OnRunnerError(string error)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                RunnerLogs.Insert(0, $"[{DateTime.Now:HH:mm:ss}] ❌ {error}");
            });
        }

        private void OnProcessExited()
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                IsListening = false;
                StatusText = "进程已退出";
            });
        }

        private void OnError(string error)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(error, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }

        private void TogglePause()
        {
            PauseUpdate = !PauseUpdate;
        }

        private void CopyMessage(MessageListItem? msg)
        {
            var messageToCopy = msg ?? SelectedMessage;

            if (messageToCopy == null)
            {
                StatusText = "没有可复制的消息";
                return;
            }

            var copyText = $"[{messageToCopy.Time}] {messageToCopy.DisplayText}";
            Clipboard.SetText(copyText);
            StatusText = "已复制消息到剪贴板";
        }

        #region IDisposable Implementation

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    StopFetching();
                    
                    lock (_processedMessageIds)
                    {
                        _processedMessageIds.Clear();
                    }
                    
                    lock (_recentMessageHashes)
                    {
                        _recentMessageHashes.Clear();
                    }
                    
                    _allMessages.Clear();
                    Messages.Clear();
                    
                    if (_receiverService != null)
                    {
                        _receiverService.OnMessageReceived -= OnMessageReceived;
                        _receiverService.OnError -= OnError;
                        _receiverService.OnStarted -= () => StatusText = "UDP监听已启动";
                        _receiverService.OnStopped -= () => StatusText = $"UDP监听已停止 (共接收 {_receiverService.ReceivedMessageCount} 条消息)";
                        _receiverService.Dispose();
                    }
                    
                    if (_runnerService != null)
                    {
                        _runnerService.OnOutputReceived -= OnRunnerOutput;
                        _runnerService.OnErrorReceived -= OnRunnerError;
                        _runnerService.OnProcessExited -= OnProcessExited;
                        _runnerService.Dispose();
                    }
                }
                
                _disposed = true;
            }
        }

        #endregion
    }
}
