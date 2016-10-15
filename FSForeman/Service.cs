using System.IO;
using System.Threading;
using System.Diagnostics;

namespace FSForeman {
    internal class Service {
        private readonly WebHost webhost;
        private readonly FileCache cache;
        private readonly Watcher watcher;
        private bool isRunning;

        public Service() {
            // Logger
            Logger.FilePath = Path.Combine(Directory.GetCurrentDirectory(), "log.txt");
            Logger.DeleteLogFile();
            Logger.UseTimestamp = true;
            Logger.Output = Logger.OutputType.File;

            // Configuration
            new Configuration("config.db", true);
            var roots = Configuration.Global.Roots;
            Logger.LogLine("Using the following directories as roots:");
            foreach (var r in roots)
                Logger.LogLine($"\t{r}");
            var ignores = Configuration.Global.Ignores;
            Logger.LogLine("Ignoring the following patterns:");
            foreach (var pattern in ignores)
                Logger.LogLine($"\t{pattern}");
            
            // FileCache
            cache = new FileCache();
            
            // Watcher
            watcher = new Watcher(roots, ref cache);

            // Web Host
            webhost = new WebHost("localhost", Configuration.Global.Port, cache, watcher);
        }

        public void Start() {
            // Web Host
            webhost.Start();

            // Watcher
            watcher.Lock();
            watcher.StartWatching();

            // File Cache
            var sw = Stopwatch.StartNew();
            cache.Populate(Configuration.Global.Roots);
            sw.Stop();
            watcher.Unlock();
            Logger.LogLine($"Populated {cache.Size} items in {sw.Elapsed.ToString(@"hh\:mm\:ss\.fff")}.");

            isRunning = true;

            Loop(Configuration.Global.UpdateDelay);
        }

        public void Stop() {
            webhost.Stop();
            isRunning = false;
        }

        private void Loop(int sleeptime) {
            while(isRunning) {
                cache.StartUpdate();
                Thread.Sleep(sleeptime);
            }
        }
    }
}
