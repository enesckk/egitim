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
  FileText,
  MessageSquare,
  User,
} from 'lucide-react';
import { AppShell } from '@/components/layout/AppShell';
import { NavItem } from '@/components/layout/BottomNav';
import { StudentTodayView } from '@/features/student/today';
import { StudentPlansView } from '@/features/student/plans';
import { StudentExamsView } from '@/features/student/exams';
import { StudentMessagesView } from '@/features/student/messages';
import { StudentProfileView } from '@/features/student/profile';

interface StudentNavItem extends NavItem {
  path: string;
}

const STUDENT_NAV_ITEMS: StudentNavItem[] = [
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

const StudentPortalLayout: React.FC = () => {
  const location = useLocation();
  const navigate = useNavigate();

  // Find active nav item based on URL path
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

export const App: React.FC = () => {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/student/*" element={<StudentPortalLayout />} />
        <Route path="/" element={<Navigate to="/student/today" replace />} />
        <Route path="*" element={<Navigate to="/student/today" replace />} />
      </Routes>
    </BrowserRouter>
  );
};
