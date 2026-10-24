using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

using var mtx = new Mutex(true, "ZeroDash.single", out var first);
if (!first) return;
AppDomain.CurrentDomain.UnhandledException += (_, e) => File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "error.log"), e.ExceptionObject + "\n");

// ---- Ajuste aqui (IDs vêm do seu LibreHardwareMonitor_Report.txt) ----
const string Url = "http://127.0.0.1:5173";
const int PollMs = 1000;
var drives = new[] { ("C", "XPG SX8200 PRO NVME") };
const string CpuLoad = "/amdcpu/0/load/0", CpuTemp = "/amdcpu/0/temperature/2", CpuVolt = "/lpc/nct6687dr/0/voltage/4";
const string GpuLoad = "/gpu-nvidia/0/load/0", GpuTemp = "/gpu-nvidia/0/temperature/0", GpuClock = "/gpu-nvidia/0/clock/0", GpuVolt = "/gpu-nvidia/0/voltage/0";
const string PumpRpm = "/lpc/nct6687dr/0/fan/1", PumpPct = "/lpc/nct6687dr/0/control/1";
const string CaseRpm = "/lpc/nct6687dr/0/fan/10", CasePct = "/lpc/nct6687dr/0/control/10";
const string GpuFanRpm = "/gpu-nvidia/0/fan/1", GpuFanPct = "/gpu-nvidia/0/control/1";
const string Vrm = "/lpc/nct6687dr/0/temperature/2";
const string CpuPower = "/amdcpu/0/power/0", GpuPower = "/gpu-nvidia/0/power/0";
// ----------------------------------------------------------------------

// Só o necessário: sem Storage/Memory(SPD)/Network, evita polling pesado de SMBus/SMART.
var pc = new Computer { IsCpuEnabled = true, IsGpuEnabled = true, IsMotherboardEnabled = true };
pc.Open();
var hw = new List<IHardware>();
void Walk(IHardware h) { if (h.HardwareType == HardwareType.GpuAmd) return; hw.Add(h); foreach (var s in h.SubHardware) Walk(s); }
foreach (var h in pc.Hardware) Walk(h);

var sensors = new Dictionary<string, ISensor>();
void Refresh() { foreach (var h in hw) { h.Update(); foreach (var s in h.Sensors) sensors[s.Identifier.ToString()] = s; } }
float? V(string id) => sensors.TryGetValue(id, out var s) ? s.Value : null;
var coreClk = new Regex(@"^Core #\d+$");
float? Boost() => sensors.Values.Where(s => s.SensorType == SensorType.Clock && s.Identifier.ToString().StartsWith("/amdcpu/0/") && coreClk.IsMatch(s.Name)).Max(s => s.Value);

[DllImport("kernel32.dll")] static extern bool GlobalMemoryStatusEx(ref MemSt m);

object? snap = null;
string bios = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BIOSVersion", "")?.ToString() ?? "";
string board = Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS", "BaseBoardProduct", "")?.ToString() ?? "";
string Nm(HardwareType t) => Regex.Replace(pc.Hardware.FirstOrDefault(h => h.HardwareType == t)?.Name ?? "", @"\s+(\d+-Core )?Processor|^(AMD|NVIDIA) ", "");
string cpuName = Nm(HardwareType.Cpu), gpuName = Nm(HardwareType.GpuNvidia);
string os = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "ProductName", "")?.ToString() ?? "Windows";
if (int.TryParse(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion", "CurrentBuildNumber", "0")?.ToString(), out var bld) && bld >= 22000) os = os.Replace("Windows 10", "Windows 11");
// Tipo e velocidade da RAM (SMBIOS via WMI, lido uma vez).
(string, int) RamInfo()
{
    try
    {
        using var q = new System.Management.ManagementObjectSearcher("SELECT ConfiguredClockSpeed, SMBIOSMemoryType FROM Win32_PhysicalMemory");
        int sp = 0, ty = 0;
        foreach (System.Management.ManagementBaseObject o in q.Get()) { sp = Math.Max(sp, Convert.ToInt32(o["ConfiguredClockSpeed"] ?? 0)); ty = Convert.ToInt32(o["SMBIOSMemoryType"] ?? 0); }
        return (ty == 34 ? "DDR5" : ty == 26 ? "DDR4" : ty == 24 ? "DDR3" : "RAM", sp);
    }
    catch { return ("RAM", 0); }
}
var ramInfo = RamInfo();

object Build()
{
    var m = new MemSt { Len = (uint)Marshal.SizeOf<MemSt>() }; GlobalMemoryStatusEx(ref m);
    double usedP = m.TotP - m.AvP, commit = m.TotPF - m.AvPF;
    var dk = drives.Select(d => { try { var i = new DriveInfo(d.Item1); return new { name = d.Item1 + ":\\", label = d.Item2, freeGB = i.AvailableFreeSpace / 1e9, totalGB = i.TotalSize / 1e9 }; } catch { return new { name = d.Item1 + ":\\", label = d.Item2, freeGB = 0.0, totalGB = 0.0 }; } });
    return new
    {
        cpu = new { name = cpuName, load = V(CpuLoad), temp = V(CpuTemp), clock = Boost(), volt = V(CpuVolt), power = V(CpuPower) },
        gpu = new { name = gpuName, load = V(GpuLoad), temp = V(GpuTemp), clock = V(GpuClock), volt = V(GpuVolt), power = V(GpuPower) },
        ram = new { pct = usedP / m.TotP * 100, usedMB = usedP / 1048576, freeMB = m.AvP / 1048576.0, totalMB = m.TotP / 1048576.0, type = ramInfo.Item1, speed = ramInfo.Item2 },
        virt = new { pct = commit / m.TotPF * 100 },
        fans = new { pump = new { rpm = V(PumpRpm), pct = V(PumpPct) }, @case = new { rpm = V(CaseRpm), pct = V(CasePct) }, gpu = new { rpm = V(GpuFanRpm), pct = V(GpuFanPct) } },
        disks = dk, board, bios, vrm = V(Vrm), os, uptimeSec = Environment.TickCount64 / 1000
    };
}

// FPS via memória compartilhada do RTSS (RTSS precisa estar rodando).
float? Fps()
{
    try
    {
        using var f = MemoryMappedFile.OpenExisting("RTSSSharedMemoryV2");
        using var a = f.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        if (a.ReadUInt32(4) < 0x00020000) return null;
        uint size = a.ReadUInt32(8), off = a.ReadUInt32(12), n = a.ReadUInt32(16);
        float? best = null; uint bestT = 0;
        for (uint i = 0; i < n; i++)
        {
            long b = off + (long)i * size;
            if (a.ReadUInt32(b) == 0) continue;               // dwProcessID
            uint t0 = a.ReadUInt32(b + 268), t1 = a.ReadUInt32(b + 272), fr = a.ReadUInt32(b + 276);
            if (t1 > t0 && fr > 0 && t1 >= bestT) { bestT = t1; best = 1000f * fr / (t1 - t0); }
        }
        return best;
    }
    catch { return null; }
}

Refresh(); snap = Build();
_ = Task.Run(async () => { var t = new PeriodicTimer(TimeSpan.FromMilliseconds(PollMs)); while (await t.WaitForNextTickAsync()) { try { Refresh(); snap = Build(); } catch { } } });

var builder = WebApplication.CreateSlimBuilder();
builder.WebHost.UseUrls(Url);
var web = builder.Build();
web.MapGet("/", (HttpContext c) => { c.Response.Headers.CacheControl = "no-store"; var id = c.Request.Query["skin"].ToString(); return Results.Text(Skins.Html(id == "" ? Skins.Current : id), "text/html; charset=utf-8"); });
web.MapGet("/api/metrics", () => Results.Json(new { fps = Fps(), data = snap }));
web.Start();
var ui = new Thread(() => { Application.SetHighDpiMode(HighDpiMode.PerMonitorV2); Application.EnableVisualStyles(); Application.Run(new DashForm(Url)); });
ui.SetApartmentState(ApartmentState.STA); ui.Start(); ui.Join();
web.StopAsync().Wait();

[StructLayout(LayoutKind.Sequential)] struct MemSt { public uint Len, Load; public ulong TotP, AvP, TotPF, AvPF, TotV, AvV, AvX; }

class DashForm : Form
{
    readonly WebView2 wv = new() { Dock = DockStyle.Fill };
    static System.Drawing.Icon LoadIcon(int size) { using var r = typeof(DashForm).Assembly.GetManifestResourceStream("zerodash.ico")!; return new System.Drawing.Icon(r, size, size); }
    static string Ver => typeof(DashForm).Assembly.GetName().Version!.ToString(3);
    const string TaskName = "ZeroDash";
    static int Schtasks(string args)
    {
        try { using var p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("schtasks.exe", args) { CreateNoWindow = true, UseShellExecute = false }); p!.WaitForExit(15000); return p.ExitCode; }
        catch { return -1; }
    }
    static bool AutostartOn() => Schtasks($"/Query /TN {TaskName}") == 0;
    // O app já roda como administrador, então pode criar/remover a tarefa agendada (logon, privilégio máximo, sem UAC).
    static bool AutostartSet(bool on)
    {
        if (!on) return Schtasks($"/Delete /TN {TaskName} /F") == 0;
        Schtasks("/Delete /TN ZeroModDash /F"); Schtasks("/Delete /TN ZeroModDashView /F");
        string E(string v) => System.Security.SecurityElement.Escape(v)!;
        var exe = Environment.ProcessPath!; var user = Environment.UserDomainName + "\\" + Environment.UserName;
        var xml = $"""
<?xml version="1.0" encoding="UTF-16"?>
<Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
  <RegistrationInfo><Description>ZeroDash - dashboard do monitor ZeroMOD</Description></RegistrationInfo>
  <Triggers><LogonTrigger><Enabled>true</Enabled><UserId>{E(user)}</UserId><Delay>PT10S</Delay></LogonTrigger></Triggers>
  <Principals><Principal id="Author"><UserId>{E(user)}</UserId><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>
  <Settings><MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy><DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries><StopIfGoingOnBatteries>false</StopIfGoingOnBatteries><ExecutionTimeLimit>PT0S</ExecutionTimeLimit><StartWhenAvailable>true</StartWhenAvailable><Enabled>true</Enabled></Settings>
  <Actions Context="Author"><Exec><Command>{E(exe)}</Command><WorkingDirectory>{E(Path.GetDirectoryName(exe)!)}</WorkingDirectory></Exec></Actions>
</Task>
""";
        var f = Path.Combine(Path.GetTempPath(), "zerodash-task.xml");
        try { File.WriteAllText(f, xml, System.Text.Encoding.Unicode); return Schtasks($"/Create /TN {TaskName} /XML \"{f}\" /F") == 0; }
        finally { try { File.Delete(f); } catch { } }
    }
    readonly NotifyIcon tray = new() { Icon = LoadIcon(System.Windows.Forms.SystemInformation.SmallIconSize.Width), Text = "ZeroDash v" + typeof(DashForm).Assembly.GetName().Version!.ToString(3), Visible = true };
    public DashForm(string url)
    {
        Icon = LoadIcon(32); FormBorderStyle = FormBorderStyle.None; StartPosition = FormStartPosition.Manual; ShowInTaskbar = false; BackColor = System.Drawing.Color.Black;
        Controls.Add(wv);
        var m = new ContextMenuStrip();
        var title = new ToolStripLabel("ZeroDash v" + Ver) { Font = new Font(m.Font, FontStyle.Bold) };
        var auto = new ToolStripMenuItem("Iniciar com Windows");
        auto.Click += (_, _) =>
        {
            var want = !AutostartOn();
            if (!AutostartSet(want)) tray.ShowBalloonTip(4000, "ZeroDash", "Não foi possível alterar o início com o Windows.", ToolTipIcon.Warning);
            auto.Checked = AutostartOn();
        };
        var skinMenu = new ToolStripMenuItem("Skins");
        m.Opening += (_, _) =>
        {
            auto.Checked = AutostartOn();
            skinMenu.DropDownItems.Clear();
            foreach (var (id, name) in Skins.List())
            {
                var it = new ToolStripMenuItem(name) { Checked = id == Skins.Current };
                it.Click += (_, _) => { Skins.Set(id); wv.CoreWebView2?.Reload(); };
                skinMenu.DropDownItems.Add(it);
            }
        };
        m.Items.AddRange(new ToolStripItem[] {
            title, new ToolStripSeparator(), auto, skinMenu,
            new ToolStripMenuItem("Recarregar", null, (_, _) => wv.CoreWebView2?.Reload()),
            new ToolStripMenuItem("Sair", null, (_, _) => { tray.Visible = false; Application.Exit(); }) });
        tray.ContextMenuStrip = m;
        Load += async (_, _) =>
        {
            Place();
            var env = await CoreWebView2Environment.CreateAsync(null, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeroDash", "wv2"));
            await wv.EnsureCoreWebView2Async(env);
            var st = wv.CoreWebView2.Settings; st.AreDefaultContextMenusEnabled = false; st.IsZoomControlEnabled = false; st.IsStatusBarEnabled = false; st.AreBrowserAcceleratorKeysEnabled = false;
            wv.Source = new Uri(url);
        };
        var t = new System.Windows.Forms.Timer { Interval = 5000 }; t.Tick += (_, _) => Place(); t.Start();
    }
    // Encaixa a janela no monitor 1920x480; se ele não estiver ligado, esconde (não aparece no monitor principal).
    void Place()
    {
        var s = Screen.AllScreens.FirstOrDefault(x => x.Bounds.Width == 1920 && x.Bounds.Height == 480);
        if (s == null) { if (Visible) Hide(); return; }
        if (!Visible) Show();
        if (Bounds != s.Bounds) Bounds = s.Bounds;
    }
}

// Skins: arquivos .html embutidos (src/ZeroDash/skins) + qualquer .html na pasta "skins" ao lado do exe (esta tem prioridade).
static class Skins
{
    const string Default = "completo";
    static readonly string Dir = Path.Combine(AppContext.BaseDirectory, "skins");
    static readonly string Cfg = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeroDash", "skin.txt");
    public static string Current = Load();
    static string Load() { try { var s = File.ReadAllText(Cfg).Trim(); if (List().Any(x => x.id == s)) return s; } catch { } return Default; }
    public static void Set(string id) { Current = id; try { Directory.CreateDirectory(Path.GetDirectoryName(Cfg)!); File.WriteAllText(Cfg, id); } catch { } }
    static string? Read(string id)
    {
        if (!Regex.IsMatch(id, "^[a-z0-9-]+$")) return null;
        var f = Path.Combine(Dir, id + ".html");
        if (File.Exists(f)) return File.ReadAllText(f);
        using var r = typeof(Skins).Assembly.GetManifestResourceStream("skin." + id);
        return r == null ? null : new StreamReader(r).ReadToEnd();
    }
    public static string Html(string id) => Read(id) ?? Read(Default) ?? "<h1>Skin não encontrada</h1>";
    public static List<(string id, string name)> List()
    {
        var ids = typeof(Skins).Assembly.GetManifestResourceNames().Where(n => n.StartsWith("skin.")).Select(n => n[5..]).ToList();
        if (Directory.Exists(Dir)) ids.AddRange(Directory.GetFiles(Dir, "*.html").Select(x => Path.GetFileNameWithoutExtension(x)));
        return ids.Where(i => Regex.IsMatch(i, "^[a-z0-9-]+$")).Distinct().OrderBy(i => i).Select(i => (i, Name(i))).ToList();
    }
    static string Name(string id) { var m = Regex.Match(Read(id) ?? "", "name=\"skin-name\" content=\"([^\"]+)\""); return m.Success ? m.Groups[1].Value : id; }
}
