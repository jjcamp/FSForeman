using System;
using System.Diagnostics;
using System.IO;
using Nancy;

namespace FSForeman {
    public class ApiModule : NancyModule {
        public ApiModule(FileCache cache) : base("/api") {
            Get["/system"] = _ => {
                var info = new SystemInfo {
                    Memory = (((float)GC.GetTotalMemory(false)) / 1048576),
                    Threads = Process.GetCurrentProcess().Threads.Count
                };

                return Response.AsJson(info);
            };

            Get["/log"] = _ => {
                if (Logger.Output != Logger.OutputType.File)
                    return Response.AsJson(false);
                string log = "";
                lock (Logger.FilePath) {
                    var fs = File.OpenRead(Logger.FilePath);
                    using (var sr = new StreamReader(fs)) {
                        log = sr.ReadToEnd();
                    }
                }
                return Response.AsText(log);;
            };

            Get["/ignores"] = _ => Response.AsJson(Configuration.Global.Ignores);
            Post["/ignores"] = _ => {
                string pattern = Request.Query["pattern"];
                Configuration.Global.AddIgnore(pattern);
                return Response.AsJson(true);
            };
            Delete["/ignores"] = _ => {
                string pattern = Request.Query["pattern"];
                Configuration.Global.RemoveIgnore(pattern);
                return Response.AsJson(true);
            };

            Get["/roots"] = _ => Response.AsJson(Configuration.Global.Roots);
            Post["/roots"] = _ => {
                string dir = Request.Query["dir"];
                Configuration.Global.AddRoot(dir);
                return Response.AsJson(true);
            };
            Delete["/roots"] = _ => {
                string dir = Request.Query["dir"];
                Configuration.Global.RemoveRoot(dir);
                return Response.AsJson(true);
            };

            Get["/duplicates"] = _ => {
                return Response.AsJson(cache.GetDuplicates());
            };
        }

        [Serializable]
        private struct SystemInfo {
            public float Memory;
            public int Threads;
        }
    }
}
