# DEMO / STAGING SHOWCASE V1 DATASET DOCUMENTATION

---

## 1. OVERVIEW

The **Demo Showcase Dataset (DEMO_SHOWCASE_V1)** provides an idempotent, realistic staging environment for live platform demonstrations. It populates an entire single-tenant institution (**Demo Eğitim Kurumu**) with valid academic data, longitudinal exam attempts, coach task assignments, notifications, and dynamic AI interpretation inputs.

---

## 2. DEMO ACCOUNTS & CREDENTIALS

All demo accounts belong exclusively to **Demo Eğitim Kurumu** (`d0000000-0000-0000-0000-000000000001`).

| Role | Display Name | Username / Email | Password Configuration Key |
| :--- | :--- | :--- | :--- |
| **InstitutionAdmin** | Demo Yönetici | `admin.demo@egitim.com` | `DemoSeed:AdminPassword` |
| **Teacher** | Ayşe Öğretmen | `teacher.demo@egitim.com` | `DemoSeed:TeacherPassword` |
| **Coach** | Mehmet Koç | `coach.demo@egitim.com` | `DemoSeed:CoachPassword` |
| **Student** | Ahmet Yılmaz | `student.demo@egitim.com` | `DemoSeed:StudentPassword` |
| **Parent** | Fatma Yılmaz | `parent.demo@egitim.com` | `DemoSeed:ParentPassword` |

> **Security Note**: Real passwords are **never** committed to source code or logged in plain text. Passwords must be provided via environment variables, user-secrets, or secret managers.

---

## 3. CONFIGURATION & ENABLE INSTRUCTION

Demo seeding is **disabled by default** (`DemoSeed:Enabled = false`). To enable demo seeding in Development or Staging environments, set the configuration in `appsettings.json` or environment variables:

```json
{
  "DemoSeed": {
    "Enabled": true,
    "AdminPassword": "YourSecureDemoPassword123!",
    "TeacherPassword": "YourSecureDemoPassword123!",
    "CoachPassword": "YourSecureDemoPassword123!",
    "StudentPassword": "YourSecureDemoPassword123!",
    "ParentPassword": "YourSecureDemoPassword123!"
  }
}
```

Environment variable syntax:
```bash
export DemoSeed__Enabled=true
export DemoSeed__AdminPassword="YourSecureDemoPassword123!"
export DemoSeed__TeacherPassword="YourSecureDemoPassword123!"
export DemoSeed__CoachPassword="YourSecureDemoPassword123!"
export DemoSeed__StudentPassword="YourSecureDemoPassword123!"
export DemoSeed__ParentPassword="YourSecureDemoPassword123!"
```

---

## 4. IDEMPOTENCY & SAFE RESET PROCEDURE

### Idempotency
`DemoDataSeeder` uses deterministic Guid markers (`DEMO_SHOWCASE_V1`). Re-running application startup or executing seed operations multiple times is safely idempotent and will **never** duplicate institutions, users, students, exams, attempts, tasks, or notifications.

### Safe Reset Command
To purge and re-seed only the demo dataset (without touching any non-demo tenants):

1. **CLI Command**:
   ```bash
   dotnet run --project src/Api/EgitimPlatform.Api -- --reset-demo
   ```
2. **Programmatic Reset**:
   Invoke `await demoDataSeeder.ResetDemoDataAsync(ct)` in non-Production environments.

---

## 5. DEMO PRESENTATION SCRIPT (8–10 MINUTES)

### Step 1: Admin Overview (1.5 mins)
* **Login**: `admin.demo@egitim.com`
* **Action**:
  1. Open `/admin/overview` to view operational metrics for **Demo Eğitim Kurumu**.
  2. Navigate to `/admin/exams` to highlight the 5 finalized TYT exams (`Demo TYT 1` through `Demo TYT 5`).
  3. Show the Excel Import pipeline under `/admin/exams/{examId}/results/entry`.

### Step 2: Student Academic Intelligence & Planning (3 mins)
* **Login**: `student.demo@egitim.com`
* **Action**:
  1. Open `/student/today` to view the student dashboard greeting, upcoming goals, and completed coach tasks.
  2. Navigate to `/student/academic` to show Academic Intelligence:
     * **Türkçe**: Strong performance (82% success rate).
     * **Matematik**: Clear improvement curve (38% → 67%). Highlight *Trigonometri* (Critical) vs. *Temel Kavramlar* (Strong).
     * **Fizik**: Decline curve (67% → 44%). Highlight *Elektrik* (Critical + Decreasing).
     * **Biyoloji**: *InsufficientData* (Below evidence threshold).
  3. View **Yapay Zeka Destekli Yorum** card generated live from the deterministic fact package.
  4. Navigate to `/student/plans` and trigger **Study Plan Proposal** (7 days, 120 mins/day). Click **Planı Uygula** to create an active plan.

### Step 3: Coach Portfolio & Task Creation (2 mins)
* **Login**: `coach.demo@egitim.com`
* **Action**:
  1. Open `/coach/students` and select **Ahmet Yılmaz**.
  2. Review student academic priorities (*Fizik / Elektrik* and *Matematik / Trigonometri*).
  3. Click **Yeni Görevi Ata** to create a live Coach Task:
     * Title: `Trigonometri Soru Çözümü`
     * Target: `Matematik` / `Trigonometri`
     * Duration: `45 dakika`

### Step 4: Student Task Completion (1 min)
* **Login**: `student.demo@egitim.com`
* **Action**:
  1. Open `/student/plans` (or `/student/today`).
  2. View notification banner for the new task assigned by Mehmet Koç.
  3. Click **Tamamlandı Olarak İşaretle**.

### Step 5: Parent Monitoring & AI Interpretation (1.5 mins)
* **Login**: `parent.demo@egitim.com`
* **Action**:
  1. Open `/parent/summary` to inspect **Ahmet Yılmaz**'s progress.
  2. View read-only academic summary, exam results, and completed coach tasks.
  3. Read the parent-tailored **Yapay Zeka Destekli Yorum** explaining student progress without raw technical terminology.
