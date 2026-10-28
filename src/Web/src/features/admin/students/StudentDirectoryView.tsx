import React, { useState, useMemo, useEffect, useCallback } from 'react';
import {
  Search,
  CheckCircle2,
  ChevronRight,
  ChevronLeft,
  AlertCircle,
  RefreshCw,
  UserPlus,
  GraduationCap,
} from 'lucide-react';
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
} from './types';
import { StudentModal } from './components/StudentModal';

const EMPTY_FILTERS: AdminStudentFilterState = {
  search: '',
  grade: '',
  status: '',
};

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
    gradeLevel: dto.gradeLevel,
    exam: 'YKS',
    schoolName: dto.schoolName,
    studentNumber: dto.studentNumber,
    status: dto.status || 'Active',
    enrollmentDate: dto.enrollmentDate,
    createdAt: dto.createdAt,
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

  // Pagination & Filters
  const [page, setPage] = useState<number>(1);
  const pageSize = 100;
  const [totalCount, setTotalCount] = useState<number>(0);
  const [totalPages, setTotalPages] = useState<number>(1);

  const [filters, setFilters] = useState<AdminStudentFilterState>(EMPTY_FILTERS);
  const [selectedStudent, setSelectedStudent] = useState<AdminStudentDirectoryItem | null>(null);

  // New Student Modal state
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [newFirstName, setNewFirstName] = useState('');
  const [newLastName, setNewLastName] = useState('');
  const [newEmail, setNewEmail] = useState('');
  const [newPassword, setNewPassword] = useState('Ogrenci@123!');
  const [newGradeLevel, setNewGradeLevel] = useState('12');
  const [newSchoolName, setNewSchoolName] = useState('');
  const [newStudentNumber, setNewStudentNumber] = useState('');
  const [newTargetExam] = useState('YKS');
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  const fetchStudents = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const res = await adminService.listStudents({
        page,
        pageSize,
        searchTerm: filters.search.trim() || undefined,
      });

      const items = (res?.items || []).map(mapStudentSummaryToItem);
      setData({ students: items });
      setTotalCount(res?.totalCount || items.length);
      setTotalPages(res?.totalPages || 1);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Öğrenci listesi alınamadı.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [initialData, page, filters.search]);

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
    if (!newFirstName.trim() || !newLastName.trim()) {
      setCreateError('Lütfen ad ve soyad alanlarını doldurunuz.');
      return;
    }

    setIsCreating(true);
    setCreateError(null);

    try {
      await adminService.createStudent({
        firstName: newFirstName.trim(),
        lastName: newLastName.trim(),
        email: newEmail.trim() || undefined,
        password: newPassword || undefined,
        gradeLevel: parseInt(newGradeLevel, 10) || 12,
        schoolName: newSchoolName.trim() || undefined,
        studentNumber: newStudentNumber.trim() || undefined,
        targetExam: newTargetExam || 'YKS',
      });

      setIsAddModalOpen(false);
      setNewFirstName('');
      setNewLastName('');
      setNewEmail('');
      setNewSchoolName('');
      setNewStudentNumber('');
      setPage(1);
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

  const filteredStudents = useMemo(() => {
    return activeData.students.filter((s) => {
      if (filters.grade && s.gradeLevel !== parseInt(filters.grade, 10)) {
        return false;
      }
      if (filters.status && s.status.toLowerCase() !== filters.status.toLowerCase()) {
        return false;
      }
      return true;
    });
  }, [activeData.students, filters.grade, filters.status]);

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="admin-students-loading">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-12 w-full rounded-2xl" />
        <div className="space-y-2">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-16 w-full rounded-2xl" />
          ))}
        </div>
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none p-4" data-testid="admin-students-error">
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
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="admin-students-view">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-neutral-200/70 pb-4">
        <div>
          <div className="flex items-center gap-2.5 flex-wrap">
            <h1 className="font-serif text-2xl sm:text-3xl font-bold text-navy-950 tracking-tight leading-tight">
              Öğrenciler
            </h1>
            <span className="text-xs font-bold text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-full border border-primary-100">
              {filteredStudents.length !== (totalCount || activeData.students.length)
                ? `${filteredStudents.length} / ${totalCount || activeData.students.length} Kayıtlı Öğrenci`
                : `${totalCount || activeData.students.length} Kayıtlı Öğrenci`}
            </span>
          </div>
          <p className="text-xs sm:text-sm text-neutral-500 mt-1">
            Kuruma kayıtlı öğrencilerin akademik profil, koçluk ataması ve kademe takibi
          </p>
        </div>

        <Button
          variant="primary"
          size="sm"
          onClick={() => setIsAddModalOpen(true)}
          leftIcon={<UserPlus className="h-4 w-4" />}
          className="bg-[#315FE8] hover:bg-blue-700 rounded-xl shadow-soft-xs font-semibold w-full md:w-auto shrink-0 py-2.5 px-4"
        >
          Yeni Öğrenci Ekle
        </Button>
      </div>

      {/* 2. Filters & Search Bar */}
      <div className="bg-white p-3.5 sm:p-4 rounded-2xl border border-neutral-200/80 shadow-soft-xs flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3">
        <div className="relative flex-1 w-full min-w-0 md:max-w-md">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Öğrenci adı, okul veya numara ara..."
            value={filters.search}
            onChange={(e) => setFilters((prev) => ({ ...prev, search: e.target.value }))}
            className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-neutral-200 bg-neutral-50/60 text-xs sm:text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all font-medium"
          />
        </div>

        <div className="flex items-center gap-2.5 w-full md:w-auto overflow-x-auto">
          <select
            value={filters.grade}
            onChange={(e) => setFilters((prev) => ({ ...prev, grade: e.target.value }))}
            className="flex-1 md:flex-none px-3.5 py-2.5 rounded-xl border border-neutral-200 bg-neutral-50/60 text-xs sm:text-sm font-medium text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all cursor-pointer whitespace-nowrap"
          >
            <option value="">Tüm Kademeler</option>
            <option value="9">9. Sınıf</option>
            <option value="10">10. Sınıf</option>
            <option value="11">11. Sınıf</option>
            <option value="12">12. Sınıf</option>
          </select>

          <select
            value={filters.status}
            onChange={(e) => setFilters((prev) => ({ ...prev, status: e.target.value }))}
            className="flex-1 md:flex-none px-3.5 py-2.5 rounded-xl border border-neutral-200 bg-neutral-50/60 text-xs sm:text-sm font-medium text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all cursor-pointer whitespace-nowrap"
          >
            <option value="">Tüm Durumlar</option>
            <option value="Active">Aktif Kayıt</option>
            <option value="Inactive">Pasif</option>
          </select>
        </div>
      </div>

      {/* 3. Students Directory List Table */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 shadow-soft-xs overflow-hidden">
        {filteredStudents.length === 0 ? (
          <div className="py-16 text-center text-neutral-400 p-4">
            <GraduationCap className="h-10 w-10 mx-auto text-neutral-300 mb-2" />
            <p className="text-sm font-semibold text-neutral-700">Öğrenci bulunamadı.</p>
            <p className="text-xs text-neutral-400 mt-1">
              Filtrelerinizi sıfırlayabilir veya kuruma yeni öğrenci kaydedebilirsiniz.
            </p>
          </div>
        ) : (
          <div className="divide-y divide-neutral-100">
            {filteredStudents.map((student) => (
              <div
                key={student.id}
                onClick={() => setSelectedStudent(student)}
                className="p-4 sm:px-5 flex items-center justify-between gap-4 hover:bg-[#F8F7F3]/70 transition-colors cursor-pointer group select-none"
              >
                <div className="flex items-center gap-3.5 min-w-0">
                  <div className="w-10 h-10 rounded-2xl bg-[#14233E] text-white text-xs font-bold flex items-center justify-center shrink-0 shadow-soft-xs group-hover:scale-105 transition-transform">
                    {student.initials}
                  </div>

                  <div className="min-w-0 flex-1">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="text-sm font-bold text-navy-950 group-hover:text-primary-600 transition-colors">
                        {student.name}
                      </span>
                      {student.studentNumber && (
                        <span className="text-xs font-mono font-medium text-neutral-500 bg-neutral-100 px-2 py-0.5 rounded-md">
                          #{student.studentNumber}
                        </span>
                      )}
                      <span className="inline-flex items-center gap-1 text-[11px] font-bold text-emerald-700 bg-emerald-50 px-2 py-0.5 rounded-full border border-emerald-200">
                        <CheckCircle2 className="h-3 w-3" />
                        {student.status === 'Active' ? 'Aktif' : student.status}
                      </span>
                    </div>

                    <div className="flex items-center gap-3 text-xs text-neutral-500 mt-1 flex-wrap">
                      <span className="font-semibold text-neutral-700">{student.grade}</span>
                      <span>·</span>
                      <span>{student.schoolName || 'Kurum İçi'}</span>
                      {student.createdAt && (
                        <>
                          <span className="hidden md:inline">·</span>
                          <span className="hidden md:inline text-neutral-400">
                            Kayıt: {new Date(student.createdAt).toLocaleDateString('tr-TR')}
                          </span>
                        </>
                      )}
                    </div>
                  </div>
                </div>

                <div className="flex items-center gap-3 shrink-0">
                  <span className="text-xs font-semibold text-primary-700 hidden sm:inline-block">
                    Detay Gör
                  </span>
                  <ChevronRight className="h-4 w-4 text-neutral-400 group-hover:text-primary-600 group-hover:translate-x-0.5 transition-all" />
                </div>
              </div>
            ))}
          </div>
        )}

        {/* 4. Pagination */}
        {totalPages > 1 && (
          <div className="p-3.5 border-t border-neutral-100 bg-neutral-50/60 flex items-center justify-between text-xs text-neutral-600">
            <span>
              Sayfa <strong>{page}</strong> / <strong>{totalPages}</strong> ({totalCount} toplam öğrenci)
            </span>
            <div className="flex items-center gap-1.5">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                disabled={page <= 1}
                className="h-8 px-2.5 text-xs rounded-lg"
              >
                <ChevronLeft className="h-3.5 w-3.5 mr-1" /> Önceki
              </Button>
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                disabled={page >= totalPages}
                className="h-8 px-2.5 text-xs rounded-lg"
              >
                Sonraki <ChevronRight className="h-3.5 w-3.5 ml-1" />
              </Button>
            </div>
          </div>
        )}
      </div>

      {/* New Student Registration Modal */}
      <Modal
        isOpen={isAddModalOpen}
        onClose={() => setIsAddModalOpen(false)}
        title="Yeni Öğrenci Kaydı"
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

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">
                Sınıf Düzeyi
              </label>
              <select
                value={newGradeLevel}
                onChange={(e) => setNewGradeLevel(e.target.value)}
                className="w-full px-3 py-2 text-xs sm:text-sm rounded-xl border border-neutral-200 bg-white text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500 font-medium"
              >
                <option value="9">9. Sınıf</option>
                <option value="10">10. Sınıf</option>
                <option value="11">11. Sınıf</option>
                <option value="12">12. Sınıf</option>
              </select>
            </div>
            <Input
              label="Okul Numarası"
              value={newStudentNumber}
              onChange={(e) => setNewStudentNumber(e.target.value)}
              placeholder="Örn: 1205"
            />
          </div>

          <Input
            label="Mevcut Okulu"
            value={newSchoolName}
            onChange={(e) => setNewSchoolName(e.target.value)}
            placeholder="Örn: Atatürk Anadolu Lisesi"
          />

          <div className="grid grid-cols-2 gap-3">
            <Input
              label="E-Posta (İsteğe bağlı)"
              type="email"
              value={newEmail}
              onChange={(e) => setNewEmail(e.target.value)}
              placeholder="ogrenci@okul.com"
            />
            <Input
              label="Başlangıç Şifresi"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="Ogrenci@123!"
            />
          </div>

          {createError && (
            <p className="text-xs text-rose-700 bg-rose-50 border border-rose-200 p-2.5 rounded-xl font-medium">
              {createError}
            </p>
          )}

          <div className="flex justify-end gap-2 pt-2 border-t border-neutral-100">
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
              className="bg-[#315FE8] hover:bg-blue-700 font-semibold"
            >
              {isCreating ? 'Kaydediliyor...' : 'Kaydet'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Student Detail Modal */}
      {selectedStudent && (
        <StudentModal
          student={selectedStudent}
          onClose={() => setSelectedStudent(null)}
          onStudentUpdated={fetchStudents}
        />
      )}
    </div>
  );
};

export default StudentDirectoryView;
