// مصروف — مثبِّت ويندوز
//
// مثبِّتٌ كامل بلا أدوات خارجية: ينسخ التطبيق إلى مجلّد المستخدم، ويضع
// اختصارين، ويسجّل نفسه في «إضافة أو إزالة البرامج» فيُزال كأيّ برنامج.
// وكلّه في نطاق المستخدم — لا يطلب صلاحية مسؤول ولا يمسّ نظامه.
//
// والمشغّل masrouf.exe مضمَّنٌ داخله مورداً، فالمثبِّت ملفٌّ واحد يُرسَل
// ويُشغَّل، والمثبِّت نفسه يُنسخ ليكون المُزيل.
//
//   csc /target:winexe /codepage:65001 /win32icon:masrouf.ico
//       /resource:masrouf.exe,app /resource:masrouf.ico,icon
//       /out:masrouf-setup.exe Setup.cs

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.Win32;

static class Setup
{
    const string NAME = "مصروف";
    const string ID = "Masrouf";
    const string VER = "1.1";
    const string URL = "https://relifeg1.github.io/masrouf/";
    const string REGKEY =
        @"Software\Microsoft\Windows\CurrentVersion\Uninstall\Masrouf";

    static readonly string NL = Environment.NewLine;

    static string Dir
    {
        get
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ID);
        }
    }

    static string Desktop
    {
        get { return Environment.GetFolderPath(Environment.SpecialFolder.Desktop); }
    }

    static string Programs
    {
        get { return Environment.GetFolderPath(Environment.SpecialFolder.Programs); }
    }

    // ── أدوات ──

    static void Extract(string res, string path)
    {
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(res))
        using (FileStream f = File.Create(path))
        {
            byte[] buf = new byte[8192];
            int n;
            while ((n = s.Read(buf, 0, buf.Length)) > 0) f.Write(buf, 0, n);
        }
    }

    // الاسم العربي يفشل حفظه على نظامٍ لغته غير العربية — يصير «؟؟؟؟؟»
    // وهي محرَّمة في أسماء الملفّات. فيُجرَّب ثم يُرتدّ إلى اللاتيني.
    static bool Shortcut(string folder, string name, string target, string icon)
    {
        try
        {
            string lnk = Path.Combine(folder, name + ".lnk");
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            if (t == null) return false;
            object sh = Activator.CreateInstance(t);
            object sc = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod,
                null, sh, new object[] { lnk });
            Type st = sc.GetType();
            st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc,
                new object[] { target });
            st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc,
                new object[] { Path.GetDirectoryName(target) });
            st.InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc,
                new object[] { icon + ",0" });
            st.InvokeMember("Description", BindingFlags.SetProperty, null, sc,
                new object[] { "خطتك الشهرية" });
            st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
            return File.Exists(lnk);
        }
        catch { return false; }
    }

    static void MakeShortcuts(string target, string icon)
    {
        foreach (string folder in new string[] { Desktop, Programs })
            if (!Shortcut(folder, NAME, target, icon))
                Shortcut(folder, ID, target, icon);
    }

    static void DropShortcuts()
    {
        foreach (string folder in new string[] { Desktop, Programs })
            foreach (string n in new string[] { NAME, ID })
                try { File.Delete(Path.Combine(folder, n + ".lnk")); }
                catch { }
    }

    // ── التثبيت ──

    static void Install()
    {
        Directory.CreateDirectory(Dir);
        string app = Path.Combine(Dir, "masrouf.exe");
        string ico = Path.Combine(Dir, "masrouf.ico");
        string unins = Path.Combine(Dir, "uninstall.exe");

        Extract("app", app);
        Extract("icon", ico);
        File.Copy(Application.ExecutablePath, unins, true);

        MakeShortcuts(app, ico);

        // «إضافة أو إزالة البرامج» — في فرع المستخدم، بلا صلاحية مسؤول
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(REGKEY))
        {
            k.SetValue("DisplayName", NAME + " — خطتي الشهرية");
            k.SetValue("DisplayVersion", VER);
            k.SetValue("Publisher", "relifeg1");
            k.SetValue("DisplayIcon", ico);
            k.SetValue("InstallLocation", Dir);
            k.SetValue("URLInfoAbout", URL);
            k.SetValue("UninstallString", "\"" + unins + "\" /uninstall");
            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            k.SetValue("EstimatedSize", 60, RegistryValueKind.DWord);
        }
    }

    static void Uninstall()
    {
        DropShortcuts();
        try { Registry.CurrentUser.DeleteSubKeyTree(REGKEY, false); }
        catch { }

        // لا يحذف الملفّ نفسه وهو يعمل: يُؤجَّل الحذف إلى أمرٍ ينتظر خروجه
        string bat = Path.Combine(Path.GetTempPath(), "masrouf_rm.bat");
        File.WriteAllText(bat,
            "@echo off" + NL
          + "ping 127.0.0.1 -n 3 >nul" + NL
          + "rmdir /s /q \"" + Dir + "\"" + NL
          + "del \"%~f0\"" + NL);
        ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c \"" + bat + "\"");
        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        Process.Start(psi);
    }

    // ── الواجهة ──

    static Form Window(string title, string body, string okText, EventHandler onOk)
    {
        Form f = new Form();
        f.Text = NAME;
        f.RightToLeft = RightToLeft.Yes;
        f.RightToLeftLayout = true;
        f.FormBorderStyle = FormBorderStyle.FixedDialog;
        f.MaximizeBox = false;
        f.MinimizeBox = false;
        f.StartPosition = FormStartPosition.CenterScreen;
        f.ClientSize = new Size(470, 230);
        f.Font = new Font("Segoe UI", 10F);
        try
        {
            using (Stream s = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("icon"))
                if (s != null) f.Icon = new Icon(s);
        }
        catch { }

        Label h = new Label();
        h.Text = title;
        h.Font = new Font("Segoe UI", 15F, FontStyle.Bold);
        h.SetBounds(20, 18, 430, 34);
        f.Controls.Add(h);

        Label p = new Label();
        p.Text = body;
        p.SetBounds(20, 58, 430, 110);
        f.Controls.Add(p);

        Button ok = new Button();
        ok.Text = okText;
        ok.SetBounds(20, 180, 140, 34);
        ok.Click += onOk;
        f.Controls.Add(ok);
        f.AcceptButton = ok;

        Button no = new Button();
        no.Text = "إلغاء";
        no.SetBounds(170, 180, 110, 34);
        no.Click += delegate { f.Close(); };
        f.Controls.Add(no);
        f.CancelButton = no;

        return f;
    }

    [STAThread]
    static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        bool silent = false, remove = false;
        foreach (string a in args)
        {
            string s = a.TrimStart('/', '-').ToLowerInvariant();
            if (s.StartsWith("uninstall")) remove = true;
            if (s == "s" || s == "silent" || s == "quiet") silent = true;
        }

        if (silent)
        {
            if (remove) Uninstall(); else Install();
            return;
        }

        Form f;
        if (remove)
        {
            f = Window("إزالة " + NAME,
                "ستُحذف أيقونات التطبيق وملفّاته من هذا الجهاز." + NL + NL
              + "وبياناتك ليست هنا — هي محفوظة في متصفّحك، ولا تمسّها" + NL
              + "الإزالة. ولو كنت داخلاً بحسابك فنسختك فيه باقية.",
                "أزِل", delegate
                {
                    Uninstall();
                    MessageBox.Show("أُزيل " + NAME + ".", NAME,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Application.Exit();
                });
        }
        else
        {
            f = Window("تثبيت " + NAME,
                "خطتك الشهرية — بالعربية، وبياناتك في جهازك." + NL + NL
              + "المكان: " + Dir + NL + NL
              + "بلا صلاحية مسؤول. ويُزال متى شئت من «إضافة أو إزالة" + NL
              + "البرامج» كأيّ برنامج آخر.",
                "ثبّت", delegate
                {
                    try
                    {
                        Install();
                        Process.Start(Path.Combine(Dir, "masrouf.exe"));
                        Application.Exit();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("تعذّر التثبيت:" + NL + ex.Message, NAME,
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
        }
        Application.Run(f);
    }
}
