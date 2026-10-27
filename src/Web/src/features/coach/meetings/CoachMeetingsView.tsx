import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Calendar, Plus, AlertCircle, RefreshCw, Search, CheckCircle2, Clock } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Select } from '@/components/ui/Select';
import { Input } from '@/components/ui/Input';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { useAuth } from '@/auth';
import { MeetingItemCard } from './components/MeetingItemCard';
import { NewMeetingModal } from './components/NewMeetingModal';
import { coachMeetingService } from '@/services/coachMeetingService';
import { CoachMeetingItem, CoachMeetingsViewModel } from './types';

export interface CoachMeetingsViewProps {
  initialData?: CoachMeetingsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

const ITEMS_PER_PAGE = 10;

export const CoachMeetingsView: React.FC<CoachMeetingsViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();
  const { user } = useAuth();

  const [meetings, setMeetings] = useState<CoachMeetingItem[]>(initialData?.meetings || []);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const [searchQuery, setSearchQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [isNewModalOpen, setIsNewModalOpen] = useState(false);
  const [currentPage, setCurrentPage] = useState(1);

  const fetchMeetings = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const data = await coachMeetingService.listMeetings();
      setMeetings(data as CoachMeetingItem[]);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Görüşmeler yüklenemedi.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [initialData]);

  useEffect(() => {
    if (initialData) {
      setMeetings(initialData.meetings);
      setIsLoading(false);
    } else {
      fetchMeetings();
    }
  }, [initialData, fetchMeetings]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchMeetings();
    }
  };

  const handleAddMeeting = (newMeeting: CoachMeetingItem) => {
    setMeetings((prev) => [newMeeting, ...prev]);
  };

  const handleStatusChange = async (meetingId: string, newStatus: 'completed' | 'cancelled' | 'upcoming') => {
    try {
      const updated = await coachMeetingService.updateMeeting(meetingId, { status: newStatus });
      setMeetings((prev) =>
        prev.map((m) => (m.id === meetingId ? (updated as CoachMeetingItem) : m))
      );
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Durum güncellenemedi.';
      alert(msg);
    }
  };

  const handleDeleteMeeting = async (meetingId: string) => {
    try {
      await coachMeetingService.deleteMeeting(meetingId);
      setMeetings((prev) => prev.filter((m) => m.id !== meetingId));
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Görüşme silinemedi.';
      alert(msg);
    }
  };

  // Filtered list
  const filteredMeetings = useMemo(() => {
    return meetings.filter((m) => {
      const q = searchQuery.toLowerCase().trim();
      const matchesSearch =
        !q ||
        m.studentName.toLowerCase().includes(q) ||
        m.type.toLowerCase().includes(q) ||
        (m.notes && m.notes.toLowerCase().includes(q));

      const matchesStatus = !statusFilter || m.status === statusFilter;
      return matchesSearch && matchesStatus;
    });
  }, [meetings, searchQuery, statusFilter]);

  // Reset page when filter changes
  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, statusFilter]);

  // Date Grouping & Pagination
  const totalPages = Math.ceil(filteredMeetings.length / ITEMS_PER_PAGE) || 1;
  const paginatedMeetings = useMemo(() => {
    const start = (currentPage - 1) * ITEMS_PER_PAGE;
    return filteredMeetings.slice(start, start + ITEMS_PER_PAGE);
  }, [filteredMeetings, currentPage]);

  const todayStr = new Date().toISOString().split('T')[0];
  const upcomingCount = meetings.filter((m) => m.status === 'upcoming').length;
  const completedCount = meetings.filter((m) => m.status === 'completed').length;
  const todaysCount = meetings.filter(
    (m) => m.startTime && m.startTime.startsWith(todayStr) && m.status !== 'cancelled'
  ).length;

  if (propIsLoading || isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="coach-meetings-loading">
        <Skeleton className="h-28 w-full rounded-3xl" />
        <Skeleton className="h-14 w-full rounded-2xl" />
        <div className="space-y-3">
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  if (propErrorMessage || errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4 select-none p-4" data-testid="coach-meetings-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Görüşmeler Yüklenemedi">
          {propErrorMessage || errorMessage}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={handleRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="coach-meetings-view">
      {/* 1. Header Hero Banner */}
      <div className="bg-[#14233E] rounded-3xl p-5 sm:p-7 text-white shadow-soft-sm relative overflow-hidden border border-navy-800">
        <div className="relative z-10 flex flex-col md:flex-row md:items-center justify-between gap-4">
          <div>
            <div className="flex items-center gap-2 mb-1.5">
              <span className="text-xs px-2.5 py-0.5 rounded-full bg-navy-800/80 text-blue-300 border border-navy-700 font-medium">
                {user?.institutionName || 'Kurum Portalı'}
              </span>
              <span className="text-navy-500">•</span>
              <span className="text-xs font-mono text-navy-300">
                {meetings.length} Toplam Seans
              </span>
            </div>
            <h1 className="font-serif text-2xl sm:text-3xl font-bold tracking-tight text-white leading-tight">
              Koçluk Görüşmeleri
            </h1>
            <p className="text-xs sm:text-sm text-navy-200 mt-1 max-w-xl leading-relaxed">
              Atanmış öğrencilerinizle birebir görüşme takvimi, seans gündemleri ve durum takibi.
            </p>
          </div>

          <div className="flex items-center gap-2.5 shrink-0 flex-wrap">
            <Button
              variant="primary"
              size="sm"
              onClick={() => setIsNewModalOpen(true)}
              leftIcon={<Plus className="h-4 w-4" />}
              className="bg-[#315FE8] hover:bg-blue-600 text-white text-xs rounded-xl shadow-soft-xs"
            >
              Yeni Görüşme Planla
            </Button>
          </div>
        </div>
      </div>

      {/* 2. Real Metrics KPI Strip */}
      <div className="grid grid-cols-2 sm:grid-cols-3 gap-3 sm:gap-4">
        <div className="bg-white p-4 rounded-2xl border border-neutral-200/80 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Bugünkü Görüşmeler</span>
            <Clock className="h-4 w-4 text-blue-600" />
          </div>
          <div className="flex items-baseline gap-1.5">
            <span className="text-2xl font-bold text-neutral-900">{todaysCount}</span>
            <span className="text-xs text-neutral-500 font-medium">seans</span>
          </div>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-neutral-200/80 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Gelecek Seanslar</span>
            <Calendar className="h-4 w-4 text-primary-600" />
          </div>
          <div className="flex items-baseline gap-1.5">
            <span className="text-2xl font-bold text-neutral-900">{upcomingCount}</span>
            <span className="text-xs text-neutral-500 font-medium">planlı</span>
          </div>
        </div>

        <div className="bg-white p-4 rounded-2xl border border-neutral-200/80 shadow-soft-xs flex flex-col justify-between col-span-2 sm:col-span-1">
          <div className="flex items-center justify-between text-neutral-500 mb-2">
            <span className="text-xs font-semibold uppercase tracking-wider">Tamamlanan</span>
            <CheckCircle2 className="h-4 w-4 text-emerald-600" />
          </div>
          <div className="flex items-baseline gap-1.5">
            <span className="text-2xl font-bold text-neutral-900">{completedCount}</span>
            <span className="text-xs text-neutral-500 font-medium">görüşme</span>
          </div>
        </div>
      </div>

      {/* 3. Search & Filter Bar */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-3.5 sm:p-4 flex flex-col sm:flex-row items-center gap-3 shadow-soft-xs">
        <div className="flex-1 w-full relative">
          <Input
            placeholder="Öğrenci adı, görüşme tipi veya gündem ara..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            leftIcon={<Search className="h-4 w-4 text-neutral-400" />}
            className="text-xs min-h-[40px] bg-neutral-50/50 border-neutral-200"
          />
        </div>

        <div className="flex items-center gap-2.5 w-full sm:w-auto">
          <Select
            placeholder="Tüm Durumlar"
            options={[
              { value: 'upcoming', label: 'Planlananlar' },
              { value: 'completed', label: 'Tamamlananlar' },
              { value: 'cancelled', label: 'İptal Edilenler' },
            ]}
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="text-xs min-h-[40px] py-1.5 pl-2.5 pr-7 flex-1 sm:w-48 bg-neutral-50/50 border-neutral-200"
          />
        </div>
      </div>

      {/* 4. Meetings List */}
      {paginatedMeetings.length > 0 ? (
        <div className="space-y-3">
          {paginatedMeetings.map((meeting) => (
            <MeetingItemCard
              key={meeting.id}
              meeting={meeting}
              onSelectStudent={(studentId) => navigate(`/coach/students/${studentId}`)}
              onStatusChange={handleStatusChange}
              onDeleteMeeting={handleDeleteMeeting}
            />
          ))}

          {/* Pagination Controls */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between pt-2 px-1 text-xs text-neutral-500">
              <span>
                Toplam {filteredMeetings.length} görüşmeden {(currentPage - 1) * ITEMS_PER_PAGE + 1} -{' '}
                {Math.min(currentPage * ITEMS_PER_PAGE, filteredMeetings.length)} arası gösteriliyor
              </span>
              <div className="flex items-center gap-1.5">
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage((p) => Math.max(p - 1, 1))}
                  disabled={currentPage === 1}
                  className="text-xs px-2.5 py-1 rounded-lg"
                >
                  Önceki
                </Button>
                <span className="font-mono font-medium px-2">
                  {currentPage} / {totalPages}
                </span>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => setCurrentPage((p) => Math.min(p + 1, totalPages))}
                  disabled={currentPage === totalPages}
                  className="text-xs px-2.5 py-1 rounded-lg"
                >
                  Sonraki
                </Button>
              </div>
            </div>
          )}
        </div>
      ) : (
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-8 sm:p-12 text-center shadow-soft-xs">
          <EmptyState
            icon={<Calendar className="h-8 w-8 text-neutral-400" />}
            title="Görüşme Bulunamadı"
            description="Arama kriterlerinize veya seçilen filtrelere uygun koçluk görüşmesi kaydı yok."
          />
        </div>
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

export default CoachMeetingsView;
