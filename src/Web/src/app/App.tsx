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
} from 'lucide-react';
import { AppShell } from '@/components/layout/AppShell';
import { NavItem } from '@/components/layout/BottomNav';

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

// Student Portal Layout Component
const StudentPortalLayout: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();

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
        institutionName: 'Merkez Şube',
        userName: 'Ayşe Kaya',
        userRole: '11. Sınıf • Sayısal',
        onProfileClick: () => navigate('/student/profile'),
      }}
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
        institutionName: 'Kadıköy Şubesi',
        userName: 'Hasan Yılmaz',
        userRole: 'Kıdemli YKS Koçu',
      }}
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
        institutionName: 'Kadıköy Şubesi',
        userName: 'Kemal Bey',
        userRole: 'Matematik & Fizik Zümresi',
      }}
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
  return (
    <AppShell
      role="parent"
      showSidebar={false}
      showBottomNav={false}
      headerProps={{
        pageTitle: 'Öğrenci Özeti',
        brandTitle: 'Bilim Akademi',
        brandSubtitle: 'Veli Portalı',
        institutionName: 'Kadıköy Şubesi',
        userName: 'Merve Kaya',
        userRole: 'Veli',
      }}
    >
      <Routes>
        <Route path="/summary" element={<ParentSummaryView />} />
        <Route path="*" element={<Navigate to="/parent/summary" replace />} />
      </Routes>
    </AppShell>
  );
};

export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/student/*" element={<StudentPortalLayout />} />
        <Route path="/coach/*" element={<CoachPortalLayout />} />
        <Route path="/teacher/*" element={<TeacherPortalLayout />} />
        <Route path="/parent/*" element={<ParentPortalLayout />} />
        <Route path="/" element={<Navigate to="/student/today" replace />} />
        <Route path="*" element={<Navigate to="/student/today" replace />} />
      </Routes>
    </BrowserRouter>
  );
};
