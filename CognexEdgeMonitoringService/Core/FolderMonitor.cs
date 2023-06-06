using System;
using System.IO;

namespace CognexEdgeMonitoringService.Core
{
    public class FolderMonitor
    {
        private readonly FileSystemWatcher watcher;
        private bool isRunning;
        public string CurrentImageName;
        public event EventHandler<FileChangedEventArgs> FileChanged;

        public FolderMonitor(string folderPath)
        {
            watcher = new FileSystemWatcher();
            watcher.Path = folderPath;

            watcher.Created += OnFileCreated;
            //watcher.Deleted += OnFileDeleted;
        }

        public void StartMonitoring()
        {
            if (!isRunning)
            {
                watcher.EnableRaisingEvents = true;
                isRunning = true;
            }
        }

        public void StopMonitoring()
        {
            if (isRunning)
            {
                watcher.EnableRaisingEvents = false;
                isRunning = false;
            }
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            NotifyFileChanged(e.FullPath, FileChangeType.Created);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            NotifyFileChanged(e.FullPath, FileChangeType.Modified);
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            NotifyFileChanged(e.FullPath, FileChangeType.Deleted);
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            NotifyFileChanged(e.FullPath, FileChangeType.Renamed);
        }

        private void NotifyFileChanged(string filePath, FileChangeType changeType)
        {
            FileChanged?.Invoke(this, new FileChangedEventArgs(filePath, changeType));
        }
    }

    public class FileChangedEventArgs : EventArgs
    {
        public string FilePath { get; }
        public FileChangeType ChangeType { get; }

        public FileChangedEventArgs(string filePath, FileChangeType changeType)
        {
            FilePath = filePath;
            ChangeType = changeType;
        }
    }

    public enum FileChangeType
    {
        Created,
        Modified,
        Deleted,
        Renamed
    }

}