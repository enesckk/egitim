import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowLeft, MessageSquare, AlertCircle, RefreshCw, CheckCircle2, AlertTriangle, XCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { academicService } from '@/services/academicService';
import { coachTaskService, CoachTaskStatus } from '@/services/coachTaskService';
import { coachNoteService } from '@/services/coachNoteService';
import { examService } from '@/services/examService';
import { Button } from '@/components/ui/Button';
import { Tabs } from '@/components/ui/Tabs';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { CoachStudentExamsTab } from '@/components/coach/CoachStudentExamsTab';
import { StudentOverviewTab } from './components/StudentOverviewTab';
import { StudentPlanTab } from './components/StudentPlanTab';
import { CoachTasksTab } from './components/CoachTasksTab';
import { CoachPrivateNotesTab } from './components/CoachPrivateNotesTab';
import {
  AssignedStudentDetail,
  CoachPrivateNote,
  StudentPlanTaskReview,
  StudentStatus,
} from './types';

export interface CoachStudentDetailViewProps {
  initialStudents?: AssignedStudentDetail[];
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
    icon: <CheckCircle2 className="h-3.5 w-3.5 text-success" />,
    badgeClass: 'bg-success-light text-success-dark border-green-200',
  },
  dikkat: {
    label: 'Dikkat',
    icon: <AlertTriangle className="h-3.5 w-3.5 text-warning" />,
    badgeClass: 'bg-warning-light text-warning-dark border-amber-200',
  },
  kritik: {
    label: 'Kritik',
    icon: <XCircle className="h-3.5 w-3.5 text-danger" />,
    badgeClass: 'bg-danger-light text-danger-dark border-red-200',
  },
  plansiz: {
    label: 'Plan Yok',
    icon: <AlertTriangle className="h-3.5 w-3.5 text-neutral-400" />,
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
  const [searchParams] = useSearchParams();
  const initialTab = searchParams.get('tab') || 'overview';

  const [activeTab, setActiveTab] = useState(initialTab);
  const [student, setStudent] = useState<AssignedStudentDetail | null>(
    () => initialStudents?.find((s) => s.id === studentId) || null
  );
  const [examCount, setExamCount] = useState<number | undefined>(undefined);
  const [isLoading, setIsLoading] = useState<boolean>(
    propIsLoading || (!initialStudents && !propErrorMessage)
  );
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const fetchStudentDetail = useCallback(async () => {
    if (initialStudents) {
      setStudent(initialStudents.find((s) => s.id === studentId) || null);
      setIsLoading(false);
      return;
    }

    if (!studentId) {
      setStudent(null);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      let baseStudentDto = null;
      let goalExamName = 'TYT / AYT';

      try {
        const s360 = await academicService.getStudent360(studentId);
        baseStudentDto = s360.student;
        if (s360.examTypes && s360.examTypes.length > 0) {
          goalExamName = s360.examTypes.map((e) => e.name).join(' / ');
        } else if (s360.recentExam?.examType) {
          goalExamName = s360.recentExam.examType;
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
      let adherence = 100;
      let weeklyHours = 0;
      let hasPlanMetrics = false;

      if (activePlan && activePlan.tasks?.length > 0) {
        hasPlanMetrics = true;
        adherence = Math.round((activePlan.plan.taskProgressRate || 0) * 100);
        weeklyHours = Math.round(((activePlan.plan.completedTaskMinutes || 0) / 60) * 10) / 10;
        const startObj = activePlan.plan.startDate ? new Date(activePlan.plan.startDate) : new Date();

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
      } else if (coachTasks.length > 0) {
        const nonCancelled = coachTasks.filter((c) => c.status !== CoachTaskStatus.Cancelled);
        if (nonCancelled.length > 0) {
          hasPlanMetrics = true;
          const doneCount = nonCancelled.filter(
            (c) => c.isCompleted || c.status === CoachTaskStatus.Completed
          ).length;
          adherence = Math.round((doneCount / nonCancelled.length) * 100);
          const doneMins = nonCancelled
            .filter((c) => c.isCompleted || c.status === CoachTaskStatus.Completed)
            .reduce((sum, c) => sum + (c.durationMinutes || 30), 0);
          weeklyHours = Math.round((doneMins / 60) * 10) / 10;
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

      let status: StudentStatus = 'takipte';
      if (hasPlanMetrics) {
        if (adherence < 40) status = 'kritik';
        else if (adherence < 70) status = 'dikkat';
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
        name: fullName,
        initials,
        grade: baseStudentDto.gradeLevel ? `${baseStudentDto.gradeLevel}. Sınıf` : '12. Sınıf',
        field: baseStudentDto.schoolName || 'Genel Alan',
        exam: goalExamName,
        status,
        planAdherence: adherence,
        weeklyStudyHours: weeklyHours,
        lastActivity: baseStudentDto.createdAt
          ? new Date(baseStudentDto.createdAt).toLocaleDateString('tr-TR')
          : 'Aktif',
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

  const handleAddPrivateNote = async (newNoteData: Omit<CoachPrivateNote, 'id' | 'createdAt'>) => {
    if (!studentId) return;
    try {
      const created = await coachNoteService.createPrivateNote(studentId, {
        category: newNoteData.category,
        content: newNoteData.content,
      });

      const formatted: CoachPrivateNote = {
        id: created.id,
        category: created.category,
        content: created.content,
        createdAt: created.createdAt ? new Date(created.createdAt).toLocaleDateString('tr-TR') : 'Bugün',
        updatedAt: created.updatedAt ?? undefined,
      };

      setStudent((prev) => (prev ? { ...prev, privateNotes: [formatted, ...prev.privateNotes] } : prev));
    } catch (err) {
      console.error('Failed to create coach private note:', err);
      throw err;
    }
  };

  const handleDeletePrivateNote = async (noteId: string) => {
    try {
      await coachNoteService.deletePrivateNote(noteId);
      setStudent((prev) =>
        prev ? { ...prev, privateNotes: prev.privateNotes.filter((n) => n.id !== noteId) } : prev
      );
    } catch (err) {
      console.error('Failed to delete coach private note:', err);
      throw err;
    }
  };

  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="coach-student-detail-loading">
        <Skeleton className="h-10 w-32 rounded-xl" />
        <Skeleton className="h-32 w-full rounded-2xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (activeError || !student) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4 select-none" data-testid="coach-student-detail-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Öğrenci Bulunamadı">
          {activeError || 'İstenen öğrenci kaydına ulaşılamadı veya yetkiniz bulunmuyor.'}
        </Alert>
        <div className="flex justify-center gap-2 pt-2">
          {onRetry && (
            <Button variant="secondary" size="sm" onClick={onRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
              Tekrar Dene
            </Button>
          )}
          <Button variant="primary" size="sm" onClick={() => navigate('/coach/students')} leftIcon={<ArrowLeft className="h-4 w-4" />}>
            Öğrenci Listesine Dön
          </Button>
        </div>
      </div>
    );
  }

  const statusInfo = STATUS_MAP[student.status];

  const detailTabs = [
    { id: 'overview', label: 'Özet', count: undefined },
    { id: 'tasks', label: 'Görevler', count: undefined },
    { id: 'plan', label: 'Plan İnceleme', count: student.weeklyPlanTasks.length },
    { id: 'exams', label: 'Denemeler', count: examCount ?? student.recentExams.length },
    { id: 'notes', label: '🔒 Koça Özel Notlar', count: student.privateNotes.length },
  ];

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="coach-student-detail-view">
      {/* Back Button */}
      <div>
        <button
          type="button"
          onClick={() => navigate('/coach/students')}
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-neutral-500 hover:text-primary-600 transition-colors py-1 px-2 rounded-lg hover:bg-neutral-100"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Öğrenci Listesine Dön
        </button>
      </div>

      {/* 1. Student Identity Header Card */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-6 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-soft-sm">
        <div className="flex items-center gap-4 min-w-0">
          <div className="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl bg-navy-900 text-white font-semibold text-xl flex items-center justify-center flex-shrink-0">
            {student.initials}
          </div>
          <div className="min-w-0">
            <div className="flex flex-wrap items-center gap-2">
              <h1 className="font-serif text-xl sm:text-2xl font-bold text-neutral-900 leading-tight truncate">
                {student.name}
              </h1>
              <span
                className={cn(
                  'inline-flex items-center gap-1 text-[10px] font-semibold uppercase px-2 py-0.5 rounded-full border',
                  statusInfo.badgeClass
                )}
              >
                {statusInfo.icon}
                {statusInfo.label}
              </span>
            </div>
            <p className="text-xs sm:text-sm text-neutral-500 truncate mt-0.5">
              {student.grade} • {student.field} ({student.exam}) — Son Etkinlik: {student.lastActivity}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2 self-start sm:self-center">
          <Button
            variant="secondary"
            size="sm"
            onClick={() => navigate(`/coach/messages?studentId=${student.id}`)}
            leftIcon={<MessageSquare className="h-3.5 w-3.5" />}
          >
            Mesaj Gönder
          </Button>
        </div>
      </div>

      {/* 2. Detail Tabs */}
      <div className="bg-white rounded-2xl border border-neutral-100 px-4 pt-1 shadow-soft-sm">
        <Tabs
          tabs={detailTabs}
          activeId={activeTab}
          onChange={setActiveTab}
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
