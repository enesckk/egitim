import React, { useState } from 'react';
import { useParams, useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowLeft, MessageSquare, AlertCircle, RefreshCw, CheckCircle2, AlertTriangle, XCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Tabs } from '@/components/ui/Tabs';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { StudentOverviewTab } from './components/StudentOverviewTab';
import { StudentPlanTab } from './components/StudentPlanTab';
import { StudentExamsTab } from './components/StudentExamsTab';
import { CoachPrivateNotesTab } from './components/CoachPrivateNotesTab';
import { initialCoachStudentsData } from './mockData';
import { AssignedStudentDetail, CoachPrivateNote, StudentStatus } from './types';

export interface CoachStudentDetailViewProps {
  initialStudents?: AssignedStudentDetail[];
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

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
};

export const CoachStudentDetailView: React.FC<CoachStudentDetailViewProps> = ({
  initialStudents = initialCoachStudentsData.students,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const { studentId } = useParams<{ studentId: string }>();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const initialTab = searchParams.get('tab') || 'overview';

  const [activeTab, setActiveTab] = useState(initialTab);
  const [students, setStudents] = useState<AssignedStudentDetail[]>(initialStudents);

  const student = students.find((s) => s.id === studentId) || students[0];

  const handleAddPrivateNote = (newNoteData: Omit<CoachPrivateNote, 'id' | 'createdAt'>) => {
    const newNote: CoachPrivateNote = {
      id: `pnote-${Date.now()}`,
      createdAt: 'Bugün',
      category: newNoteData.category,
      content: newNoteData.content,
    };

    setStudents((prev) =>
      prev.map((s) => {
        if (s.id === student?.id) {
          return {
            ...s,
            privateNotes: [newNote, ...s.privateNotes],
          };
        }
        return s;
      })
    );
  };

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
        <Skeleton className="h-10 w-32 rounded-xl" />
        <Skeleton className="h-32 w-full rounded-2xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (errorMessage || !student) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4 select-none">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Öğrenci Bulunamadı">
          {errorMessage || 'İstenen öğrenci kaydına ulaşılamadı veya yetkiniz bulunmuyor.'}
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
    { id: 'plan', label: 'Plan İnceleme', count: student.weeklyPlanTasks.length },
    { id: 'exams', label: 'Denemeler', count: student.recentExams.length },
    { id: 'notes', label: '🔒 Koça Özel Notlar', count: student.privateNotes.length },
  ];

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6">
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
        {activeTab === 'plan' && (
          <StudentPlanTab tasks={student.weeklyPlanTasks} studentName={student.name} />
        )}
        {activeTab === 'exams' && (
          <StudentExamsTab exams={student.recentExams} studentName={student.name} />
        )}
        {activeTab === 'notes' && (
          <CoachPrivateNotesTab
            notes={student.privateNotes}
            studentName={student.name}
            onAddNote={handleAddPrivateNote}
          />
        )}
      </div>
    </div>
  );
};
