# -*- coding: utf-8 -*-
"""يبني masrouf.exe بلا تنزيل أيّ شيء.

يستعمل csc الموجود مع .NET Framework في كل ويندوز، فلا nuget ولا
حزمة. والأيقونة تُصنع هنا: صيغة ICO تقبل PNG داخلها كما هو، فلا
حاجة إلى مكتبة صور.
"""
import io
import os
import struct
import subprocess

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
CSC = r'C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe'
PNG = os.path.join(ROOT, 'icon-192.png')
ICO = os.path.join(HERE, 'masrouf.ico')
CS = os.path.join(HERE, 'Masrouf.cs')
EXE = os.path.join(HERE, 'masrouf.exe')


def make_ico():
    png = io.open(PNG, 'rb').read()
    assert png[:8] == b'\x89PNG\r\n\x1a\n', 'ليست PNG'
    w, h = struct.unpack('>II', png[16:24])
    assert w <= 255 and h <= 255, 'ICO يخزّن المقاس في بايت واحد'
    head = struct.pack('<HHH', 0, 1, 1)              # محجوز · نوع=أيقونة · عدد=1
    entry = struct.pack('<BBBBHHII', w, h, 0, 0, 1, 32, len(png), 22)
    io.open(ICO, 'wb').write(head + entry + png)
    return w, h, len(png)


w, h, n = make_ico()
print('الأيقونة: %dx%d · %d بايت' % (w, h, n))

r = subprocess.run(
    # ‏/codepage:65001 ضروريّ: بدونه يقرأ csc المصدر بترميز النظام
    # فتتشوّه كل النصوص العربية — والاختصار يفشل باسمٍ لا يصلح ملفّاً.
    [CSC, '/nologo', '/target:winexe', '/optimize+', '/codepage:65001',
     '/win32icon:' + ICO, '/out:' + EXE,
     '/reference:System.dll', '/reference:System.Windows.Forms.dll',
     '/reference:System.Drawing.dll', CS],
    capture_output=True, text=True, encoding='utf-8', errors='replace')

if r.returncode != 0:
    print('فشل البناء:\n' + (r.stdout or '') + (r.stderr or ''))
    raise SystemExit(1)

print('بُني: %s · %d بايت' % (EXE, os.path.getsize(EXE)))
