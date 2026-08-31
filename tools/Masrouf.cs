// مصروف — مشغّلٌ لسطح المكتب
//
// لماذا مشغّلٌ لا تطبيقٌ مُحزَّم: لأن التطبيق يُحدَّث من الموقع نفسه.
// لو حُزِمت الشيفرة داخل الملفّ التنفيذي لتجمّدت النسخة عند من حمّلها،
// ولعاد كلّ تحديثٍ يحتاج تنزيلاً جديداً — وهو نقيض ما بُني من أجله.
// فهذا الملفّ يفتح التطبيق في نافذةٍ مستقلّة بلا شريط متصفّح، ويضع
// أيقونةً على سطح المكتب وفي قائمة ابدأ. والتحديث يبقى تلقائياً.
//
// يُبنى بلا مكتبات خارجية:
//   csc /target:winexe /win32icon:masrouf.ico /out:masrouf.exe Masrouf.cs

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

static class Masrouf
{
    const string URL = "https://relifeg1.github.io/masrouf/";
    const string NAME = "مصروف";

    // ترتيب البحث: Edge أوّلاً لأنه على كل ويندوز ١٠ بلا تثبيت
    static readonly string[] Browsers =
    {
        @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
        @"C:\Program Files\Google\Chrome\Application\chrome.exe",
        @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe"
    };

    static string FindBrowser()
    {
        foreach (string p in Browsers)
            if (File.Exists(p)) return p;
        return null;
    }

    // الاختصارات تُنشأ بربطٍ متأخّر — كي لا يحتاج البناء مرجعاً لـCOM.
    //
    // والاسم يُجرَّب عربياً ثم لاتينياً: على نظامٍ لغتُه غير العربية يفشل
    // WScript.Shell في حفظ اسمٍ خارج ترميز النظام — يحوّله إلى «؟؟؟؟؟»
    // وهي محرَّمةٌ في أسماء الملفّات. قِيس ذلك، ولا يُكتفى بافتراضه.
    static void MakeShortcut(string folder)
    {
        if (!TryShortcut(folder, NAME)) TryShortcut(folder, "Masrouf");
    }

    static bool TryShortcut(string folder, string name)
    {
        try
        {
            string lnk = Path.Combine(folder, name + ".lnk");
            if (File.Exists(lnk)) return true;
            Type t = Type.GetTypeFromProgID("WScript.Shell");
            if (t == null) return false;
            object shell = Activator.CreateInstance(t);
            object sc = t.InvokeMember("CreateShortcut",
                System.Reflection.BindingFlags.InvokeMethod, null, shell,
                new object[] { lnk });
            Type st = sc.GetType();
            string exe = Application.ExecutablePath;
            st.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty,
                null, sc, new object[] { exe });
            st.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty,
                null, sc, new object[] { Path.GetDirectoryName(exe) });
            st.InvokeMember("IconLocation", System.Reflection.BindingFlags.SetProperty,
                null, sc, new object[] { exe + ",0" });
            st.InvokeMember("Description", System.Reflection.BindingFlags.SetProperty,
                null, sc, new object[] { "خطتك الشهرية" });
            st.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod,
                null, sc, null);
            return File.Exists(lnk);
        }
        catch { return false; }   /* تعذّر اختصار لا يمنع فتح التطبيق */
    }

    [STAThread]
    static void Main()
    {
        MakeShortcut(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
        MakeShortcut(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu));

        string br = FindBrowser();
        try
        {
            if (br != null)
            {
                // ‏--app يفتحها نافذةً بلا شريط عنوان ولا تبويبات:
                // أقرب ما يكون إلى تطبيقٍ مستقلّ.
                var psi = new ProcessStartInfo(br,
                    "--app=" + URL + " --window-size=1280,900");
                psi.UseShellExecute = false;
                Process.Start(psi);
            }
            else
            {
                // لا Edge ولا Chrome: المتصفّح الافتراضي أفضل من لا شيء
                Process.Start(URL);
            }
        }
        catch (Exception e)
        {
            MessageBox.Show("تعذّر فتح التطبيق:\n" + e.Message
                + "\n\nافتح هذا العنوان في متصفّحك:\n" + URL,
                NAME, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
