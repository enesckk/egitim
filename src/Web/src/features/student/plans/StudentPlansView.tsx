import React, { useState } from 'react';
import { Calendar, Sparkles, AlertCircle, RefreshCw } from 'lucide-react';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { PlanHeaderCard } from './components/PlanHeaderCard';
import { DaySelectorRow } from './components/DaySelectorRow';
import { PlanTaskCard } from './components/PlanTaskCard';
import { PlanSubjectProgressCard } from './components/PlanSubjectProgressCard';
import { TaskDetailModal } from './components/TaskDetailModal';
import { initialStudentPlanData } from './mockData';
import { PlanTaskItem, StudentPlanViewModel } from './types';

export interface StudentPlansViewProps {
  initialData?: StudentPlanViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const StudentPlansView: React.FC<StudentPlansViewProps> = ({
  initialData = initialStudentPlanData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const [selectedDayIndex, setSelectedDayIndex] = useState<number>(1); // Default to Tuesday (today)
  const [subjectFilter, setSubjectFilter] = useState<string>('');
  const [tasks, setTasks] = useState<PlanTaskItem[]>(initialData.tasks);
  const [selectedTask, setSelectedTask] = useState<PlanTaskItem | null>(null);
  const [isDetailOpen, setIsDetailOpen] = useState<boolean>(false);

  // Status toggle handler
  const handleToggleStatus = (id: string) => {
    setTasks((prev) =>
      prev.map((t) => {
        if (t.id === id) {
          if (t.status === 'completed') {
            return { ...t, status: 'pending', completedAt: undefined };
          } else if (t.status === 'pending') {
            return { ...t, status: 'in_progress' };
          } else {
            return {
              ...t,
              status: 'completed',
              completedAt: new Date().toLocaleTimeString('tr-TR', {
                hour: '2-digit',
                minute: '2-digit',
              }),
            };
          }
        }
        return t;
      })
    );
  };

  const handleOpenDetail = (task: PlanTaskItem) => {
    setSelectedTask(task);
    setIsDetailOpen(true);
  };

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
        <Skeleton className="h-44 w-full rounded-2xl" />
        <Skeleton className="h-16 w-full rounded-2xl" />
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
          <div className="lg:col-span-2 space-y-3">
            <Skeleton className="h-24 w-full rounded-2xl" />
            <Skeleton className="h-24 w-full rounded-2xl" />
            <Skeleton className="h-24 w-full rounded-2xl" />
          </div>
          <div>
            <Skeleton className="h-64 w-full rounded-2xl" />
          </div>
        </div>
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Plan Yüklenemedi">
          {errorMessage}
        </Alert>
        {onRetry && (
          <div className="text-center pt-2">
            <Button variant="primary" size="sm" onClick={onRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
              Tekrar Dene
            </Button>
          </div>
        )}
      </div>
    );
  }

  const filteredTasks = tasks.filter((t) => {
    const matchesDay = t.dayIndex === selectedDayIndex;
    const matchesSubject = !subjectFilter || t.subject === subjectFilter;
    return matchesDay && matchesSubject;
  });

  const selectedDay = initialData.days.find((d) => d.dayIndex === selectedDayIndex);

  return (
    <div className="max-w-5xl mx-auto select-none space-y-3.5 sm:space-y-4">
      {/* 1. Plan Overview Hero Card */}
      <PlanHeaderCard plan={initialData} />

      {/* 2. Day Selector */}
      <DaySelectorRow
        days={initialData.days}
        selectedDayIndex={selectedDayIndex}
        onSelectDay={setSelectedDayIndex}
      />

      {/* 3. Main Content & Sidebar */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-3.5 sm:gap-4">
        {/* Left Column: Daily Tasks */}
        <div className="lg:col-span-2 space-y-3">
          {/* Day Title & Filter Bar */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 bg-white rounded-2xl border border-neutral-100 p-3 sm:px-4">
            <div className="flex items-center gap-2">
              <Calendar className="h-4 w-4 text-primary-600" />
              <h3 className="text-sm font-semibold text-neutral-900">
                {selectedDay?.dayName} Günü Görevleri ({selectedDay?.dateStr})
              </h3>
            </div>

            <div className="flex items-center gap-2">
              <Select
                options={[
                  { value: 'Matematik', label: 'Matematik' },
                  { value: 'TYT Analizi', label: 'TYT Analizi' },
                  { value: 'Türkçe', label: 'Türkçe' },
                  { value: 'Felsefe', label: 'Felsefe' },
                  { value: 'Fizik', label: 'Fizik' },
                  { value: 'Kimya', label: 'Kimya' },
                  { value: 'Geometri', label: 'Geometri' },
                ]}
                value={subjectFilter}
                onChange={(e) => setSubjectFilter(e.target.value)}
                placeholder="Tüm Dersler"
                className="text-xs min-h-[34px] py-1 pl-2.5 pr-7"
              />
            </div>
          </div>

          {/* Task List or Empty State */}
          {filteredTasks.length > 0 ? (
            <div className="space-y-2.5">
              {filteredTasks.map((task) => (
                <PlanTaskCard
                  key={task.id}
                  task={task}
                  onToggleStatus={handleToggleStatus}
                  onViewDetails={handleOpenDetail}
                />
              ))}
            </div>
          ) : (
            <EmptyState
              icon={<Sparkles className="h-6 w-6 text-primary-400" />}
              title="Bu Gün İçin Görev Yok"
              description={
                subjectFilter
                  ? `Seçili filtreye (${subjectFilter}) uygun görev bulunamadı.`
                  : 'Bu gün için planlanmış herhangi bir çalışma görevi bulunmuyor.'
              }
              action={
                subjectFilter ? (
                  <Button size="sm" variant="secondary" onClick={() => setSubjectFilter('')}>
                    Filtreyi Temizle
                  </Button>
                ) : undefined
              }
            />
          )}
        </div>

        {/* Right Sidebar: Subject Progress Targets */}
        <div className="space-y-3.5 sm:space-y-4">
          <PlanSubjectProgressCard subjects={initialData.subjectProgress} />
        </div>
      </div>

      {/* Task Detail Modal */}
      <TaskDetailModal
        task={selectedTask}
        isOpen={isDetailOpen}
        onClose={() => setIsDetailOpen(false)}
        onToggleStatus={handleToggleStatus}
      />
    </div>
  );
};
