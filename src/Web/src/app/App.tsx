import React, { useState } from 'react';
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

export const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState<string>('today');

  const studentNavItems: NavItem[] = [
    {
      id: 'today',
      label: 'Bugün',
      icon: <LayoutDashboard className="h-5 w-5" />,
    },
    {
      id: 'plans',
      label: 'Planlar',
      icon: <CalendarDays className="h-5 w-5" />,
      badge: 3,
    },
    {
      id: 'exams',
      label: 'Denemeler',
      icon: <FileText className="h-5 w-5" />,
    },
    {
      id: 'messages',
      label: 'Mesajlar',
      icon: <MessageSquare className="h-5 w-5" />,
    },
    {
      id: 'profile',
      label: 'Profil',
      icon: <User className="h-5 w-5" />,
    },
  ];

  const currentNav = studentNavItems.find((n) => n.id === activeTab);

  return (
    <AppShell
      role="student"
      navItems={studentNavItems}
      activeNavId={activeTab}
      onNavChange={setActiveTab}
      headerProps={{
        pageTitle: currentNav?.label || 'Bugün',
        brandTitle: 'Bilim Akademi',
        brandSubtitle: 'Öğrenci Portalı',
        institutionName: 'Merkez Şube',
        userName: 'Ayşe Kaya',
        userRole: '11. Sınıf • Sayısal',
      }}
    >
      <StudentTodayView />
    </AppShell>
  );
};
