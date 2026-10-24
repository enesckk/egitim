import React, { useState, useEffect } from 'react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { academicService, StudentSummaryDto } from '@/services/academicService';
import { coachMeetingService } from '@/services/coachMeetingService';
import { CoachMeetingItem } from '../types';

export interface NewMeetingModalProps {
  isOpen: boolean;
  onClose: () => void;
  onAddMeeting: (meeting: CoachMeetingItem) => void;
}

export const NewMeetingModal: React.FC<NewMeetingModalProps> = ({
  isOpen,
  onClose,
  onAddMeeting,
}) => {
  const [students, setStudents] = useState<StudentSummaryDto[]>([]);
  const [selectedStudentId, setSelectedStudentId] = useState('');
  const [dateStr, setDateStr] = useState('');
  const [timeStr, setTimeStr] = useState('14:00');
  const [durationMinutes, setDurationMinutes] = useState(30);
  const [type, setType] = useState('Haftalık Takip & Analiz');
  const [locationType, setLocationType] = useState<'Online Video' | 'Yüz Yüze • Kurum'>('Online Video');
  const [notes, setNotes] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (isOpen) {
      setError(null);
      // Default to tomorrow 14:00 to avoid any UTC time zone offset issues or past time validation
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      const tomorrowStr = tomorrow.toISOString().split('T')[0];
      setDateStr(tomorrowStr);
      academicService.getStudents({ page: 1, pageSize: 50 }).then((res) => {
        const items = res?.items || [];
        setStudents(items);
        if (items.length > 0 && !selectedStudentId) {
          setSelectedStudentId(items[0].id);
        }
      }).catch(() => {});
    }
  }, [isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedStudentId) {
      setError('Lütfen bir öğrenci seçin.');
      return;
    }

    try {
      setIsSubmitting(true);
      setError(null);

      const [hours, minutes] = timeStr.split(':').map(Number);
      const start = new Date(dateStr);
      start.setHours(hours || 14, minutes || 0, 0, 0);

      const created = await coachMeetingService.createMeeting({
        studentId: selectedStudentId,
        startTime: start.toISOString(),
        durationMinutes,
        meetingType: type,
        locationType,
        notes: notes.trim() || undefined,
      });

      onAddMeeting(created as CoachMeetingItem);
      onClose();
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Görüşme oluşturulamadı.';
      setError(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Yeni Görüşme Planla"
      subtitle="Atanmış öğrenci ile birebir koçluk seansı oluşturun"
      headerVariant="dark"
      footer={
        <div className="flex justify-end gap-2 w-full">
          <Button variant="secondary" size="sm" onClick={onClose} disabled={isSubmitting}>
            İptal
          </Button>
          <Button
            type="submit"
            form="new-meeting-form"
            variant="primary"
            size="sm"
            disabled={isSubmitting || !selectedStudentId}
          >
            {isSubmitting ? 'Kaydediliyor...' : 'Görüşmeyi Kaydet'}
          </Button>
        </div>
      }
    >
      <form id="new-meeting-form" onSubmit={handleSubmit} className="space-y-3 select-none">
        {error && (
          <div className="p-2.5 text-xs text-danger bg-danger/10 rounded-xl">
            {error}
          </div>
        )}

        <div>
          <label className="text-xs font-semibold text-neutral-700 block mb-1">
            Öğrenci
          </label>
          <select
            value={selectedStudentId}
            onChange={(e) => setSelectedStudentId(e.target.value)}
            className="w-full text-xs bg-white border border-neutral-200 rounded-xl px-3 py-2 text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
          >
            {students.map((s) => (
              <option key={s.id} value={s.id}>
                {s.fullName || `${s.firstName} ${s.lastName}`} {s.gradeLevel ? `(${s.gradeLevel}. Sınıf)` : ''}
              </option>
            ))}
          </select>
        </div>

        <div className="grid grid-cols-2 gap-2.5">
          <Input
            type="date"
            label="Tarih"
            value={dateStr}
            onChange={(e) => setDateStr(e.target.value)}
          />
          <Input
            type="time"
            label="Saat"
            value={timeStr}
            onChange={(e) => setTimeStr(e.target.value)}
          />
        </div>

        <div className="grid grid-cols-2 gap-2.5">
          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1">
              Süre
            </label>
            <select
              value={durationMinutes}
              onChange={(e) => setDurationMinutes(Number(e.target.value))}
              className="w-full text-xs bg-white border border-neutral-200 rounded-xl px-3 py-2 text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value={30}>30 dakika</option>
              <option value={45}>45 dakika</option>
              <option value={60}>60 dakika</option>
            </select>
          </div>

          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1">
              Görüşme Türü
            </label>
            <select
              value={locationType}
              onChange={(e) => setLocationType(e.target.value as 'Online Video' | 'Yüz Yüze • Kurum')}
              className="w-full text-xs bg-white border border-neutral-200 rounded-xl px-3 py-2 text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value="Online Video">Online Video</option>
              <option value="Yüz Yüze • Kurum">Yüz Yüze • Kurum</option>
            </select>
          </div>
        </div>

        <Input
          label="Görüşme Konusu"
          value={type}
          onChange={(e) => setType(e.target.value)}
          placeholder="Örn: Deneme Analizi & Hedef Takibi"
        />

        <div>
          <label className="text-xs font-semibold text-neutral-700 block mb-1">
            Gündem Notu
          </label>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            placeholder="Görüşmede ele alınacak ana başlıklar..."
            className="w-full text-xs bg-surface-alt border border-neutral-200 rounded-xl p-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
        </div>
      </form>
    </Modal>
  );
};
