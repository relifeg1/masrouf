# تهيئة النشر والحساب

ثلاث خطوات: تستضيف الملفّات، تُنشئ قاعدة، تبني نسخةً تحمل مفتاحها.
بعدها يُثبَّت التطبيق على الأجهزة، ويدخل صاحبه ببريده، وتصله التحديثات.

---

## ١ · لماذا لا يُثبَّت من الملفّ

فتحُ `index.html` بنقرتين يجعل عنوانه `file://`. والمتصفّحات تمنع في
هذا العنوان ثلاثة أشياء تحتاجها كلّها:

| ما يُمنع | فيضيع |
|---|---|
| تسجيل عامل الخدمة | العمل بلا إنترنت، ووصول التحديثات |
| «تثبيت التطبيق» | الأيقونة على الشاشة الرئيسة |
| نداء خادمٍ خارجي | الدخول والمزامنة |

القيد في المتصفّح لا في التطبيق، ولا يُلتفّ عليه. الحلّ أن تُستضاف
الملفّات على عنوان `https://` — عندها يُثبَّت التطبيق كأيّ تطبيقٍ آخر،
ويُفتح من الأيقونة بلا شريط عنوان.

للتجربة محليّاً يكفي `http://localhost` (المتصفّحات تستثنيه):

```bash
cd E:/Claude/masrouf && python -m http.server 8824
```

---

## ٢ · الاستضافة

المستودع خاصّ، وصفحات GitHub لا تخدم الخاصّ مجّاناً. اثنان يخدمانه:

- **Cloudflare Pages** — يربط المستودع الخاصّ، ينشر عند كل دفعة، مجّاناً.
- **Netlify** — مثله.

ولا بناء ولا أمر: المجلّد كما هو هو الموقع. اضبط مجلّد النشر على جذر
المستودع واترك أمر البناء فارغاً.

بعد أوّل نشر، افتح الرابط في الجوّال ← قائمة المتصفّح ← «إضافة إلى
الشاشة الرئيسة».

---

## ٣ · القاعدة

في [supabase.com](https://supabase.com) أنشئ مشروعاً مجّانياً. ثم في
**SQL Editor** ألصق هذا كلّه ونفّذه:

```sql
create table if not exists budget (
  id          uuid primary key references auth.users on delete cascade,
  data        jsonb not null,
  updated_at  timestamptz not null default now()
);

alter table budget enable row level security;

-- كل واحدٍ يرى صفّه وحده. هذه هي الحماية — لا إخفاء المفتاح.
create policy "own row read"   on budget for select using (auth.uid() = id);
create policy "own row write"  on budget for insert with check (auth.uid() = id);
create policy "own row update" on budget for update using (auth.uid() = id);
```

ثم **Authentication ← URL Configuration**: ضع رابط موقعك في
`Site URL`، وأضفه في `Redirect URLs`.

### رمز البريد

القالب الافتراضي يرسل رابطاً فقط. ليصل رمزٌ من ستّة أرقام أيضاً، افتح
**Authentication ← Email Templates ← Magic Link** وأضف سطراً:

```html
<p>رمزك: <b>{{ .Token }}</b></p>
```

والتطبيق يقبل الاثنين: من ضغط الرابط دخل، ومن نسخ الرمز دخل.

### Google (اختياري)

**Authentication ← Providers ← Google**: فعّله وضع بيانات العميل من
Google Cloud Console. بلا هذا يبقى الدخول بالبريد وحده، وهو كافٍ.

---

## ٤ · بناء نسخةٍ تحمل المفتاح

أنشئ `E:\Claude\budget\supabase.json` — **خارج المستودع، لا يُرفع**:

```json
{
  "url": "https://xxxxxxxx.supabase.co",
  "anon_key": "eyJhbGciOi..."
}
```

خذهما من **Settings ← API**: `Project URL` و`anon public`. والمفتاح
العلني علنيّ بطبعه — من فتح الصفحة قرأه، ولا ضير: RLS يمنعه من رؤية
صفٍّ ليس لصاحبه. **ولا تضع `service_role` هنا أبداً** — ذاك يتجاوز RLS.

ثم:

```bash
python E:/Claude/budget/build_dist.py
```

يبني `index.html`، ويحقن المفتاح ورقم النسخة في التطبيق وعامل الخدمة
معاً. وإن غاب الملفّ بُنيت نسخةٌ بلا حساب — تعمل على الجهاز وحده،
ويقول تقرير البناء ذلك صراحةً.

---

## ٥ · إصدار نسخةٍ جديدة

```bash
python E:/Claude/budget/build_dist.py
cd E:/Claude/masrouf && git add -A && git commit -m "..." && git push
```

الاستضافة تنشر، والمتصفّح يسأل عن نسخةٍ جديدة عند كل فتح — لأن الصفحة
تُطلب من الشبكة أوّلاً بتجاوز ذاكرة المتصفّح، ولأن عامل الخدمة يُطلب
بـ`updateViaCache: "none"`. فإذا وصلت ظهر شريط «نسخة جديدة جاهزة»،
والتحديث بضغطة — لا يُفرض على أحدٍ وهو يكتب.

بيانات المستخدم في `localStorage` وفي صفّه بالقاعدة؛ التحديث لا يمسّها.
