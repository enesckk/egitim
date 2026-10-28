import React, { useState, useMemo, useEffect, useCallback } from 'react';
import {
  Search,
  UserPlus,
  RefreshCw,
  AlertCircle,
  Users,
  CheckCircle2,
  Compass,
  Plus,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { adminService, CoachSummaryDto, StudentSummaryDto } from '@/services/adminService';
import { AdminCoachDirectoryItem, AdminCoachesViewModel } from './types';

function mapCoachToItem(c: CoachSummaryDto): AdminCoachDirectoryItem {
  const initials =
    `${c.firstName?.[0] || ''}${c.lastName?.[0] || ''}`.toUpperCase() || 'KÇ';
  return {
    id: c.id,
    name: c.fullName || `${c.firstName} ${c.lastName}`.trim(),
    firstName: c.firstName,
    lastName: c.lastName,
    initials,
    institutionId: c.institutionId,
    assignedStudentCount: c.assignedStudentCount || (c.assignedStudents?.length ?? 0),
    assignedStudents: c.assignedStudents || [],
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
  const [selectedCoach, setSelectedCoach] = useState<AdminCoachDirectoryItem | null>(null);

  // Student Assignment Modal state
  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [availableStudents, setAvailableStudents] = useState<StudentSummaryDto[]>([]);
  const [isLoadingStudents, setIsLoadingStudents] = useState(false);
  const [studentSearchTerm, setStudentSearchTerm] = useState('');
  const [selectedStudentId, setSelectedStudentId] = useState('');
  const [isAssigning, setIsAssigning] = useState(false);
  const [assignError, setAssignError] = useState<string | null>(null);
  const [assignSuccess, setAssignSuccess] = useState(false);

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
      // Backend may return PaginatedResult or raw list
      const itemsList = Array.isArray(res) ? res : (res?.items || []);
      const items = itemsList.map(mapCoachToItem);
      setData({ coaches: items });

      // Update selected coach reference if open
      setSelectedCoach((prev) => {
        if (!prev) return null;
        return items.find((c) => c.id === prev.id) || null;
      });
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
    if (!newFirstName.trim() || !newLastName.trim() || !newEmail.trim() || !newPassword) {
      setCreateError('Lütfen tüm zorunlu alanları doldurunuz.');
      return;
    }

    setIsCreating(true);
    setCreateError(null);

    try {
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

  const handleOpenAssignModal = async (coach: AdminCoachDirectoryItem) => {
    setSelectedCoach(coach);
    setIsAssignModalOpen(true);
    setSelectedStudentId('');
    setAssignError(null);
    setAssignSuccess(false);
    setIsLoadingStudents(true);

    try {
      const res = await adminService.listStudents({ page: 1, pageSize: 150 });
      setAvailableStudents(res?.items || []);
    } catch (err: unknown) {
      setAssignError('Öğrenci listesi alınamadı.');
    } finally {
      setIsLoadingStudents(false);
    }
  };

  const handleAssignStudent = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedCoach || !selectedStudentId) {
      setAssignError('Lütfen atanacak bir öğrenci seçiniz.');
      return;
    }

    setIsAssigning(true);
    setAssignError(null);
    setAssignSuccess(false);

    try {
      await adminService.assignCoach(selectedStudentId, selectedCoach.id);
      setAssignSuccess(true);
      await fetchCoaches();
      setTimeout(() => {
        setIsAssignModalOpen(false);
        setAssignSuccess(false);
      }, 1200);
    } catch (err: unknown) {
      setAssignError(err instanceof Error ? err.message : 'Koç ataması yapılamadı.');
    } finally {
      setIsAssigning(false);
    }
  };

  const activeData = data || initialData || { coaches: [] };
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  const filteredCoaches = useMemo(() => {
    return activeData.coaches.filter((coach) => {
      if (
        searchTerm &&
        !coach.name.toLowerCase().includes(searchTerm.toLowerCase())
      ) {
        return false;
      }
      return true;
    });
  }, [activeData.coaches, searchTerm]);

  const totalAssignedStudents = useMemo(() => {
    return activeData.coaches.reduce((acc, c) => acc + (c.assignedStudentCount || 0), 0);
  }, [activeData.coaches]);

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="admin-coaches-loading">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-12 w-full rounded-2xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <Skeleton className="h-44 w-full rounded-2xl" />
          <Skeleton className="h-44 w-full rounded-2xl" />
          <Skeleton className="h-44 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none p-4" data-testid="admin-coaches-error">
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
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="admin-coaches-view">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-neutral-200/70 pb-4">
        <div>
          <div className="flex items-center gap-2.5 flex-wrap">
            <h1 className="font-serif text-2xl sm:text-3xl font-bold text-navy-950 tracking-tight leading-tight">
              Öğrenci Koçları
            </h1>
            <span className="text-xs font-bold text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-full border border-primary-100">
              {filteredCoaches.length !== activeData.coaches.length
                ? `${filteredCoaches.length} / ${activeData.coaches.length} Koç`
                : `${activeData.coaches.length} Aktif Koç`}
            </span>
            <span className="text-xs font-medium text-neutral-600 bg-neutral-100 px-2.5 py-0.5 rounded-full border border-neutral-200">
              {totalAssignedStudents} Atanmış Öğrenci
            </span>
          </div>
          <p className="text-xs sm:text-sm text-neutral-500 mt-1">
            Kuruma bağlı koçların portföy takibi, öğrenci eşleştirmesi ve danışmanlık yönetimi
          </p>
        </div>

        <Button
          variant="primary"
          size="sm"
          onClick={() => setIsAddModalOpen(true)}
          leftIcon={<UserPlus className="h-4 w-4" />}
          className="bg-[#315FE8] hover:bg-blue-700 rounded-xl shadow-soft-xs font-semibold w-full md:w-auto shrink-0 py-2.5 px-4"
        >
          Yeni Koç Ekle
        </Button>
      </div>

      {/* 2. Search & Action Bar */}
      <div className="bg-white p-3.5 sm:p-4 rounded-2xl border border-neutral-200/80 shadow-soft-xs flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3">
        <div className="relative flex-1 w-full min-w-0 md:max-w-md">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Koç adı ile filtrele..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-neutral-200 bg-neutral-50/60 text-xs sm:text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all font-medium"
          />
        </div>
      </div>

      {/* 3. Coaches Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {filteredCoaches.length === 0 ? (
          <div className="col-span-full py-16 text-center text-neutral-400 bg-white rounded-2xl border border-neutral-200/80 shadow-soft-xs p-4">
            <Compass className="h-10 w-10 mx-auto text-neutral-300 mb-2" />
            <p className="text-sm font-semibold text-neutral-700">Kayıtlı öğrenci koçu bulunamadı.</p>
            <p className="text-xs text-neutral-400 mt-1">
              Kurumunuza yeni öğrenci koçu kaydedebilir veya arama filtresini temizleyebilirsiniz.
            </p>
          </div>
        ) : (
          filteredCoaches.map((coach) => (
            <div
              key={coach.id}
              className="bg-white rounded-2xl border border-neutral-200/80 p-5 shadow-soft-xs hover:border-primary-200 hover:shadow-soft-sm transition-all flex flex-col justify-between space-y-4"
            >
              {/* Card Top: Coach Identity */}
              <div>
                <div className="flex items-start justify-between gap-3">
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="w-11 h-11 rounded-2xl bg-[#14233E] text-white text-sm font-bold flex items-center justify-center shrink-0 shadow-soft-xs">
                      {coach.initials}
                    </div>
                    <div className="min-w-0">
                      <h3 className="text-sm font-bold text-navy-950 truncate">
                        {coach.name}
                      </h3>
                      <p className="text-xs text-neutral-500 font-medium truncate mt-0.5">
                        Öğrenci Koçu
                      </p>
                    </div>
                  </div>

                  <span className="inline-flex items-center gap-1 text-[11px] font-bold text-emerald-700 bg-emerald-50 px-2.5 py-0.5 rounded-full border border-emerald-200 shrink-0">
                    <CheckCircle2 className="h-3 w-3" />
                    Aktif
                  </span>
                </div>

                {/* Real Assigned Student Metric Banner */}
                <div className="mt-4 p-3 rounded-xl bg-neutral-50 border border-neutral-100 flex items-center justify-between">
                  <div className="flex items-center gap-2 text-xs text-neutral-600 font-medium">
                    <Users className="h-4 w-4 text-neutral-400" />
                    <span>Danışmanlık Portföyü</span>
                  </div>
                  <span className="text-xs font-bold text-navy-950 font-mono">
                    {coach.assignedStudentCount} Öğrenci
                  </span>
                </div>

                {/* Real Student Roster Preview */}
                <div className="mt-3 space-y-1.5">
                  <p className="text-[11px] font-semibold text-neutral-400 uppercase tracking-wider">
                    Atanmış Öğrenciler ({coach.assignedStudents.length})
                  </p>
                  {coach.assignedStudents.length === 0 ? (
                    <p className="text-xs text-neutral-400 italic py-1">
                      Henüz atanmış öğrenci bulunmuyor.
                    </p>
                  ) : (
                    <div className="max-h-36 overflow-y-auto space-y-1 pr-1">
                      {coach.assignedStudents.map((st) => (
                        <div
                          key={st.studentId}
                          className="flex items-center justify-between text-xs py-1 px-2 rounded-lg bg-neutral-50/80 hover:bg-neutral-100/70 transition-colors"
                        >
                          <span className="font-medium text-neutral-800 truncate">
                            {st.fullName}
                          </span>
                          <span className="text-[11px] text-neutral-400 shrink-0 ml-2">
                            {st.gradeLevel ? `${st.gradeLevel}. Sınıf` : 'Öğrenci'}
                          </span>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              </div>

              {/* Card Footer Actions */}
              <div className="pt-3 border-t border-neutral-100 flex items-center justify-between gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setSelectedCoach(coach)}
                  className="text-xs h-8 px-3 rounded-lg text-neutral-700 font-medium"
                >
                  Detay İncele
                </Button>
                <Button
                  variant="primary"
                  size="sm"
                  onClick={() => handleOpenAssignModal(coach)}
                  leftIcon={<Plus className="h-3.5 w-3.5" />}
                  className="bg-[#315FE8] hover:bg-blue-700 text-xs h-8 px-3 rounded-lg font-semibold"
                >
                  Öğrenci Ata
                </Button>
              </div>
            </div>
          ))
        )}
      </div>

      {/* 4. Coach Detail Drawer/Modal */}
      {selectedCoach && !isAssignModalOpen && (
        <Modal
          isOpen={Boolean(selectedCoach)}
          onClose={() => setSelectedCoach(null)}
          title="Koç Portföy Detayı"
        >
          <div className="space-y-4">
            <div className="flex items-center gap-3.5 p-3.5 rounded-2xl bg-neutral-50 border border-neutral-200/80">
              <div className="w-12 h-12 rounded-2xl bg-[#14233E] text-white text-base font-bold flex items-center justify-center shrink-0 shadow-soft-xs">
                {selectedCoach.initials}
              </div>
              <div className="min-w-0 flex-1">
                <h3 className="text-base font-bold text-navy-950 truncate">
                  {selectedCoach.name}
                </h3>
                <p className="text-xs text-neutral-500 font-medium mt-0.5">
                  Öğrenci Koçu • {selectedCoach.assignedStudentCount} Öğrenci Takipte
                </p>
              </div>
            </div>

            <div>
              <div className="flex items-center justify-between mb-2">
                <h4 className="text-xs font-bold uppercase tracking-wider text-neutral-500">
                  Atanmış Öğrenci Portföyü
                </h4>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleOpenAssignModal(selectedCoach)}
                  leftIcon={<Plus className="h-3.5 w-3.5 text-primary-600" />}
                  className="text-primary-600 hover:text-primary-700 text-xs h-7 px-2 font-semibold"
                >
                  Yeni Öğrenci Ata
                </Button>
              </div>

              {selectedCoach.assignedStudents.length === 0 ? (
                <div className="p-6 text-center rounded-xl bg-neutral-50 border border-neutral-100 text-neutral-400 text-xs">
                  Bu koça henüz atanmış öğrenci bulunmuyor.
                </div>
              ) : (
                <div className="divide-y divide-neutral-100 max-h-60 overflow-y-auto rounded-xl border border-neutral-200/80 bg-white">
                  {selectedCoach.assignedStudents.map((st) => (
                    <div
                      key={st.studentId}
                      className="p-3 flex items-center justify-between text-xs hover:bg-[#F8F7F3] transition-colors"
                    >
                      <div>
                        <span className="font-semibold text-neutral-900 block">
                          {st.fullName}
                        </span>
                        <span className="text-[11px] text-neutral-400">
                          {st.gradeLevel ? `${st.gradeLevel}. Sınıf` : 'Kayıtlı Öğrenci'}
                        </span>
                      </div>
                      {st.isPrimary && (
                        <span className="text-[10px] font-bold text-primary-700 bg-primary-50 px-2 py-0.5 rounded-full border border-primary-100">
                          Asil Danışman
                        </span>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="flex justify-end pt-2 border-t border-neutral-100">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setSelectedCoach(null)}
              >
                Kapat
              </Button>
            </div>
          </div>
        </Modal>
      )}

      {/* 5. Student Assignment Modal */}
      <Modal
        isOpen={isAssignModalOpen}
        onClose={() => {
          setIsAssignModalOpen(false);
          setAssignError(null);
          setAssignSuccess(false);
        }}
        title={`Öğrenci Ata — ${selectedCoach?.name || 'Koç'}`}
      >
        <form onSubmit={handleAssignStudent} className="space-y-4">
          <p className="text-xs text-neutral-500">
            Kuruma kayıtlı öğrencilerden birini seçerek bu koça danışmanlık sorumluluğu atayabilirsiniz.
          </p>

          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1.5">
              Öğrenci Seçin
            </label>

            <div className="relative mb-2">
              <Search className="h-3.5 w-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-neutral-400" />
              <input
                type="text"
                placeholder="Öğrenci ara..."
                value={studentSearchTerm}
                onChange={(e) => setStudentSearchTerm(e.target.value)}
                className="w-full pl-9 pr-3 py-1.5 text-xs rounded-xl border border-neutral-200 bg-neutral-50/60 text-neutral-800 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 font-medium"
              />
            </div>

            {isLoadingStudents ? (
              <div className="py-6 text-center text-xs text-neutral-400">
                Öğrenciler yükleniyor...
              </div>
            ) : (
              <div className="max-h-52 overflow-y-auto border border-neutral-200/80 rounded-xl divide-y divide-neutral-100 bg-white">
                {availableStudents
                  .filter((s) => {
                    if (!studentSearchTerm) return true;
                    const full = `${s.firstName || ''} ${s.lastName || ''} ${s.fullName || ''}`.toLowerCase();
                    return full.includes(studentSearchTerm.toLowerCase());
                  })
                  .map((s) => {
                    const isAlreadyAssigned = selectedCoach?.assignedStudents.some(
                      (st) => st.studentId === s.id
                    );
                    const isSelected = selectedStudentId === s.id;

                    return (
                      <div
                        key={s.id}
                        onClick={() => {
                          if (!isAlreadyAssigned) setSelectedStudentId(s.id);
                        }}
                        className={`p-2.5 flex items-center justify-between text-xs cursor-pointer transition-colors ${
                          isSelected
                            ? 'bg-primary-50 text-primary-900 font-semibold'
                            : isAlreadyAssigned
                            ? 'bg-neutral-50 opacity-50 cursor-not-allowed'
                            : 'hover:bg-neutral-50 text-neutral-700'
                        }`}
                      >
                        <div>
                          <p className="font-semibold">{s.fullName || `${s.firstName} ${s.lastName}`}</p>
                          <p className="text-[11px] text-neutral-400">
                            {s.gradeLevel ? `${s.gradeLevel}. Sınıf` : ''} {s.schoolName ? `· ${s.schoolName}` : ''}
                          </p>
                        </div>
                        {isAlreadyAssigned ? (
                          <span className="text-[10px] font-medium text-neutral-400">Zaten Atanmış</span>
                        ) : isSelected ? (
                          <CheckCircle2 className="h-4 w-4 text-primary-600" />
                        ) : null}
                      </div>
                    );
                  })}
              </div>
            )}
          </div>

          {assignError && (
            <p className="text-xs text-rose-700 bg-rose-50 border border-rose-200 p-2.5 rounded-xl font-medium">
              {assignError}
            </p>
          )}

          {assignSuccess && (
            <p className="text-xs text-emerald-700 bg-emerald-50 border border-emerald-200 p-2.5 rounded-xl font-medium">
              Öğrenci koça başarıyla atandı.
            </p>
          )}

          <div className="flex justify-end gap-2 pt-2 border-t border-neutral-100">
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setIsAssignModalOpen(false);
                setAssignError(null);
                setAssignSuccess(false);
              }}
            >
              İptal
            </Button>
            <Button
              type="submit"
              variant="primary"
              disabled={isAssigning || !selectedStudentId || assignSuccess}
              className="bg-[#315FE8] hover:bg-blue-700 font-semibold"
            >
              {isAssigning ? 'Atanıyor...' : 'Atamayı Onayla'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* 6. Add Coach Modal */}
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
            <p className="text-xs text-rose-700 bg-rose-50 border border-rose-200 p-2.5 rounded-xl font-medium">
              {createError}
            </p>
          )}

          <div className="flex justify-end gap-2 pt-2 border-t border-neutral-100">
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
              className="bg-[#315FE8] hover:bg-blue-700 font-semibold"
            >
              {isCreating ? 'Kaydediliyor...' : 'Kaydet'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};

export default AdminCoachesView;
