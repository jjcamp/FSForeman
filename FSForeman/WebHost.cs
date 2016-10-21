using System;
using Nancy;
using Nancy.Hosting.Self;
using Nancy.Json;
using Nancy.TinyIoc;

namespace FSForeman {
    /// <summary>
    /// Hosts the API and web interface.
    /// </summary>
    public class WebHost : NancyHost {
        private readonly Uri uri;

        /// <summary>
        /// Creates a new <see cref="WebHost"/> instance.
        /// </summary>
        /// <param name="hostName">Hostname</param>
        /// <param name="port">Port</param>
        /// <param name="register">A list of initialized objects that may be used by the web modules.</param>
        public WebHost(string hostName, int port, params object[] register) : this(MakeUriAndConfig(hostName, port), register) { }

        // Helper to simplifiy the constructor.
        private WebHost(Tuple<Uri, HostConfiguration> uriAndConfig, params object[] register)
            : base(uriAndConfig.Item1, new Bootstrapper(register), uriAndConfig.Item2) {
            uri = uriAndConfig.Item1;
        }

        // See constructor above.
        private static Tuple<Uri, HostConfiguration> MakeUriAndConfig(string hostName, int port) {
            var uri = new Uri($"http://{hostName}:{port}/fsforeman/");
            var config = new HostConfiguration();
            config.UrlReservations.CreateAutomatically = true;
            JsonSettings.MaxJsonLength = Int32.MaxValue;
            return new Tuple<Uri, HostConfiguration>(uri, config);
        }
        
        /// <summary>
        /// Starts the web host.
        /// </summary>
        public new void Start() {
            Logger.LogLine($"Starting web host at {uri}");
            base.Start();
        }
    }

    /// <summary>
    /// Custom bootstrapper to allow passing of inititalized objects into nancy modules.
    /// </summary>
    public class Bootstrapper : DefaultNancyBootstrapper {
        private readonly object[] registerObjects;

        public Bootstrapper(params object[] objectsToRegister) {
            registerObjects = objectsToRegister;
        }

        protected override void ConfigureApplicationContainer(TinyIoCContainer container) {
            base.ConfigureApplicationContainer(container);

            foreach (var o in registerObjects)
                container.Register(o.GetType(), o);
        }
    }
}
