using System.IO;
using System.Threading;
using System.Diagnostics;

namespace FSForeman {
    class Service {
        private WebHost webhost;
        private FileCache cache;
        private Watcher watcher;
        private bool IsRunning;

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

            IsRunning = true;

            Loop(Configuration.Global.UpdateDelay);
        }

        public void Stop() {
            webhost.Stop();
            IsRunning = false;
        }

        private void Loop(int sleeptime) {
            while(IsRunning) {
                cache.StartUpdate();
                Thread.Sleep(sleeptime);
            }
        }
    }
}
