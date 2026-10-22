import React, { useState, useMemo, useEffect, useCallback } from 'react';
import { Search, GraduationCap, Users, AlertCircle, RefreshCw, Plus } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { adminService, ClassroomDto } from '@/services/adminService';
import { AdminClassDirectoryItem, AdminClassesViewModel } from './types';

function mapClassroomToItem(c: ClassroomDto): AdminClassDirectoryItem {
  return {
    id: c.id,
    name: c.name,
    grade: c.gradeLevel ? `${c.gradeLevel}. Sınıf` : '12. Sınıf',
    field: c.branch || 'Genel',
    studentCount: c.studentCount || 0,
    assignedTeachers: (c.teachers || []).map((t) => `${t.fullName}${t.title ? ` (${t.title})` : ''}`),
    averageAdherencePercentage: 85,
    averageNet: 65.5,
    branch: c.branch || 'Merkez',
  };
}

export interface AdminClassesViewProps {
  initialData?: AdminClassesViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const AdminClassesView: React.FC<AdminClassesViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const [data, setData] = useState<AdminClassesViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const [searchTerm, setSearchTerm] = useState('');
  const [gradeFilter, setGradeFilter] = useState('');

  // Add Classroom Modal State
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [newName, setNewName] = useState('');
  const [newBranch, setNewBranch] = useState('Merkez');
  const [newGradeLevel, setNewGradeLevel] = useState('12');
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const fetchClasses = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const res = await adminService.listClassrooms();
      const items = (res || []).map(mapClassroomToItem);
      setData({ classes: items });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Sınıf listesi alınamadı.';
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
      fetchClasses();
    }
  }, [initialData, fetchClasses]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchClasses();
    }
  };

  const handleCreateClass = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newName.trim()) {
      setCreateError('Lütfen sınıf adını giriniz.');
      return;
    }

    setIsCreating(true);
    setCreateError(null);

    try {
      await adminService.createClassroom({
        name: newName.trim(),
        branch: newBranch.trim() || undefined,
        gradeLevel: parseInt(newGradeLevel, 10) || 12,
      });

      setIsAddModalOpen(false);
      setNewName('');
      await fetchClasses();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : 'Sınıf oluşturulamadı.');
    } finally {
      setIsCreating(false);
    }
  };

  const activeData = data || initialData || { classes: [] };
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  const filteredClasses = useMemo(() => {
    return activeData.classes.filter((cls) => {
      if (
        searchTerm &&
        !cls.name.toLowerCase().includes(searchTerm.toLowerCase()) &&
        !cls.field.toLowerCase().includes(searchTerm.toLowerCase())
      ) {
        return false;
      }
      if (gradeFilter && !cls.grade.includes(gradeFilter)) return false;
      return true;
    });
  }, [activeData.classes, searchTerm, gradeFilter]);

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none" data-testid="admin-classes-loading">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-48 w-full rounded-2xl" />
          <Skeleton className="h-48 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none" data-testid="admin-classes-error">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Sınıf Listesi Yüklenemedi"
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
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="admin-classes-view">
      {/* 1. Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Sınıflar & Gruplar
          </h1>
          <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
            Kurum bünyesindeki {filteredClasses.length} aktif şube ve öğrenci grupları
          </p>
        </div>
        <Button
          variant="primary"
          size="sm"
          onClick={() => setIsAddModalOpen(true)}
          leftIcon={<Plus className="h-4 w-4" />}
        >
          Yeni Sınıf Ekle
        </Button>
      </div>

      {/* 2. Search & Grade Filter */}
      <div className="flex flex-col sm:flex-row items-center gap-3">
        <div className="relative flex-1 w-full max-w-sm">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Sınıf veya alan ara..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-neutral-200 bg-white text-xs sm:text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
          />
        </div>

        <div className="flex items-center gap-2 self-start sm:self-center">
          <select
            value={gradeFilter}
            onChange={(e) => setGradeFilter(e.target.value)}
            className="px-3 py-2.5 rounded-xl border border-neutral-200 bg-white text-xs sm:text-sm text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500 shadow-soft-xs"
          >
            <option value="">Tüm Kademeler</option>
            <option value="9">9. Sınıf</option>
            <option value="10">10. Sınıf</option>
            <option value="11">11. Sınıf</option>
            <option value="12">12. Sınıf</option>
          </select>
        </div>
      </div>

      {/* 3. Class Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {filteredClasses.length === 0 ? (
          <div className="col-span-full py-12 text-center text-neutral-400 bg-white rounded-2xl border border-neutral-100">
            Kayıtlı sınıf veya grup bulunamadı.
          </div>
        ) : (
          filteredClasses.map((cls) => (
            <div
              key={cls.id}
              className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm hover:border-neutral-200 transition-colors space-y-4"
            >
              <div className="flex items-start justify-between gap-2">
                <div>
                  <h3 className="text-base font-bold text-neutral-900">{cls.name}</h3>
                  <p className="text-xs text-neutral-400 mt-0.5">
                    {cls.grade} • {cls.field}
                  </p>
                </div>
                <span className="inline-flex items-center gap-1.5 text-xs font-semibold bg-neutral-100 text-neutral-700 px-2.5 py-1 rounded-full">
                  <Users className="h-3.5 w-3.5 text-neutral-500" />
                  {cls.studentCount} Öğrenci
                </span>
              </div>

              <div className="grid grid-cols-2 gap-2 bg-surface-alt p-3 rounded-xl">
                <div>
                  <span className="text-[10px] text-neutral-400 block font-medium">Sınıf Net Ort.</span>
                  <span className="font-mono text-base font-bold text-primary-700">
                    {cls.averageNet} net
                  </span>
                </div>
                <div>
                  <span className="text-[10px] text-neutral-400 block font-medium">Plan Uyumu</span>
                  <span className="font-mono text-base font-bold text-success">
                    %{cls.averageAdherencePercentage}
                  </span>
                </div>
              </div>

              <div className="space-y-1.5 pt-1">
                <span className="text-[11px] text-neutral-400 font-semibold uppercase tracking-wider block">
                  Zümre Öğretmenleri:
                </span>
                <div className="space-y-1">
                  {cls.assignedTeachers.length === 0 ? (
                    <span className="text-xs text-neutral-400 italic">Öğretmen atanmamış</span>
                  ) : (
                    cls.assignedTeachers.map((t, idx) => (
                      <div key={idx} className="flex items-center gap-1.5 text-xs text-neutral-600">
                        <GraduationCap className="h-3.5 w-3.5 text-neutral-400 flex-shrink-0" />
                        <span className="truncate">{t}</span>
                      </div>
                    ))
                  )}
                </div>
              </div>
            </div>
          ))
        )}
      </div>

      {/* Add Class Modal */}
      <Modal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        title="Yeni Sınıf / Grup Ekle"
      >
        <form onSubmit={handleCreateClass} className="space-y-4">
          <Input
            label="Sınıf Adı"
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="Örn: 12-A Sayısal"
            required
          />

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-medium text-neutral-700 block mb-1">
                Kademe
              </label>
              <select
                value={newGradeLevel}
                onChange={(e) => setNewGradeLevel(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-xl border border-neutral-200 bg-white text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                <option value="9">9. Sınıf</option>
                <option value="10">10. Sınıf</option>
                <option value="11">11. Sınıf</option>
                <option value="12">12. Sınıf</option>
              </select>
            </div>
            <Input
              label="Şube / Alan"
              value={newBranch}
              onChange={(e) => setNewBranch(e.target.value)}
              placeholder="Örn: Sayısal"
            />
          </div>

          {createError && (
            <p className="text-xs text-danger font-medium">{createError}</p>
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button
              type="button"
              variant="secondary"
              onClick={() => setIsAddModalOpen(false)}
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
