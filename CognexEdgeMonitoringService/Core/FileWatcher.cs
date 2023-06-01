using System;
using System.IO;
using System.Security.AccessControl;
using Org.BouncyCastle.Crypto.Prng.Drbg;

namespace CognexEdgeMonitoringService.Core
{
    public class FileWatcher
    {
        public string FolderPath { get; set; }
        public bool IsRunning { get; set; }
        private FileSystemWatcher _fileSystemWatcher;
        public FileSystemWatcher FileSystemWatcher
        {
            get => _watcher;
        }

        private void CreateFileWatcher()
        {
            File
        }
        public FileWatcher()
        {
            
        }

    }
}