import { UserRole } from '@/features/auth/types';

export const ROLE_DEFAULT_ROUTES: Record<UserRole, string> = {
  student: '/student/today',
  coach: '/coach/today',
  teacher: '/teacher/today',
  parent: '/parent/summary',
  admin: '/admin/overview',
};

export const getRoleDefaultRoute = (role: UserRole): string => {
  return ROLE_DEFAULT_ROUTES[role] || '/student/today';
};
