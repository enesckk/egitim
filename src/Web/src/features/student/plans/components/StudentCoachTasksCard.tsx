import React, { useEffect, useState } from 'react';
import { Sparkles, CheckCircle2, Clock, Calendar } from 'lucide-react';
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
    <div className="bg-white rounded-2xl border border-purple-200/80 p-5 shadow-xs space-y-3 select-none">
      {/* Header Badge */}
      <div className="flex items-center justify-between border-b border-purple-100 pb-3">
        <div className="flex items-center gap-2">
          <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-purple-50 text-purple-700 border border-purple-100">
            <Sparkles className="h-3.5 w-3.5" />
            Koçumdan Özel Görevler
          </span>
          <span className="text-xs text-neutral-400">
            {tasks.filter((t) => t.isCompleted).length}/{tasks.length} tamamlandı
          </span>
        </div>
      </div>

      {/* Task list */}
      <div className="space-y-2.5">
        {tasks.map((task) => {
          const isDone = task.isCompleted || task.status === CoachTaskStatus.Completed;

          return (
            <div
              key={task.id}
              className={cn(
                'p-3.5 rounded-xl border transition-all flex items-start gap-3',
                isDone
                  ? 'bg-neutral-50/60 border-neutral-200/60 opacity-75'
                  : 'bg-purple-50/20 border-purple-100 hover:border-purple-300'
              )}
            >
              <button
                type="button"
                onClick={() => handleToggleCompletion(task)}
                className="mt-0.5 flex-shrink-0 min-h-[32px] min-w-[32px] rounded-lg flex items-center justify-center hover:bg-white transition-colors"
                aria-label={isDone ? 'Tamamlandı' : 'Tamamla'}
              >
                {isDone ? (
                  <CheckCircle2 className="h-5 w-5 text-emerald-600" />
                ) : (
                  <div className="h-4 w-4 rounded-full border-2 border-purple-400 hover:border-purple-600 transition-colors" />
                )}
              </button>

              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-1">
                  <span className="text-xs font-bold text-neutral-800">{task.title}</span>
                  {task.subjectName && (
                    <span className="text-[10px] bg-white border border-neutral-200 px-2 py-0.5 rounded-md text-neutral-500 font-medium">
                      {task.subjectName}
                    </span>
                  )}
                </div>

                {task.description && (
                  <p className="text-xs text-neutral-600 leading-relaxed mt-0.5">{task.description}</p>
                )}

                <div className="flex items-center gap-3 mt-2 text-[11px] text-neutral-400">
                  {task.durationMinutes && (
                    <span className="flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {task.durationMinutes} dk
                    </span>
                  )}
                  {task.dueDate && (
                    <span className="flex items-center gap-1">
                      <Calendar className="h-3 w-3" />
                      {new Date(task.dueDate).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short' })}
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
