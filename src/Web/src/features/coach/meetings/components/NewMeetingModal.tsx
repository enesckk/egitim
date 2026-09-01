import React, { useState } from 'react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
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
  const [studentName, setStudentName] = useState('Ayşe Kaya');
  const [dateStr, setDateStr] = useState('Bugün');
  const [timeStr, setTimeStr] = useState('18:00');
  const [durationMinutes, setDurationMinutes] = useState(30);
  const [type, setType] = useState('Haftalık Takip & Analiz');
  const [locationType, setLocationType] = useState<'Online Video' | 'Yüz Yüze • Kurum'>('Online Video');
  const [notes, setNotes] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onAddMeeting({
      id: `meet-${Date.now()}`,
      studentId: 'student-1',
      studentName,
      initials: studentName.split(' ').map((n) => n[0]).join(''),
      grade: '11. Sınıf',
      field: 'Sayısal',
      dateStr,
      timeStr,
      durationMinutes,
      type,
      locationType,
      status: 'upcoming',
      notes,
    });
    onClose();
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
          <Button variant="secondary" size="sm" onClick={onClose}>
            İptal
          </Button>
          <Button variant="primary" size="sm" onClick={handleSubmit}>
            Görüşmeyi Kaydet
          </Button>
        </div>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-3 select-none">
        <div>
          <label className="text-xs font-semibold text-neutral-700 block mb-1">
            Öğrenci
          </label>
          <select
            value={studentName}
            onChange={(e) => setStudentName(e.target.value)}
            className="w-full text-xs bg-white border border-neutral-200 rounded-xl px-3 py-2 text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
          >
            <option value="Ayşe Kaya">Ayşe Kaya (11. Sınıf • Sayısal)</option>
            <option value="Zeynep Demir">Zeynep Demir (12. Sınıf • Sayısal)</option>
            <option value="Ali Çelik">Ali Çelik (11. Sınıf • EA)</option>
            <option value="Emre Türk">Emre Türk (11. Sınıf • Sözel)</option>
            <option value="Nur Aydın">Nur Aydın (10. Sınıf • EA)</option>
          </select>
        </div>

        <div className="grid grid-cols-2 gap-2.5">
          <Input
            label="Tarih"
            value={dateStr}
            onChange={(e) => setDateStr(e.target.value)}
            placeholder="Örn: Yarın veya 3 Eyl"
          />
          <Input
            label="Saat"
            value={timeStr}
            onChange={(e) => setTimeStr(e.target.value)}
            placeholder="15:30"
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
