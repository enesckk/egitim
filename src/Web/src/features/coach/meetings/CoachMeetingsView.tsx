import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Calendar, Plus, AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Select';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { MeetingItemCard } from './components/MeetingItemCard';
import { NewMeetingModal } from './components/NewMeetingModal';
import { initialCoachMeetingsData } from './mockData';
import { CoachMeetingItem, CoachMeetingsViewModel } from './types';

export interface CoachMeetingsViewProps {
  initialData?: CoachMeetingsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const CoachMeetingsView: React.FC<CoachMeetingsViewProps> = ({
  initialData = initialCoachMeetingsData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();
  const [meetings, setMeetings] = useState<CoachMeetingItem[]>(initialData.meetings);
  const [statusFilter, setStatusFilter] = useState('');
  const [isNewModalOpen, setIsNewModalOpen] = useState(false);

  const handleAddMeeting = (newMeeting: CoachMeetingItem) => {
    setMeetings((prev) => [newMeeting, ...prev]);
  };

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <div className="space-y-3">
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Görüşmeler Yüklenemedi">
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

  const filteredMeetings = meetings.filter((m) => {
    if (!statusFilter) return true;
    return m.status === statusFilter;
  });

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Koçluk Görüşmeleri
          </h1>
          <p className="text-xs text-neutral-400 mt-0.5">
            Birebir takip, motivasyon ve akademik analiz seansları
          </p>
        </div>

        <Button
          variant="primary"
          size="sm"
          onClick={() => setIsNewModalOpen(true)}
          leftIcon={<Plus className="h-4 w-4" />}
          className="self-start sm:self-auto"
        >
          Yeni Görüşme Planla
        </Button>
      </div>

      {/* Filter Bar */}
      <div className="flex items-center justify-between gap-2 bg-white rounded-2xl border border-neutral-100 p-3 sm:px-4 shadow-soft-sm">
        <div className="flex items-center gap-2">
          <Calendar className="h-4 w-4 text-primary-600" />
          <span className="text-xs sm:text-sm font-semibold text-neutral-800">
            Görüşme Takvimi ({filteredMeetings.length})
          </span>
        </div>

        <Select
          options={[
            { value: 'upcoming', label: 'Yalnızca Planlananlar' },
            { value: 'completed', label: 'Tamamlananlar' },
          ]}
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
          placeholder="Tüm Durumlar"
          className="text-xs min-h-[34px] py-1 pl-2.5 pr-7"
        />
      </div>

      {/* Meetings List */}
      {filteredMeetings.length > 0 ? (
        <div className="space-y-3">
          {filteredMeetings.map((meeting) => (
            <MeetingItemCard
              key={meeting.id}
              meeting={meeting}
              onSelectStudent={(studentId) => navigate(`/coach/students/${studentId}`)}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<Calendar className="h-6 w-6 text-neutral-400" />}
          title="Görüşme Bulunamadı"
          description="Seçilen duruma uygun koçluk görüşmesi kaydı yok."
        />
      )}

      {/* New Meeting Modal */}
      <NewMeetingModal
        isOpen={isNewModalOpen}
        onClose={() => setIsNewModalOpen(false)}
        onAddMeeting={handleAddMeeting}
      />
    </div>
  );
};
