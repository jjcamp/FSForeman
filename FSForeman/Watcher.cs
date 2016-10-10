using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FSForeman {
    public class Watcher {
        private List<FileSystemWatcher> watchers;
        private Queue<Action> events;
        private FileCache cache;
        private string[] ignorePatterns;
        private List<Regex> ignores;
        private volatile bool locked = false;
        private volatile bool watching = false;

        /// <summary>
        /// Creates a new <see cref="Watcher"/> instance.
        /// </summary>
        /// <param name="dirs">An array of directories to watch.</param>
        /// <param name="cache">A reference to the <see cref="FileCache"/></param>
        public Watcher(string[] dirs, ref FileCache cache) {
            this.cache = cache;
            events = new Queue<Action>();
            watchers = new List<FileSystemWatcher>();
            foreach (var d in dirs) {
                var fsw = new FileSystemWatcher(d);
                fsw.IncludeSubdirectories = true;
                watchers.Add(fsw);
                AddEventHandlers(fsw);
            }
            ignores = new List<Regex>();
        }

        /// <summary>
        /// Starts watching the specified directories in a worker thread.
        /// </summary>
        public void StartWatching() {
            watching = true;
            foreach (var fsw in watchers)
                fsw.EnableRaisingEvents = true;
            Task.Run(() => {
                while (watching) {
                    // If no events are in queue or watcher is locked, sleep the thread
                    if (!TryDequeue())
                        Thread.Sleep(1);
                }
            });
            Task.Run(() => UpdateIgnores());
        }

        /// <summary>
        /// Stops watching the specified directories.
        /// </summary>
        public void StopWatching() {
            foreach (var fsw in watchers)
                fsw.EnableRaisingEvents = false;
            watching = false;
        }

        /// <summary>
        /// Prevents file system events from firing while still capturing them.
        /// </summary>
        public void Lock() { locked = true; }

        /// <summary>
        /// Allows file system events to fire again, including any queued events.
        /// </summary>
        public void Unlock() { locked = false; }

        private void AddEventHandlers(FileSystemWatcher fsw) {
            fsw.Created += (source, e) => {
                events.Enqueue(() => OnCreated(e.FullPath));
            };
            fsw.Deleted += (source, e) => {
                events.Enqueue(() => OnDeleted(e.FullPath));
            };
            fsw.Renamed += (source, e) => {
                events.Enqueue(() => OnRenamed(e.FullPath, e.OldFullPath));
            };
        }

        private bool TryDequeue() {
            if (!locked && events.Count > 0) {
                var e = events.Dequeue();
                Task.Run(() => e());
                return true;
            }
            return false;
        }

        private void OnCreated(string path) {
            if (!IsIgnored(path)) {
                Logger.LogLine($"New file: {path}");
                cache.Add(new FileInfo(path));
            }
        }

        private void OnDeleted(string path) {
            // Always attempt a delete in case the ignore was added after the
            // file was added to the cache
            Logger.LogLine($"Removed file: {path}");
            cache.Remove(new FileInfo(path));
        }

        private void OnRenamed(string newPath, string oldPath) {
            var ignoreOld = IsIgnored(oldPath);
            var ignoreNew = IsIgnored(newPath);
            if (!ignoreOld && !ignoreNew) {
                Logger.LogLine($"Renamed file: {oldPath} to {newPath}");
                // Perform synchronously
                cache.Remove(new FileInfo(oldPath));
                cache.Add(new FileInfo(newPath));
            }
            else if (ignoreOld)
                OnCreated(newPath);
            else if (ignoreNew)
                OnDeleted(oldPath);
        }

        private void UpdateIgnores() {
            lock (ignores) {
                var confIgnores = Configuration.Global.Ignores;
                if (ignorePatterns != confIgnores) {
                    ignorePatterns = confIgnores;
                    ignores = new List<Regex>(ignorePatterns.Length);
                    foreach (var p in ignorePatterns)
                        ignores.Add(new Regex(p));
                }
            }
        }

        private bool IsIgnored(string path) {
            UpdateIgnores();
            lock (ignores) {
                foreach (var r in ignores) {
                    if (r.IsMatch(path))
                        return true;
                }
            }
            return false;
        }
    }
}
