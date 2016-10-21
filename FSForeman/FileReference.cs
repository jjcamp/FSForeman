using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Data.HashFunction;

namespace FSForeman {
    /// <summary>
    /// A class which holds hash information about a file.
    /// NOTE: To reduce memory footprint, this class does not actually know the
    /// path of the file it is referencing.  This information is stored in a
    /// <see cref="FileCache"/>.
    /// </summary>
    [Serializable]
    public class FileReference {
        // Non-Serialized Fields
        [NonSerialized]
        private bool isHashing;
        [NonSerialized]
        private CancellationTokenSource cts;

        public delegate void OnHashFinished(ulong hash);

        // Properties

        /// <summary>Hash for the file</summary>
        public ulong? Hash { get; private set; }
        /// <summary>An object describing when the file was last written</summary>
        public DateTime Modified { get; private set; }
        /// <summary>The size of the file in bytes</summary>
        public long Size { get; private set; }
        /// <summary>A bool which determines whether or not the hash string is current</summary>
        public bool Dirty { get; private set; }


        /// <summary>
        /// Creates a new <see cref="FileReference"/> instance.
        /// </summary>
        /// <param name="file">A <see cref="FileInfo"/>  object.</param>
        /// <param name="startHash">If true, immediately start a new task to hash the file.</param>
        public FileReference(FileInfo file, bool startHash = false) {
            try {
                Hash = null;
                Modified = file.LastWriteTimeUtc;
                Size = file.Length;
                Dirty = true;
                if (startHash)
                    Task.Run(() => StartHash(file));
            }
            catch (FileNotFoundException) {
                // File was removed or renamed during construction, just set dirty to false and
                // ignore since the reference will soon be removed.
                Modified = new DateTime();
                Size = 0;
                Dirty = false;
            }
        }

        /// <summary>
        /// Used to tell the instance that the file has changed, cancelling any hashes
        /// in progress and marking the <see cref="FileReference"/>  as dirty.
        /// </summary>
        /// <param name="file">A <see cref="FileInfo"/>  object.</param>
        /// <param name="startHash">If true, immediately start a new task to hash the file.</param>
        public void Change(FileInfo file, bool startHash = false) {
            if (isHashing)
                cts?.Cancel();
            Modified = file.LastWriteTimeUtc;
            Size = file.Length;
            Dirty = true;
            if (startHash)
                Task.Run(() => StartHash(file));
        }

        /// <summary>
        /// Asynchronously hashes the file.
        /// </summary>
        /// <param name="file">A <see cref="FileInfo"/>  object.</param>
        /// <param name="func">An optional callback function which is only called on
        /// successful completion.</param>
        /// <returns></returns>
        public async Task StartHash(FileInfo file, OnHashFinished func = null) {
            try {
                isHashing = true;
                cts = new CancellationTokenSource();
                var hashTask = Task.Factory.StartNew(() => GetHash(file), cts.Token);
                Hash = await hashTask;
                isHashing = false;
                if (!hashTask.IsCanceled && Hash != null) {
                    Dirty = false;
                    func?.Invoke(Hash.Value);
                }
            }
            catch (Exception e) {
                Logger.LogLine($"Exception hashing file {file.FullName}: {e.Message}");
            }
        }

        /// <summary>
        /// Computes a 64-bit hash for a file.
        /// </summary>
        /// <param name="file">File to hash</param>
        /// <returns>ulong hash or null if the hash failed.</returns>
        private static ulong? GetHash(FileInfo file) {
            byte[] bhash;
            var xh = new xxHash(64);
            try {
                using (var fs = file.OpenRead()) {
                    bhash = xh.ComputeHash(fs);
                }
            }
            catch (IOException) {
                return null;
            }
            ulong lhash = 0;
            for (var i = 0; i < bhash.Length; i++)
                lhash |= (ulong)bhash[i] << (8 * i);
            return lhash;
        }
    }
}
