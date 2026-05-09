using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace newplayer.Core
{
    /// <summary>
    /// 音乐文件管理模块 - 扫描、解析、排序MP3文件
    /// </summary>
    public class MusicManager
    {
        private List<SongInfo> _songs = new List<SongInfo>();

        /// <summary>
        /// 歌曲列表（只读）
        /// </summary>
        public IReadOnlyList<SongInfo> Songs => _songs.AsReadOnly();

        /// <summary>
        /// 歌曲数量
        /// </summary>
        public int SongCount => _songs.Count;

        /// <summary>
        /// 歌曲扫描完成事件
        /// </summary>
        public event EventHandler? SongsLoaded;

        /// <summary>
        /// 初始化音乐管理器
        /// </summary>
        public MusicManager()
        {
            LoadSongs();
        }

        /// <summary>
        /// 加载所有歌曲
        /// </summary>
        private void LoadSongs()
        {
            try
            {
                // 检查音乐文件夹是否存在
                if (!Directory.Exists(Helpers.Constants.MusicFolderPath))
                {
                    Directory.CreateDirectory(Helpers.Constants.MusicFolderPath);
                    Console.WriteLine($"音乐文件夹已创建: {Helpers.Constants.MusicFolderPath}");
                    return;
                }

                // 扫描所有MP3文件
                var mp3Files = Directory.GetFiles(
                    Helpers.Constants.MusicFolderPath,
                    Helpers.Constants.MusicFileExtension);

                if (mp3Files.Length == 0)
                {
                    Console.WriteLine($"未找到MP3文件，请将歌曲放入 {Helpers.Constants.MusicFolderPath} 文件夹");
                    return;
                }

                var orderedMp3Files = GetOrderedSongFiles(mp3Files);

                // 解析每个文件
                _songs.Clear();
                foreach (var filePath in orderedMp3Files)
                {
                    var songInfo = ParseSongFile(filePath);
                    if (songInfo != null)
                    {
                        _songs.Add(songInfo);
                    }
                }

                // 触发加载完成事件
                SongsLoaded?.Invoke(this, EventArgs.Empty);

                Console.WriteLine($"成功加载 {_songs.Count} 首歌曲");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载歌曲时出错: {ex.Message}");
            }
        }

        private static IEnumerable<string> GetOrderedSongFiles(string[] mp3Files)
        {
            var filesByName = mp3Files.ToDictionary(
                file => Path.GetFileName(file) ?? string.Empty,
                StringComparer.OrdinalIgnoreCase);

            foreach (var fixedFileName in Helpers.Constants.FixedSongFileNames)
            {
                if (filesByName.TryGetValue(fixedFileName, out var filePath))
                {
                    yield return filePath;
                }
            }

            var fixedNames = Helpers.Constants.FixedSongFileNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var extraFile in mp3Files
                .Where(file => !fixedNames.Contains(Path.GetFileName(file) ?? string.Empty))
                .OrderBy(file => Path.GetFileName(file) ?? string.Empty, StringComparer.OrdinalIgnoreCase))
            {
                yield return extraFile;
            }
        }

        /// <summary>
        /// 解析歌曲文件信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>歌曲信息对象，解析失败返回null</returns>
        private SongInfo? ParseSongFile(string filePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);

                // 使用分隔符分割歌名和作者
                var separatorIndex = fileName.LastIndexOf(Helpers.Constants.SongNameSeparator);

                string songName, artistName;

                if (separatorIndex > 0 && separatorIndex < fileName.Length - 1)
                {
                    // 有分隔符的情况：歌名-作者
                    songName = fileName.Substring(0, separatorIndex).Trim();
                    artistName = fileName.Substring(separatorIndex + 1).Trim();
                }
                else
                {
                    // 没有分隔符的情况：使用文件名作为歌名
                    songName = fileName;
                    artistName = "未知作者";
                }

                return new SongInfo
                {
                    FilePath = filePath,
                    FileName = Path.GetFileName(filePath),
                    SongName = songName,
                    ArtistName = artistName,
                    DisplayName = $"{songName} - {artistName}"
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析歌曲文件失败 {filePath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 根据索引获取歌曲信息
        /// </summary>
        public SongInfo? GetSongByIndex(int index)
        {
            if (index >= 0 && index < _songs.Count)
            {
                return _songs[index];
            }
            return null;
        }

        /// <summary>
        /// 刷新歌曲列表（重新扫描文件夹）
        /// </summary>
        public void RefreshSongs()
        {
            LoadSongs();
        }

        /// <summary>
        /// 检查是否有足够的歌曲
        /// </summary>
        public bool HasEnoughSongs()
        {
            return _songs.Count >= Helpers.Constants.SongButtonCount;
        }

        /// <summary>
        /// 获取所有歌曲的路径列表
        /// </summary>
        /// <returns>歌曲路径列表</returns>
        public List<string> GetSongPaths()
        {
            return _songs.Select(s => s.FilePath).ToList();
        }
    }

    /// <summary>
    /// 歌曲信息类
    /// </summary>
    public class SongInfo
    {
        /// <summary>
        /// 文件完整路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件名（包含扩展名）
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// 歌曲名称（从文件名解析）
        /// </summary>
        public string SongName { get; set; } = string.Empty;

        /// <summary>
        /// 作者名称（从文件名解析）
        /// </summary>
        public string ArtistName { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称（用于界面显示）
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
