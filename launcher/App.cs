// MQuest Service Report launcher.
// A small desktop window that runs a local web server for the report app and opens it
// in the browser. Phones/tablets on the same Wi-Fi can use the link shown in the window.
// The app files (index.html, logo.jpeg, customers.js) are built into the exe; if a file with
// the same name sits next to app.exe, that file is used instead (e.g. an edited customers.js).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

[assembly: AssemblyTitle("MQuest Service Report")]
[assembly: AssemblyProduct("MQuest Service Report")]
[assembly: AssemblyCompany("MQuest Pte Ltd")]
[assembly: AssemblyVersion("1.0.0.0")]

static class App
{
    static readonly Dictionary<string, string> Types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
        { ".html", "text/html; charset=utf-8" }, { ".js", "application/javascript; charset=utf-8" },
        { ".css", "text/css; charset=utf-8" }, { ".jpeg", "image/jpeg" }, { ".jpg", "image/jpeg" },
        { ".png", "image/png" }, { ".ico", "image/x-icon" }, { ".svg", "image/svg+xml" }
    };
    static readonly string[] Embedded = { "index.html", "logo.jpeg", "customers.js" };
    static string baseDir;
    static TcpListener listener;
    static int port;

    [STAThread]
    static void Main()
    {
        bool isNew;
        using (var mutex = new Mutex(true, "MQuestServiceReportApp", out isNew))
        {
            baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!isNew)
            {
                // Already running: just open the browser again.
                OpenBrowser("http://localhost:" + ReadPortFile());
                return;
            }
            if (!StartServer())
            {
                MessageBox.Show("Could not start the app (ports 8080-8099 are busy).", "MQuest Service Report",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            WritePortFile();
            new Thread(ServeLoop) { IsBackground = true }.Start();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            OpenBrowser(LocalUrl);
            Application.Run(BuildWindow());
            try { listener.Stop(); } catch { }
            try { File.Delete(PortFile); } catch { }
        }
    }

    static string LocalUrl { get { return "http://localhost:" + port; } }
    static string PortFile { get { return Path.Combine(Path.GetTempPath(), "mquest-service-report.port"); } }
    static void WritePortFile() { try { File.WriteAllText(PortFile, port.ToString()); } catch { } }
    static string ReadPortFile() { try { return File.ReadAllText(PortFile).Trim(); } catch { return "8080"; } }

    static bool StartServer()
    {
        for (int p = 8080; p < 8100; p++)
        {
            try { listener = new TcpListener(IPAddress.Any, p); listener.Start(); port = p; return true; }
            catch { listener = null; }
        }
        return false;
    }

    static List<string> LanUrls()
    {
        try
        {
            return Dns.GetHostAddresses(Dns.GetHostName())
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork && !a.ToString().StartsWith("169.254"))
                .Select(a => "http://" + a + ":" + port).ToList();
        }
        catch { return new List<string>(); }
    }

    static void OpenBrowser(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
    }

    // ---------------- Window ----------------
    static Form BuildWindow()
    {
        var f = new Form {
            Text = "MQuest Service Report", Width = 480, Height = 330, StartPosition = FormStartPosition.CenterScreen,
            FormBorderStyle = FormBorderStyle.FixedSingle, MaximizeBox = false, BackColor = Color.White,
            Font = new Font("Segoe UI", 10f)
        };
        try { f.Icon = Icon.ExtractAssociatedIcon(Assembly.GetExecutingAssembly().Location); } catch { }

        var title = new Label { Text = "Service Report app is running", Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            AutoSize = true, Left = 20, Top = 16, ForeColor = Color.FromArgb(31, 111, 181) };
        var open = new Button { Text = "Open Service Report", Left = 20, Top = 56, Width = 200, Height = 38,
            BackColor = Color.FromArgb(31, 111, 181), ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
        open.FlatAppearance.BorderSize = 0;
        open.Click += delegate { OpenBrowser(LocalUrl); };

        var info = new Label { Left = 20, Top = 108, Width = 430, Height = 22,
            Text = "To use on a phone or tablet (same Wi-Fi), open this link:" };
        f.Controls.Add(title); f.Controls.Add(open); f.Controls.Add(info);

        int top = 132;
        var urls = LanUrls();
        if (urls.Count == 0) urls.Add("(not connected to a network)");
        foreach (var u in urls.Take(3))
        {
            var box = new TextBox { Text = u, ReadOnly = true, Left = 20, Top = top, Width = 300,
                Font = new Font("Consolas", 11f), BackColor = Color.White };
            f.Controls.Add(box);
            if (u.StartsWith("http"))
            {
                var copy = new Button { Text = "Copy", Left = 330, Top = top - 1, Width = 70, Height = 28 };
                string url = u;
                copy.Click += delegate { try { Clipboard.SetText(url); } catch { } };
                f.Controls.Add(copy);
            }
            top += 34;
        }
        var note = new Label { Left = 20, Top = top + 10, Width = 430, Height = 40, ForeColor = Color.DimGray,
            Text = "Keep this window open while using the app. Closing it stops the app.\n" +
                   "If Windows Firewall asks, click Allow so phones can connect." };
        f.Controls.Add(note);
        f.Height = Math.Max(330, top + 110);
        return f;
    }

    // ---------------- Web server ----------------
    static void ServeLoop()
    {
        while (true)
        {
            TcpClient client;
            try { client = listener.AcceptTcpClient(); } catch { return; }
            ThreadPool.QueueUserWorkItem(delegate { Handle(client); });
        }
    }

    static void Handle(TcpClient client)
    {
        try
        {
            using (client)
            using (var stream = client.GetStream())
            {
                stream.ReadTimeout = 5000;
                var reader = new StreamReader(stream, Encoding.ASCII);
                string requestLine = reader.ReadLine() ?? "";
                string line;
                while ((line = reader.ReadLine()) != null && line.Length > 0) { }

                string path = "/";
                var parts = requestLine.Split(' ');
                if (parts.Length >= 2) path = Uri.UnescapeDataString(parts[1].Split('?', '#')[0]);
                string name = path.TrimStart('/');
                if (name.Length == 0) name = "index.html";

                byte[] body = Load(name);
                string status = "200 OK", type;
                if (body == null) { body = Encoding.UTF8.GetBytes("Not found"); status = "404 Not Found"; type = "text/plain"; }
                else if (!Types.TryGetValue(Path.GetExtension(name), out type)) type = "application/octet-stream";

                string head = "HTTP/1.1 " + status + "\r\nContent-Type: " + type + "\r\nContent-Length: " + body.Length +
                              "\r\nCache-Control: no-cache\r\nConnection: close\r\n\r\n";
                byte[] hb = Encoding.ASCII.GetBytes(head);
                stream.Write(hb, 0, hb.Length);
                stream.Write(body, 0, body.Length);
            }
        }
        catch { }
    }

    static byte[] Load(string name)
    {
        // Only plain file names (no folders) - blocks access to anything else on the computer.
        if (name.IndexOfAny(new[] { '/', '\\', ':' }) >= 0 || name.Contains("..")) return null;

        string local = Path.Combine(baseDir, name);
        if (File.Exists(local) && Types.ContainsKey(Path.GetExtension(name)))
            return File.ReadAllBytes(local);

        if (Array.IndexOf(Embedded, name) < 0) return null;
        using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        {
            if (s == null) return null;
            var ms = new MemoryStream();
            s.CopyTo(ms);
            return ms.ToArray();
        }
    }
}
