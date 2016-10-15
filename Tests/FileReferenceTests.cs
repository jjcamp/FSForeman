using System.IO;
using System.Text;
using FSForeman;
using Xunit;

namespace Tests {
    public class FileReferenceTests {
        [Fact]
        public void Constructor() {
            const string fileName = "constructor.fr";

            File.Create(fileName);
            var fi = new FileInfo(fileName);
            
            var fr = new FileReference(fi);

            Assert.Equal(fi.LastWriteTimeUtc, fr.Modified);
            Assert.Equal(fi.Length, fr.Size);
        }

        [Fact]
        public void ConstructorFileNotExist() {
            const string fileName = "constructorFail.fr";
            
            File.Delete(fileName);
            
            Assert.Equal(false, new FileReference(new FileInfo(fileName)).Dirty);
        }

        [Fact]
        public void StartHash() {
            const string fileName = "startHash.fr";
            
            using (var fs = File.OpenWrite(fileName)) {
                var data = Encoding.ASCII.GetBytes("just some data");
                fs.Write(data, 0, data.Length);
            }
            var fi = new FileInfo(fileName);

            var fr = new FileReference(fi);
            fr.StartHash(fi).Wait();

            Assert.NotNull(fr.Hash);
        }
    }
}
