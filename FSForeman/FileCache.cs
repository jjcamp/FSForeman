using System;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Formatters.Binary;

namespace FSForeman {
    /// <summary>
    /// Caches files.  Once initially populated, The cache does not monitor for filesystem
    /// changes.  A <see cref="Watcher"/> should be used for this purpose.
    /// </summary>
    [Serializable]
    public class FileCache {
        private ConcurrentDictionary<string, FileReference> files;
        private ConcurrentDictionary<ulong, List<string>> hashes;
        
        /// <summary>
        /// Creates a new instance of <see cref="FileCache"/>.
        /// </summary>
        public FileCache() {
            files = new ConcurrentDictionary<string, FileReference>();
            hashes = new ConcurrentDictionary<ulong, List<string>>();
        }

        /// <summary>
        /// Adds a file to the cache.
        /// </summary>
        /// <param name="file">The file to add.</param>
        public void Add(FileInfo file) {
            if (files.ContainsKey(file.FullName)) return;
            var fr = new FileReference(file);
            try {
                files.TryAdd(file.FullName, fr);
            }
            catch (OverflowException) {
                // Thrown on Int32.MaxValue (~2tril)
                // Should be some graceful handling which causes a dictionary split
            }
        }

        /// <summary>
        /// Asynchronously adds a file to the cache.
        /// </summary>
        /// <param name="file">The file to add.</param>
        public async void AddAsync(FileInfo file) {
            await new Task(() => Add(file));
        }

        /// <summary>
        /// Removes a file from the cache.
        /// </summary>
        /// <param name="file">The file to remove.</param>
        public void Remove(FileInfo file) {
            if (!files.ContainsKey(file.FullName)) return;
            FileReference fr;
            if (files.TryRemove(file.FullName, out fr))
                RemoveFromHashes(file.FullName, fr);
        }

        /// <summary>
        /// Asynchronously removes a file from the cache.
        /// </summary>
        /// <param name="file">The file to remove.</param>
        public async void RemoveAsync(FileInfo file) {
            await new Task(() => Remove(file));
        }

        /// <summary>
        /// Updates a file in the cache.
        /// </summary>
        /// <param name="file">The file to update.</param>
        public void Change(FileInfo file) {
            if (!files.ContainsKey(file.FullName))
                Add(file);
            else {
                FileReference fr;
                if (!files.TryGetValue(file.FullName, out fr)) return;
                fr.Change(file);
                RemoveFromHashes(file.FullName, fr);
            }
        }

        /// <summary>
        /// Asynchronously updates a file in the cache.
        /// </summary>
        /// <param name="file">The file to update.</param>
        public async void ChangeAsync(FileInfo file) {
            await new Task(() => Change(file));
        }

        /// <summary>
        /// Begins hashing files.
        /// </summary>
        public void StartUpdate() {
            Parallel.ForEach(files, async kv => {
                if (!kv.Value.Dirty) return;
                var fi = new FileInfo(kv.Key);
                await kv.Value.StartHash(fi, hash => {
                    hashes.AddOrUpdate(hash, new List<string>() { kv.Key }, (h, hl) => {
                        lock (hl) {
                            if (!hl.Contains(kv.Key))
                                hl.Add(kv.Key);
                        }
                        return hl;
                    });
                });
            });
        }

        /// <summary>
        /// Removes a <see cref="FileReference"/> from the hash dictionary.
        /// </summary>
        /// <param name="path">The path to the file to remove.</param>
        /// <param name="fr">The FileReference attached to the path.</param>
        private void RemoveFromHashes(string path, FileReference fr) {
            List<string> hl;
            if (fr.Hash == null || !hashes.TryGetValue(fr.Hash.Value, out hl)) return;
            lock (hl) {
                hl.Remove(path);
            }
        }

        /// <summary>
        /// Recursively counts files.
        /// </summary>
        /// <param name="dir">The directory to crawl.</param>
        /// <param name="ignores">A list of patterns to ignore.</param>
        /// <returns>Number of files found.</returns>
        public uint PreCount(DirectoryInfo dir, List<Regex> ignores) {
            var count = dir.EnumerateDirectories().AsParallel().Aggregate<DirectoryInfo, uint>(0, (current, d) => {
                try {
                    return current + PreCount(d, ignores);
                }
                catch (UnauthorizedAccessException) {
                    return current + 0;
                }
            });
            return count + (uint)dir.GetFiles().Length;
        }

        /// <summary>
        /// Counts the number of files that will be cached and allocates the internal
        /// dictionaries based on that number.
        /// </summary>
        /// <param name="dirs">An array of directories to crawl.</param>
        /// <returns>Number of files found.</returns>
        public uint PreCount(string[] dirs) {
            var ignorePatterns = Configuration.Global.Ignores;
            var ignores = new List<Regex>(ignorePatterns.Length);
            ignores.AddRange(ignorePatterns.Select(p => new Regex(p)));

            var count = dirs.Aggregate<string, uint>(0, (current, d) => current + PreCount(new DirectoryInfo(d), ignores));
            if (!files.IsEmpty) return count; // Dummy check

            // Re-initialize the dictionaries with sizes based on the count
            int newSize;
            try {
                checked {
                    newSize = (int)(count * 1.2);
                }
            }
            catch (OverflowException) {
                newSize = int.MaxValue; // luckily, this number is also a prime
                // Since there is no ConcurrentDictionary constructor allowing a uint,
                // even with gcAllowVeryLargeObjects the max will have to be that
                // of a signed int.
            }
            if (newSize % 2 == 0)
                newSize++;      // Make odd
            while (newSize % 3 == 1 || newSize % 5 == 1 || newSize % 7 == 1)
                newSize += 2;   // Ensure number is not divisible by small primes
            files = new ConcurrentDictionary<string, FileReference>(Environment.ProcessorCount, newSize);
            // hashes is unlikely to be as big as files, but unless the extra memory becomes and issue
            // this is less work than calculating another size.
            hashes = new ConcurrentDictionary<ulong, List<string>>(Environment.ProcessorCount, newSize);

            return count;
        }

        /// <summary>
        /// Recursively populates the file cache.
        /// </summary>
        /// <param name="dir">The directory to crawl.</param>
        /// <param name="ignores">A list of patterns to ignore.</param>
        public void Populate(DirectoryInfo dir, List<Regex> ignores) {
            // There is a significant speedup by processing each directory in its own thread...
            Parallel.ForEach(dir.EnumerateDirectories(), d => {
                if (ignores.Any(regex => regex.IsMatch(d.FullName)))
                    return;
                try {
                    Populate(d, ignores);
                }
                catch (UnauthorizedAccessException) {
                    Logger.LogLine($"Unauthorized Access: Directory {d.FullName}");
                }
            });
            // ... but not for individual files.
            foreach (var f in dir.EnumerateFiles()) {
                if (ignores.Any(regex => regex.IsMatch(f.FullName)))
                    return;
                Add(f);
            }
        }

        /// <summary>
        /// Populates the file cache.
        /// </summary>
        /// <param name="dirs">An array of directories to crawl.</param>
        public void Populate(string[] dirs) {
            // Create the regex's once
            var ignorePatterns = Configuration.Global.Ignores;
            var ignores = new List<Regex>(ignorePatterns.Length);
            ignores.AddRange(ignorePatterns.Select(p => new Regex(p)));

            foreach (var d in dirs)
                Populate(new DirectoryInfo(d), ignores);
        }
        
        /// <summary>
        /// Finds the duplicate files in the cache.
        /// </summary>
        /// <returns>A list of a lists of duplicate files.</returns>
        public List<List<string>> GetDuplicates() {
            var dupes = new List<List<string>>();
            foreach (var kv in hashes) {
                if (kv.Value.Count <= 1) continue;
                var ls = new List<string>(kv.Value.Count);
                ls.AddRange(kv.Value);
                dupes.Add(ls);
            }
            return dupes;
        }

        /// <summary>[DEBUG] Don't use this because it locks the cache.</summary>
        public int GetDirtyCount() {
            return files.Count(kv => kv.Value.Dirty);
        }

        /// <summary>
        /// Saves a <see cref="FSForeman.FileCache"/> to memory.
        /// </summary>
        /// <param name="cache">The cache to save.</param>
        /// <returns>Binary representation of the cache in memory.</returns>
        public static MemoryStream SaveToMemory(FileCache cache) {
            var stream = new MemoryStream();
            var fmt = new BinaryFormatter();
            fmt.Serialize(stream, cache);
            return stream;
        }

        /// <summary>
        /// Loads a <see cref="FileCache"/> from memory.
        /// </summary>
        /// <param name="stream">Binary representation of the cache in memory.</param>
        /// <returns>The loaded cache.</returns>
        public static FileCache LoadFromMemory(MemoryStream stream) {
            var fmt = new BinaryFormatter();
            stream.Seek(0, SeekOrigin.Begin);
            return (FileCache)fmt.Deserialize(stream);
        }
    }
}
