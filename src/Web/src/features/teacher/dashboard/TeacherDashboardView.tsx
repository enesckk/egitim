import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  GraduationCap,
  Users,
  AlertCircle,
  RefreshCw,
  FolderOpen,
  HelpCircle,
  MessageSquare,
  ChevronRight,
  TrendingUp,
  Award,
  Layers,
  ArrowRight,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { AcademicGapsCard } from './components/AcademicGapsCard';
import { TeacherDashboardViewModel } from './types';
import { teacherService } from '@/services/teacherService';
import { ClassroomDto } from '@/services/adminService';

export interface TeacherDashboardViewProps {
  initialData?: TeacherDashboardViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherDashboardView: React.FC<TeacherDashboardViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();
  const [data, setData] = useState<TeacherDashboardViewModel | undefined>(initialData);
  const [classrooms, setClassrooms] = useState<ClassroomDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const fetchDashboard = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const [dashboard, clsList] = await Promise.all([
        teacherService.getDashboard(),
        teacherService.listClassrooms().catch(() => [] as ClassroomDto[]),
      ]);
      setData(dashboard);
      setClassrooms(clsList || []);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Öğretmen paneli yüklenemedi.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [initialData]);

  useEffect(() => {
    if (initialData) {
      setData(initialData);
      setIsLoading(false);
    } else {
      fetchDashboard();
    }
  }, [initialData, fetchDashboard]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchDashboard();
    }
  };

  const activeData = data || initialData;
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none px-4 sm:px-0" data-testid="teacher-dashboard-loading">
        <Skeleton className="h-32 w-full rounded-3xl" />
        <Skeleton className="h-24 w-full rounded-2xl" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <Skeleton className="h-64 lg:col-span-2 rounded-2xl" />
          <Skeleton className="h-64 rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError || !activeData) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4 px-4 sm:px-0" data-testid="teacher-dashboard-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Genel Bakış Yüklenemedi">
          {activeError || 'Öğretmen paneli verisi bulunamadı.'}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={handleRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  const todayStr = new Date().toLocaleDateString('tr-TR', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  });

  return (
    <div className="max-w-6xl mx-auto select-none space-y-5 sm:space-y-6 pb-20 sm:pb-12 px-4 sm:px-0" data-testid="teacher-dashboard-view">
      {/* 1. Compact Teacher Identity Hero Banner */}
      <div className="relative overflow-hidden bg-gradient-to-br from-[#14233E] via-[#1a2e51] to-[#0f1b30] text-white rounded-3xl p-5 sm:p-6 lg:p-7 shadow-lg border border-neutral-800/40">
        <div className="absolute -right-8 -top-8 w-44 h-44 bg-[#315FE8]/20 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -left-8 -bottom-8 w-44 h-44 bg-blue-500/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-4 sm:gap-5">
          <div className="space-y-2 min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <span className="text-[11px] font-semibold text-blue-200 bg-[#315FE8]/25 border border-blue-400/30 px-2.5 py-0.5 rounded-full inline-flex items-center gap-1">
                <Award className="h-3 w-3 text-blue-300" />
                Öğretmen Akademik Kontrol Merkezi
              </span>
              <span className="text-xs text-neutral-300 capitalize">{todayStr}</span>
            </div>

            <h1 className="font-serif text-2xl sm:text-3xl font-bold tracking-tight text-white leading-tight truncate">
              Günaydın, {activeData.teacherName}
            </h1>

            <div className="text-xs sm:text-sm text-neutral-300 flex items-center gap-1.5 flex-wrap">
              <span className="font-medium text-blue-100">
                {activeData.assignedSubjects.length > 0 ? activeData.assignedSubjects.join(' · ') : 'Branş Dersi'}
              </span>
              <span className="text-neutral-500">•</span>
              <span>{activeData.totalAssignedClasses} Atanmış Şube</span>
              <span className="text-neutral-500">•</span>
              <span>{activeData.totalAssignedStudents} Öğrenci</span>
            </div>
          </div>

          {/* Key Stat Badges - Responsive flex on tablet and mobile */}
          <div className="grid grid-cols-2 sm:flex items-center gap-2.5 sm:gap-3 pt-3 md:pt-0 border-t md:border-t-0 border-white/10 flex-shrink-0">
            <div className="bg-white/10 backdrop-blur-md border border-white/15 px-3.5 sm:px-4 py-2.5 sm:py-3 rounded-2xl text-center min-w-[88px] sm:min-w-[100px]">
              <span className="text-[10px] uppercase tracking-wider text-neutral-300 block font-medium">Şubelerim</span>
              <span className="font-mono text-lg sm:text-2xl font-extrabold text-white">{activeData.totalAssignedClasses}</span>
            </div>
            <div className="bg-white/10 backdrop-blur-md border border-white/15 px-3.5 sm:px-4 py-2.5 sm:py-3 rounded-2xl text-center min-w-[88px] sm:min-w-[100px]">
              <span className="text-[10px] uppercase tracking-wider text-neutral-300 block font-medium">Öğrenci</span>
              <span className="font-mono text-lg sm:text-2xl font-extrabold text-white">{activeData.totalAssignedStudents}</span>
            </div>
          </div>
        </div>
      </div>

      {/* 2. Assigned Classrooms Quick-Access Chips/Cards */}
      <div className="bg-white rounded-2xl border border-neutral-200/70 p-4 sm:p-5 shadow-xs space-y-3">
        <div className="flex items-center justify-between border-b border-neutral-100 pb-2.5">
          <div className="flex items-center gap-2">
            <GraduationCap className="h-4 w-4 text-[#315FE8]" />
            <h2 className="text-sm font-bold text-[#14233E]">Atanmış Şubelerim</h2>
          </div>
          <button
            type="button"
            onClick={() => navigate('/teacher/classes')}
            className="text-xs font-semibold text-[#315FE8] hover:underline flex items-center gap-1"
          >
            Tüm Sınıflar <ChevronRight className="h-3.5 w-3.5" />
          </button>
        </div>

        {classrooms.length === 0 ? (
          <div className="text-xs text-neutral-400 py-3 text-center">
            Atanmış sınıf bulunamadı veya henüz şube listesi yüklenmedi.
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-3">
            {classrooms.map((cls) => (
              <div
                key={cls.id}
                onClick={() => navigate(`/teacher/classes/${cls.id}`)}
                className="p-3.5 rounded-xl border border-neutral-200/60 bg-[#F8F7F3]/60 hover:bg-white hover:border-[#315FE8]/50 hover:shadow-xs transition-all cursor-pointer group flex items-center justify-between"
                data-testid={`quick-class-card-${cls.id}`}
              >
                <div className="min-w-0">
                  <p className="text-sm font-bold text-[#14233E] group-hover:text-[#315FE8] transition-colors truncate">
                    {cls.name}
                  </p>
                  <p className="text-[11px] text-neutral-500 mt-0.5 truncate">
                    {cls.gradeLevel ? `${cls.gradeLevel}. Sınıf` : '12. Sınıf'} • {cls.studentCount || 0} Öğrenci
                  </p>
                </div>
                <ArrowRight className="h-4 w-4 text-neutral-300 group-hover:text-[#315FE8] group-hover:translate-x-0.5 transition-all flex-shrink-0" />
              </div>
            ))}
          </div>
        )}
      </div>

      {/* 3. Main Dashboard Grid: Attention Required Students & Quick Actions */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-5 sm:gap-6">
        {/* Left 2 Columns: "Dikkat Gerektiren Öğrenciler" Grouped Cards */}
        <div className="lg:col-span-2 space-y-4">
          <AcademicGapsCard
            gaps={activeData.academicGaps}
            onSelectStudent={(studentId) => navigate(`/teacher/students/${studentId}`)}
          />
        </div>

        {/* Right 1 Column: Compact Quick Actions & Scope Notice */}
        <div className="space-y-4">
          {/* Quick Actions Panel */}
          <div className="bg-white rounded-2xl border border-neutral-200/70 p-4 sm:p-5 shadow-xs space-y-3">
            <h3 className="text-sm font-bold text-[#14233E] flex items-center gap-2">
              <Layers className="h-4 w-4 text-[#315FE8]" />
              Öğretmen Hızlı İşlemler
            </h3>

            <div className="divide-y divide-neutral-100 text-xs">
              <button
                type="button"
                onClick={() => navigate('/teacher/classes')}
                className="w-full flex items-center justify-between py-3 hover:text-[#315FE8] text-neutral-700 transition-colors group text-left"
              >
                <div className="flex items-center gap-2.5">
                  <div className="p-1.5 rounded-lg bg-blue-50 text-[#315FE8]">
                    <Users className="h-3.5 w-3.5" />
                  </div>
                  <span className="font-semibold">Sınıflarım & Öğrenci Listesi</span>
                </div>
                <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-[#315FE8] transition-colors" />
              </button>

              <button
                type="button"
                onClick={() => navigate('/teacher/academic')}
                className="w-full flex items-center justify-between py-3 hover:text-[#315FE8] text-neutral-700 transition-colors group text-left"
              >
                <div className="flex items-center gap-2.5">
                  <div className="p-1.5 rounded-lg bg-emerald-50 text-emerald-600">
                    <TrendingUp className="h-3.5 w-3.5" />
                  </div>
                  <span className="font-semibold">Akademik Takip & Analiz</span>
                </div>
                <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-[#315FE8] transition-colors" />
              </button>

              <button
                type="button"
                onClick={() => navigate('/teacher/questions')}
                className="w-full flex items-center justify-between py-3 hover:text-[#315FE8] text-neutral-700 transition-colors group text-left"
              >
                <div className="flex items-center gap-2.5">
                  <div className="p-1.5 rounded-lg bg-indigo-50 text-indigo-600">
                    <HelpCircle className="h-3.5 w-3.5" />
                  </div>
                  <span className="font-semibold">Soru Bankası & Test Hazırla</span>
                </div>
                <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-[#315FE8] transition-colors" />
              </button>

              <button
                type="button"
                onClick={() => navigate('/teacher/content')}
                className="w-full flex items-center justify-between py-3 hover:text-[#315FE8] text-neutral-700 transition-colors group text-left"
              >
                <div className="flex items-center gap-2.5">
                  <div className="p-1.5 rounded-lg bg-amber-50 text-amber-600">
                    <FolderOpen className="h-3.5 w-3.5" />
                  </div>
                  <span className="font-semibold">Ders Materyali Paylaşımı</span>
                </div>
                <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-[#315FE8] transition-colors" />
              </button>

              <button
                type="button"
                onClick={() => navigate('/teacher/messages')}
                className="w-full flex items-center justify-between py-3 hover:text-[#315FE8] text-neutral-700 transition-colors group text-left"
              >
                <div className="flex items-center gap-2.5">
                  <div className="p-1.5 rounded-lg bg-teal-50 text-teal-600">
                    <MessageSquare className="h-3.5 w-3.5" />
                  </div>
                  <span className="font-semibold">Mesajlar & İletişim</span>
                </div>
                <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-[#315FE8] transition-colors" />
              </button>
            </div>
          </div>

          {/* Scope & Authorization Guard Notice */}
          <div className="p-3.5 rounded-2xl bg-[#F8F7F3] border border-neutral-200/70 text-[11px] text-neutral-500 leading-relaxed space-y-1">
            <p className="font-semibold text-[#14233E]">Yetki ve Kapsam İlkesi</p>
            <p>
              Bu panelde yalnızca atanmış olduğunuz şubeler ve branşınıza kayıtlı öğrencilerin akademik analizleri listelenir. Diğer şube ve gizli koçluk kayıtlarına erişim engellidir.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};
