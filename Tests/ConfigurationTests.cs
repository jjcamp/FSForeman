using System;
using System.IO;
using Xunit;
using System.Reflection;
using FSForeman;

namespace Tests {
    public class ConfigurationTests : IDisposable {
        private readonly Configuration conf;
        private readonly string[] defaultIgnores;
        private readonly string[] defaultRoots;
        private readonly object defaultOtherOpts;

        private readonly string fileName;

        public ConfigurationTests() {
            //const string fileName = "test.db";
            fileName = new Random().Next().ToString() + ".db";
            conf = new Configuration(fileName);

            defaultIgnores =
                (string[])conf.GetType().GetField("DefaultIgnoreDirs", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(conf);
            defaultRoots =
                (string[])conf.GetType().GetField("DefaultRoots", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(conf);
            defaultOtherOpts = conf.GetType().GetNestedType("OtherOpts", BindingFlags.NonPublic).GetConstructor(new Type[0])?.Invoke(null);
        }

        public void Dispose() {
            for (var i = 0; i < 10000; i++) {
                try {
                    File.Delete(fileName);
                    break;
                }
                catch (IOException) {
                    System.Threading.Thread.Sleep(0);
                }
            }
        }

        [Fact]
        public void ReadIgnores() {
            Assert.Equal(defaultIgnores, conf.Ignores);
        }

        [Fact]
        public void AddIgnore() {
            const string testString = "test";

            var correctIgnores = new string[defaultIgnores.Length + 1];
            defaultIgnores.CopyTo(correctIgnores, 0);
            correctIgnores[defaultIgnores.Length] = testString;

            conf.AddIgnore(testString);
            Assert.Equal(correctIgnores, conf.Ignores);
        }

        [Fact]
        public void RemoveIgnore() {
            const string testString = "test";

            conf.AddIgnore(testString);
            conf.RemoveIgnore(testString);
            Assert.Equal(defaultIgnores, conf.Ignores);
        }

        [Fact]
        public void ReadRoots() {
            Assert.Equal(defaultRoots, conf.Roots);
        }

        [Fact]
        public void AddRoot() {
            const string testString = "test";

            var correctRoots = new string[defaultRoots.Length + 1];
            defaultRoots.CopyTo(correctRoots, 0);
            correctRoots[defaultRoots.Length] = testString;

            conf.AddRoot(testString);
            Assert.Equal(correctRoots, conf.Roots);
        }

        [Fact]
        public void RemoveRoot() {
            const string testString = "test";

            conf.AddRoot(testString);
            conf.RemoveRoot(testString);
            Assert.Equal(defaultRoots, conf.Roots);
        }

        [Fact]
        public void ReadOtherOpts() {
            var defaultPort = (int)defaultOtherOpts.GetType().GetField("Port").GetValue(defaultOtherOpts);
            var defaultUpdateDelay = (int)defaultOtherOpts.GetType().GetField("UpdateDelay").GetValue(defaultOtherOpts);

            Assert.Equal(defaultPort, conf.Port);
            Assert.Equal(defaultUpdateDelay, conf.UpdateDelay);
        }

        [Fact]
        public void ChangeOtherOpts() {
            const int testInt = 7777;

            conf.Port = testInt;
            conf.UpdateDelay = testInt;

            Assert.Equal(testInt, conf.Port);
            Assert.Equal(testInt, conf.UpdateDelay);
        }
    }
}
