using Nancy;

namespace FSForeman {
    public class InterfaceModule : NancyModule {
        public InterfaceModule() {
            Get["/"] = _ => View["status"];
            Get["/configure"] = _ => View["configure"];
            Get["/log"] = _ => View["log"];
            Get["/duplicates"] = _ => View["duplicates"];
        }
    }
}
