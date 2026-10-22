import React, { useState, useMemo, useEffect, useCallback } from 'react';
import { Search, GraduationCap, AlertCircle, RefreshCw, UserPlus } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { adminService, TeacherListDto } from '@/services/adminService';
import { AdminTeacherDirectoryItem, AdminTeachersViewModel } from './types';

function mapTeacherToItem(t: TeacherListDto): AdminTeacherDirectoryItem {
  const initials =
    `${t.firstName?.[0] || ''}${t.lastName?.[0] || ''}`.toUpperCase() || 'ÖĞ';
  return {
    id: t.id,
    name: `${t.firstName} ${t.lastName}`.trim(),
    initials,
    subject: t.title || 'Branş Öğretmeni',
    assignedClasses: [],
    totalStudentCount: 0,
    branch: 'Merkez',
    status: 'active',
  };
}

export interface AdminTeachersViewProps {
  initialData?: AdminTeachersViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const AdminTeachersView: React.FC<AdminTeachersViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const [data, setData] = useState<AdminTeachersViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const [searchTerm, setSearchTerm] = useState('');
  const [branchFilter, setBranchFilter] = useState('');

  // Add Teacher Modal
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [newFirstName, setNewFirstName] = useState('');
  const [newLastName, setNewLastName] = useState('');
  const [newEmail, setNewEmail] = useState('');
  const [newPassword, setNewPassword] = useState('Ogretmen@123!');
  const [newTitle, setNewTitle] = useState('Matematik Öğretmeni');
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const fetchTeachers = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const res = await adminService.listTeachers({ page: 1, pageSize: 100 });
      const items = (res?.items || []).map(mapTeacherToItem);
      setData({ teachers: items });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Öğretmen listesi alınamadı.';
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
      fetchTeachers();
    }
  }, [initialData, fetchTeachers]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchTeachers();
    }
  };

  const handleCreateTeacher = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newFirstName || !newLastName || !newEmail || !newPassword) {
      setCreateError('Lütfen tüm zorunlu alanları doldurunuz.');
      return;
    }

    setIsCreating(true);
    setCreateError(null);

    try {
      // Backend-atomic provisioning: Creates user and teacher profile transactionally
      await adminService.createTeacher({
        institutionId: '',
        firstName: newFirstName.trim(),
        lastName: newLastName.trim(),
        email: newEmail.trim(),
        password: newPassword,
        title: newTitle.trim() || undefined,
      });

      setIsAddModalOpen(false);
      setNewFirstName('');
      setNewLastName('');
      setNewEmail('');
      setNewPassword('Ogretmen@123!');
      await fetchTeachers();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : 'Öğretmen kaydı oluşturulamadı.');
    } finally {
      setIsCreating(false);
    }
  };

  const activeData = data || initialData || { teachers: [] };
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  const filteredTeachers = useMemo(() => {
    return activeData.teachers.filter((teacher) => {
      if (
        searchTerm &&
        !teacher.name.toLowerCase().includes(searchTerm.toLowerCase()) &&
        !teacher.subject.toLowerCase().includes(searchTerm.toLowerCase())
      ) {
        return false;
      }
      if (branchFilter && teacher.branch !== branchFilter) return false;
      return true;
    });
  }, [activeData.teachers, searchTerm, branchFilter]);

  const branches = useMemo(
    () => [...new Set(activeData.teachers.map((t) => t.branch))],
    [activeData.teachers]
  );

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none" data-testid="admin-teachers-loading">
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
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none" data-testid="admin-teachers-error">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Öğretmen Listesi Yüklenemedi"
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
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="admin-teachers-view">
      {/* 1. Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Öğretmenler & Zümre Yönetimi
          </h1>
          <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
            Kurum bünyesindeki {filteredTeachers.length} aktif branş öğretmeni
          </p>
        </div>
        <Button
          variant="primary"
          size="sm"
          onClick={() => setIsAddModalOpen(true)}
          leftIcon={<UserPlus className="h-4 w-4" />}
        >
          Yeni Öğretmen Ekle
        </Button>
      </div>

      {/* 2. Search & Branch Filter */}
      <div className="flex flex-col sm:flex-row items-center gap-3">
        <div className="relative flex-1 w-full max-w-sm">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Öğretmen adı veya branş ara..."
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

      {/* 3. Teachers Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {filteredTeachers.length === 0 ? (
          <div className="col-span-full py-12 text-center text-neutral-400 bg-white rounded-2xl border border-neutral-100">
            Kayıtlı öğretmen bulunamadı.
          </div>
        ) : (
          filteredTeachers.map((teacher) => (
            <div
              key={teacher.id}
              className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm hover:border-neutral-200 transition-colors space-y-4"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="flex items-center gap-3">
                  <div className="w-11 h-11 rounded-2xl bg-primary-50 text-primary-700 text-sm font-bold flex items-center justify-center flex-shrink-0">
                    {teacher.initials}
                  </div>
                  <div>
                    <h3 className="text-sm font-semibold text-neutral-900">{teacher.name}</h3>
                    <p className="text-xs text-neutral-500 font-medium">
                      {teacher.subject} • {teacher.branch} Şubesi
                    </p>
                  </div>
                </div>

                <span className="inline-flex items-center gap-1 text-[11px] font-semibold text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-full">
                  {teacher.totalStudentCount} Öğrenci
                </span>
              </div>

              <div className="space-y-1.5 pt-1">
                <span className="text-[11px] text-neutral-400 font-semibold uppercase tracking-wider block">
                  Atanmış Şubeler:
                </span>
                <div className="flex flex-wrap gap-1.5">
                  {teacher.assignedClasses.length === 0 ? (
                    <span className="text-xs text-neutral-400 italic">Sınıf atanmamış</span>
                  ) : (
                    teacher.assignedClasses.map((cls) => (
                      <span
                        key={cls}
                        className="inline-flex items-center gap-1 text-xs bg-surface-alt border border-neutral-200 text-neutral-700 px-2.5 py-1 rounded-lg"
                      >
                        <GraduationCap className="h-3 w-3 text-neutral-400" />
                        {cls}
                      </span>
                    ))
                  )}
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Add Teacher Modal */}
      <Modal
        isOpen={isAddModalOpen}
        onClose={() => {
          setIsAddModalOpen(false);
          setCreateError(null);
        }}
        title="Yeni Öğretmen Ekle"
      >
        <form onSubmit={handleCreateTeacher} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Input
              label="Ad"
              value={newFirstName}
              onChange={(e) => setNewFirstName(e.target.value)}
              placeholder="Örn: Kemal"
              required
            />
            <Input
              label="Soyad"
              value={newLastName}
              onChange={(e) => setNewLastName(e.target.value)}
              placeholder="Örn: Bey"
              required
            />
          </div>

          <Input
            label="E-posta"
            type="email"
            value={newEmail}
            onChange={(e) => setNewEmail(e.target.value)}
            placeholder="kemal@okul.com"
            required
          />

          <Input
            label="Başlangıç Şifresi"
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
          />

          <Input
            label="Branş / Unvan"
            value={newTitle}
            onChange={(e) => setNewTitle(e.target.value)}
            placeholder="Örn: Matematik & Geometri"
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
