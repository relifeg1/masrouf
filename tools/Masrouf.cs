// مصروف — مشغّلٌ لسطح المكتب
//
// لماذا مشغّلٌ لا تطبيقٌ مُحزَّم: لأن التطبيق يُحدَّث من الموقع نفسه.
// لو حُزِمت الشيفرة داخل الملفّ التنفيذي لتجمّدت النسخة عند من حمّلها،
// ولعاد كلّ تحديثٍ يحتاج تنزيلاً جديداً — وهو نقيض ما بُني من أجله.
// فهذا الملفّ يفتح التطبيق في نافذةٍ مستقلّة بلا شريط متصفّح، ولا
// يفعل غير ذلك: الاختصارات عملُ المثبِّت وحده. كان ينشئها في كل
// تشغيل فاجتمع للاختصارات صانعان في مواضع مختلفة.
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

    [STAThread]
    static void Main()
    {
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
