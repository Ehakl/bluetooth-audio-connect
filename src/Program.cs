using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

// Windows x64; no external packages. Reconnect uses a device-scoped audio-driver request.
class Program
{
    static readonly string DataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BluetoothAudioConnect");
    static readonly string ConfigPath = Path.Combine(DataDir, "settings.txt");
    static void Log(string text) {
        Console.WriteLine(text);
        try { Directory.CreateDirectory(DataDir); File.AppendAllText(Path.Combine(DataDir, "last-run.txt"), text + Environment.NewLine); }
        catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
    static void Check(int hr) { if (hr < 0) Marshal.ThrowExceptionForHR(hr); }
    static void Release(object obj) { if (obj != null && Marshal.IsComObject(obj)) Marshal.ReleaseComObject(obj); }
    static string Normalize(string value) { return (value ?? "").Replace('\u2019', '\'').Trim(); }
    static bool Matches(string name, string iface, string target) {
        return !String.IsNullOrWhiteSpace(target) && (String.Equals(Normalize(name), Normalize(target), StringComparison.OrdinalIgnoreCase)
            || String.Equals(Normalize(iface), Normalize(target), StringComparison.OrdinalIgnoreCase));
    }    static string GetString(IPropertyStore store, Guid fmt, uint pid) {
        PropertyKey key = new PropertyKey { Format = fmt, Id = pid }; PropVariant value;
        if (store.GetValue(ref key, out value) < 0) return "";
        try { return value.Type == 31 ? Marshal.PtrToStringUni(value.Pointer) : ""; }
        finally { PropVariantClear(ref value); }
    }
    static Guid GetContainer(IPropertyStore store) {
        PropertyKey key = new PropertyKey { Format = new Guid("8c7ed206-3f8a-4827-b3ab-ae9e1faefc6c"), Id = 2 }; PropVariant value;
        if (store.GetValue(ref key, out value) < 0) return Guid.Empty;
        try { return value.Type == 72 && value.Pointer != IntPtr.Zero ? (Guid)Marshal.PtrToStructure(value.Pointer, typeof(Guid)) : Guid.Empty; }
        finally { PropVariantClear(ref value); }
    }
    class Endpoint {
        public string Id, Name, InterfaceName; public Guid Container; public uint State; public int Flow;
    }
    static List<Endpoint> Enumerate(IMMDeviceEnumerator enumerator) {
        var result = new List<Endpoint>();
        for (int flow = 0; flow < 2; flow++) {
            IMMDeviceCollection collection; Check(enumerator.EnumAudioEndpoints(flow, 15, out collection));
            try {
                uint count; Check(collection.GetCount(out count));
                for (uint i = 0; i < count; i++) {
                    IMMDevice device = null; IPropertyStore store = null;
                    try {
                        Check(collection.Item(i, out device)); Check(device.OpenPropertyStore(0, out store));
                        string id; uint state; Check(device.GetId(out id)); Check(device.GetState(out state));
                        result.Add(new Endpoint { Id = id, State = state, Flow = flow,
                            Name = GetString(store, new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), 14),
                            InterfaceName = GetString(store, new Guid("026e516e-b814-414b-83cd-856d6fef4822"), 2),
                            Container = GetContainer(store) });
                    } finally { Release(store); Release(device); }
                }
            } finally { Release(collection); }
        }
        return result;
    }
    static List<string> Filters(IMMDeviceEnumerator enumerator, Endpoint endpoint) {
        var result = new List<string>(); IMMDevice device = null; object topologyObj = null;
        try {
            Check(enumerator.GetDevice(endpoint.Id, out device)); Guid iid = typeof(IDeviceTopology).GUID;
            Check(device.Activate(ref iid, 23, IntPtr.Zero, out topologyObj));
            var topology = (IDeviceTopology)topologyObj; uint count; Check(topology.GetConnectorCount(out count));
            for (uint i = 0; i < count; i++) {
                IConnector connector = null;
                try {
                    Check(topology.GetConnector(i, out connector)); string id;
                    Check(connector.GetDeviceIdConnectedTo(out id));
                    // Only Bluetooth audio filters; never target USB, the radio, or input devices.
                    if (id != null && id.StartsWith(@"{2}.\\?\bth", StringComparison.OrdinalIgnoreCase)) result.Add(id);
                } finally { Release(connector); }
            }
        } finally { Release(topologyObj); Release(device); }
        return result;
    }
    static int Request(IMMDeviceEnumerator enumerator, string filterId) {
        IMMDevice filter = null; object control = null;
        try {
            Check(enumerator.GetDevice(filterId, out filter)); Guid iid = typeof(IKsControl).GUID;
            Check(filter.Activate(ref iid, 23, IntPtr.Zero, out control));
            KsProperty property = new KsProperty { Set = new Guid("7fa06c40-b8f6-4c7e-8556-e8c33a12e54d"), Id = 0, Flags = 1 };
            uint returned;
            return ((IKsControl)control).KsProperty(ref property, (uint)Marshal.SizeOf(typeof(KsProperty)), IntPtr.Zero, 0, out returned);
        } finally { Release(control); Release(filter); }
    }
    static string DefaultId(IMMDeviceEnumerator enumerator, int flow, int role) {
        IMMDevice device = null;
        try {
            Check(enumerator.GetDefaultAudioEndpoint(flow, role, out device));
            string id; Check(device.GetId(out id)); return id;
        } finally { Release(device); }
    }
    static void ReportDefaults(IMMDeviceEnumerator enumerator, List<Endpoint> endpoints) {
        for (int flow = 0; flow < 2; flow++) for (int role = 0; role < 3; role++) {
            try {
                string id = DefaultId(enumerator, flow, role);
                var endpoint = endpoints.FirstOrDefault(e => e.Id == id);
                Log("Default " + (flow == 0 ? "output" : "input") + " (" + new[] {"console", "multimedia", "communications"}[role] + "): " + (endpoint == null ? id : endpoint.Name));
            } catch (Exception ex) { Log("Could not read default audio endpoint: " + ex.Message); }
        }
    }
    static void SetDefaults(IMMDeviceEnumerator enumerator, Guid container, string mode) {
        if (mode == "none") return;
        Endpoint output = null, input = null;
        // The microphone may appear after the playback endpoint finishes connecting.
        for (int i = 0; i < 30; i++) {
            var active = Enumerate(enumerator).Where(e => e.Container == container && e.State == 1).ToList();
            output = active.Where(e => e.Flow == 0).OrderBy(e => e.Name.StartsWith("Headphones", StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenBy(e => e.Id).FirstOrDefault();
            input = active.Where(e => e.Flow == 1).OrderBy(e => e.Id).FirstOrDefault();
            if (output != null && (input != null || mode == "output" || (mode == "auto" && !Enumerate(enumerator).Any(e => e.Container == container && e.Flow == 1 && (e.State == 1 || e.State == 8))))) break;
            Thread.Sleep(500);
        }
        if (output == null || (input == null && mode == "both"))
            throw new InvalidOperationException("The requested active output/microphone was not available. Default audio devices were not changed.");
        if (mode == "output") input = null;
        object policyObject = new PolicyConfigClient();
        try {
            var policy = (IPolicyConfig)policyObject;
            for (int role = 0; role < 3; role++) {
                Check(policy.SetDefaultEndpoint(output.Id, role));
                if (input != null) Check(policy.SetDefaultEndpoint(input.Id, role));
            }
            for (int attempt = 0; attempt < 10; attempt++) {
                bool verified = true;
                for (int role = 0; role < 3; role++)
                    verified &= DefaultId(enumerator, 0, role) == output.Id && (input == null || DefaultId(enumerator, 1, role) == input.Id);
                if (verified) {
                    Log("Verified default output: " + output.Name);
                    Log(input == null ? "Microphone selection unchanged." : "Verified default input: " + input.Name);
                    Log("Requested defaults verified for console, multimedia and communications.");
                    return;
                }
                Thread.Sleep(200);
            }
            throw new InvalidOperationException("Audio default changes could not be verified. Some defaults may have changed; check --status.");
        } finally { Release(policyObject); }
    }
    class Device {
        public Guid Container; public string Name; public List<Endpoint> Endpoints;
    }
    static List<Device> Discover(IMMDeviceEnumerator enumerator, List<Endpoint> endpoints) {
        var devices = new List<Device>();
        foreach (var group in endpoints.Where(e => e.Container != Guid.Empty).GroupBy(e => e.Container)) {
            bool bluetooth = false;
            foreach (var endpoint in group.Where(e => e.Flow == 0 && (e.State == 1 || e.State == 8))) {
                try { if (Filters(enumerator, endpoint).Count > 0) { bluetooth = true; break; } }
                catch (COMException) { } // Unsupported topology is not treated as a Bluetooth device.
            }
            if (bluetooth) {
                var render = group.First(e => e.Flow == 0);
                devices.Add(new Device { Container = group.Key, Name = String.IsNullOrWhiteSpace(render.InterfaceName) ? render.Name : render.InterfaceName, Endpoints = group.ToList() });
            }
        }
        return devices.OrderBy(d => d.Name).ToList();
    }
    static Device Select(List<Device> devices, string name, Guid? id) {
        var matches = devices.Where(d => id.HasValue ? d.Container == id.Value :
            d.Endpoints.Any(e => Matches(e.Name, e.InterfaceName, name))).ToList();
        if (matches.Count == 0) throw new InvalidOperationException("No matching Bluetooth audio device. Run --list; pair the device in Windows first.");
        if (matches.Count > 1) throw new InvalidOperationException("Device name is ambiguous. Use --container with the ID from --list.");
        return matches[0];
    }
    static void ListDevices(List<Device> devices) {
        for (int i = 0; i < devices.Count; i++) {
            var d = devices[i];
            Console.WriteLine((i+1) + ". " + d.Name + " | " + d.Container + " | " +
                (d.Endpoints.Any(e => e.Flow == 0 && e.State == 1) ? "connected" : "disconnected"));
        }
        if (devices.Count == 0) Console.WriteLine("No supported paired Bluetooth audio devices found.");
    }
    static Device Pick(List<Device> devices) {
        ListDevices(devices);
        if (devices.Count == 0) throw new InvalidOperationException("Pair a Bluetooth audio device in Windows first.");
        Console.Write("Select a device number (Enter cancels): ");
        int index;
        if (!Int32.TryParse(Console.ReadLine(), out index) || index < 1 || index > devices.Count)
            throw new InvalidOperationException("Selection cancelled. Nothing changed.");
        return devices[index-1];
    }
    static void Help() {
        Console.WriteLine("Bluetooth Audio Connect\n\n" +
            "No arguments: connect saved device, or choose and save a device.\n" +
            "--setup                         Choose and save a device without connecting\n" +
            "--list                          List supported paired Bluetooth audio devices\n" +
            "--device \"NAME\"                 Connect by exact device or endpoint name\n" +
            "--container GUID                Connect by device container ID\n" +
            "--defaults auto|both|output|none  Audio selection (default: auto)\n" +
            "--status                        Read status and defaults; no device changes\n" +
            "--self-test                     Offline tests\n\n" +
            "auto: set output and an available mic; output: leave mic unchanged;\n" +
            "both: require output and mic; none: connect without changing defaults.");
    }
    static void Assert(bool value, string message) { if (!value) throw new Exception("Test failed: " + message); }
    static void SelfTest() {
        Assert(Matches("", "Pat\u2019s Headset", "Pat's Headset"), "apostrophe normalization");
        Assert(!Matches("Headset Pro", "", "Headset"), "no partial matching");
        Assert(!Matches("", "", ""), "empty name rejected");
        var a = new Device { Container = Guid.NewGuid(), Endpoints = new List<Endpoint> { new Endpoint { Name = "Headset" } } };
        var b = new Device { Container = Guid.NewGuid(), Endpoints = new List<Endpoint> { new Endpoint { Name = "Headset" } } };
        var devices = new List<Device> {a,b}; bool rejected = false;
        try { Select(devices, "Headset", null); } catch (InvalidOperationException) { rejected = true; }
        Assert(rejected, "duplicate names rejected");
        Assert(Select(devices, null, b.Container) == b, "container disambiguation");
        rejected = false; try { Select(devices, "Missing", null); } catch (InvalidOperationException) { rejected = true; }
        Assert(rejected, "missing device rejected");
        Assert(Marshal.SizeOf(typeof(KsProperty)) == 24 && Marshal.SizeOf(typeof(PropVariant)) == 24, "native x64 layouts");
        Console.WriteLine("7 offline tests passed. No audio changes made.");
    }
    [STAThread]
    static int Main(string[] args) {
        string name = null, defaults = "auto"; Guid? id = null;
        bool setup = false, list = false, status = false, defaultsSpecified = false;
        IMMDeviceEnumerator enumerator = null;
        try {
            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                if (arg == "--help" || arg == "-h") { Help(); return 0; }
                if (arg == "--self-test") { SelfTest(); return 0; }
                if (arg == "--setup") setup = true;
                else if (arg == "--list") list = true;
                else if (arg == "--status") status = true;
                else if (arg == "--device" || arg == "--container" || arg == "--defaults") {
                    if (++i >= args.Length) throw new ArgumentException("Missing value for " + arg);
                    if (arg == "--device") name = args[i];
                    else if (arg == "--container") id = Guid.Parse(args[i]);
                    else { defaults = args[i]; defaultsSpecified = true; }
                } else throw new ArgumentException("Unknown option: " + arg);
            }
            if (name != null && id.HasValue) throw new ArgumentException("Use either --device or --container, not both.");
            if (!new[] {"auto", "both", "output", "none"}.Contains(defaults)) throw new ArgumentException("Invalid --defaults value.");
            if ((setup && (status || list || name != null || id.HasValue)) || (list && (status || name != null || id.HasValue)))
                throw new ArgumentException("Conflicting command options.");
            bool owns;
            using (var mutex = new Mutex(true, "Local\\BluetoothAudioConnect", out owns)) {
                if (!owns) { Console.Error.WriteLine("Another instance is running."); return 4; }
                Directory.CreateDirectory(DataDir);
                File.WriteAllText(Path.Combine(DataDir, "last-run.txt"), DateTime.Now.ToString("s") + Environment.NewLine);
                enumerator = (IMMDeviceEnumerator)new MMDeviceEnumerator();
                var endpoints = Enumerate(enumerator); var devices = Discover(enumerator, endpoints);
                if (list) { ListDevices(devices); return 0; }
                if (!setup && name == null && !id.HasValue && File.Exists(ConfigPath)) {
                    var config = File.ReadAllLines(ConfigPath); Guid saved;
                    if (config.Length < 2 || !Guid.TryParse(config[0], out saved) || !new[] {"auto","both","output","none"}.Contains(config[1]))
                        throw new InvalidOperationException("Invalid saved settings. Run --setup to replace them.");
                    id = saved; if (!defaultsSpecified) defaults = config[1];
                }
                if (status && name == null && !id.HasValue) {
                    ListDevices(devices); ReportDefaults(enumerator, endpoints); return 0;
                }
                bool pick = setup || (name == null && !id.HasValue);
                Device target = pick ? Pick(devices) : Select(devices, name, id);
                if (pick) {
                    File.WriteAllLines(ConfigPath, new[] {target.Container.ToString(), defaults});
                    Log("Saved device: " + target.Name);
                    if (setup) return 0;
                }
                Log("Selected: " + target.Name);
                bool connected = target.Endpoints.Any(e => e.Flow == 0 && e.State == 1);
                if (status) { ReportDefaults(enumerator, endpoints); Log(connected ? "Connected." : "Not connected."); return connected ? 0 : 3; }
                if (!connected) {
                    var filters = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var endpoint in target.Endpoints.Where(e => e.State == 8)) {
                        try { foreach (var filter in Filters(enumerator, endpoint)) filters.Add(filter); }
                        catch (COMException ex) { Log("Unsupported endpoint: " + ex.Message); }
                    }
                    if (filters.Count == 0) throw new InvalidOperationException("No reconnect-capable Bluetooth audio filter found.");
                    bool accepted = false;
                    foreach (var filter in filters) {
                        try { int hr = Request(enumerator, filter); Log("Reconnect result: 0x" + hr.ToString("X8")); accepted |= hr >= 0; }
                        catch (COMException ex) { Log("Reconnect request failed: " + ex.Message); }
                    }
                    if (!accepted) throw new InvalidOperationException("Driver rejected reconnect requests.");
                    for (int attempt = 0; attempt < 40; attempt++) {
                        Thread.Sleep(500);
                        connected = Enumerate(enumerator).Any(e => e.Container == target.Container && e.Flow == 0 && e.State == 1);
                        if (connected) break;
                    }
                    if (!connected) { Log("Request accepted, but not connected after 20 seconds. Wake the device and retry."); return 3; }
                }
                Log("Connection verified."); SetDefaults(enumerator, target.Container, defaults); return 0;
            }
        } catch (ArgumentException ex) { Console.Error.WriteLine(ex.Message); return 2; }
        catch (Exception ex) { Log("Error: " + ex.Message + " (0x" + ex.HResult.ToString("X8") + ")"); return 1; }
        finally { Release(enumerator); }
    }
    [DllImport("ole32.dll")] static extern int PropVariantClear(ref PropVariant value);
}
