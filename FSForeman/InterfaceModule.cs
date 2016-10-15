using Nancy;

namespace FSForeman {
    public class InterfaceModule : NancyModule {
        public InterfaceModule() {
            Get["/"] = _ => View["index"];
        }
    }
}
