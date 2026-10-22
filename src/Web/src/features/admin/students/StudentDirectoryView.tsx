import React, { useState, useMemo, useEffect, useCallback } from 'react';
import {
  Search,
  Filter,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  ChevronRight,
  AlertCircle,
  RefreshCw,
  UserPlus,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { Input } from '@/components/ui/Input';
import { Modal } from '@/components/ui/Modal';
import { adminService, StudentSummaryDto } from '@/services/adminService';
import {
  AdminStudentDirectoryItem,
  AdminStudentDirectoryViewModel,
  AdminStudentFilterState,
  StudentStatus,
} from './types';
import { StudentModal } from './components/StudentModal';

const EMPTY_FILTERS: AdminStudentFilterState = {
  search: '',
  grade: '',
  exam: '',
  field: '',
  status: '',
  adherence: '',
  activity: '',
  coach: '',
  branch: '',
};

const STATUS_CONFIG = {
  takipte: {
    label: 'Takipte',
    icon: <CheckCircle2 className="h-3.5 w-3.5 text-success" />,
    badge: 'bg-success-light text-success-dark border-green-200',
  },
  dikkat: {
    label: 'Dikkat',
    icon: <AlertTriangle className="h-3.5 w-3.5 text-warning" />,
    badge: 'bg-warning-light text-warning-dark border-amber-200',
  },
  kritik: {
    label: 'Kritik',
    icon: <XCircle className="h-3.5 w-3.5 text-danger" />,
    badge: 'bg-danger-light text-danger-dark border-red-200',
  },
};

const ADHERENCE_COLOR = (v: number) =>
  v >= 80
    ? 'text-success font-bold'
    : v >= 50
    ? 'text-warning-dark font-semibold'
    : 'text-danger font-bold';

function mapStudentSummaryToItem(dto: StudentSummaryDto): AdminStudentDirectoryItem {
  const fullName = dto.fullName || `${dto.firstName || ''} ${dto.lastName || ''}`.trim() || 'Öğrenci';
  const initials =
    `${dto.firstName?.[0] || ''}${dto.lastName?.[0] || ''}`.toUpperCase() ||
    fullName
      .split(' ')
      .filter(Boolean)
      .map((p: string) => p[0])
      .join('')
      .toUpperCase()
      .slice(0, 2) ||
    'ÖĞ';

  return {
    id: dto.id,
    name: fullName,
    initials,
    grade: dto.gradeLevel ? `${dto.gradeLevel}. Sınıf` : '12. Sınıf',
    exam: 'YKS',
    field: dto.schoolName || 'Genel Alan',
    status: 'takipte' as StudentStatus,
    planAdherence: 85,
    lastActivity: dto.createdAt ? new Date(dto.createdAt).toLocaleDateString('tr-TR') : 'Aktif',
    lastActivityDays: 0,
    coach: '',
    branch: 'Merkez',
  };
}

export interface StudentDirectoryViewProps {
  initialData?: AdminStudentDirectoryViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const StudentDirectoryView: React.FC<StudentDirectoryViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const [data, setData] = useState<AdminStudentDirectoryViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const [filters, setFilters] = useState<AdminStudentFilterState>(EMPTY_FILTERS);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [selectedStudent, setSelectedStudent] = useState<AdminStudentDirectoryItem | null>(null);

  // New Student Modal state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [newFirstName, setNewFirstName] = useState('');
  const [newLastName, setNewLastName] = useState('');
  const [newEmail, setNewEmail] = useState('');
  const [newPassword, setNewPassword] = useState('Ogrenci@123!');
  const [newGradeLevel, setNewGradeLevel] = useState('12');
  const [newTargetExam, setNewTargetExam] = useState('YKS');
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const fetchStudents = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const res = await adminService.listStudents({ page: 1, pageSize: 100 });
      const items = (res?.items || []).map(mapStudentSummaryToItem);
      setData({ students: items });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Öğrenci listesi alınamadı.';
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
      fetchStudents();
    }
  }, [initialData, fetchStudents]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchStudents();
    }
  };

  const handleCreateStudent = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newFirstName || !newLastName || !newEmail || !newPassword) {
      setCreateError('Lütfen tüm zorunlu alanları doldurunuz.');
      return;
    }

    setIsCreating(true);
    setCreateError(null);

    try {
      // Backend-atomic provisioning: Creates user and student profile transactionally
      await adminService.createStudent({
        firstName: newFirstName.trim(),
        lastName: newLastName.trim(),
        email: newEmail.trim(),
        password: newPassword,
        targetExam: newTargetExam,
        gradeLevel: parseInt(newGradeLevel, 10) || 12,
      });

      // Reset form and refresh list
      setIsAddModalOpen(false);
      setNewFirstName('');
      setNewLastName('');
      setNewEmail('');
      setNewPassword('Ogrenci@123!');
      await fetchStudents();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : 'Öğrenci kaydı oluşturulamadı.');
    } finally {
      setIsCreating(false);
    }
  };

  const activeData = data || initialData || { students: [] };
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  const activeFilterCount = Object.entries(filters).filter(
    ([k, v]) => v !== '' && k !== 'search'
  ).length;

  const setFilter = (key: keyof AdminStudentFilterState, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
  };

  const clearFilters = () => setFilters(EMPTY_FILTERS);

  const filtered = useMemo(() => {
    return activeData.students.filter((s) => {
      if (
        filters.search &&
        !s.name.toLowerCase().includes(filters.search.toLowerCase()) &&
        !s.coach?.toLowerCase().includes(filters.search.toLowerCase())
      ) {
        return false;
      }
      if (filters.grade && s.grade !== filters.grade) return false;
      if (filters.exam && s.exam !== filters.exam) return false;
      if (filters.field && s.field !== filters.field) return false;
      if (filters.status && s.status !== filters.status) return false;
      if (filters.adherence) {
        if (filters.adherence === 'high' && s.planAdherence < 80) return false;
        if (filters.adherence === 'mid' && (s.planAdherence < 50 || s.planAdherence >= 80)) return false;
        if (filters.adherence === 'low' && s.planAdherence >= 50) return false;
      }
      if (filters.activity) {
        if (filters.activity === '1' && s.lastActivityDays > 1) return false;
        if (filters.activity === '3' && s.lastActivityDays > 3) return false;
        if (filters.activity === '7' && s.lastActivityDays > 7) return false;
        if (filters.activity === '7+' && s.lastActivityDays <= 7) return false;
      }
      if (filters.coach && s.coach !== filters.coach) return false;
      if (filters.branch && s.branch !== filters.branch) return false;
      return true;
    });
  }, [activeData.students, filters]);

  const coaches = useMemo(
    () => [...new Set(activeData.students.map((s) => s.coach).filter(Boolean))],
    [activeData.students]
  );
  const branches = useMemo(
    () => [...new Set(activeData.students.map((s) => s.branch).filter(Boolean))],
    [activeData.students]
  );

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none" data-testid="admin-students-loading">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <Skeleton className="h-96 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none" data-testid="admin-students-error">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Öğrenci Listesi Yüklenemedi"
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
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="admin-students-view">
      {/* 1. Header */}
      <div className="flex items-start justify-between">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Öğrenciler
          </h1>
          <p className="text-xs sm:text-sm text-neutral-400 mt-0.5">
            Kurum geneli {filtered.length} kayıtlı öğrenci
          </p>
        </div>
        <Button
          variant="primary"
          size="sm"
          onClick={() => setIsAddModalOpen(true)}
          leftIcon={<UserPlus className="h-4 w-4" />}
        >
          Yeni Öğrenci Ekle
        </Button>
      </div>

      {/* 2. Search + Filter Bar */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Öğrenci veya koç ara..."
            value={filters.search}
            onChange={(e) => setFilter('search', e.target.value)}
            className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-neutral-200 bg-white text-xs sm:text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500 transition-colors shadow-soft-xs"
          />
        </div>
        <button
          type="button"
          onClick={() => setFiltersOpen(!filtersOpen)}
          className={cn(
            'flex items-center gap-2 px-4 py-2.5 rounded-xl border text-xs sm:text-sm font-medium transition-colors shadow-soft-xs',
            filtersOpen || activeFilterCount > 0
              ? 'bg-primary-50 border-primary-300 text-primary-700 font-semibold'
              : 'bg-white border-neutral-200 text-neutral-700 hover:bg-neutral-50'
          )}
        >
          <Filter className="h-4 w-4" />
          <span>Filtrele</span>
          {activeFilterCount > 0 && (
            <span className="w-4 h-4 rounded-full bg-primary-600 text-white text-[10px] flex items-center justify-center font-bold">
              {activeFilterCount}
            </span>
          )}
        </button>
        {activeFilterCount > 0 && (
          <Button
            variant="ghost"
            size="sm"
            onClick={clearFilters}
            className="text-xs text-neutral-400 hover:text-neutral-600"
          >
            Filtreleri Temizle
          </Button>
        )}
      </div>

      {/* Filter Drawer */}
      {filtersOpen && (
        <div className="bg-surface-alt rounded-2xl border border-neutral-200 p-4 space-y-3 animate-fade-in shadow-soft-xs">
          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3">
            <div>
              <label className="text-[11px] font-semibold text-neutral-600 uppercase tracking-wider block mb-1">
                Sınıf Kademesi
              </label>
              <select
                value={filters.grade}
                onChange={(e) => setFilter('grade', e.target.value)}
                className="w-full px-3 py-1.5 rounded-lg border border-neutral-200 bg-white text-xs text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                <option value="">Tümü</option>
                <option value="9. Sınıf">9. Sınıf</option>
                <option value="10. Sınıf">10. Sınıf</option>
                <option value="11. Sınıf">11. Sınıf</option>
                <option value="12. Sınıf">12. Sınıf</option>
              </select>
            </div>
            <div>
              <label className="text-[11px] font-semibold text-neutral-600 uppercase tracking-wider block mb-1">
                Durum
              </label>
              <select
                value={filters.status}
                onChange={(e) => setFilter('status', e.target.value)}
                className="w-full px-3 py-1.5 rounded-lg border border-neutral-200 bg-white text-xs text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                <option value="">Tümü</option>
                <option value="takipte">Takipte</option>
                <option value="dikkat">Dikkat</option>
                <option value="kritik">Kritik</option>
              </select>
            </div>
            <div>
              <label className="text-[11px] font-semibold text-neutral-600 uppercase tracking-wider block mb-1">
                Atanmış Koç
              </label>
              <select
                value={filters.coach}
                onChange={(e) => setFilter('coach', e.target.value)}
                className="w-full px-3 py-1.5 rounded-lg border border-neutral-200 bg-white text-xs text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                <option value="">Tüm Koçlar</option>
                {coaches.map((c) => (
                  <option key={c} value={c}>
                    {c}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="text-[11px] font-semibold text-neutral-600 uppercase tracking-wider block mb-1">
                Şube
              </label>
              <select
                value={filters.branch}
                onChange={(e) => setFilter('branch', e.target.value)}
                className="w-full px-3 py-1.5 rounded-lg border border-neutral-200 bg-white text-xs text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500"
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
        </div>
      )}

      {/* Table */}
      <div className="bg-white rounded-2xl border border-neutral-200 overflow-hidden shadow-soft-xs">
        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse text-xs sm:text-sm">
            <thead>
              <tr className="border-b border-neutral-100 bg-neutral-50/50 text-[11px] font-bold text-neutral-500 uppercase tracking-wider">
                <th className="py-3 px-4">Öğrenci</th>
                <th className="py-3 px-4">Kademe & Alan</th>
                <th className="py-3 px-4">Durum</th>
                <th className="py-3 px-4">Plan Uyumu</th>
                <th className="py-3 px-4">Son Aktivite</th>
                <th className="py-3 px-4 text-right">İşlem</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-neutral-100">
              {filtered.length === 0 ? (
                <tr>
                  <td colSpan={6} className="py-8 text-center text-neutral-400">
                    Kayıtlı öğrenci bulunamadı.
                  </td>
                </tr>
              ) : (
                filtered.map((student) => {
                  const status = STATUS_CONFIG[student.status] || STATUS_CONFIG.takipte;
                  return (
                    <tr
                      key={student.id}
                      onClick={() => setSelectedStudent(student)}
                      className="hover:bg-neutral-50/80 transition-colors cursor-pointer group"
                    >
                      <td className="py-3.5 px-4">
                        <div className="flex items-center gap-3">
                          <div className="w-8 h-8 rounded-full bg-navy-100 text-navy-800 text-xs font-bold flex items-center justify-center flex-shrink-0">
                            {student.initials}
                          </div>
                          <div>
                            <p className="font-semibold text-neutral-900 group-hover:text-primary-700 transition-colors">
                              {student.name}
                            </p>
                            <p className="text-[11px] text-neutral-400">
                              {student.coach ? `Koç: ${student.coach}` : 'Koç atanmamış'}
                            </p>
                          </div>
                        </div>
                      </td>
                      <td className="py-3.5 px-4 text-neutral-600">
                        {student.grade} · {student.field}
                      </td>
                      <td className="py-3.5 px-4">
                        <span
                          className={cn(
                            'inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-semibold border',
                            status.badge
                          )}
                        >
                          {status.icon}
                          {status.label}
                        </span>
                      </td>
                      <td className="py-3.5 px-4">
                        <span className={cn('font-mono', ADHERENCE_COLOR(student.planAdherence))}>
                          %{student.planAdherence}
                        </span>
                      </td>
                      <td className="py-3.5 px-4 text-neutral-500">{student.lastActivity}</td>
                      <td className="py-3.5 px-4 text-right">
                        <ChevronRight className="h-4 w-4 text-neutral-300 inline group-hover:text-primary-600 transition-colors" />
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Student Detail Modal */}
      {selectedStudent && (
        <StudentModal
          student={selectedStudent}
          onClose={() => setSelectedStudent(null)}
          onStudentUpdated={fetchStudents}
        />
      )}

      {/* Add New Student Modal */}
      <Modal
        isOpen={isAddModalOpen}
        onClose={() => {
          setIsAddModalOpen(false);
          setCreateError(null);
        }}
        title="Yeni Öğrenci Ekle"
      >
        <form onSubmit={handleCreateStudent} className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <Input
              label="Ad"
              value={newFirstName}
              onChange={(e) => setNewFirstName(e.target.value)}
              placeholder="Örn: Ayşe"
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
            placeholder="ogrenci@okul.com"
            required
          />

          <Input
            label="Başlangıç Şifresi"
            type="password"
            value={newPassword}
            onChange={(e) => setNewPassword(e.target.value)}
            required
          />

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-medium text-neutral-700 block mb-1">
                Sınıf Kademesi
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
            <div>
              <label className="text-xs font-medium text-neutral-700 block mb-1">
                Hedef Sınav
              </label>
              <select
                value={newTargetExam}
                onChange={(e) => setNewTargetExam(e.target.value)}
                className="w-full px-3 py-2 text-sm rounded-xl border border-neutral-200 bg-white text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                <option value="YKS">YKS (TYT/AYT)</option>
                <option value="LGS">LGS</option>
                <option value="TYT">TYT</option>
                <option value="AYT">AYT</option>
              </select>
            </div>
          </div>

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
