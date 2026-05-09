using System;
using System.Collections.Generic;
using System.Linq;

namespace newplayer.Core
{
    /// <summary>
    /// 播放模式枚举
    /// </summary>
    public enum PlayMode
    {
        Sequential = 0,  // 顺序播放
        Random = 1,      // 随机播放
        Loop = 2         // 单曲循环
    }

    public class PlayModeManager
    {
        private PlayMode _currentMode = PlayMode.Sequential; // 默认顺序播放
        private Random _random = new Random();
        private List<int>? _availableSongs;
        private int _currentSongIndex = -1;
        private int _lastRandomSong = -1; // 记录上一次随机播放的歌曲，避免连续重复

        /// <summary>
        /// 播放模式变化事件
        /// </summary>
        public event EventHandler<PlayMode>? PlayModeChanged;

        /// <summary>
        /// 获取当前播放模式
        /// </summary>
        public PlayMode CurrentMode => _currentMode;

        /// <summary>
        /// 获取当前播放模式的中文名称
        /// </summary>
        public string CurrentModeName => GetModeName(_currentMode);

        /// <summary>
        /// 初始化歌曲列表
        /// </summary>
        /// <param name="songCount">歌曲总数</param>
        public void Initialize(int songCount)
        {
            _availableSongs = Enumerable.Range(0, songCount).ToList();
        }

        /// <summary>
        /// 设置当前歌曲索引
        /// </summary>
        /// <param name="songIndex">歌曲索引</param>
        public void SetCurrentSong(int songIndex)
        {
            if (_availableSongs != null && songIndex >= 0 && songIndex < _availableSongs.Count)
            {
                _currentSongIndex = songIndex;

                // 只在随机播放模式下更新 _lastRandomSong
                if (_currentMode == PlayMode.Random)
                {
                    _lastRandomSong = songIndex;
                }
            }
        }

        /// <summary>
        /// 切换播放模式
        /// </summary>
        /// <param name="mode">目标播放模式</param>
        public void SwitchMode(PlayMode mode)
        {
            if (_currentMode != mode)
            {
                _currentMode = mode;
                PlayModeChanged?.Invoke(this, _currentMode);
            }
        }

        /// <summary>
        /// 获取下一首歌曲的索引
        /// </summary>
        /// <returns>下一首歌曲的索引，如果无法确定则返回-1</returns>
        public int GetNextSongIndex()
        {
            if (_availableSongs == null || _availableSongs.Count == 0)
                return -1;

            // 如果没有当前歌曲，返回第一首
            if (_currentSongIndex < 0 || _currentSongIndex >= _availableSongs.Count)
            {
                return 0;
            }

            switch (_currentMode)
            {
                case PlayMode.Sequential:
                    return GetNextSequentialIndex();
                case PlayMode.Random:
                    return GetNextRandomIndex();
                case PlayMode.Loop:
                    return GetNextLoopIndex();
                default:
                    return GetNextSequentialIndex();
            }
        }

        /// <summary>
        /// 顺序播放模式下获取下一首索引
        /// </summary>
        private int GetNextSequentialIndex()
        {
            if (_currentSongIndex < 0)
                return 0; // 如果当前没有播放，从第一首开始

            int nextIndex = _currentSongIndex + 1;

            // 如果已经是最后一首，循环到第一首
            if (nextIndex >= _availableSongs!.Count)
                nextIndex = 0;

            return nextIndex;
        }

        /// <summary>
        /// 随机播放模式下获取下一首索引
        /// </summary>
        private int GetNextRandomIndex()
        {
            if (_availableSongs!.Count <= 1)
                return 0;

            int randomIndex;
            int maxAttempts = 10; // 防止无限循环
            int attempts = 0;

            do
            {
                randomIndex = _random.Next(0, _availableSongs.Count);
                attempts++;

                // 如果尝试次数过多，直接返回一个索引
                if (attempts >= maxAttempts)
                {
                    // 强制返回不同的索引
                    randomIndex = (_lastRandomSong + 1) % _availableSongs.Count;
                    break;
                }
            }
            while (randomIndex == _lastRandomSong);

            _lastRandomSong = randomIndex;
            return randomIndex;
        }

        /// <summary>
        /// 单曲循环模式下获取下一首索引
        /// </summary>
        private int GetNextLoopIndex()
        {
            // 单曲循环返回当前歌曲索引
            return _currentSongIndex >= 0 ? _currentSongIndex : 0;
        }

        /// <summary>
        /// 获取播放模式的中文名称
        /// </summary>
        /// <param name="mode">播放模式</param>
        /// <returns>中文名称</returns>
        public static string GetModeName(PlayMode mode)
        {
            return mode switch
            {
                PlayMode.Sequential => "顺序播放",
                PlayMode.Random => "随机播放",
                PlayMode.Loop => "单曲循环",
                _ => "顺序播放"
            };
        }

        /// <summary>
        /// 循环切换播放模式
        /// </summary>
        public void CycleNextMode()
        {
            var currentInt = (int)_currentMode;
            var nextInt = (currentInt + 1) % 3; // 0->1->2->0
            SwitchMode((PlayMode)nextInt);
        }
    }
}