using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using newplayer.Core;

namespace newplayer
{
    public partial class Form1 : Form
    {
        private PlaybackController? _playbackController;
        private VolumeController? _volumeController;
        private PlayModeManager? _playModeManager;
        private MusicManager? _musicManager;

        private const int WmHotkey = 0x0312;
        private const int HotkeyTogglePlayPause = 1;
        private const int HotkeyPreviousSong = 2;
        private const int HotkeyNextSong = 3;
        private const uint ModControl = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public Form1()
        {
            InitializeComponent();
            KeyPreview = true;
            KeyDown += Form1_KeyDown;
            InitializeSongButtons();
            InitializeControllers();
            InitializeMusicManager();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterGlobalHotkeys();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            UnregisterGlobalHotkeys();
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmHotkey)
            {
                HandleGlobalHotkey(m.WParam.ToInt32());
                return;
            }

            base.WndProc(ref m);
        }

        private void RegisterGlobalHotkeys()
        {
            RegisterGlobalHotkey(HotkeyTogglePlayPause, Keys.N);
            RegisterGlobalHotkey(HotkeyPreviousSong, Keys.Q);
            RegisterGlobalHotkey(HotkeyNextSong, Keys.E);
        }

        private void RegisterGlobalHotkey(int id, Keys key)
        {
            if (!RegisterHotKey(Handle, id, ModControl, (uint)key))
            {
                Console.WriteLine($"全局快捷键 Ctrl+{key} 注册失败，可能已被其他程序占用。");
            }
        }

        private void UnregisterGlobalHotkeys()
        {
            UnregisterHotKey(Handle, HotkeyTogglePlayPause);
            UnregisterHotKey(Handle, HotkeyPreviousSong);
            UnregisterHotKey(Handle, HotkeyNextSong);
        }

        private void HandleGlobalHotkey(int hotkeyId)
        {
            switch (hotkeyId)
            {
                case HotkeyTogglePlayPause:
                    TogglePlayPause();
                    break;
                case HotkeyPreviousSong:
                    PlayPreviousSong();
                    break;
                case HotkeyNextSong:
                    PlayNextSong();
                    break;
            }
        }

        private void InitializeSongButtons()
        {
            var songButtons = GetSongButtons();

            for (int i = 0; i < songButtons.Length; i++)
            {
                songButtons[i].Tag = i;
            }
        }

        private Button[] GetSongButtons()
        {
            return new[]
            {
                button1, button2, button3, button4, button5, button6,
                button7, button8, button9, button10, button11, button12
            };
        }

        private void InitializeControllers()
        {
            Console.WriteLine("初始化控制器...");

            _playbackController = new PlaybackController();
            _playbackController.PlaybackStarted += OnPlaybackStarted;
            _playbackController.PlaybackStopped += OnPlaybackStopped;
            _playbackController.SongChanged += OnSongChanged;

            Console.WriteLine("播放控制器初始化完成");

            _volumeController = new VolumeController();
            _volumeController.SetPlaybackController(_playbackController);
            _volumeController.VolumeChanged += OnVolumeChanged;

            Console.WriteLine("音量控制器初始化完成");

            _playModeManager = new PlayModeManager();
            _playModeManager.PlayModeChanged += OnPlayModeChanged;

            Console.WriteLine("播放模式管理器初始化完成");
            Console.WriteLine($"当前播放模式: {_playModeManager.CurrentModeName}");

            label10.Text = _volumeController.Volume.ToString();
        }

        private void InitializeMusicManager()
        {
            _musicManager = new MusicManager();
            _musicManager.SongsLoaded += OnSongsLoaded;

            if (_musicManager.SongCount > 0)
            {
                UpdateSongButtons();
                InitializePlaybackControllerWithSongs();
            }
        }

        private void InitializePlaybackControllerWithSongs()
        {
            if (_playbackController != null && _musicManager != null && _musicManager.SongCount > 0)
            {
                var songs = _musicManager.Songs.ToList();
                _playbackController.Initialize(songs, _playModeManager);
                Console.WriteLine($"播放控制器已初始化，加载了 {songs.Count} 首歌曲");
            }
        }

        private void OnSongsLoaded(object? sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    UpdateSongButtons();
                    InitializePlaybackControllerWithSongs();
                }));
            }
            else
            {
                UpdateSongButtons();
                InitializePlaybackControllerWithSongs();
            }
        }

        private void UpdateSongButtons()
        {
            if (_musicManager == null)
            {
                return;
            }

            var songButtons = GetSongButtons();

            for (int i = 0; i < songButtons.Length; i++)
            {
                var button = songButtons[i];
                var song = _musicManager.GetSongByIndex(i);

                if (song != null)
                {
                    button.Tag = song;
                    button.Text = song.SongName;
                    button.Enabled = true;
                    toolTip1.SetToolTip(button, song.DisplayName);
                }
                else
                {
                    button.Tag = null;
                    button.Text = $"歌曲{i + 1}";
                    button.Enabled = false;
                    toolTip1.SetToolTip(button, "暂无歌曲");
                }
            }

            if (!_musicManager.HasEnoughSongs())
            {
                MessageBox.Show(
                    $"检测到 {_musicManager.SongCount} 首歌曲，建议添加 {Helpers.Constants.SongButtonCount} 首歌曲以获得完整体验。\n\n" +
                    $"歌曲格式要求: {Helpers.Constants.SongFileFormat}\n" +
                    $"存放路径: {Helpers.Constants.MusicFolderPath}",
                    "歌曲数量不足",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void OnPlaybackStarted(object? sender, EventArgs e)
        {
            UpdateCurrentPlayingDisplay();
        }

        private void OnPlaybackStopped(object? sender, EventArgs e)
        {
            if (label5.InvokeRequired)
            {
                label5.Invoke(new Action(() => label5.Text = "未播放歌曲"));
            }
            else
            {
                label5.Text = "未播放歌曲";
            }
        }

        private void OnSongChanged(object? sender, SongInfo song)
        {
            UpdateCurrentPlayingDisplay();
        }

        private void OnVolumeChanged(object? sender, int volume)
        {
            if (label10.InvokeRequired)
            {
                label10.Invoke(new Action(() => label10.Text = volume.ToString()));
            }
            else
            {
                label10.Text = volume.ToString();
            }
        }

        private void OnPlayModeChanged(object? sender, PlayMode mode)
        {
            if (label7.InvokeRequired)
            {
                label7.Invoke(new Action(() => label7.Text = PlayModeManager.GetModeName(mode)));
            }
            else
            {
                label7.Text = PlayModeManager.GetModeName(mode);
            }
        }

        private void UpdateCurrentPlayingDisplay()
        {
            if (_playbackController == null)
            {
                return;
            }

            if (label5.InvokeRequired)
            {
                label5.Invoke(new Action(UpdateCurrentPlayingDisplayCore));
            }
            else
            {
                UpdateCurrentPlayingDisplayCore();
            }
        }

        private void UpdateCurrentPlayingDisplayCore()
        {
            var currentSong = _playbackController?.CurrentSong;
            label5.Text = currentSong?.SongName ?? "未播放歌曲";
        }

        private void HandleSongButtonClick(SongInfo song)
        {
            if (_playbackController == null || _musicManager == null)
            {
                MessageBox.Show("播放控制器未初始化或音乐管理器为空", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Console.WriteLine($"点击按钮: {song.SongName}");
            Console.WriteLine($"当前播放: {_playbackController.CurrentSong?.SongName}, 索引: {_playbackController.CurrentSongIndex}");
            Console.WriteLine($"是否正在播放: {_playbackController.IsPlaying}");

            bool isSameSong = _playbackController.CurrentSong?.FilePath == song.FilePath;

            if (isSameSong)
            {
                if (_playbackController.IsPlaying)
                {
                    _playbackController.Pause();
                    button16.Text = "继续播放";
                    ShowPausedSongName();
                    Console.WriteLine($"暂停播放: {song.SongName}");
                }
                else
                {
                    _playbackController.Resume();
                    button16.Text = "暂停播放";
                    UpdateCurrentPlayingDisplay();
                    Console.WriteLine($"继续播放: {song.SongName}");
                }

                return;
            }

            button16.Text = "暂停播放";
            _playbackController.PlaySong(song.FilePath);
            Console.WriteLine($"开始播放新歌曲: {song.SongName}");
        }

        private void SongButton_Click(object sender, EventArgs e)
        {
            if (sender is Button { Tag: SongInfo song })
            {
                HandleSongButtonClick(song);
            }
        }

        private void button13_Click(object sender, EventArgs e)
        {
            _playModeManager?.SwitchMode(PlayMode.Sequential);
        }

        private void button14_Click(object sender, EventArgs e)
        {
            _playModeManager?.SwitchMode(PlayMode.Random);
        }

        private void button15_Click(object sender, EventArgs e)
        {
            _playModeManager?.SwitchMode(PlayMode.Loop);
        }

        private void button16_Click(object sender, EventArgs e)
        {
            TogglePlayPause();
        }

        private void TogglePlayPause()
        {
            if (_playbackController == null)
            {
                MessageBox.Show("播放控制器未初始化", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (_playbackController.IsPlaying)
            {
                _playbackController.Pause();
                button16.Text = "继续播放";
                ShowPausedSongName();
            }
            else if (_playbackController.CurrentSong == null)
            {
                _playbackController.Play(0);
                button16.Text = "暂停播放";
                UpdateCurrentPlayingDisplay();
            }
            else
            {
                _playbackController.Resume();
                button16.Text = "暂停播放";
                UpdateCurrentPlayingDisplay();
            }
        }

        private void PlayPreviousSong()
        {
            if (_playbackController == null)
            {
                return;
            }

            _playbackController.PlayPreviousManual();
            button16.Text = "暂停播放";
            UpdateCurrentPlayingDisplay();
        }

        private void PlayNextSong()
        {
            if (_playbackController == null)
            {
                return;
            }

            _playbackController.PlayNextManual();
            button16.Text = "暂停播放";
            UpdateCurrentPlayingDisplay();
        }

        private void Form1_KeyDown(object? sender, KeyEventArgs e)
        {
            if (!e.Control)
            {
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.N:
                    TogglePlayPause();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.Q:
                    PlayPreviousSong();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
                case Keys.E:
                    PlayNextSong();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    break;
            }
        }

        private void ShowPausedSongName()
        {
            if (_playbackController?.CurrentSong == null)
            {
                return;
            }

            var pausedText = $"{_playbackController.CurrentSong.SongName} (暂停中)";

            if (label5.InvokeRequired)
            {
                label5.Invoke(new Action(() => label5.Text = pausedText));
            }
            else
            {
                label5.Text = pausedText;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _playbackController?.Dispose();
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            _volumeController?.SetVolume(trackBar1.Value);
        }

        private void refreshSongsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _musicManager?.RefreshSongs();
            UpdateSongButtons();
            InitializePlaybackControllerWithSongs();
        }
    }
}
