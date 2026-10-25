import React from 'react';
import {
  BrowserRouter,
  Routes,
  Route,
  Navigate,
  useLocation,
  useNavigate,
} from 'react-router-dom';
import {
  LayoutDashboard,
  CalendarDays,
  Calendar,
  FileText,
  MessageSquare,
  User,
  Users,
  BarChart3,
  GraduationCap,
  BookOpen,
  BookMarked,
  FolderArchive,
  UserCheck,
  Building2,
  LogOut,
  HelpCircle,
} from 'lucide-react';
import { AppShell } from '@/components/layout/AppShell';
import { NavItem } from '@/components/layout/BottomNav';
import { AuthProvider, useAuth, ProtectedRoute, getRoleDefaultRoute } from '@/auth';

import { Suspense, lazy } from 'react';
import { LoadingState } from '@/components/ui/LoadingState';

// Auth Features
import { LoginView, ForgotPasswordView, ResetPasswordView } from '@/features/auth';

// Platform Common Views
import { ForbiddenView } from '@/features/common/ForbiddenView';
import { NotFoundView } from '@/features/common/NotFoundView';

// Question Bank & Assembly Features (Lazy Loaded)
const QuestionBankView = lazy(() => import('@/features/questions/QuestionBankView').then((m) => ({ default: m.QuestionBankView })));
const ExamQuestionAssemblyView = lazy(() => import('@/features/admin/exams/ExamQuestionAssemblyView').then((m) => ({ default: m.ExamQuestionAssemblyView })));
const ExamResultEntryView = lazy(() => import('@/features/exams/results').then((m) => ({ default: m.ExamResultEntryView })));

// Student Portal Features (Lazy Loaded)
const StudentTodayView = lazy(() => import('@/features/student/today').then((m) => ({ default: m.StudentTodayView })));
const StudentAcademicView = lazy(() => import('@/features/student/academic').then((m) => ({ default: m.StudentAcademicView })));
const StudentPlansView = lazy(() => import('@/features/student/plans').then((m) => ({ default: m.StudentPlansView })));
const StudentExamsView = lazy(() => import('@/features/student/exams').then((m) => ({ default: m.StudentExamsView })));
const StudentNotebookView = lazy(() => import('@/features/student/notebook/StudentNotebookView'));
const StudentMaterialsView = lazy(() => import('@/features/student/materials').then((m) => ({ default: m.StudentMaterialsView })));
const StudentMessagesView = lazy(() => import('@/features/student/messages').then((m) => ({ default: m.StudentMessagesView })));
const StudentProfileView = lazy(() => import('@/features/student/profile').then((m) => ({ default: m.StudentProfileView })));

// Coach Portal Features (Lazy Loaded)
const CoachDashboardView = lazy(() => import('@/features/coach/dashboard').then((m) => ({ default: m.CoachDashboardView })));
const CoachStudentsView = lazy(() => import('@/features/coach/students').then((m) => ({ default: m.CoachStudentsView })));
const CoachStudentDetailView = lazy(() => import('@/features/coach/students').then((m) => ({ default: m.CoachStudentDetailView })));
const CoachMeetingsView = lazy(() => import('@/features/coach/meetings').then((m) => ({ default: m.CoachMeetingsView })));
const CoachMessagesView = lazy(() => import('@/features/coach/messages').then((m) => ({ default: m.CoachMessagesView })));
const CoachReportsView = lazy(() => import('@/features/coach/reports').then((m) => ({ default: m.CoachReportsView })));

// Teacher Portal Features (Lazy Loaded)
const TeacherDashboardView = lazy(() => import('@/features/teacher/dashboard').then((m) => ({ default: m.TeacherDashboardView })));
const TeacherClassesView = lazy(() => import('@/features/teacher/classes').then((m) => ({ default: m.TeacherClassesView })));
const TeacherClassDetailView = lazy(() => import('@/features/teacher/classes').then((m) => ({ default: m.TeacherClassDetailView })));
const TeacherStudentDetailView = lazy(() => import('@/features/teacher/students').then((m) => ({ default: m.TeacherStudentDetailView })));
const TeacherAcademicView = lazy(() => import('@/features/teacher/academic').then((m) => ({ default: m.TeacherAcademicView })));
const TeacherContentView = lazy(() => import('@/features/teacher/content').then((m) => ({ default: m.TeacherContentView })));
const TeacherMessagesView = lazy(() => import('@/features/teacher/messages').then((m) => ({ default: m.TeacherMessagesView })));

// Parent Portal Features (Lazy Loaded)
const ParentSummaryView = lazy(() => import('@/features/parent/summary').then((m) => ({ default: m.ParentSummaryView })));
const ParentMessagesView = lazy(() => import('@/features/parent/messages').then((m) => ({ default: m.ParentMessagesView })));

// Admin Portal Features (Lazy Loaded)
const InstitutionDashboardView = lazy(() => import('@/features/admin/overview').then((m) => ({ default: m.InstitutionDashboardView })));
const StudentDirectoryView = lazy(() => import('@/features/admin/students').then((m) => ({ default: m.StudentDirectoryView })));
const AdminCoachesView = lazy(() => import('@/features/admin/coaches').then((m) => ({ default: m.AdminCoachesView })));
const AdminTeachersView = lazy(() => import('@/features/admin/teachers').then((m) => ({ default: m.AdminTeachersView })));
const AdminClassesView = lazy(() => import('@/features/admin/classes').then((m) => ({ default: m.AdminClassesView })));
const AdminExamsView = lazy(() => import('@/features/admin/exams').then((m) => ({ default: m.AdminExamsView })));
const AdminReportsView = lazy(() => import('@/features/admin/reports').then((m) => ({ default: m.AdminReportsView })));


interface PortalNavItem extends NavItem {
  path: string;
}

// 1. Student Navigation Items
const STUDENT_NAV_ITEMS: PortalNavItem[] = [
  {
    id: 'today',
    path: '/student/today',
    label: 'Bugün',
    icon: <LayoutDashboard className="h-5 w-5" />,
  },
  {
    id: 'academic',
    path: '/student/academic',
    label: 'Akademik Analiz',
    icon: <BookOpen className="h-5 w-5" />,
  },
  {
    id: 'plans',
    path: '/student/plans',
    label: 'Planlar',
    icon: <CalendarDays className="h-5 w-5" />,
  },
  {
    id: 'exams',
    path: '/student/exams',
    label: 'Denemeler',
    icon: <FileText className="h-5 w-5" />,
  },
  {
    id: 'notebook',
    path: '/student/notebook',
    label: 'Defter',
    icon: <BookMarked className="h-5 w-5" />,
  },
  {
    id: 'materials',
    path: '/student/materials',
    label: 'Materyaller',
    icon: <FolderArchive className="h-5 w-5" />,
  },
  {
    id: 'messages',
    path: '/student/messages',
    label: 'Mesajlar',
    icon: <MessageSquare className="h-5 w-5" />,
  },
  {
    id: 'profile',
    path: '/student/profile',
    label: 'Profil',
    icon: <User className="h-5 w-5" />,
  },
];

// 2. Coach Navigation Items (Desktop Dark Navy Sidebar & Mobile Drawer)
const COACH_NAV_ITEMS: PortalNavItem[] = [
  {
    id: 'today',
    path: '/coach/today',
    label: 'Genel Bakış',
    icon: <LayoutDashboard className="h-5 w-5" />,
  },
  {
    id: 'students',
    path: '/coach/students',
    label: 'Öğrenciler',
    icon: <Users className="h-5 w-5" />,
  },
  {
    id: 'meetings',
    path: '/coach/meetings',
    label: 'Görüşmeler',
    icon: <Calendar className="h-5 w-5" />,
  },
  {
    id: 'messages',
    path: '/coach/messages',
    label: 'Mesajlar',
    icon: <MessageSquare className="h-5 w-5" />,
  },
  {
    id: 'reports',
    path: '/coach/reports',
    label: 'Raporlar',
    icon: <BarChart3 className="h-5 w-5" />,
  },
];

// 3. Teacher Navigation Items (Desktop Dark Navy Sidebar & Mobile Drawer)
const TEACHER_NAV_ITEMS: PortalNavItem[] = [
  {
    id: 'today',
    path: '/teacher/today',
    label: 'Genel Bakış',
    icon: <LayoutDashboard className="h-5 w-5" />,
  },
  {
    id: 'classes',
    path: '/teacher/classes',
    label: 'Sınıflar',
    icon: <GraduationCap className="h-5 w-5" />,
  },
  {
    id: 'academic',
    path: '/teacher/academic',
    label: 'Akademik Takip',
    icon: <BookOpen className="h-5 w-5" />,
  },
  {
    id: 'content',
    path: '/teacher/content',
    label: 'İçerikler',
    icon: <FileText className="h-5 w-5" />,
  },
  {
    id: 'questions',
    path: '/teacher/questions',
    label: 'Soru Bankası',
    icon: <HelpCircle className="h-5 w-5" />,
  },
  {
    id: 'messages',
    path: '/teacher/messages',
    label: 'Mesajlar',
    icon: <MessageSquare className="h-5 w-5" />,
  },
];

// 4. Parent Navigation Items (Desktop & Mobile)
const PARENT_NAV_ITEMS: PortalNavItem[] = [
  {
    id: 'summary',
    path: '/parent/summary',
    label: 'Öğrenci Özeti',
    icon: <LayoutDashboard className="h-5 w-5" />,
  },
  {
    id: 'messages',
    path: '/parent/messages',
    label: 'Mesajlar',
    icon: <MessageSquare className="h-5 w-5" />,
  },
];

// 5. Institution Admin Navigation Items (Desktop Dark Navy Sidebar & Mobile Drawer)
const ADMIN_NAV_ITEMS: PortalNavItem[] = [
  {
    id: 'overview',
    path: '/admin/overview',
    label: 'Genel Bakış',
    icon: <LayoutDashboard className="h-5 w-5" />,
  },
  {
    id: 'students',
    path: '/admin/students',
    label: 'Öğrenciler',
    icon: <Users className="h-5 w-5" />,
  },
  {
    id: 'coaches',
    path: '/admin/coaches',
    label: 'Koçlar',
    icon: <UserCheck className="h-5 w-5" />,
  },
  {
    id: 'teachers',
    path: '/admin/teachers',
    label: 'Öğretmenler',
    icon: <GraduationCap className="h-5 w-5" />,
  },
  {
    id: 'classes',
    path: '/admin/classes',
    label: 'Sınıflar',
    icon: <Building2 className="h-5 w-5" />,
  },
  {
    id: 'exams',
    path: '/admin/exams',
    label: 'Deneme Sınavları',
    icon: <FileText className="h-5 w-5" />,
  },
  {
    id: 'questions',
    path: '/admin/questions',
    label: 'Soru Bankası',
    icon: <HelpCircle className="h-5 w-5" />,
  },
  {
    id: 'reports',
    path: '/admin/reports',
    label: 'Raporlar',
    icon: <BarChart3 className="h-5 w-5" />,
  },
];

// Student Portal Layout Component
const StudentPortalLayout: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const currentNav =
    STUDENT_NAV_ITEMS.find((item) => location.pathname.startsWith(item.path)) ||
    STUDENT_NAV_ITEMS[0];

  const handleNavChange = (id: string) => {
    const target = STUDENT_NAV_ITEMS.find((item) => item.id === id);
    if (target) {
      navigate(target.path);
    }
  };

  return (
    <AppShell
      role="student"
      navItems={STUDENT_NAV_ITEMS}
      activeNavId={currentNav.id}
      onNavChange={handleNavChange}
      headerProps={{
        pageTitle: currentNav.label,
        brandTitle: 'Eğitim Platformu',
        brandSubtitle: 'Öğrenci Portalı',
        institutionName: user?.institutionName || 'Kurumsal Şube',
        userName: user?.name && !user.name.includes('@') && !user.name.includes('.demo') ? user.name : 'Ahmet Yılmaz',
        userRole: user?.roleLabel || 'Öğrenci',
        onProfileClick: () => navigate('/student/profile'),
      }}
      sidebarFooter={
        <button
          type="button"
          onClick={() => {
            logout();
            navigate('/login', { replace: true });
          }}
          className="flex items-center gap-2 text-xs text-navy-400 hover:text-white px-3 py-2 rounded-lg hover:bg-navy-900 transition-colors w-full"
        >
          <LogOut className="h-4 w-4" />
          <span>Çıkış Yap</span>
        </button>
      }
    >
      <Suspense fallback={<LoadingState message="Sayfa yükleniyor..." />}>
        <Routes>
          <Route path="/today" element={<StudentTodayView />} />
          <Route path="/academic" element={<StudentAcademicView />} />
          <Route path="/plans" element={<StudentPlansView />} />
          <Route path="/exams" element={<StudentExamsView />} />
          <Route path="/notebook" element={<StudentNotebookView />} />
          <Route path="/materials" element={<StudentMaterialsView />} />
          <Route path="/messages" element={<StudentMessagesView />} />
          <Route path="/profile" element={<StudentProfileView />} />
          <Route path="*" element={<Navigate to="/student/today" replace />} />
        </Routes>
      </Suspense>
    </AppShell>
  );
};

// Coach Portal Layout Component
const CoachPortalLayout: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  let activeNavId = 'today';
  let pageTitle = 'Genel Bakış';

  if (location.pathname.startsWith('/coach/students/')) {
    activeNavId = 'students';
    pageTitle = 'Öğrenci Detayı';
  } else {
    const currentNav =
      COACH_NAV_ITEMS.find((item) => location.pathname.startsWith(item.path)) ||
      COACH_NAV_ITEMS[0];
    activeNavId = currentNav.id;
    pageTitle = currentNav.label;
  }

  const handleNavChange = (id: string) => {
    const target = COACH_NAV_ITEMS.find((item) => item.id === id);
    if (target) {
      navigate(target.path);
    }
  };

  return (
    <AppShell
      role="coach"
      navItems={COACH_NAV_ITEMS}
      activeNavId={activeNavId}
      onNavChange={handleNavChange}
      headerProps={{
        pageTitle,
        brandTitle: 'Bilim Akademi',
        brandSubtitle: 'Koçluk Portalı',
        institutionName: user?.institutionName || 'Kadıköy Şubesi',
        userName: user?.name || 'Hasan Yılmaz',
        userRole: user?.roleLabel || 'Kıdemli YKS Koçu',
      }}
      sidebarFooter={
        <button
          type="button"
          onClick={() => {
            logout();
            navigate('/login', { replace: true });
          }}
          className="flex items-center gap-2 text-xs text-navy-400 hover:text-white px-3 py-2 rounded-lg hover:bg-navy-900 transition-colors w-full"
        >
          <LogOut className="h-4 w-4" />
          <span>Çıkış Yap</span>
        </button>
      }
    >
      <Suspense fallback={<LoadingState message="Sayfa yükleniyor..." />}>
        <Routes>
          <Route path="/today" element={<CoachDashboardView />} />
          <Route path="/students" element={<CoachStudentsView />} />
          <Route path="/students/:studentId" element={<CoachStudentDetailView />} />
          <Route path="/meetings" element={<CoachMeetingsView />} />
          <Route path="/messages" element={<CoachMessagesView />} />
          <Route path="/reports" element={<CoachReportsView />} />
          <Route path="/exams/:examId/results/entry" element={<ExamResultEntryView portalRole="coach" />} />
          <Route path="*" element={<Navigate to="/coach/today" replace />} />
        </Routes>
      </Suspense>
    </AppShell>

  );
};

// Teacher Portal Layout Component
const TeacherPortalLayout: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  let activeNavId = 'today';
  let pageTitle = 'Genel Bakış';

  if (location.pathname.startsWith('/teacher/students/')) {
    activeNavId = 'classes';
    pageTitle = 'Öğrenci Detayı';
  } else if (location.pathname.startsWith('/teacher/classes/')) {
    activeNavId = 'classes';
    pageTitle = 'Sınıf Detayı';
  } else {
    const currentNav =
      TEACHER_NAV_ITEMS.find((item) => location.pathname.startsWith(item.path)) ||
      TEACHER_NAV_ITEMS[0];
    activeNavId = currentNav.id;
    pageTitle = currentNav.label;
  }

  const handleNavChange = (id: string) => {
    const target = TEACHER_NAV_ITEMS.find((item) => item.id === id);
    if (target) {
      navigate(target.path);
    }
  };

  return (
    <AppShell
      role="teacher"
      navItems={TEACHER_NAV_ITEMS}
      activeNavId={activeNavId}
      onNavChange={handleNavChange}
      headerProps={{
        pageTitle,
        brandTitle: 'Bilim Akademi',
        brandSubtitle: 'Öğretmen Portalı',
        institutionName: user?.institutionName || 'Kadıköy Şubesi',
        userName: user?.name || 'Kemal Bey',
        userRole: user?.roleLabel || 'Matematik & Fizik Zümresi',
      }}
      sidebarFooter={
        <button
          type="button"
          onClick={() => {
            logout();
            navigate('/login', { replace: true });
          }}
          className="flex items-center gap-2 text-xs text-navy-400 hover:text-white px-3 py-2 rounded-lg hover:bg-navy-900 transition-colors w-full"
        >
          <LogOut className="h-4 w-4" />
          <span>Çıkış Yap</span>
        </button>
      }
    >
      <Suspense fallback={<LoadingState message="Sayfa yükleniyor..." />}>
        <Routes>
          <Route path="/today" element={<TeacherDashboardView />} />
          <Route path="/classes" element={<TeacherClassesView />} />
          <Route path="/classes/:classId" element={<TeacherClassDetailView />} />
          <Route path="/students/:studentId" element={<TeacherStudentDetailView />} />
          <Route path="/academic" element={<TeacherAcademicView />} />
          <Route path="/content" element={<TeacherContentView />} />
          <Route path="/questions" element={<QuestionBankView role="teacher" />} />
          <Route path="/exams/:examId/questions" element={<ExamQuestionAssemblyView />} />
          <Route path="/exams/:examId/results/entry" element={<ExamResultEntryView portalRole="teacher" />} />
          <Route path="/messages" element={<TeacherMessagesView />} />
          <Route path="*" element={<Navigate to="/teacher/today" replace />} />
        </Routes>
      </Suspense>
    </AppShell>
  );
};

// Parent Portal Layout Component
const ParentPortalLayout: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const currentNav =
    PARENT_NAV_ITEMS.find((item) => location.pathname.startsWith(item.path)) ||
    PARENT_NAV_ITEMS[0];

  const handleNavChange = (id: string) => {
    const target = PARENT_NAV_ITEMS.find((item) => item.id === id);
    if (target) {
      navigate(target.path);
    }
  };

  return (
    <AppShell
      role="parent"
      navItems={PARENT_NAV_ITEMS}
      activeNavId={currentNav.id}
      onNavChange={handleNavChange}
      headerProps={{
        pageTitle: currentNav.label,
        brandTitle: 'Bilim Akademi',
        brandSubtitle: 'Veli Portalı',
        institutionName: user?.institutionName || 'Kadıköy Şubesi',
        userName: user?.name || 'Merve Kaya',
        userRole: user?.roleLabel || 'Veli',
        onProfileClick: () => {
          logout();
          navigate('/login', { replace: true });
        },
      }}
    >
      <Suspense fallback={<LoadingState message="Sayfa yükleniyor..." />}>
        <Routes>
          <Route path="/summary" element={<ParentSummaryView />} />
          <Route path="/messages" element={<ParentMessagesView />} />
          <Route path="*" element={<Navigate to="/parent/summary" replace />} />
        </Routes>
      </Suspense>
    </AppShell>
  );
};

// Institution Admin Portal Layout Component
const AdminPortalLayout: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  const currentNav =
    ADMIN_NAV_ITEMS.find((item) => location.pathname.startsWith(item.path)) ||
    ADMIN_NAV_ITEMS[0];

  const handleNavChange = (id: string) => {
    const target = ADMIN_NAV_ITEMS.find((item) => item.id === id);
    if (target) {
      navigate(target.path);
    }
  };

  return (
    <AppShell
      role="admin"
      navItems={ADMIN_NAV_ITEMS}
      activeNavId={currentNav.id}
      onNavChange={handleNavChange}
      headerProps={{
        pageTitle: currentNav.label,
        brandTitle: 'Bilim Akademi',
        brandSubtitle: 'Kurum Yönetim Portalı',
        institutionName: user?.institutionName || 'Merkez & 6 Şube',
        userName: user?.name || 'Ahmet Yılmaz',
        userRole: user?.roleLabel || 'Kurum Müdürü',
      }}
      sidebarFooter={
        <button
          type="button"
          onClick={() => {
            logout();
            navigate('/login', { replace: true });
          }}
          className="flex items-center gap-2 text-xs text-navy-400 hover:text-white px-3 py-2 rounded-lg hover:bg-navy-900 transition-colors w-full"
        >
          <LogOut className="h-4 w-4" />
          <span>Çıkış Yap</span>
        </button>
      }
    >
      <Suspense fallback={<LoadingState message="Sayfa yükleniyor..." />}>
        <Routes>
          <Route path="/overview" element={<InstitutionDashboardView />} />
          <Route path="/students" element={<StudentDirectoryView />} />
          <Route path="/coaches" element={<AdminCoachesView />} />
          <Route path="/teachers" element={<AdminTeachersView />} />
          <Route path="/classes" element={<AdminClassesView />} />
          <Route path="/exams" element={<AdminExamsView />} />
          <Route path="/questions" element={<QuestionBankView role="admin" />} />
          <Route path="/exams/:examId/questions" element={<ExamQuestionAssemblyView />} />
          <Route path="/exams/:examId/results/entry" element={<ExamResultEntryView portalRole="admin" />} />
          <Route path="/reports" element={<AdminReportsView />} />
          <Route path="*" element={<Navigate to="/admin/overview" replace />} />
        </Routes>
      </Suspense>
    </AppShell>

  );
};

const RootRedirect: React.FC = () => {
  const { user, isAuthenticated } = useAuth();

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" replace />;
  }

  return <Navigate to={getRoleDefaultRoute(user.role)} replace />;
};

export const App: React.FC = () => {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Public Auth Routes */}
          <Route path="/login" element={<LoginView />} />
          <Route path="/forgot-password" element={<ForgotPasswordView />} />
          <Route path="/reset-password" element={<ResetPasswordView />} />

          {/* Common Error Routes */}
          <Route path="/403" element={<ForbiddenView />} />
          <Route path="/404" element={<NotFoundView />} />

          {/* Protected Role Portals */}
          <Route
            path="/student/*"
            element={
              <ProtectedRoute allowedRoles={['student']}>
                <StudentPortalLayout />
              </ProtectedRoute>
            }
          />
          <Route
            path="/coach/*"
            element={
              <ProtectedRoute allowedRoles={['coach']}>
                <CoachPortalLayout />
              </ProtectedRoute>
            }
          />
          <Route
            path="/teacher/*"
            element={
              <ProtectedRoute allowedRoles={['teacher']}>
                <TeacherPortalLayout />
              </ProtectedRoute>
            }
          />
          <Route
            path="/parent/*"
            element={
              <ProtectedRoute allowedRoles={['parent']}>
                <ParentPortalLayout />
              </ProtectedRoute>
            }
          />
          <Route
            path="/admin/*"
            element={
              <ProtectedRoute allowedRoles={['admin']}>
                <AdminPortalLayout />
              </ProtectedRoute>
            }
          />

          {/* Root and Unknown Routes */}
          <Route path="/" element={<RootRedirect />} />
          <Route path="*" element={<NotFoundView />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
};
