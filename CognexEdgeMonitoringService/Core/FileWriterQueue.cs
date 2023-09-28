using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;

namespace CognexEdgeMonitoringService.Core
{
    public class FileWriterQueue
    {
        private readonly BlockingCollection<string> _queue = new BlockingCollection<string>();
        private readonly string _filePath;

        public FileWriterQueue(string filePath)
        {
            _filePath = filePath;
            Task.Run(() => ProcessQueue());
        }

        public void Enqueue(string data)
        {
            _queue.Add(data);
        }

        private void ProcessQueue()
        {
            using (StreamWriter writer = new StreamWriter(_filePath, true))
            {
                foreach (var data in _queue.GetConsumingEnumerable())
                {
                    writer.WriteLine(data);
                    writer.Flush();  // Ensure data is written to the file regularly.
                }
            }
        }

        public void CompleteAdding()
        {
            _queue.CompleteAdding();
        }
    }


}