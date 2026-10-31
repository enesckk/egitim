import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import {
  ArrowLeft,
  MessageSquare,
  AlertCircle,
  PlusCircle,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  Clock,
} from 'lucide-react';
import { academicService, StudentSummaryDto } from '@/services/academicService';
import { coachTaskService, CoachTaskStatus } from '@/services/coachTaskService';
import { coachNoteService } from '@/services/coachNoteService';
import { examService } from '@/services/examService';
import { Button } from '@/components/ui/Button';
import { Tabs, TabItem } from '@/components/ui/Tabs';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { CoachStudentExamsTab } from '@/components/coach/CoachStudentExamsTab';
import { StudentOverviewTab } from './components/StudentOverviewTab';
import { CoachTasksTab } from './components/CoachTasksTab';
import { StudentPlanTab } from './components/StudentPlanTab';
import { CoachPrivateNotesTab } from './components/CoachPrivateNotesTab';
import {
  AssignedStudentDetail,
  CoachPrivateNote,
  StudentPlanTaskReview,
  StudentStatus,
  CoachStudentsViewModel,
} from './types';

export interface CoachStudentDetailViewProps {
  initialStudents?: CoachStudentsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

const DAY_NAMES_FULL_TR = ['Pazar', 'Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi'];

const STATUS_MAP: Record<
  StudentStatus,
  { label: string; icon: React.ReactNode; badgeClass: string }
> = {
  takipte: {
    label: 'Takipte',
    icon: <CheckCircle2 className="h-3.5 w-3.5 text-emerald-600" />,
    badgeClass: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  },
  dikkat: {
    label: 'Dikkat',
    icon: <AlertTriangle className="h-3.5 w-3.5 text-amber-600" />,
    badgeClass: 'bg-amber-50 text-amber-700 border-amber-200',
  },
  kritik: {
    label: 'Kritik',
    icon: <XCircle className="h-3.5 w-3.5 text-rose-600" />,
    badgeClass: 'bg-rose-50 text-rose-700 border-rose-200',
  },
  plansiz: {
    label: 'Aktif Plan Yok',
    icon: <Clock className="h-3.5 w-3.5 text-neutral-500" />,
    badgeClass: 'bg-neutral-100 text-neutral-600 border-neutral-200',
  },
};

export const CoachStudentDetailView: React.FC<CoachStudentDetailViewProps> = ({
  initialStudents,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const { studentId } = useParams<{ studentId: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const tabParam = searchParams.get('tab') || 'overview';
  const [activeTab, setActiveTab] = useState(tabParam);
  const [student, setStudent] = useState<AssignedStudentDetail | null>(null);
  const [isLoading, setIsLoading] = useState(propIsLoading || !initialStudents);
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);
  const [examCount, setExamCount] = useState<number | undefined>(undefined);

  // Sync tab with URL
  const handleTabChange = (newTab: string) => {
    setActiveTab(newTab);
    setSearchParams({ tab: newTab });
  };

  const fetchStudentDetail = useCallback(async () => {
    if (!studentId) return;

    if (initialStudents?.students) {
      const found = initialStudents.students.find((s) => s.id === studentId);
      if (found) {
        setStudent(found);
        setIsLoading(false);
        return;
      }
    }

    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      let baseStudentDto: StudentSummaryDto | null = null;
      let goalExamName = 'YKS (TYT/AYT)';

      try {
        const studentDetail = await academicService.getStudent360(studentId);
        if (studentDetail?.student) {
          baseStudentDto = studentDetail.student;
        }
      } catch {
        baseStudentDto = await academicService.getStudent(studentId);
      }

      if (!baseStudentDto || !baseStudentDto.id) {
        setStudent(null);
        setIsLoading(false);
        return;
      }

      const [activePlanRes, coachTasksRes, examsRes, notesRes] = await Promise.allSettled([
        academicService.getActiveStudyPlan(studentId),
        coachTaskService.getCoachTasks(studentId),
        examService.getStudentExamResults(studentId),
        coachNoteService.getPrivateNotes(studentId),
      ]);

      const activePlan = activePlanRes.status === 'fulfilled' ? activePlanRes.value : null;
      const coachTasks =
        coachTasksRes.status === 'fulfilled' && Array.isArray(coachTasksRes.value)
          ? coachTasksRes.value
          : [];
      const examsTotal =
        examsRes.status === 'fulfilled' && examsRes.value?.items
          ? examsRes.value.items.length
          : undefined;
      setExamCount(examsTotal);

      const privateNotes: CoachPrivateNote[] =
        notesRes.status === 'fulfilled' && Array.isArray(notesRes.value)
          ? notesRes.value.map((n) => ({
              id: n.id,
              category: n.category,
              content: n.content,
              createdAt: n.createdAt ? new Date(n.createdAt).toLocaleDateString('tr-TR') : 'Bugün',
              updatedAt: n.updatedAt ?? undefined,
            }))
          : [];

      const weeklyPlanTasks: StudentPlanTaskReview[] = [];
      let adherence: number | undefined = undefined;
      let weeklyHours = 0;
      let hasActivePlan = false;

      if (activePlan && activePlan.plan) {
        hasActivePlan = true;
        adherence = Math.round((activePlan.plan.taskProgressRate || 0) * 100);
        weeklyHours = Math.round(((activePlan.plan.completedTaskMinutes || 0) / 60) * 10) / 10;
        const startObj = activePlan.plan.startDate ? new Date(activePlan.plan.startDate) : new Date();

        if (Array.isArray(activePlan.tasks)) {
          for (const t of activePlan.tasks) {
            const dObj = t.plannedDate
              ? new Date(t.plannedDate)
              : new Date(startObj.getTime() + (t.dayIndex - 1) * 86400000);
            weeklyPlanTasks.push({
              id: t.id,
              dayName: DAY_NAMES_FULL_TR[dObj.getDay()] || `${t.dayIndex}. Gün`,
              subject: t.subjectName || (t.nodeType === 'QuestionStyle' ? 'Soru Stili' : 'Genel Çalışma'),
              topic: t.titleSnapshot || t.topicName || t.learningOutcomeName || 'Çalışma Görevi',
              durationMinutes: t.durationMinutes,
              status: t.isCompleted ? 'completed' : 'pending',
              isCoachAssigned: false,
            });
          }
        }
      }

      for (const ct of coachTasks.filter((c) => c.status !== CoachTaskStatus.Cancelled)) {
        const dObj = ct.dueDate ? new Date(ct.dueDate) : new Date(ct.createdAt);
        weeklyPlanTasks.push({
          id: ct.id,
          dayName: DAY_NAMES_FULL_TR[dObj.getDay()] || 'Haftalık',
          subject: ct.subjectName || 'Koç Görevi',
          topic: ct.title,
          durationMinutes: ct.durationMinutes || 30,
          status: ct.isCompleted || ct.status === CoachTaskStatus.Completed ? 'completed' : 'pending',
          isCoachAssigned: true,
          coachNote: ct.description || undefined,
        });
      }

      let status: StudentStatus = 'plansiz';
      if (hasActivePlan && typeof adherence === 'number') {
        if (adherence < 40) status = 'kritik';
        else if (adherence < 70) status = 'dikkat';
        else status = 'takipte';
      }

      const fullName =
        baseStudentDto.fullName ||
        `${baseStudentDto.firstName || ''} ${baseStudentDto.lastName || ''}`.trim() ||
        'Öğrenci';
      const initials =
        `${baseStudentDto.firstName?.[0] || ''}${baseStudentDto.lastName?.[0] || ''}`.toUpperCase() ||
        fullName
          .split(' ')
          .filter(Boolean)
          .map((p) => p[0])
          .join('')
          .toUpperCase()
          .slice(0, 2) ||
        'ÖĞ';

      setStudent((prev) => ({
        id: baseStudentDto.id,
        userId: baseStudentDto.userId || undefined,
        name: fullName,
        initials,
        grade: baseStudentDto.gradeLevel ? `${baseStudentDto.gradeLevel}. Sınıf` : '12. Sınıf',
        field: baseStudentDto.schoolName || 'Genel Alan',
        schoolName: baseStudentDto.schoolName || undefined,
        exam: goalExamName,
        status,
        hasActivePlan,
        planAdherence: adherence,
        weeklyStudyHours: weeklyHours,
        privateNotes: prev?.id === baseStudentDto.id && prev.privateNotes.length > 0 ? prev.privateNotes : privateNotes,
        weeklyPlanTasks,
        recentExams: [],
      }));
    } catch {
      setStudent(null);
    } finally {
      setIsLoading(false);
    }
  }, [initialStudents, studentId]);

  useEffect(() => {
    fetchStudentDetail();
  }, [fetchStudentDetail]);

  const handleAddPrivateNote = async (note: Omit<CoachPrivateNote, 'id' | 'createdAt'>) => {
    if (!studentId || !student) return;
    try {
      const created = await coachNoteService.createPrivateNote(studentId, {
        category: note.category,
        content: note.content,
      });

      const newNote: CoachPrivateNote = {
        id: created.id,
        category: created.category as CoachPrivateNote['category'],
        content: created.content,
        createdAt: 'Bugün',
      };

      setStudent((prev) => (prev ? { ...prev, privateNotes: [newNote, ...prev.privateNotes] } : null));
    } catch (err) {
      console.error('Failed to create coach private note:', err);
      throw err;
    }
  };

  const handleDeletePrivateNote = async (noteId: string) => {
    if (!student) return;
    try {
      await coachNoteService.deletePrivateNote(noteId);
      setStudent((prev) =>
        prev ? { ...prev, privateNotes: prev.privateNotes.filter((n) => n.id !== noteId) } : null
      );
    } catch (err) {
      console.error('Failed to delete coach private note:', err);
      throw err;
    }
  };

  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="coach-student-detail-loading">
        <Skeleton className="h-6 w-36 rounded-lg" />
        <Skeleton className="h-28 w-full rounded-3xl" />
        <Skeleton className="h-12 w-full rounded-2xl" />
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
          <Skeleton className="h-28 rounded-2xl" />
          <Skeleton className="h-28 rounded-2xl" />
          <Skeleton className="h-28 rounded-2xl" />
        </div>
      </div>
    );
  }

  if (errorMessage || !student) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4 select-none p-4" data-testid="coach-student-detail-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Öğrenci Detayı Yüklenemedi">
          {errorMessage || 'Öğrenci bulunamadı veya bu öğrenciye erişim yetkiniz bulunmamaktadır.'}
        </Alert>
        <div className="text-center pt-2">
          {onRetry ? (
            <Button
              variant="primary"
              size="sm"
              onClick={onRetry}
              leftIcon={<ArrowLeft className="h-4 w-4" />}
            >
              Tekrar Dene
            </Button>
          ) : (
            <Button
              variant="primary"
              size="sm"
              onClick={() => navigate('/coach/students')}
              leftIcon={<ArrowLeft className="h-4 w-4" />}
            >
              Öğrenci Listesine Dön
            </Button>
          )}
        </div>
      </div>
    );
  }

  const statusInfo = STATUS_MAP[student.status] || STATUS_MAP.plansiz;

  const detailTabs: TabItem[] = [
    { id: 'overview', label: 'Özet' },
    {
      id: 'tasks',
      label: 'Görevler',
      count: student.weeklyPlanTasks.filter((t) => t.isCoachAssigned).length || undefined,
    },
    {
      id: 'plan',
      label: 'Çalışma Planı',
      count: student.weeklyPlanTasks.filter((t) => !t.isCoachAssigned).length || undefined,
    },
    { id: 'exams', label: 'Denemeler', count: examCount },
    {
      id: 'notes',
      label: 'Özel Notlar',
      count: student.privateNotes.length || undefined,
    },
  ];

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="coach-student-detail-view">
      {/* Back Button */}
      <div>
        <button
          type="button"
          onClick={() => navigate('/coach/students')}
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-neutral-500 hover:text-primary-600 transition-colors py-1 px-2.5 rounded-xl hover:bg-neutral-100 cursor-pointer"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Atanmış Öğrencilerime Dön
        </button>
      </div>

      {/* 1. Student Identity Header Banner */}
      <div className="bg-[#14233E] rounded-3xl p-5 sm:p-7 text-white shadow-soft-sm relative overflow-hidden border border-navy-800">
        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div className="flex items-center gap-4 min-w-0">
            <div className="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl bg-white/10 text-white font-bold text-xl flex items-center justify-center shrink-0 border border-white/15 shadow-soft-xs">
              {student.initials}
            </div>

            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <h1 className="font-serif text-xl sm:text-2xl font-bold tracking-tight text-white leading-tight truncate">
                  {student.name}
                </h1>

                <span className="text-xs px-2.5 py-0.5 rounded-full bg-navy-800/80 text-blue-300 border border-navy-700 font-medium">
                  {student.grade}
                </span>

                <span
                  className={`inline-flex items-center gap-1 text-[11px] font-bold px-2.5 py-0.5 rounded-full border ${statusInfo.badgeClass}`}
                >
                  {statusInfo.icon}
                  {student.hasActivePlan && typeof student.planAdherence === 'number'
                    ? `%${student.planAdherence} Uyum`
                    : statusInfo.label}
                </span>
              </div>

              <p className="text-xs sm:text-sm text-navy-200 mt-1 truncate">
                {student.schoolName || student.field || 'Kayıtlı Öğrenci'} • {student.exam}
              </p>
            </div>
          </div>

          <div className="flex items-center gap-2.5 shrink-0 flex-wrap">
            <Button
              variant="outline"
              size="sm"
              onClick={() => handleTabChange('tasks')}
              leftIcon={<PlusCircle className="h-4 w-4" />}
              className="bg-white/10 hover:bg-white/15 text-white border-white/20 text-xs rounded-xl"
            >
              Görev Ata
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={() => navigate(`/coach/messages?studentId=${student.id}${student.userId ? `&userId=${student.userId}` : ''}`)}
              leftIcon={<MessageSquare className="h-4 w-4" />}
              className="bg-[#315FE8] hover:bg-blue-600 text-white text-xs rounded-xl shadow-soft-xs"
            >
              Mesaj Gönder
            </Button>
          </div>
        </div>
      </div>

      {/* 2. Scrollable Segmented Detail Tabs */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 px-4 pt-1 shadow-soft-xs overflow-x-auto scrollbar-none">
        <Tabs
          tabs={detailTabs}
          activeId={activeTab}
          onChange={handleTabChange}
        />
      </div>

      {/* 3. Tab Content Panes */}
      <div>
        {activeTab === 'overview' && <StudentOverviewTab student={student} />}
        {activeTab === 'tasks' && <CoachTasksTab studentId={student.id} studentName={student.name} />}
        {activeTab === 'plan' && (
          <StudentPlanTab tasks={student.weeklyPlanTasks} studentName={student.name} />
        )}
        {activeTab === 'exams' && (
          <CoachStudentExamsTab studentId={student.id} />
        )}
        {activeTab === 'notes' && (
          <CoachPrivateNotesTab
            notes={student.privateNotes}
            studentName={student.name}
            onAddNote={handleAddPrivateNote}
            onDeleteNote={handleDeletePrivateNote}
          />
        )}
      </div>
    </div>
  );
};

export default CoachStudentDetailView;
