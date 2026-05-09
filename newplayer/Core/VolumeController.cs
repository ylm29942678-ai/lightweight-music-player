using System;

namespace newplayer.Core
{
    public class VolumeController
    {
        private PlaybackController? _playbackController;
        private int _volume = 50; // 默认音量50%

        /// <summary>
        /// 音量变化事件
        /// </summary>
        public event EventHandler<int>? VolumeChanged;

        /// <summary>
        /// 获取当前音量值 (0-100)
        /// </summary>
        public int Volume => _volume;

        /// <summary>
        /// 设置播放控制器
        /// </summary>
        /// <param name="playbackController">播放控制器</param>
        public void SetPlaybackController(PlaybackController playbackController)
        {
            _playbackController = playbackController;
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="volume">音量值 (0-100)</param>
        // 在VolumeController的SetVolume方法中确保通知PlaybackController
        public void SetVolume(int volume)
        {
            // 限制音量范围在0-100之间
            _volume = Math.Clamp(volume, 0, 100);

            // 转换为0.0-1.0的范围并应用到播放器
            float normalizedVolume = _volume / 100.0f;
            _playbackController?.SetVolume(normalizedVolume);

            // 触发音量变化事件
            VolumeChanged?.Invoke(this, _volume);
        }

        /// <summary>
        /// 增加音量
        /// </summary>
        /// <param name="increment">增加量</param>
        public void IncreaseVolume(int increment = 5)
        {
            SetVolume(_volume + increment);
        }

        /// <summary>
        /// 减少音量
        /// </summary>
        /// <param name="decrement">减少量</param>
        public void DecreaseVolume(int decrement = 5)
        {
            SetVolume(_volume - decrement);
        }

        /// <summary>
        /// 静音/取消静音
        /// </summary>
        public void ToggleMute()
        {
            if (_volume > 0)
            {
                // 保存当前音量并设置为0
                _lastVolumeBeforeMute = _volume;
                SetVolume(0);
            }
            else
            {
                // 恢复为静音前的音量
                SetVolume(_lastVolumeBeforeMute);
            }
        }

        /// <summary>
        /// 获取规范化音量 (0.0-1.0)
        /// </summary>
        public float GetNormalizedVolume()
        {
            return _volume / 100.0f;
        }

        private int _lastVolumeBeforeMute = 50; // 静音前保存的音量
    }
}