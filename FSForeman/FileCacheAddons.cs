using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;

namespace FSForeman {
    public partial class FileCache {
        /// <summary>
        /// Finds the duplicate files in the cache.
        /// </summary>
        /// <returns>A list of a lists of duplicate files.</returns>
        public List<SizeAndFileList> GetDuplicates() {
            var dupes = new List<SizeAndFileList>();
            foreach (var kv in hashes) {
                if (kv.Value.Count <= 1) continue;
                var verified = VerifyAndGetSize(kv.Value);
                if (verified.Count > 0)
                    dupes.AddRange(verified);
            }
            return dupes;
        }

        /// <summary>
        /// A list of file paths and their common size.
        /// </summary>
        [Serializable]
        public struct SizeAndFileList {
            // A tuple would have worked fine here if this wasn't getting
            // serialized into JSON.
            public long Size;
            public List<string> Files;

            public SizeAndFileList(long size, List<string> files) {
                Size = size;
                Files = files;
            }
        }

        /// <summary>
        /// Checks for hash collisions and adds the file size to the output
        /// </summary>
        /// <param name="fileList">Files with the same hash.</param>
        /// <returns>Zero or more lists of files with the same hash and size.</returns>
        private List<SizeAndFileList> VerifyAndGetSize(IReadOnlyList<string> fileList) {
            var outList = new List<SizeAndFileList>(1); // 99.99999% of cases
            var matches = new List<string>(fileList.Count);
            List<string> reVerify = null;
            var i = 0;  // need to save state on this indexer
            // Get the size of the first available file
            long size0 = 0;
            for (; i < fileList.Count; i++) {
                FileReference fr;
                if (!files.TryGetValue(fileList[i], out fr)) continue;
                size0 = fr.Size;
                matches.Add(fileList[i]);
                break;
            }
            // Check sizes of the remaining files
            for (; i < fileList.Count; i++) {
                FileReference fr;
                if (!files.TryGetValue(fileList[i], out fr)) continue;
                if (fr.Size == size0)
                    matches.Add(fileList[i]);
                else {
                    if (reVerify == null)
                        reVerify = new List<string>(fileList.Count);
                    reVerify.Add(fileList[i]);
                }
            }
            // Add matches to list if needed
            if (matches.Count > 1)
                outList.Add(new SizeAndFileList(size0, matches));
            // Recursive call on reVerify if needed
            if (reVerify != null && reVerify.Count > 1)
                outList.AddRange(VerifyAndGetSize(reVerify));

            return outList;
        }

        /// <summary>
        /// Saves a <see cref="FileCache"/> to memory.
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
