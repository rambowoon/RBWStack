using System;
using System.IO;

namespace RBWStack.Avalonia.Services;

public static class PathHelper
{
    private static string? _rootDir;

    public static string RootDir
    {
        get
        {
            if (_rootDir != null) return _rootDir;

            string baseDir = AppContext.BaseDirectory;
            // Walk up to find existing RBWStack root (contains data/ or bin/ or www/)
            DirectoryInfo? dir = new DirectoryInfo(baseDir);
            for (int i = 0; i < 6 && dir != null; i++)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "data")) &&
                    (Directory.Exists(Path.Combine(dir.FullName, "www")) || Directory.Exists(Path.Combine(dir.FullName, "bin"))))
                {
                    _rootDir = dir.FullName;
                    return _rootDir;
                }
                dir = dir.Parent;
            }

            _rootDir = baseDir.TrimEnd('\\', '/');
            return _rootDir;
        }
    }

    public static string DataDir
    {
        get
        {
            string p = Path.Combine(RootDir, "data");
            Directory.CreateDirectory(p);
            return p;
        }
    }

    public static string BinDir => Path.Combine(RootDir, "bin");
    public static string WwwDir => Path.Combine(RootDir, "www");
    public static string TmpDir
    {
        get
        {
            string p = Path.Combine(RootDir, "tmp");
            Directory.CreateDirectory(p);
            return p;
        }
    }

    public static string SslDir
    {
        get
        {
            string p = Path.Combine(RootDir, "ssl");
            Directory.CreateDirectory(p);
            return p;
        }
    }

    public static string GetDataFile(string fileName) => Path.Combine(DataDir, fileName);
}

