using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace FSForeman {
    /// <summary>
    /// A class which holds hash information about a file.
    /// NOTE: To reduce memory footprint, this class does not actually know the
    /// path of the file it is referencing.  This information is stored in a
    /// <see cref="FSForeman.FileCache"/>.
    /// </summary>
    [Serializable]
    class FileReference {
        // Serialized Fields
        private string hash;
        private DateTime modified;
        private long size;

        // Non-Serialized Fields
        [NonSerialized]
        private bool isHashing = false;
        [NonSerialized]
        private CancellationTokenSource cts = null;

        public delegate void OnHashFinished(string hash);

        // Properties (cannot serialize a property directly, thus the fields above)

        /// <summary>MD5 Hash for the file</summary>
        public string Hash { get { return hash; } }
        /// <summary>An object describing when the file was last written</summary>
        public DateTime Modified { get { return modified; } }
        /// <summary>The size of the file in bytes
        public long Size { get { return size; } }
        /// <summary>A bool which determines whether or not the hash string is current</summary>
        public bool Dirty { get; private set; }


        /// <summary>
        /// Creates a new <see cref="FSForeman.FileReference"/> instance.
        /// </summary>
        /// <param name="file">A <see cref="System.IO.FileInfo"/>  object.</param>
        /// <param name="startHash">If true, immediately start a new task to hash the file.</param>
        public FileReference(FileInfo file, bool startHash = false) {
            try {
                hash = null;
                modified = file.LastWriteTimeUtc;
                size = file.Length;
                Dirty = true;
                if (startHash)
                    Task.Run(() => StartHash(file));
            }
            catch (FileNotFoundException) {
                // File was removed or renamed during construction, just set dirty to false and
                // ignore since the reference will soon be removed.
                modified = new DateTime();
                size = 0;
                Dirty = false;
            }
        }

        /// <summary>
        /// Used to tell the instance that the file has changed, cancelling any hashes
        /// in progress and marking the <see cref="FSForeman.FileReference"/>  as dirty.
        /// </summary>
        /// <param name="file">A <see cref="System.IO.FileInfo"/>  object.</param>
        /// <param name="startHash">If true, immediately start a new task to hash the file.</param>
        public void Change(FileInfo file, bool startHash = false) {
            if (isHashing && cts != null)
                cts.Cancel();
            modified = file.LastWriteTimeUtc;
            size = file.Length;
            Dirty = true;
            if (startHash)
                Task.Run(() => StartHash(file));
        }

        /// <summary>
        /// Asynchronously hashes the file.
        /// </summary>
        /// <param name="file">A <see cref="System.IO.FileInfo"/>  object.</param>
        /// <param name="func">An optional callback function which is only called on
        /// successful completion.</param>
        /// <returns></returns>
        public async Task StartHash(FileInfo file, OnHashFinished func = null) {
            try {
                isHashing = true;
                cts = new CancellationTokenSource();
                var hashTask = Task.Factory.StartNew(() => GetHexStringHash(file), cts.Token);
                hash = await hashTask;
                isHashing = false;
                if (!hashTask.IsCanceled && hash != null) {
                    Dirty = false;
                    func?.Invoke(hash);
                }
            }
            catch (Exception e) {
                Logger.LogLine($"Exception hashing file {file.FullName}: {e.Message}");
            }
        }

        /// <summary>
        /// Computes an MD5 Hash for a file as a hexadecimal string.
        /// </summary>
        /// <param name="file">File to hash</param>
        /// <returns>String with the hash or null if the hash failed.</returns>
        private static string GetHexStringHash(FileInfo file) {
            byte[] bhash;
            var md5 = new MD5CryptoServiceProvider();
            try {
                using (var fs = file.OpenRead()) {
                    bhash = md5.ComputeHash(fs);   
                }
            }
            catch (IOException) {
                return null;
            }
            var c = new char[bhash.Length * 2];
            int b;
            for (var i = 0; i < bhash.Length; i++) {
                b = bhash[i] >> 4;
                c[i * 2] = (char)(87 + b + (((b - 10) >> 31) & -39));
                b = bhash[i] & 0xF;
                c[i * 2 + 1] = (char)(87 + b + (((b - 10) >> 31) & -39));
            }
            return new string(c);
        }
    }
}
