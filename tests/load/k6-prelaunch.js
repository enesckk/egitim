/**
 * Bilim Akademi — Pre-Launch k6 Load & Concurrency Test Suite
 *
 * Scenarios (150 Peak Concurrent Virtual Users):
 *   1. student_portal (105 VUs — 70.0%):
 *      - Academic 360, Subject/Topic Intelligence, Recommendations, Knowledge Map,
 *        Active Study Plan, Exam Results, Notebook list, Unread Notification count,
 *        plus isolated Notebook create/delete write cycle.
 *   2. teacher_portal (20 VUs  — 13.3%):
 *      - Teacher Dashboard (classroom + exam aggregations), Classrooms list,
 *        Question Bank pagination, Teacher Materials, Academic Subject Taxonomy (OutputCache).
 *   3. coach_portal   (15 VUs  — 10.0%):
 *      - Assigned Student Portfolio, Student 360, Coach Tasks, Private Notes list,
 *        Meetings list, plus isolated Private Note create/delete write cycle.
 *   4. admin_portal   (10 VUs  —  6.7%):
 *      - Institution Overview Dashboard, Institutional Report (unfiltered & classroom-filtered),
 *        Exams list, Classrooms list, Coaches list.
 *
 * Safety & Isolation:
 *   - Uses dedicated staging accounts (@k6.staging) in isolated tenant
 *     11111111-2222-3333-4444-666666666666.
 *   - Authenticates each VU's dedicated account once in setup() to avoid
 *     shared-account contention, unrealistic per-request login, and login rate-limit 429s.
 */

import http from 'k6/http';
import { check, group, sleep } from 'k6';
import exec from 'k6/execution';
import { Trend, Rate } from 'k6/metrics';

const BASE_URL = (__ENV.BASE_URL || 'https://gateway').replace(/\/$/, '');
const ORIGIN = __ENV.ORIGIN || 'https://localhost:28443';
const STAGING_PASSWORD = __ENV.STAGING_PASSWORD || '';
const TEST_MODE = (__ENV.TEST_MODE || 'peak').toLowerCase(); // 'smoke' | 'peak'

// Custom per-role & write latency trends (ms)
const studentLatency = new Trend('student_req_duration', true);
const teacherLatency = new Trend('teacher_req_duration', true);
const coachLatency = new Trend('coach_req_duration', true);
const adminLatency = new Trend('admin_req_duration', true);
const writeLatency = new Trend('write_req_duration', true);
const businessCheckRate = new Rate('business_checks_passed');

const COUNTS = TEST_MODE === 'smoke'
  ? { students: 4, teachers: 2, coaches: 1, admins: 1 }
  : { students: 105, teachers: 20, coaches: 15, admins: 10 };

const SCENARIOS = TEST_MODE === 'smoke'
  ? {
      student_portal: {
        executor: 'constant-vus',
        vus: 4,
        duration: '20s',
        exec: 'studentScenario',
      },
      teacher_portal: {
        executor: 'constant-vus',
        vus: 2,
        duration: '20s',
        exec: 'teacherScenario',
      },
      coach_portal: {
        executor: 'constant-vus',
        vus: 1,
        duration: '20s',
        exec: 'coachScenario',
      },
      admin_portal: {
        executor: 'constant-vus',
        vus: 1,
        duration: '20s',
        exec: 'adminScenario',
      },
    }
  : {
      student_portal: {
        executor: 'ramping-vus',
        startVUs: 0,
        stages: [
          { duration: '15s', target: 50 },
          { duration: '20s', target: 105 },
          { duration: '35s', target: 105 },
          { duration: '10s', target: 0 },
        ],
        gracefulRampDown: '5s',
        exec: 'studentScenario',
      },
      teacher_portal: {
        executor: 'ramping-vus',
        startVUs: 0,
        stages: [
          { duration: '15s', target: 10 },
          { duration: '20s', target: 20 },
          { duration: '35s', target: 20 },
          { duration: '10s', target: 0 },
        ],
        gracefulRampDown: '5s',
        exec: 'teacherScenario',
      },
      coach_portal: {
        executor: 'ramping-vus',
        startVUs: 0,
        stages: [
          { duration: '15s', target: 7 },
          { duration: '20s', target: 15 },
          { duration: '35s', target: 15 },
          { duration: '10s', target: 0 },
        ],
        gracefulRampDown: '5s',
        exec: 'coachScenario',
      },
      admin_portal: {
        executor: 'ramping-vus',
        startVUs: 0,
        stages: [
          { duration: '15s', target: 5 },
          { duration: '20s', target: 10 },
          { duration: '35s', target: 10 },
          { duration: '10s', target: 0 },
        ],
        gracefulRampDown: '5s',
        exec: 'adminScenario',
      },
    };

export const options = {
  scenarios: SCENARIOS,
  setupTimeout: '120s',
  insecureSkipTLSVerify: true,
  hosts: __ENV.GATEWAY_IP ? { localhost: __ENV.GATEWAY_IP } : undefined,
  summaryTrendStats: ['avg', 'min', 'med', 'max', 'p(90)', 'p(95)', 'p(99)'],
  thresholds: {
    http_req_failed: ['rate<0.01'],
    checks: ['rate>0.99'],
    business_checks_passed: ['rate>0.99'],
    http_req_duration: ['med<250', 'p(95)<800', 'p(99)<1500'],
    student_req_duration: ['med<250', 'p(95)<750', 'p(99)<1400'],
    teacher_req_duration: ['med<300', 'p(95)<850', 'p(99)<1500'],
    coach_req_duration: ['med<250', 'p(95)<750', 'p(99)<1400'],
    admin_req_duration: ['med<350', 'p(95)<1000', 'p(99)<1800'],
    write_req_duration: ['med<300', 'p(95)<900', 'p(99)<1600'],
  },
};

function pad12(n) {
  return String(n).padStart(12, '0');
}

function loginAccount(email, password) {
  const res = http.post(
    `${BASE_URL}/api/v1/auth/login`,
    JSON.stringify({ email, password }),
    {
      headers: {
        'Content-Type': 'application/json',
        Origin: ORIGIN,
      },
      tags: { endpoint: 'auth_login', role: 'setup' },
    }
  );

  if (res.status !== 200) {
    throw new Error(`Login failed for ${email}: HTTP ${res.status} - ${res.body}`);
  }

  const body = res.json();
  return body.accessToken;
}

export function setup() {
  if (!STAGING_PASSWORD) {
    throw new Error('STAGING_PASSWORD env var is required to authenticate disposable staging accounts.');
  }

  // Verify health endpoint before authenticating accounts
  const healthRes = http.get(`${BASE_URL}/health`, { tags: { endpoint: 'health_check' } });
  if (healthRes.status !== 200) {
    throw new Error(`Staging API health check failed: HTTP ${healthRes.status}`);
  }

  const students = [];
  for (let i = 1; i <= COUNTS.students; i++) {
    const email = `student.vu${i}@k6.staging`;
    const token = loginAccount(email, STAGING_PASSWORD);
    students.push({
      index: i,
      email,
      token,
      studentId: `66000000-0000-0000-0001-${pad12(i)}`,
    });
  }

  const teachers = [];
  for (let i = 1; i <= COUNTS.teachers; i++) {
    const email = `teacher.vu${i}@k6.staging`;
    const token = loginAccount(email, STAGING_PASSWORD);
    teachers.push({
      index: i,
      email,
      token,
      teacherId: `66000000-0000-0000-0002-${pad12(i)}`,
    });
  }

  const coaches = [];
  for (let i = 1; i <= COUNTS.coaches; i++) {
    const email = `coach.vu${i}@k6.staging`;
    const token = loginAccount(email, STAGING_PASSWORD);
    // Coach i is assigned to students (i-1)*7 + 1 .. i*7
    const primaryStudentIdx = (i - 1) * 7 + 1;
    coaches.push({
      index: i,
      email,
      token,
      coachId: `66000000-0000-0000-0003-${pad12(i)}`,
      assignedStudentId: `66000000-0000-0000-0001-${pad12(primaryStudentIdx)}`,
    });
  }

  const admins = [];
  for (let i = 1; i <= COUNTS.admins; i++) {
    const email = `admin.vu${i}@k6.staging`;
    const token = loginAccount(email, STAGING_PASSWORD);
    const classroomIdx = ((i - 1) % 6) + 1;
    admins.push({
      index: i,
      email,
      token,
      classroomId: `66000000-0000-0000-0006-${pad12(classroomIdx)}`,
    });
  }

  return { students, teachers, coaches, admins };
}

function authHeaders(token) {
  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
    Origin: ORIGIN,
  };
}

function recordCheck(res, expectedStatus, trendMetric, additionalMetric) {
  const ok = check(res, {
    [`status is ${expectedStatus}`]: (r) => r.status === expectedStatus,
  });
  businessCheckRate.add(ok);
  trendMetric.add(res.timings.duration);
  if (additionalMetric) {
    additionalMetric.add(res.timings.duration);
  }
  return ok;
}

// =============================================================================
// 1. STUDENT SCENARIO (Up to 105 Concurrent VUs — 70%)
// =============================================================================
export function studentScenario(data) {
  const account = data.students[(exec.vu.idInTest - 1) % data.students.length];
  const headers = authHeaders(account.token);
  const sid = account.studentId;

  group('student_dashboard_and_academic_reads', () => {
    const r360 = http.get(`${BASE_URL}/api/v1/students/${sid}/360`, {
      headers,
      tags: { role: 'student', endpoint: 'student_360' },
    });
    recordCheck(r360, 200, studentLatency);

    const rSub = http.get(`${BASE_URL}/api/v1/students/${sid}/academic-intelligence/subjects`, {
      headers,
      tags: { role: 'student', endpoint: 'student_ai_subjects' },
    });
    recordCheck(rSub, 200, studentLatency);

    const rTop = http.get(`${BASE_URL}/api/v1/students/${sid}/academic-intelligence/topics`, {
      headers,
      tags: { role: 'student', endpoint: 'student_ai_topics' },
    });
    recordCheck(rTop, 200, studentLatency);

    const rRec = http.get(`${BASE_URL}/api/v1/students/${sid}/academic-intelligence/recommendations`, {
      headers,
      tags: { role: 'student', endpoint: 'student_ai_recommendations' },
    });
    recordCheck(rRec, 200, studentLatency);

    const rMap = http.get(`${BASE_URL}/api/v1/students/${sid}/academic-intelligence/knowledge-map`, {
      headers,
      tags: { role: 'student', endpoint: 'student_ai_knowledge_map' },
    });
    recordCheck(rMap, 200, studentLatency);

    const rPlan = http.get(`${BASE_URL}/api/v1/students/${sid}/study-plans/active`, {
      headers,
      tags: { role: 'student', endpoint: 'student_active_plan' },
    });
    recordCheck(rPlan, 200, studentLatency);

    const rExams = http.get(`${BASE_URL}/api/v1/students/${sid}/exam-results`, {
      headers,
      tags: { role: 'student', endpoint: 'student_exam_results' },
    });
    recordCheck(rExams, 200, studentLatency);

    const rNotif = http.get(`${BASE_URL}/api/v1/notifications/unread-count`, {
      headers,
      tags: { role: 'student', endpoint: 'student_unread_notifications' },
    });
    recordCheck(rNotif, 200, studentLatency);
  });

  // Safe isolated write on each student's own notebook every 2nd iteration
  if (exec.vu.iterationInScenario % 2 === 0) {
    group('student_isolated_notebook_write', () => {
      const createRes = http.post(
        `${BASE_URL}/api/v1/notebook`,
        JSON.stringify({
          title: `k6 Note VU-${account.index} iter-${exec.vu.iterationInScenario}`,
          content: 'Isolated load test study note for formula review.',
          tags: 'k6,staging',
          isPinned: false,
        }),
        {
          headers,
          tags: { role: 'student', endpoint: 'student_notebook_create', op: 'write' },
        }
      );

      const created = recordCheck(createRes, 201, studentLatency, writeLatency);
      if (created) {
        const noteId = createRes.json('id');
        if (noteId) {
          const delRes = http.del(`${BASE_URL}/api/v1/notebook/${noteId}`, null, {
            headers,
            tags: { role: 'student', endpoint: 'student_notebook_delete', op: 'write' },
          });
          recordCheck(delRes, 204, studentLatency, writeLatency);
        }
      }
    });
  }

  sleep(1);
}

// =============================================================================
// 2. TEACHER SCENARIO (Up to 20 Concurrent VUs — 13.3%)
// =============================================================================
export function teacherScenario(data) {
  const account = data.teachers[(exec.vu.idInTest - 1) % data.teachers.length];
  const headers = authHeaders(account.token);

  group('teacher_portal_reads', () => {
    const rDash = http.get(`${BASE_URL}/api/v1/teachers/dashboard`, {
      headers,
      tags: { role: 'teacher', endpoint: 'teacher_dashboard' },
    });
    recordCheck(rDash, 200, teacherLatency);

    const rClass = http.get(`${BASE_URL}/api/v1/classrooms`, {
      headers,
      tags: { role: 'teacher', endpoint: 'teacher_classrooms' },
    });
    recordCheck(rClass, 200, teacherLatency);

    const rQuest = http.get(`${BASE_URL}/api/v1/questions?page=1&pageSize=20`, {
      headers,
      tags: { role: 'teacher', endpoint: 'teacher_questions' },
    });
    recordCheck(rQuest, 200, teacherLatency);

    const rMat = http.get(`${BASE_URL}/api/content/materials/teacher`, {
      headers,
      tags: { role: 'teacher', endpoint: 'teacher_materials' },
    });
    recordCheck(rMat, 200, teacherLatency);

    const rTax = http.get(`${BASE_URL}/api/v1/academic/subjects?page=1&pageSize=50`, {
      headers,
      tags: { role: 'teacher', endpoint: 'teacher_taxonomy_subjects' },
    });
    recordCheck(rTax, 200, teacherLatency);
  });

  sleep(1);
}

// =============================================================================
// 3. COACH SCENARIO (Up to 15 Concurrent VUs — 10%)
// =============================================================================
export function coachScenario(data) {
  const account = data.coaches[(exec.vu.idInTest - 1) % data.coaches.length];
  const headers = authHeaders(account.token);
  const assignedSid = account.assignedStudentId;

  group('coach_portfolio_reads', () => {
    const rStudents = http.get(`${BASE_URL}/api/v1/students?page=1&pageSize=20`, {
      headers,
      tags: { role: 'coach', endpoint: 'coach_students_list' },
    });
    recordCheck(rStudents, 200, coachLatency);

    const r360 = http.get(`${BASE_URL}/api/v1/students/${assignedSid}/360`, {
      headers,
      tags: { role: 'coach', endpoint: 'coach_student_360' },
    });
    recordCheck(r360, 200, coachLatency);

    const rTasks = http.get(`${BASE_URL}/api/v1/students/${assignedSid}/coach-tasks`, {
      headers,
      tags: { role: 'coach', endpoint: 'coach_student_tasks' },
    });
    recordCheck(rTasks, 200, coachLatency);

    const rNotes = http.get(`${BASE_URL}/api/v1/coaching/students/${assignedSid}/notes`, {
      headers,
      tags: { role: 'coach', endpoint: 'coach_student_notes' },
    });
    recordCheck(rNotes, 200, coachLatency);

    const rMeetings = http.get(`${BASE_URL}/api/v1/coaching/meetings`, {
      headers,
      tags: { role: 'coach', endpoint: 'coach_meetings' },
    });
    recordCheck(rMeetings, 200, coachLatency);
  });

  // Safe isolated write on the coach's dedicated assigned student
  if (exec.vu.iterationInScenario % 2 === 0) {
    group('coach_isolated_note_write', () => {
      const createRes = http.post(
        `${BASE_URL}/api/v1/coaching/students/${assignedSid}/notes`,
        JSON.stringify({
          category: 'Akademik',
          content: `k6 Coach-${account.index} private observation iter-${exec.vu.iterationInScenario}`,
        }),
        {
          headers,
          tags: { role: 'coach', endpoint: 'coach_note_create', op: 'write' },
        }
      );

      const created = recordCheck(createRes, 201, coachLatency, writeLatency);
      if (created) {
        const noteId = createRes.json('id');
        if (noteId) {
          const delRes = http.del(`${BASE_URL}/api/v1/coaching/notes/${noteId}`, null, {
            headers,
            tags: { role: 'coach', endpoint: 'coach_note_delete', op: 'write' },
          });
          recordCheck(delRes, 204, coachLatency, writeLatency);
        }
      }
    });
  }

  sleep(1);
}

// =============================================================================
// 4. INSTITUTION ADMIN SCENARIO (Up to 10 Concurrent VUs — 6.7%)
// =============================================================================
export function adminScenario(data) {
  const account = data.admins[(exec.vu.idInTest - 1) % data.admins.length];
  const headers = authHeaders(account.token);

  group('admin_dashboard_and_reporting_reads', () => {
    const rOverview = http.get(`${BASE_URL}/api/v1/institution/overview`, {
      headers,
      tags: { role: 'admin', endpoint: 'admin_institution_overview' },
    });
    recordCheck(rOverview, 200, adminLatency);

    const rReport = http.get(`${BASE_URL}/api/v1/reporting/institution`, {
      headers,
      tags: { role: 'admin', endpoint: 'admin_institutional_report' },
    });
    recordCheck(rReport, 200, adminLatency);

    const rFilteredReport = http.get(
      `${BASE_URL}/api/v1/reporting/institution?classroomId=${account.classroomId}`,
      {
        headers,
        tags: { role: 'admin', endpoint: 'admin_institutional_report_filtered' },
      }
    );
    recordCheck(rFilteredReport, 200, adminLatency);

    const rExams = http.get(`${BASE_URL}/api/v1/exams?page=1&pageSize=20`, {
      headers,
      tags: { role: 'admin', endpoint: 'admin_exams_list' },
    });
    recordCheck(rExams, 200, adminLatency);

    const rClassrooms = http.get(`${BASE_URL}/api/v1/classrooms`, {
      headers,
      tags: { role: 'admin', endpoint: 'admin_classrooms_list' },
    });
    recordCheck(rClassrooms, 200, adminLatency);

    const rCoaches = http.get(`${BASE_URL}/api/v1/coaching/coaches`, {
      headers,
      tags: { role: 'admin', endpoint: 'admin_coaches_list' },
    });
    recordCheck(rCoaches, 200, adminLatency);
  });

  sleep(1.5);
}

