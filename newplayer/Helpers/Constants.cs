using System;
using System.IO;

namespace newplayer.Helpers
{
    /// <summary>
    /// 常量定义文件 - 存储固定配置
    /// </summary>
    public static class Constants
    {
        // 应用程序输出目录的Music文件夹路径
        private static readonly string AppMusicPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Music");

        // 从运行目录向上查找包含mp3文件的Music文件夹，避免命中空目录
        private static readonly string ProjectMusicPath = FindProjectMusicPath();

        /// <summary>
        /// 音乐文件夹路径（智能选择：优先使用项目目录，不存在则使用输出目录）
        /// </summary>
        public static string MusicFolderPath
        {
            get
            {
                // 检查项目目录是否存在Music文件夹且有文件
                if (Directory.Exists(ProjectMusicPath) &&
                    Directory.GetFiles(ProjectMusicPath, "*.mp3").Length > 0)
                {
                    Console.WriteLine($"使用项目目录: {ProjectMusicPath}");
                    return ProjectMusicPath;
                }

                // 否则使用输出目录
                Console.WriteLine($"使用应用程序目录: {AppMusicPath}");
                return AppMusicPath;
            }
        }

        /// <summary>
        /// 支持的音乐文件格式
        /// </summary>
        public const string MusicFileExtension = "*.mp3";

        /// <summary>
        /// 歌曲命名分隔符
        /// </summary>
        public const char SongNameSeparator = '-';

        /// <summary>
        /// 默认音量值
        /// </summary>
        public const int DefaultVolume = 50;

        /// <summary>
        /// 歌曲按钮数量
        /// </summary>
        public const int SongButtonCount = 12;

        /// <summary>
        /// 歌曲文件名格式
        /// </summary>
        public const string SongFileFormat = "歌名-作者.mp3";

        /// <summary>
        /// 固定12首歌曲的显示和播放顺序。
        /// </summary>
        public static readonly string[] FixedSongFileNames =
        {
            "不分手的恋爱-汪苏泷.mp3",
            "你是人间四月天-邵帅.mp3",
            "倒数-G.E.M.邓紫棋.mp3",
            "句号-G.E.M.邓紫棋.mp3",
            "同桌的你-老狼.mp3",
            "够爱-曾沛慈.mp3",
            "最美的期待-周笔畅.mp3",
            "此生不换-青鸟飞鱼.mp3",
            "泡沫-G.E.M.邓紫棋.mp3",
            "玫瑰少年-五月天.mp3",
            "红色高跟鞋-是七叔呢.mp3",
            "追光者-岑宁儿.mp3"
        };

        private static string FindProjectMusicPath()
        {
            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);

            while (directory != null)
            {
                var musicPath = Path.Combine(directory.FullName, "Music");
                if (Directory.Exists(musicPath) &&
                    Directory.GetFiles(musicPath, MusicFileExtension).Length > 0)
                {
                    return musicPath;
                }

                directory = directory.Parent;
            }

            return AppMusicPath;
        }
    }
}
