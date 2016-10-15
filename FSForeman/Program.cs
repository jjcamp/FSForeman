using Topshelf;

namespace FSForeman {
    internal class Program {
        private static void Main(string[] args) {
            HostFactory.Run(x => {
                x.Service<Service>(s => {
                    s.ConstructUsing(() => new Service());
                    s.WhenStarted(si => si.Start());
                    s.WhenStopped(si => si.Stop());
                });

                x.RunAsLocalSystem();
                x.SetDescription("Monitors and reports on a file share.");
                x.SetDisplayName("FSForeman");
                x.SetServiceName("FSForeman");
            });
        }
    }
}
