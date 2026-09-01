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
  UserCheck,
  Building2,
  LogOut,
} from 'lucide-react';
import { AppShell } from '@/components/layout/AppShell';
import { NavItem } from '@/components/layout/BottomNav';
import { AuthProvider, useAuth, ProtectedRoute, getRoleDefaultRoute } from '@/auth';

// Auth Features
import { LoginView, ForgotPasswordView, ResetPasswordView } from '@/features/auth';

// Platform Common Views
import { ForbiddenView } from '@/features/common/ForbiddenView';
import { NotFoundView } from '@/features/common/NotFoundView';

// Student Portal Features
import { StudentTodayView } from '@/features/student/today';
import { StudentPlansView } from '@/features/student/plans';
import { StudentExamsView } from '@/features/student/exams';
import { StudentMessagesView } from '@/features/student/messages';
import { StudentProfileView } from '@/features/student/profile';

// Coach Portal Features
import { CoachDashboardView } from '@/features/coach/dashboard';
import { CoachStudentsView, CoachStudentDetailView } from '@/features/coach/students';
import { CoachMeetingsView } from '@/features/coach/meetings';
import { CoachMessagesView } from '@/features/coach/messages';
import { CoachReportsView } from '@/features/coach/reports';

// Teacher Portal Features
import { TeacherDashboardView } from '@/features/teacher/dashboard';
import { TeacherClassesView, TeacherClassDetailView } from '@/features/teacher/classes';
import { TeacherStudentDetailView } from '@/features/teacher/students';
import { TeacherAcademicView } from '@/features/teacher/academic';
import { TeacherContentView } from '@/features/teacher/content';
import { TeacherMessagesView } from '@/features/teacher/messages';

// Parent Portal Features
import { ParentSummaryView } from '@/features/parent/summary';

// Admin Portal Features
import { InstitutionDashboardView } from '@/features/admin/overview';
import { StudentDirectoryView } from '@/features/admin/students';
import { AdminCoachesView } from '@/features/admin/coaches';
import { AdminTeachersView } from '@/features/admin/teachers';
import { AdminClassesView } from '@/features/admin/classes';
import { AdminReportsView } from '@/features/admin/reports';

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
    id: 'plans',
    path: '/student/plans',
    label: 'Planlar',
    icon: <CalendarDays className="h-5 w-5" />,
    badge: 3,
  },
  {
    id: 'exams',
    path: '/student/exams',
    label: 'Denemeler',
    icon: <FileText className="h-5 w-5" />,
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
    id: 'messages',
    path: '/teacher/messages',
    label: 'Mesajlar',
    icon: <MessageSquare className="h-5 w-5" />,
  },
];

// 4. Institution Admin Navigation Items (Desktop Dark Navy Sidebar & Mobile Drawer)
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
        brandTitle: 'Bilim Akademi',
        brandSubtitle: 'Öğrenci Portalı',
        institutionName: user?.institutionName || 'Merkez Şube',
        userName: user?.name || 'Ayşe Kaya',
        userRole: user?.roleLabel || '11. Sınıf • Sayısal',
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
      <Routes>
        <Route path="/today" element={<StudentTodayView />} />
        <Route path="/plans" element={<StudentPlansView />} />
        <Route path="/exams" element={<StudentExamsView />} />
        <Route path="/messages" element={<StudentMessagesView />} />
        <Route path="/profile" element={<StudentProfileView />} />
        <Route path="*" element={<Navigate to="/student/today" replace />} />
      </Routes>
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
      <Routes>
        <Route path="/today" element={<CoachDashboardView />} />
        <Route path="/students" element={<CoachStudentsView />} />
        <Route path="/students/:studentId" element={<CoachStudentDetailView />} />
        <Route path="/meetings" element={<CoachMeetingsView />} />
        <Route path="/messages" element={<CoachMessagesView />} />
        <Route path="/reports" element={<CoachReportsView />} />
        <Route path="*" element={<Navigate to="/coach/today" replace />} />
      </Routes>
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
      <Routes>
        <Route path="/today" element={<TeacherDashboardView />} />
        <Route path="/classes" element={<TeacherClassesView />} />
        <Route path="/classes/:classId" element={<TeacherClassDetailView />} />
        <Route path="/students/:studentId" element={<TeacherStudentDetailView />} />
        <Route path="/academic" element={<TeacherAcademicView />} />
        <Route path="/content" element={<TeacherContentView />} />
        <Route path="/messages" element={<TeacherMessagesView />} />
        <Route path="*" element={<Navigate to="/teacher/today" replace />} />
      </Routes>
    </AppShell>
  );
};

// Parent Portal Layout Component
const ParentPortalLayout: React.FC = () => {
  const navigate = useNavigate();
  const { user, logout } = useAuth();

  return (
    <AppShell
      role="parent"
      showSidebar={false}
      showBottomNav={false}
      headerProps={{
        pageTitle: 'Öğrenci Özeti',
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
      <Routes>
        <Route path="/summary" element={<ParentSummaryView />} />
        <Route path="*" element={<Navigate to="/parent/summary" replace />} />
      </Routes>
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
      <Routes>
        <Route path="/overview" element={<InstitutionDashboardView />} />
        <Route path="/students" element={<StudentDirectoryView />} />
        <Route path="/coaches" element={<AdminCoachesView />} />
        <Route path="/teachers" element={<AdminTeachersView />} />
        <Route path="/classes" element={<AdminClassesView />} />
        <Route path="/reports" element={<AdminReportsView />} />
        <Route path="*" element={<Navigate to="/admin/overview" replace />} />
      </Routes>
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
