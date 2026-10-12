import React, { useEffect, useState } from 'react';
import { Sparkles, CheckCircle2, Clock, Calendar } from 'lucide-react';
import { Badge } from '@/components/ui/Badge';
import { coachTaskService, CoachTaskDto, CoachTaskStatus } from '@/services/coachTaskService';
import { academicService } from '@/services/academicService';
import { cn } from '@/lib/utils';

export interface StudentCoachTasksCardProps {
  studentId?: string;
}

export const StudentCoachTasksCard: React.FC<StudentCoachTasksCardProps> = ({ studentId: propStudentId }) => {
  const [tasks, setTasks] = useState<CoachTaskDto[]>([]);
  const [activeStudentId, setActiveStudentId] = useState<string | null>(propStudentId || null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    let isMounted = true;

    const init = async () => {
      let id = propStudentId;
      if (!id) {
        id = (await academicService.getCurrentStudentId()) || undefined;
      }

      if (isMounted) {
        setActiveStudentId(id || null);
        if (id) {
          try {
            setIsLoading(true);
            const data = await coachTaskService.getCoachTasks(id);
            if (isMounted) setTasks(data);
          } catch {
            // Ignore error if student has no tasks or not assigned
          } finally {
            if (isMounted) setIsLoading(false);
          }
        } else {
          setIsLoading(false);
        }
      }
    };

    init();

    return () => {
      isMounted = false;
    };
  }, [propStudentId]);

  const loadTasks = async () => {
    if (!activeStudentId) return;
    try {
      setIsLoading(true);
      const data = await coachTaskService.getCoachTasks(activeStudentId);
      setTasks(data);
    } catch {
      // Ignore
    } finally {
      setIsLoading(false);
    }
  };

  const handleToggleCompletion = async (task: CoachTaskDto) => {
    if (!activeStudentId || task.status === CoachTaskStatus.Cancelled) return;

    const newCompleted = !task.isCompleted;
    try {
      await coachTaskService.toggleTaskCompletion(activeStudentId, task.id, newCompleted);
      await loadTasks();
    } catch (err: any) {
      alert(err?.response?.data?.detail || err.message || 'Görev durumu güncellenemedi.');
    }
  };

  if (isLoading || tasks.length === 0) {
    return null;
  }

  return (
    <div className="bg-white rounded-2xl border border-attention/30 p-4 shadow-soft-sm space-y-3 select-none">
      {/* Header Badge */}
      <div className="flex items-center justify-between border-b border-purple-50 pb-2.5">
        <div className="flex items-center gap-2">
          <Badge variant="attention" size="md">
            <Sparkles className="h-3.5 w-3.5 mr-1" />
            Koç Görevleri
          </Badge>
          <span className="text-xs text-neutral-500 font-medium">
            Koçunuz tarafından özel atanan birebir takip görevleri
          </span>
        </div>
        <span className="font-mono text-xs text-purple-700 font-bold">
          {tasks.filter((t) => t.isCompleted).length}/{tasks.length}
        </span>
      </div>

      {/* List */}
      <div className="space-y-2">
        {tasks.map((task) => {
          const isCompleted = task.status === CoachTaskStatus.Completed;
          const isCancelled = task.status === CoachTaskStatus.Cancelled;

          return (
            <div
              key={task.id}
              className={cn(
                'rounded-xl border p-3 flex items-start justify-between gap-3 transition-colors',
                isCompleted
                  ? 'bg-neutral-50/60 border-neutral-100 opacity-75'
                  : isCancelled
                  ? 'bg-neutral-50 border-neutral-200 opacity-50'
                  : 'bg-purple-50/20 border-purple-100 hover:border-purple-200'
              )}
            >
              {/* Checkbox Toggle */}
              <button
                type="button"
                disabled={isCancelled}
                onClick={() => handleToggleCompletion(task)}
                className="mt-0.5 flex-shrink-0 flex items-center justify-center min-h-[28px] min-w-[28px] rounded-lg hover:bg-purple-100 transition-colors"
                aria-label={isCompleted ? 'Tamamlandı olarak işaretli' : 'Görevi tamamla'}
              >
                {isCompleted ? (
                  <CheckCircle2 className="h-5 w-5 text-success stroke-[2.2]" />
                ) : (
                  <span className="w-4 h-4 rounded-md border-2 border-purple-300 bg-white" />
                )}
              </button>

              {/* Task Details */}
              <div className="flex-1 min-w-0">
                <div className="flex flex-wrap items-center gap-1.5 mb-0.5">
                  {task.subjectName && (
                    <span className="text-[10px] font-semibold uppercase tracking-wider text-purple-800 bg-purple-100 px-1.5 py-0.5 rounded">
                      {task.subjectName}
                    </span>
                  )}
                  {isCancelled && (
                    <Badge variant="secondary" size="sm">İptal Edildi</Badge>
                  )}
                </div>

                <h4
                  className={cn(
                    'text-xs sm:text-sm font-semibold text-neutral-900 leading-tight',
                    isCompleted && 'line-through text-neutral-500'
                  )}
                >
                  {task.title}
                </h4>

                {task.description && (
                  <p className="text-xs text-neutral-600 mt-1 leading-relaxed">
                    💡 {task.description}
                  </p>
                )}

                <div className="flex items-center gap-3 text-[11px] text-neutral-400 mt-1.5">
                  {task.durationMinutes && (
                    <span className="flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {task.durationMinutes} dk
                    </span>
                  )}
                  {task.dueDate && (
                    <span className="flex items-center gap-1">
                      <Calendar className="h-3 w-3" />
                      Son Tarih: {new Date(task.dueDate).toLocaleDateString('tr-TR')}
                    </span>
                  )}
                </div>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};
