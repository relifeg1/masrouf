-- مصروف — تهيئة القاعدة. تُنفَّذ مرّة واحدة في Supabase ← SQL Editor.
-- بعدها لا يفعل المستخدم شيئاً: يسجّل دخوله فيُنشأ صفّه من تلقائه.

create table if not exists budget (
  id          uuid primary key references auth.users on delete cascade,
  data        jsonb not null,
  updated_at  timestamptz not null default now()
);

alter table budget enable row level security;

-- الحماية هنا، لا في إخفاء المفتاح العلني: من حمل المفتاح لا يرى إلا
-- صفّه. وبلا هذه السطور يرى الجميعُ الجميع.
drop policy if exists "own row read"   on budget;
drop policy if exists "own row write"  on budget;
drop policy if exists "own row update" on budget;

create policy "own row read"   on budget for select using      (auth.uid() = id);
create policy "own row write"  on budget for insert with check (auth.uid() = id);
create policy "own row update" on budget for update using      (auth.uid() = id);

-- تحقّقٌ سريع بعد التنفيذ: يجب أن تعود ثلاث سياسات وrowsecurity = true.
-- select relrowsecurity from pg_class where relname = 'budget';
-- select policyname from pg_policies where tablename = 'budget';
