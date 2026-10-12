import React, { useEffect, useState } from 'react';
import { Plus, CheckCircle2, XCircle, Clock, Calendar, Edit2, AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { Alert } from '@/components/ui/Alert';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { EmptyState } from '@/components/ui/EmptyState';
import { coachTaskService, CoachTaskDto, CoachTaskStatus, CreateCoachTaskPayload } from '@/services/coachTaskService';

export interface CoachTasksTabProps {
  studentId: string;
  studentName: string;
}

export const CoachTasksTab: React.FC<CoachTasksTabProps> = ({ studentId, studentName }) => {
  const [tasks, setTasks] = useState<CoachTaskDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Modal States
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [editingTask, setEditingTask] = useState<CoachTaskDto | null>(null);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Form State
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [durationMinutes, setDurationMinutes] = useState<number | ''>(30);
  const [dueDate, setDueDate] = useState('');

  const loadTasks = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await coachTaskService.getCoachTasks(studentId);
      setTasks(data);
    } catch (err: any) {
      setError(err?.response?.data?.detail || err.message || 'Görevler yüklenirken bir hata oluştu.');
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadTasks();
  }, [studentId]);

  const resetForm = () => {
    setTitle('');
    setDescription('');
    setDurationMinutes(30);
    setDueDate('');
    setEditingTask(null);
  };

  const handleOpenCreate = () => {
    resetForm();
    setIsCreateOpen(true);
  };

  const handleOpenEdit = (task: CoachTaskDto) => {
    setEditingTask(task);
    setTitle(task.title);
    setDescription(task.description || '');
    setDurationMinutes(task.durationMinutes || 30);
    setDueDate(task.dueDate ? task.dueDate.split('T')[0] : '');
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!title.trim()) return;

    try {
      setIsSubmitting(true);
      const payload: CreateCoachTaskPayload = {
        title: title.trim(),
        description: description.trim() || undefined,
        durationMinutes: typeof durationMinutes === 'number' ? durationMinutes : undefined,
        dueDate: dueDate ? new Date(dueDate).toISOString() : undefined,
      };

      if (editingTask) {
        await coachTaskService.updateCoachTask(studentId, editingTask.id, payload);
      } else {
        await coachTaskService.createCoachTask(studentId, payload);
      }

      setIsCreateOpen(false);
      setEditingTask(null);
      resetForm();
      await loadTasks();
    } catch (err: any) {
      alert(err?.response?.data?.detail || err.message || 'Görev kaydedilirken hata oluştu.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleCancelTask = async (taskId: string) => {
    if (!window.confirm('Bu görevi iptal etmek istediğinize emin misiniz?')) return;
    try {
      await coachTaskService.cancelCoachTask(studentId, taskId);
      await loadTasks();
    } catch (err: any) {
      alert(err?.response?.data?.detail || err.message || 'Görev iptal edilirken hata oluştu.');
    }
  };

  const activeTasks = tasks.filter((t) => t.status === CoachTaskStatus.Active);
  const completedTasks = tasks.filter((t) => t.status === CoachTaskStatus.Completed);

  return (
    <div className="space-y-4 select-none">
      {/* Header Bar */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-soft-sm">
        <div>
          <h3 className="text-base sm:text-lg font-serif font-semibold text-neutral-900">
            {studentName} — Koç Görevleri
          </h3>
          <p className="text-xs text-neutral-500 mt-0.5">
            Öğrenciye özel tanımlanan birebir takip görevleri ({activeTasks.length} Aktif, {completedTasks.length} Tamamlandı)
          </p>
        </div>

        <Button
          variant="primary"
          size="sm"
          onClick={handleOpenCreate}
          leftIcon={<Plus className="h-4 w-4" />}
        >
          Yeni Görev Ata
        </Button>
      </div>

      {/* Error Alert */}
      {error && (
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Hata">
          {error}
          <div className="mt-2">
            <Button variant="secondary" size="sm" onClick={loadTasks} leftIcon={<RefreshCw className="h-3.5 w-3.5" />}>
              Tekrar Dene
            </Button>
          </div>
        </Alert>
      )}

      {/* Task List */}
      {isLoading ? (
        <div className="py-8 text-center text-sm text-neutral-400">Görevler yükleniyor...</div>
      ) : tasks.length === 0 ? (
        <EmptyState
          icon={<Clock className="h-6 w-6 text-neutral-400" />}
          title="Henüz Atanmış Görev Yok"
          description="Öğrenci için henüz tanımlanmış bir koç görevi bulunmuyor."
        />
      ) : (
        <div className="space-y-3">
          {tasks.map((task) => (
            <div
              key={task.id}
              className="bg-white rounded-2xl border border-neutral-100 p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-soft-sm"
            >
              <div className="space-y-1.5 min-w-0 flex-1">
                <div className="flex flex-wrap items-center gap-2">
                  <Badge
                    variant={
                      task.status === CoachTaskStatus.Completed
                        ? 'success'
                        : task.status === CoachTaskStatus.Cancelled
                        ? 'secondary'
                        : 'info'
                    }
                    size="sm"
                  >
                    {task.status === CoachTaskStatus.Completed
                      ? 'Tamamlandı'
                      : task.status === CoachTaskStatus.Cancelled
                      ? 'İptal Edildi'
                      : 'Aktif'}
                  </Badge>

                  {task.subjectName && (
                    <span className="text-[11px] font-semibold text-primary-700 bg-primary-50 px-2 py-0.5 rounded-md">
                      {task.subjectName}
                    </span>
                  )}
                </div>

                <h4 className="text-sm font-semibold text-neutral-900 leading-tight">
                  {task.title}
                </h4>

                {task.description && (
                  <p className="text-xs text-neutral-600 leading-relaxed">
                    {task.description}
                  </p>
                )}

                <div className="flex flex-wrap items-center gap-3 text-xs text-neutral-400 pt-0.5">
                  {task.durationMinutes && (
                    <div className="flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      <span>{task.durationMinutes} dk</span>
                    </div>
                  )}

                  {task.dueDate && (
                    <div className="flex items-center gap-1">
                      <Calendar className="h-3 w-3" />
                      <span>Son Tarih: {new Date(task.dueDate).toLocaleDateString('tr-TR')}</span>
                    </div>
                  )}

                  {task.completedAt && (
                    <div className="flex items-center gap-1 text-success font-medium">
                      <CheckCircle2 className="h-3 w-3" />
                      <span>Tamamlanma: {new Date(task.completedAt).toLocaleDateString('tr-TR')}</span>
                    </div>
                  )}
                </div>
              </div>

              {/* Actions for active tasks */}
              {task.status === CoachTaskStatus.Active && (
                <div className="flex items-center gap-2 self-end sm:self-center">
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => handleOpenEdit(task)}
                    leftIcon={<Edit2 className="h-3.5 w-3.5" />}
                  >
                    Düzenle
                  </Button>
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => handleCancelTask(task.id)}
                    leftIcon={<XCircle className="h-3.5 w-3.5 text-danger" />}
                  >
                    İptal Et
                  </Button>
                </div>
              )}
            </div>
          ))}
        </div>
      )}

      {/* Create / Edit Modal */}
      <Modal
        isOpen={isCreateOpen || editingTask !== null}
        onClose={() => {
          setIsCreateOpen(false);
          setEditingTask(null);
        }}
        title={editingTask ? 'Koç Görevini Düzenle' : 'Yeni Koç Görevi Tanımla'}
      >
        <form onSubmit={handleSave} className="space-y-4 pt-2">
          <Input
            label="Görev Başlığı"
            placeholder="Örn: 20 soru paragraf çöz"
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            required
          />

          <div>
            <label className="block text-xs font-semibold text-neutral-700 mb-1">Açıklama / Koç Notu</label>
            <textarea
              className="w-full text-xs rounded-xl border border-neutral-200 p-3 focus:outline-none focus:ring-2 focus:ring-primary-500 min-h-[80px]"
              placeholder="Öğrencinin dikkat etmesi gereken detaylar..."
              value={description}
              onChange={(e) => setDescription(e.target.value)}
            />
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input
              label="Planlanan Süre (Dakika)"
              type="number"
              value={durationMinutes}
              onChange={(e) => setDurationMinutes(e.target.value ? Number(e.target.value) : '')}
            />

            <Input
              label="Son Tamamlama Tarihi"
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
            />
          </div>

          <div className="flex justify-end gap-2 pt-3 border-t border-neutral-100">
            <Button
              type="button"
              variant="secondary"
              size="sm"
              onClick={() => {
                setIsCreateOpen(false);
                setEditingTask(null);
              }}
            >
              İptal
            </Button>
            <Button type="submit" variant="primary" size="sm" isLoading={isSubmitting}>
              {editingTask ? 'Güncelle' : 'Görev Ata'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
