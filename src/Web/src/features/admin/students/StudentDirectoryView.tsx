import React, { useState, useMemo } from 'react';
import {
  Search,
  Filter,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  ChevronRight,
  X,
  ChevronDown,
  AlertCircle,
  RefreshCw,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { initialAdminStudentsData } from './mockData';
import {
  AdminStudentDirectoryItem,
  AdminStudentDirectoryViewModel,
  AdminStudentFilterState,
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

export interface StudentDirectoryViewProps {
  initialData?: AdminStudentDirectoryViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const StudentDirectoryView: React.FC<StudentDirectoryViewProps> = ({
  initialData = initialAdminStudentsData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const [filters, setFilters] = useState<AdminStudentFilterState>(EMPTY_FILTERS);
  const [filtersOpen, setFiltersOpen] = useState(false);
  const [selectedStudent, setSelectedStudent] = useState<AdminStudentDirectoryItem | null>(null);

  const activeFilterCount = Object.entries(filters).filter(
    ([k, v]) => v !== '' && k !== 'search'
  ).length;

  const setFilter = (key: keyof AdminStudentFilterState, value: string) => {
    setFilters((prev) => ({ ...prev, [key]: value }));
  };

  const clearFilters = () => setFilters(EMPTY_FILTERS);

  const filtered = useMemo(() => {
    return initialData.students.filter((s) => {
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
  }, [initialData.students, filters]);

  const coaches = useMemo(
    () => [...new Set(initialData.students.map((s) => s.coach).filter(Boolean))],
    [initialData.students]
  );
  const branches = useMemo(
    () => [...new Set(initialData.students.map((s) => s.branch).filter(Boolean))],
    [initialData.students]
  );

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <Skeleton className="h-96 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Öğrenci Listesi Yüklenemedi"
        >
          {errorMessage}
        </Alert>
        {onRetry && (
          <div className="text-center pt-2">
            <Button
              variant="primary"
              size="sm"
              onClick={onRetry}
              leftIcon={<RefreshCw className="h-4 w-4" />}
            >
              Tekrar Dene
            </Button>
          </div>
        )}
      </div>
    );
  }

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6">
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
          <button
            type="button"
            onClick={clearFilters}
            className="flex items-center gap-1 text-xs text-neutral-500 hover:text-neutral-800 transition-colors py-1 px-2"
          >
            <X className="h-3.5 w-3.5" /> Temizle
          </button>
        )}
      </div>

      {/* 3. Filter Panel */}
      {filtersOpen && (
        <div className="bg-white rounded-2xl border border-neutral-200 p-4 grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-3 shadow-soft-xs">
          <FilterSelect
            label="Sınıf"
            value={filters.grade}
            onChange={(v) => setFilter('grade', v)}
            options={[
              { value: '9', label: '9. Sınıf' },
              { value: '10', label: '10. Sınıf' },
              { value: '11', label: '11. Sınıf' },
              { value: '12', label: '12. Sınıf' },
              { value: 'Mezun', label: 'Mezun' },
            ]}
          />
          <FilterSelect
            label="Sınav Tipi"
            value={filters.exam}
            onChange={(v) => setFilter('exam', v)}
            options={[
              { value: 'TYT', label: 'TYT' },
              { value: 'AYT', label: 'AYT' },
            ]}
          />
          <FilterSelect
            label="Alan"
            value={filters.field}
            onChange={(v) => setFilter('field', v)}
            options={[
              { value: 'Sayısal', label: 'Sayısal' },
              { value: 'Sözel', label: 'Sözel' },
              { value: 'EA', label: 'EA' },
              { value: 'Dil', label: 'Dil' },
            ]}
          />
          <FilterSelect
            label="Durum"
            value={filters.status}
            onChange={(v) => setFilter('status', v)}
            options={[
              { value: 'takipte', label: 'Takipte' },
              { value: 'dikkat', label: 'Dikkat' },
              { value: 'kritik', label: 'Kritik' },
            ]}
          />
          <FilterSelect
            label="Plan Uyumu"
            value={filters.adherence}
            onChange={(v) => setFilter('adherence', v)}
            options={[
              { value: 'high', label: 'Yüksek (≥%80)' },
              { value: 'mid', label: 'Orta (%50–79)' },
              { value: 'low', label: 'Düşük (<%50)' },
            ]}
          />
          <FilterSelect
            label="Son Aktivite"
            value={filters.activity}
            onChange={(v) => setFilter('activity', v)}
            options={[
              { value: '1', label: 'Son 1 gün' },
              { value: '3', label: 'Son 3 gün' },
              { value: '7', label: 'Son 7 gün' },
              { value: '7+', label: '7+ gün önce' },
            ]}
          />
          <FilterSelect
            label="Koç"
            value={filters.coach}
            onChange={(v) => setFilter('coach', v)}
            options={coaches.map((c) => ({ value: c, label: c }))}
          />
          <FilterSelect
            label="Şube"
            value={filters.branch}
            onChange={(v) => setFilter('branch', v)}
            options={branches.map((b) => ({ value: b, label: b }))}
          />
        </div>
      )}

      {/* 4. Active Filter Chips */}
      {activeFilterCount > 0 && (
        <div className="flex flex-wrap gap-2">
          {Object.entries(filters)
            .filter(([k, v]) => v !== '' && k !== 'search')
            .map(([key, value]) => (
              <button
                key={key}
                type="button"
                onClick={() => setFilter(key as keyof AdminStudentFilterState, '')}
                className="flex items-center gap-1.5 text-xs font-semibold bg-primary-50 text-primary-700 border border-primary-200 px-2.5 py-1 rounded-full hover:bg-primary-100 transition-colors"
              >
                <span>{value}</span>
                <X className="h-3 w-3" />
              </button>
            ))}
        </div>
      )}

      {/* 5. Desktop Table */}
      <div className="hidden md:block bg-white rounded-2xl border border-neutral-100 overflow-hidden shadow-soft-sm">
        <table className="w-full">
          <thead>
            <tr className="bg-neutral-50 border-b border-neutral-100">
              <th className="text-left px-4 py-3 text-xs font-semibold text-neutral-500 uppercase tracking-wide">
                Öğrenci
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-neutral-500 uppercase tracking-wide">
                Sınıf / Alan
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-neutral-500 uppercase tracking-wide">
                Durum
              </th>
              <th className="text-right px-4 py-3 text-xs font-semibold text-neutral-500 uppercase tracking-wide">
                Plan Uyumu
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-neutral-500 uppercase tracking-wide">
                Son Aktivite
              </th>
              <th className="text-left px-4 py-3 text-xs font-semibold text-neutral-500 uppercase tracking-wide">
                Koç / Şube
              </th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-neutral-50">
            {filtered.map((student) => {
              const sc = STATUS_CONFIG[student.status];

              return (
                <tr
                  key={student.id}
                  onClick={() => setSelectedStudent(student)}
                  className="hover:bg-neutral-50/80 cursor-pointer transition-colors group"
                >
                  <td className="px-4 py-3.5">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8 rounded-xl bg-navy-50 text-navy-800 text-xs font-bold flex items-center justify-center flex-shrink-0">
                        {student.initials}
                      </div>
                      <span className="text-sm font-semibold text-neutral-900 group-hover:text-primary-700 transition-colors">
                        {student.name}
                      </span>
                    </div>
                  </td>
                  <td className="px-4 py-3.5">
                    <span className="text-sm text-neutral-700 font-medium">
                      {student.grade}. Sınıf
                    </span>
                    <span className="text-xs text-neutral-400 ml-1.5">
                      {student.exam} · {student.field}
                    </span>
                  </td>
                  <td className="px-4 py-3.5">
                    <span
                      className={cn(
                        'inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold border',
                        sc.badge
                      )}
                    >
                      {sc.icon}
                      {sc.label}
                    </span>
                  </td>
                  <td className="px-4 py-3.5 text-right">
                    <span className={cn('font-mono text-sm', ADHERENCE_COLOR(student.planAdherence))}>
                      %{student.planAdherence}
                    </span>
                  </td>
                  <td className="px-4 py-3.5">
                    <span
                      className={cn(
                        'text-sm font-medium',
                        student.lastActivityDays >= 5
                          ? 'text-danger'
                          : student.lastActivityDays >= 3
                          ? 'text-warning-dark'
                          : 'text-neutral-500'
                      )}
                    >
                      {student.lastActivity}
                    </span>
                  </td>
                  <td className="px-4 py-3.5">
                    <span className="text-xs font-medium text-neutral-700">{student.coach}</span>
                    <span className="text-[11px] text-neutral-400 block">{student.branch} Şubesi</span>
                  </td>
                  <td className="px-4 py-3.5 text-right">
                    <ChevronRight className="h-4 w-4 text-neutral-300 group-hover:text-neutral-600 transition-colors ml-auto" />
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>

        {filtered.length === 0 && (
          <div className="py-16 text-center">
            <Search className="h-8 w-8 text-neutral-300 mx-auto mb-3" />
            <p className="text-sm font-semibold text-neutral-600">Bu filtrelerle eşleşen öğrenci bulunamadı</p>
            <p className="text-xs text-neutral-400 mt-1">Filtre kriterlerini temizlemeyi deneyin</p>
            <button
              type="button"
              onClick={clearFilters}
              className="mt-3 text-xs text-primary-600 font-semibold hover:underline"
            >
              Tüm filtreleri temizle
            </button>
          </div>
        )}
      </div>

      {/* 6. Mobile Card List */}
      <div className="md:hidden space-y-2.5">
        {filtered.map((student) => {
          const sc = STATUS_CONFIG[student.status];

          return (
            <button
              key={student.id}
              type="button"
              onClick={() => setSelectedStudent(student)}
              className="w-full text-left bg-white rounded-2xl border border-neutral-100 p-4 hover:border-neutral-200 transition-colors shadow-soft-xs"
            >
              <div className="flex items-center gap-3 mb-2">
                <div className="w-9 h-9 rounded-xl bg-navy-50 text-navy-800 text-xs font-bold flex items-center justify-center flex-shrink-0">
                  {student.initials}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold text-neutral-900 truncate">{student.name}</p>
                  <p className="text-xs text-neutral-400">
                    {student.grade}. Sınıf · {student.exam} · {student.field}
                  </p>
                </div>
                <span
                  className={cn(
                    'inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold border flex-shrink-0',
                    sc.badge
                  )}
                >
                  {sc.icon}
                  {sc.label}
                </span>
              </div>
              <div className="flex items-center justify-between text-xs text-neutral-500 pt-1 border-t border-neutral-50">
                <span>
                  Plan:{' '}
                  <strong className={cn('font-mono font-bold', ADHERENCE_COLOR(student.planAdherence))}>
                    %{student.planAdherence}
                  </strong>
                </span>
                <span>Son: {student.lastActivity}</span>
                {student.coach && <span>Koç: {student.coach}</span>}
              </div>
            </button>
          );
        })}

        {filtered.length === 0 && (
          <div className="py-12 text-center bg-white rounded-2xl border border-neutral-100">
            <Search className="h-6 w-6 text-neutral-300 mx-auto mb-2" />
            <p className="text-sm text-neutral-500 font-medium">Eşleşen öğrenci bulunamadı</p>
          </div>
        )}
      </div>

      {/* 7. Student Detail Modal */}
      {selectedStudent && (
        <StudentModal
          student={selectedStudent}
          onClose={() => setSelectedStudent(null)}
        />
      )}
    </div>
  );
};

function FilterSelect({
  label,
  value,
  onChange,
  options,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  options: { value: string; label: string }[];
}) {
  return (
    <div className="relative">
      <label className="block text-xs font-semibold text-neutral-600 mb-1">{label}</label>
      <div className="relative">
        <select
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className={cn(
            'w-full appearance-none pl-3 pr-8 py-2 rounded-xl border text-xs sm:text-sm bg-white transition-colors',
            'focus:outline-none focus:ring-2 focus:ring-primary-500',
            value ? 'border-primary-300 text-primary-800 font-semibold' : 'border-neutral-200 text-neutral-700'
          )}
        >
          <option value="">Tümü</option>
          {options.map((o) => (
            <option key={o.value} value={o.value}>
              {o.label}
            </option>
          ))}
        </select>
        <ChevronDown className="h-3.5 w-3.5 absolute right-2.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
      </div>
    </div>
  );
}
