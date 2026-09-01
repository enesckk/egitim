import React from 'react';
import { Clock, BookOpen, UserCheck, CheckCircle2 } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { PlanTaskItem } from '../types';

export interface TaskDetailModalProps {
  task: PlanTaskItem | null;
  isOpen: boolean;
  onClose: () => void;
  onToggleStatus: (id: string) => void;
}

export const TaskDetailModal: React.FC<TaskDetailModalProps> = ({
  task,
  isOpen,
  onClose,
  onToggleStatus,
}) => {
  if (!task) return null;

  const isCompleted = task.status === 'completed';

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={task.subject}
      subtitle={task.topic}
      headerVariant="dark"
      footer={
        <div className="flex items-center justify-between w-full">
          <Button variant="secondary" size="sm" onClick={onClose}>
            Kapat
          </Button>
          <Button
            variant={isCompleted ? 'secondary' : 'primary'}
            size="sm"
            onClick={() => {
              onToggleStatus(task.id);
              onClose();
            }}
          >
            {isCompleted ? 'Tamamlanmadı Olarak İşaretle' : 'Görevi Tamamla'}
          </Button>
        </div>
      }
    >
      <div className="space-y-4 select-none">
        {/* Status & Badges */}
        <div className="flex flex-wrap items-center gap-2">
          <Badge
            variant={isCompleted ? 'success' : task.status === 'in_progress' ? 'info' : 'secondary'}
            size="md"
          >
            {isCompleted
              ? '✓ Tamamlandı'
              : task.status === 'in_progress'
              ? 'Devam Ediyor'
              : 'Bekliyor'}
          </Badge>
          {task.isCoachAssigned && (
            <Badge variant="attention" size="md">
              Koç Tarafından Atandı
            </Badge>
          )}
        </div>

        {/* Info Grid */}
        <div className="grid grid-cols-2 gap-3">
          <div className="bg-surface-alt rounded-xl p-3">
            <p className="text-xs text-neutral-400 flex items-center gap-1 mb-1">
              <Clock className="h-3.5 w-3.5 text-primary-500" />
              Süre
            </p>
            <p className="font-mono text-sm font-semibold text-neutral-800">
              {task.durationMinutes} dakika
            </p>
          </div>
          <div className="bg-surface-alt rounded-xl p-3">
            <p className="text-xs text-neutral-400 flex items-center gap-1 mb-1">
              <BookOpen className="h-3.5 w-3.5 text-success" />
              Hedef Soru
            </p>
            <p className="font-mono text-sm font-semibold text-neutral-800">
              {task.targetQuestionCount ? `${task.targetQuestionCount} soru` : 'Konu Tekrarı'}
            </p>
          </div>
        </div>

        {/* Coach Note */}
        {task.coachNote && (
          <div className="bg-attention-light border border-purple-200 rounded-xl p-3.5">
            <p className="text-xs font-semibold text-attention-dark flex items-center gap-1 mb-1">
              <UserCheck className="h-3.5 w-3.5" />
              Koç Hasan Yılmaz&apos;ın Notu
            </p>
            <p className="text-xs text-purple-900 leading-relaxed">
              {task.coachNote}
            </p>
          </div>
        )}

        {isCompleted && task.completedAt && (
          <div className="flex items-center gap-2 text-xs text-success bg-success-light border border-green-200 rounded-xl p-3">
            <CheckCircle2 className="h-4 w-4" />
            <span>Bu görev saat <strong>{task.completedAt}</strong>&apos;de başarıyla tamamlandı.</span>
          </div>
        )}
      </div>
    </Modal>
  );
};
