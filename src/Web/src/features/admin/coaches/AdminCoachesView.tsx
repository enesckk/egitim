import React, { useState, useMemo, useEffect, useCallback } from 'react';
import { Search, AlertTriangle, AlertCircle, RefreshCw, UserPlus } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { adminService, CoachSummaryDto } from '@/services/adminService';
import { AdminCoachDirectoryItem, AdminCoachesViewModel } from './types';

function mapCoachToItem(c: CoachSummaryDto): AdminCoachDirectoryItem {
  const initials =
    `${c.firstName?.[0] || ''}${c.lastName?.[0] || ''}`.toUpperCase() || 'KÇ';
  return {
    id: c.id,
    name: c.fullName || `${c.firstName} ${c.lastName}`.trim(),
    title: 'Öğrenci Koçu',
    initials,
    assignedStudentCount: c.assignedStudentCount || 0,
    portfolioCapacity: 25,
    averageAdherencePercentage: 88,
    averageWeeklyStudyHours: 24,
    attentionStudentCount: 0,
    branch: 'Merkez',
    status: 'active',
  };
}

export interface AdminCoachesViewProps {
  initialData?: AdminCoachesViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const AdminCoachesView: React.FC<AdminCoachesViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const [data, setData] = useState<AdminCoachesViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const [searchTerm, setSearchTerm] = useState('');
  const [branchFilter, setBranchFilter] = useState('');

  // Add Coach Modal
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [newFirstName, setNewFirstName] = useState('');
  const [newLastName, setNewLastName] = useState('');
  const [newEmail, setNewEmail] = useState('');
  const [newPassword, setNewPassword] = useState('Koc@12345!');
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const fetchCoaches = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const res = await adminService.listCoaches({ page: 1, pageSize: 100 });
      const items = (res?.items || []).map(mapCoachToItem);
      setData({ coaches: items });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Koç listesi alınamadı.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [initialData]);

  useEffect(() => {
    if (initialData) {
      setData(initialData);
      setIsLoading(false);
    } else {
      fetchCoaches();
    }
  }, [initialData, fetchCoaches]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchCoaches();
    }
  };

  const handleCreateCoach = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newFirstName || !newLastName || !newEmail || !newPassword) {
      setCreateError('Lütfen tüm zorunlu alanları doldurunuz.');
      return;
    }

    setIsCreating(true);
    setCreateError(null);

    try {
      // Backend-atomic provisioning: Creates user and coach profile transactionally
      await adminService.createCoach({
        institutionId: '',
        firstName: newFirstName.trim(),
        lastName: newLastName.trim(),
        email: newEmail.trim(),
        password: newPassword,
      });

      setIsAddModalOpen(false);
      setNewFirstName('');
      setNewLastName('');
      setNewEmail('');
      setNewPassword('Koc@12345!');
      await fetchCoaches();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : 'Koç kaydı oluşturulamadı.');
    } finally {
      setIsCreating(false);
    }
  };

  const activeData = data || initialData || { coaches: [] };
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  const filteredCoaches = useMemo(() => {
    return activeData.coaches.filter((coach) => {
      if (
        searchTerm &&
        !coach.name.toLowerCase().includes(searchTerm.toLowerCase()) &&
        !coach.title.toLowerCase().includes(searchTerm.toLowerCase())
      ) {
        return false;
      }
      if (branchFilter && coach.branch !== branchFilter) return false;
      return true;
    });
  }, [activeData.coaches, searchTerm, branchFilter]);

  const branches = useMemo(
    () => [...new Set(activeData.coaches.map((c) => c.branch))],
    [activeData.coaches]
  );

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none" data-testid="admin-coaches-loading">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-44 w-full rounded-2xl" />
          <Skeleton className="h-44 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none" data-testid="admin-coaches-error">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Koç Listesi Yüklenemedi"
        >
          {activeError}
        </Alert>
        <div className="text-center pt-2">
          <Button
            variant="primary"
            size="sm"
            onClick={handleRetry}
            leftIcon={<RefreshCw className="h-4 w-4" />}
          >
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="admin-coaches-view">
      {/* 1. Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Koç Yönetimi & Portföy
          </h1>
          <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
            Kurum bünyesindeki {filteredCoaches.length} aktif öğrenci koçu
          </p>
        </div>
        <Button
          variant="primary"
          size="sm"
          onClick={() => setIsAddModalOpen(true)}
          leftIcon={<UserPlus className="h-4 w-4" />}
        >
          Yeni Koç Ekle
        </Button>
      </div>

      {/* 2. Search & Branch Filter */}
      <div className="flex flex-col sm:flex-row items-center gap-3">
        <div className="relative flex-1 w-full max-w-sm">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Koç adı veya unvan ara..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-neutral-200 bg-white text-xs sm:text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
          />
        </div>

        <div className="flex items-center gap-2 self-start sm:self-center">
          <select
            value={branchFilter}
            onChange={(e) => setBranchFilter(e.target.value)}
            className="px-3 py-2.5 rounded-xl border border-neutral-200 bg-white text-xs sm:text-sm text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
          >
            <option value="">Tüm Şubeler</option>
            {branches.map((b) => (
              <option key={b} value={b}>
                {b} Şubesi
              </option>
            ))}
          </select>
        </div>
      </div>

      {/* 3. Coaches Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {filteredCoaches.length === 0 ? (
          <div className="col-span-full py-12 text-center text-neutral-400 bg-white rounded-2xl border border-neutral-100">
            Kayıtlı öğrenci koçu bulunamadı.
          </div>
        ) : (
          filteredCoaches.map((coach) => (
            <div
              key={coach.id}
              className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm hover:border-neutral-200 transition-colors space-y-4"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-center gap-3">
                  <div className="w-11 h-11 rounded-2xl bg-navy-50 text-navy-800 text-sm font-bold flex items-center justify-center flex-shrink-0">
                    {coach.initials}
                  </div>
                  <div>
                    <h3 className="text-sm font-semibold text-neutral-900">{coach.name}</h3>
                    <p className="text-xs text-neutral-400">
                      {coach.title} • {coach.branch} Şubesi
                    </p>
                  </div>
                </div>

                {coach.attentionStudentCount > 0 && (
                  <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-danger-dark bg-danger-light border border-red-200 px-2.5 py-1 rounded-full flex-shrink-0">
                    <AlertTriangle className="h-3 w-3 text-danger" />
                    {coach.attentionStudentCount} Takip
                  </span>
                )}
              </div>

              <div className="grid grid-cols-3 gap-2 bg-surface-alt p-3 rounded-xl text-center">
                <div>
                  <span className="text-[10px] text-neutral-400 block font-medium">Öğrenci</span>
                  <span className="font-mono text-sm font-bold text-neutral-900">
                    {coach.assignedStudentCount} / {coach.portfolioCapacity}
                  </span>
                </div>
                <div>
                  <span className="text-[10px] text-neutral-400 block font-medium">Plan Uyumu</span>
                  <span
                    className={cn(
                      'font-mono text-sm font-bold',
                      coach.averageAdherencePercentage >= 80
                        ? 'text-success'
                        : coach.averageAdherencePercentage >= 70
                        ? 'text-warning-dark'
                        : 'text-danger'
                    )}
                  >
                    %{coach.averageAdherencePercentage}
                  </span>
                </div>
                <div>
                  <span className="text-[10px] text-neutral-400 block font-medium">Haftalık Ort.</span>
                  <span className="font-mono text-sm font-bold text-neutral-900">
                    {coach.averageWeeklyStudyHours} sa
                  </span>
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Add Coach Modal */}
      <Modal
        isOpen={isAddModalOpen}
        onClose={() => {
          setIsAddModalOpen(false);
          setCreateError(null);
        }}
        title="Yeni Koç Ekle"
      >
        <form onSubmit={handleCreateCoach} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Input
              label="Ad"
              value={newFirstName}
              onChange={(e) => setNewFirstName(e.target.value)}
              placeholder="Örn: Hasan"
              required
            />
            <Input
              label="Soyad"
              value={newLastName}
              onChange={(e) => setNewLastName(e.target.value)}
              placeholder="Örn: Yılmaz"
              required
            />
          </div>

          <Input
            label="E-posta"
            type="email"
            value={newEmail}
            onChange={(e) => setNewEmail(e.target.value)}
            placeholder="hasan@okul.com"
            required
          />

          <Input
            label="Başlangıç Şifresi"
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
          />

          {createError && (
            <p className="text-xs text-danger font-medium">{createError}</p>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setIsAddModalOpen(false);
                setCreateError(null);
              }}
            >
              İptal
            </Button>
            <Button
              type="submit"
              variant="primary"
              disabled={isCreating}
            >
              {isCreating ? 'Kaydediliyor...' : 'Kaydet'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
