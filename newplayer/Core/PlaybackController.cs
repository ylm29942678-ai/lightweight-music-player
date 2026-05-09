using System;
using System.Collections.Generic;
using NAudio.Wave;

namespace newplayer.Core
{
    public class PlaybackController : IDisposable
    {
        private WaveOutEvent? _waveOut;
        private AudioFileReader? _audioFileReader;
        private bool _isPlaying = false;
        private int _currentSongIndex = -1;
        private List<SongInfo>? _songList;
        private PlayModeManager? _playModeManager;
        private float _currentVolume = 0.5f; // 默认音量设为0.5（对应50%）

        public event EventHandler? PlaybackStarted;
        public event EventHandler? PlaybackPaused;
        public event EventHandler? PlaybackStopped;
        public event EventHandler<SongInfo>? SongChanged;

        /// <summary>
        /// 获取当前播放状态
        /// </summary>
        public bool IsPlaying => _isPlaying;

        /// <summary>
        /// 获取当前播放歌曲的索引
        /// </summary>
        public int CurrentSongIndex => _currentSongIndex;

        /// <summary>
        /// 当前播放的歌曲信息
        /// </summary>
        public SongInfo? CurrentSong => _currentSongIndex >= 0 && _songList != null && _currentSongIndex < _songList.Count
            ? _songList[_currentSongIndex]
            : null;

        /// <summary>
        /// 初始化播放控制器
        /// </summary>
        /// <param name="songList">歌曲信息列表</param>
        /// <param name="playModeManager">播放模式管理器（可选）</param>
        public void Initialize(List<SongInfo> songList, PlayModeManager? playModeManager = null)
        {
            _songList = songList;

            // 使用传入的PlayModeManager或创建新的
            _playModeManager = playModeManager;
            if (_playModeManager != null)
            {
                _playModeManager.Initialize(songList.Count);
            }
        }

        /// <summary>
        /// 设置播放模式管理器
        /// </summary>
        public void SetPlayModeManager(PlayModeManager playModeManager)
        {
            _playModeManager = playModeManager;
            if (_songList != null)
            {
                _playModeManager.Initialize(_songList.Count);
            }
        }

        /// <summary>
        /// 新增：从头重新播放当前歌曲
        /// </summary>
        public void RestartCurrentSong()
        {
            if (_currentSongIndex >= 0 && _songList != null && _currentSongIndex < _songList.Count)
            {
                try
                {
                    // 切歌/重播时停止旧输出，不触发自动下一首
                    StopCurrentPlayback(notifyStopped: false);

                    // 创建新的音频读取器
                    var song = _songList[_currentSongIndex];
                    _audioFileReader = new AudioFileReader(song.FilePath);
                    _audioFileReader.Volume = _currentVolume;

                    // 重置播放位置到开头
                    _audioFileReader.Position = 0;

                    // 创建新的输出设备
                    _waveOut = new WaveOutEvent();
                    _waveOut.Init(_audioFileReader);
                    _waveOut.PlaybackStopped += OnPlaybackStopped;

                    _waveOut.Play();
                    _isPlaying = true;

                    // 触发事件
                    SongChanged?.Invoke(this, song);
                    PlaybackStarted?.Invoke(this, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"重新播放失败: {ex.Message}");
                    _isPlaying = false;
                }
            }
        }

        /// <summary>
        /// 播放指定索引的歌曲
        /// </summary>
        /// <param name="songIndex">歌曲索引</param>
        public void Play(int songIndex)
        {
            if (_songList == null || songIndex < 0 || songIndex >= _songList.Count)
                return;

            try
            {
                // 如果正在播放同一首歌，暂停它
                if (_currentSongIndex == songIndex && _isPlaying)
                {
                    Pause();
                    return;
                }

                // 切歌时停止旧输出，不触发自动下一首
                StopCurrentPlayback(notifyStopped: false);

                // 更新当前歌曲信息
                _currentSongIndex = songIndex;
                _playModeManager?.SetCurrentSong(songIndex);

                // 获取歌曲
                var song = _songList[songIndex];
                if (string.IsNullOrEmpty(song.FilePath) || !System.IO.File.Exists(song.FilePath))
                {
                    Console.WriteLine($"歌曲文件不存在: {song.FilePath}");
                    return;
                }

                // 创建新的音频读取器
                _audioFileReader = new AudioFileReader(song.FilePath);
                _audioFileReader.Volume = _currentVolume;

                // 创建新的输出设备
                _waveOut = new WaveOutEvent();
                _waveOut.Init(_audioFileReader);
                _waveOut.PlaybackStopped += OnPlaybackStopped;

                _waveOut.Play();
                _isPlaying = true;

                // 触发事件
                SongChanged?.Invoke(this, song);
                PlaybackStarted?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"播放失败: {ex.Message}");
                _isPlaying = false;
                _currentSongIndex = -1;
            }
        }

        /// <summary>
        /// 播放指定路径的歌曲
        /// </summary>
        public void PlaySong(string filePath)
        {
            if (_songList == null) return;

            int songIndex = -1;
            for (int i = 0; i < _songList.Count; i++)
            {
                if (_songList[i].FilePath == filePath)
                {
                    songIndex = i;
                    break;
                }
            }

            if (songIndex >= 0)
            {
                Play(songIndex);
            }
        }

        /// <summary>
        /// 暂停播放
        /// </summary>
        public void Pause()
        {
            if (_waveOut != null && _isPlaying)
            {
                _waveOut.Pause();
                _isPlaying = false;
                PlaybackPaused?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 恢复播放
        /// </summary>
        public void Resume()
        {
            if (_waveOut != null && !_isPlaying)
            {
                _waveOut.Play();
                _isPlaying = true;
                PlaybackStarted?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 停止播放
        /// </summary>
        public void Stop()
        {
            StopCurrentPlayback(notifyStopped: true);
        }

        /// <summary>
        /// 内部停止方法
        /// </summary>
        private void StopCurrentPlayback(bool notifyStopped)
        {
            if (_waveOut != null)
            {
                _waveOut.PlaybackStopped -= OnPlaybackStopped;
                _waveOut.Stop();
                _waveOut.Dispose();
                _waveOut = null;
            }

            if (_audioFileReader != null)
            {
                _audioFileReader.Dispose();
                _audioFileReader = null;
            }

            _isPlaying = false;
            if (notifyStopped)
            {
                PlaybackStopped?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// 播放下一首歌曲
        /// </summary>
        public void PlayNext()
        {
            if (_playModeManager == null || _songList == null || _songList.Count == 0)
                return;

            int nextIndex = _playModeManager.GetNextSongIndex();
            if (nextIndex >= 0 && nextIndex < _songList.Count)
            {
                Play(nextIndex);
            }
        }

        /// <summary>
        /// 手动播放上一首；如果当前是第一首，则重播第一首。
        /// </summary>
        public void PlayPreviousManual()
        {
            if (_songList == null || _songList.Count == 0)
            {
                return;
            }

            if (_currentSongIndex <= 0)
            {
                if (_currentSongIndex == 0)
                {
                    RestartCurrentSong();
                }
                else
                {
                    Play(0);
                }

                return;
            }

            Play(_currentSongIndex - 1);
        }

        /// <summary>
        /// 手动播放下一首；如果当前是最后一首，则重播最后一首。
        /// </summary>
        public void PlayNextManual()
        {
            if (_songList == null || _songList.Count == 0)
            {
                return;
            }

            if (_currentSongIndex < 0)
            {
                Play(0);
                return;
            }

            if (_currentSongIndex >= _songList.Count - 1)
            {
                RestartCurrentSong();
                return;
            }

            Play(_currentSongIndex + 1);
        }

        /// <summary>
        /// 播放完成事件处理
        /// </summary>
        private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
        {
            if (!ReferenceEquals(sender, _waveOut))
            {
                return;
            }

            if (e.Exception != null)
            {
                Console.WriteLine($"播放异常: {e.Exception.Message}");
            }

            _isPlaying = false;
            PlaybackStopped?.Invoke(this, EventArgs.Empty);

            // 只有自然播放结束才从这里进入下一首；手动点歌会先解除事件绑定。
            if (e.Exception == null && _songList != null && _songList.Count > 0)
            {
                PlayNext();
            }
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="volume">音量值 (0.0 - 1.0)</param>
        public void SetVolume(float volume)
        {
            _currentVolume = Math.Clamp(volume, 0.0f, 1.0f);
            if (_audioFileReader != null)
            {
                _audioFileReader.Volume = _currentVolume;
            }
        }

        /// <summary>
        /// 获取当前音量
        /// </summary>
        public float GetVolume()
        {
            return _currentVolume;
        }

        /// <summary>
        /// 获取歌曲总数
        /// </summary>
        public int GetSongCount()
        {
            return _songList?.Count ?? 0;
        }

        /// <summary>
        /// 获取指定索引的歌曲信息
        /// </summary>
        public SongInfo? GetSongByIndex(int index)
        {
            return _songList != null && index >= 0 && index < _songList.Count
                ? _songList[index]
                : null;
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}
