import React from 'react';
import { Calendar, CheckCircle2, Clock, Sparkles } from 'lucide-react';
import { Badge } from '@/components/ui/Badge';
import { EmptyState } from '@/components/ui/EmptyState';
import { StudentPlanTaskReview } from '../types';

export interface StudentPlanTabProps {
  tasks: StudentPlanTaskReview[];
  studentName: string;
}

export const StudentPlanTab: React.FC<StudentPlanTabProps> = ({ tasks, studentName }) => {
  const completedCount = tasks.filter((t) => t.status === 'completed').length;
  const totalCount = tasks.length;
  const percentage = totalCount > 0 ? Math.round((completedCount / totalCount) * 100) : 0;

  return (
    <div className="space-y-4 select-none">
      {/* Plan Header Summary */}
      <div className="bg-navy-900 rounded-2xl p-4 sm:p-5 text-white flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-soft-sm">
        <div>
          <div className="flex items-center gap-2 text-xs text-navy-300">
            <Calendar className="h-3.5 w-3.5 text-primary-400" />
            <span>Aktif Çalışma Haftası</span>
          </div>
          <h3 className="text-base sm:text-lg font-serif font-semibold text-white mt-0.5">
            {studentName} — Haftalık Çalışma Programı
          </h3>
        </div>

        <div className="flex items-baseline gap-2 bg-navy-800 border border-navy-700 px-3 py-2 rounded-xl self-start sm:self-auto">
          <span className="text-xs text-navy-300">Uyum Oranı:</span>
          <span className="font-mono text-base font-bold text-success">%{percentage}</span>
          <span className="font-mono text-xs text-navy-400">({completedCount}/{totalCount})</span>
        </div>
      </div>

      {/* Task List */}
      {tasks.length > 0 ? (
        <div className="space-y-2.5">
          {tasks.map((task) => (
            <div
              key={task.id}
              className="bg-white rounded-2xl border border-neutral-100 p-3.5 sm:p-4 flex items-start justify-between gap-3 shadow-soft-sm"
            >
              <div className="space-y-1 min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-1.5">
                  <span className="text-[11px] font-semibold uppercase tracking-wider text-primary-700 bg-primary-50 px-2 py-0.5 rounded-md">
                    {task.dayName} • {task.subject}
                  </span>
                  {task.isCoachAssigned && (
                    <Badge variant="attention" size="sm">
                      <Sparkles className="h-2.5 w-2.5 mr-1" />
                      Koç Ödevi
                    </Badge>
                  )}
                  <Badge
                    variant={
                      task.status === 'completed'
                        ? 'success'
                        : task.status === 'in_progress'
                        ? 'info'
                        : 'secondary'
                    }
                    size="sm"
                  >
                    {task.status === 'completed'
                      ? 'Tamamlandı'
                      : task.status === 'in_progress'
                      ? 'Devam Ediyor'
                      : 'Bekliyor'}
                  </Badge>
                </div>

                <h4 className="text-sm font-semibold text-neutral-900 leading-tight">
                  {task.topic}
                </h4>

                {task.coachNote && (
                  <p className="text-xs text-attention-dark bg-attention-light border border-purple-100 rounded-lg px-2.5 py-1.5 mt-1.5 leading-relaxed">
                    💡 <strong>Koç Notu:</strong> {task.coachNote}
                  </p>
                )}

                <div className="flex items-center gap-2 text-xs text-neutral-400 pt-0.5">
                  <Clock className="h-3 w-3" />
                  <span>{task.durationMinutes} dakika planlandı</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<CheckCircle2 className="h-6 w-6 text-neutral-400" />}
          title="Henüz Plan Görevi Yok"
          description="Öğrenci için bu haftaya ait planlanan aktif çalışma kaydı bulunmuyor."
        />
      )}
    </div>
  );
};
