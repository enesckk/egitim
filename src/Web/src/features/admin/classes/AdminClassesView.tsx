import React, { useState, useMemo, useEffect, useCallback } from 'react';
import {
  Search,
  GraduationCap,
  Users,
  AlertCircle,
  RefreshCw,
  Plus,
  Building2,
  Calendar,
  UserMinus,
  CheckCircle2,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import {
  adminService,
  ClassroomDto,
  ClassroomDetailDto,
  TeacherListDto,
  StudentSummaryDto,
} from '@/services/adminService';
import { AdminClassDirectoryItem, AdminClassesViewModel } from './types';

function mapClassroomToItem(c: ClassroomDto): AdminClassDirectoryItem {
  return {
    id: c.id,
    name: c.name,
    grade: c.gradeLevel ? `${c.gradeLevel}. Sınıf` : '12. Sınıf',
    gradeLevel: c.gradeLevel,
    branch: c.branch || 'Merkez',
    field: c.branch || 'Genel Şube',
    academicYear: c.academicYear || '2026-2027',
    studentCount: c.studentCount || 0,
    teacherCount: c.teacherCount || (c.teachers ? c.teachers.length : 0),
    teachers: (c.teachers || []).map((t) => ({
      teacherId: t.teacherId,
      fullName: t.fullName,
      title: t.title,
      isPrimary: t.isPrimary,
    })),
    assignedTeachers: (c.teachers || []).map(
      (t) => `${t.fullName}${t.title ? ` (${t.title})` : ''}`
    ),
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
  const [newAcademicYear, setNewAcademicYear] = useState('2026-2027');
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  // Manage Roster / Teachers Modal State
  const [activeClassDetail, setActiveClassDetail] = useState<ClassroomDetailDto | null>(null);
  const [, setIsLoadingDetail] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);
  const [detailSuccess, setDetailSuccess] = useState<string | null>(null);

  // Available students & teachers for assignment
  const [allTeachers, setAllTeachers] = useState<TeacherListDto[]>([]);
  const [allStudents, setAllStudents] = useState<StudentSummaryDto[]>([]);
  const [selectedTeacherId, setSelectedTeacherId] = useState('');
  const [isTeacherPrimary, setIsTeacherPrimary] = useState(false);
  const [selectedStudentId, setSelectedStudentId] = useState('');
  const [isAssigningTeacher, setIsAssigningTeacher] = useState(false);
  const [isAssigningStudent, setIsAssigningStudent] = useState(false);

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
        academicYear: newAcademicYear.trim() || '2026-2027',
      });

      setIsAddModalOpen(false);
      setNewName('');
      setNewBranch('Merkez');
      await fetchClasses();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : 'Sınıf oluşturulamadı.');
    } finally {
      setIsCreating(false);
    }
  };

  // Open Manage Class Modal
  const handleOpenClassDetail = async (classId: string) => {
    setIsLoadingDetail(true);
    setDetailError(null);
    setDetailSuccess(null);
    setSelectedTeacherId('');
    setSelectedStudentId('');

    try {
      const [detail, teachersRes, studentsRes] = await Promise.all([
        adminService.getClassroom(classId),
        adminService.listTeachers({ page: 1, pageSize: 100 }),
        adminService.listStudents({ page: 1, pageSize: 150 }),
      ]);
      setActiveClassDetail(detail);
      setAllTeachers(teachersRes?.items || []);
      setAllStudents(studentsRes?.items || []);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Sınıf detayları yüklenemedi.';
      setDetailError(msg);
    } finally {
      setIsLoadingDetail(false);
    }
  };

  const handleAssignTeacher = async () => {
    if (!activeClassDetail || !selectedTeacherId || isAssigningTeacher) return;
    setIsAssigningTeacher(true);
    setDetailError(null);
    try {
      await adminService.assignTeacherToClassroom(
        activeClassDetail.id,
        selectedTeacherId,
        isTeacherPrimary
      );
      setDetailSuccess('Öğretmen sınıfa başarıyla atandı.');
      const updated = await adminService.getClassroom(activeClassDetail.id);
      setActiveClassDetail(updated);
      setSelectedTeacherId('');
      await fetchClasses();
    } catch (err: unknown) {
      setDetailError(err instanceof Error ? err.message : 'Öğretmen atanamadı.');
    } finally {
      setIsAssigningTeacher(false);
    }
  };

  const handleRemoveTeacher = async (teacherId: string) => {
    if (!activeClassDetail) return;
    setDetailError(null);
    try {
      await adminService.removeTeacherFromClassroom(activeClassDetail.id, teacherId);
      setDetailSuccess('Öğretmen sınıf atamasından kaldırıldı.');
      const updated = await adminService.getClassroom(activeClassDetail.id);
      setActiveClassDetail(updated);
      await fetchClasses();
    } catch (err: unknown) {
      setDetailError(err instanceof Error ? err.message : 'Öğretmen kaldırılamadı.');
    }
  };

  const handleAssignStudent = async () => {
    if (!activeClassDetail || !selectedStudentId || isAssigningStudent) return;
    setIsAssigningStudent(true);
    setDetailError(null);
    try {
      await adminService.assignStudentToClassroom(activeClassDetail.id, selectedStudentId);
      setDetailSuccess('Öğrenci sınıfa başarıyla eklendi.');
      const updated = await adminService.getClassroom(activeClassDetail.id);
      setActiveClassDetail(updated);
      setSelectedStudentId('');
      await fetchClasses();
    } catch (err: unknown) {
      setDetailError(err instanceof Error ? err.message : 'Öğrenci sınıfa eklenemedi.');
    } finally {
      setIsAssigningStudent(false);
    }
  };

  const handleRemoveStudent = async (studentId: string) => {
    if (!activeClassDetail) return;
    setDetailError(null);
    try {
      await adminService.removeStudentFromClassroom(activeClassDetail.id, studentId);
      setDetailSuccess('Öğrenci sınıftan çıkarıldı.');
      const updated = await adminService.getClassroom(activeClassDetail.id);
      setActiveClassDetail(updated);
      await fetchClasses();
    } catch (err: unknown) {
      setDetailError(err instanceof Error ? err.message : 'Öğrenci sınıftan çıkarılamadı.');
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
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="admin-classes-loading">
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
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none p-4" data-testid="admin-classes-error">
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

  // Available students not yet in active classroom
  const availableStudents = activeClassDetail
    ? allStudents.filter(
        (s) => !(activeClassDetail.students || []).some((cs) => cs.studentId === s.id)
      )
    : [];

  // Available teachers not yet in active classroom
  const availableTeachers = activeClassDetail
    ? allTeachers.filter(
        (t) => !(activeClassDetail.teachers || []).some((ct) => ct.teacherId === t.id)
      )
    : [];

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="admin-classes-view">
      {/* 1. Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-neutral-200/70 pb-4">
        <div>
          <div className="flex items-center gap-2.5">
            <h1 className="font-serif text-2xl sm:text-3xl font-bold text-navy-950 tracking-tight leading-tight">
              Sınıflar & Gruplar
            </h1>
            <span className="text-xs font-bold text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-full border border-primary-100">
              {activeData.classes.length} Kayıtlı Sınıf
            </span>
          </div>
          <p className="text-xs sm:text-sm text-neutral-500 mt-1">
            Kurum bünyesindeki aktif şubeler, ders zümre öğretmenleri ve öğrenci kadrosu
          </p>
        </div>
        <Button
          variant="primary"
          size="sm"
          onClick={() => setIsAddModalOpen(true)}
          leftIcon={<Plus className="h-4 w-4" />}
          className="bg-[#315FE8] hover:bg-blue-700 shadow-soft-xs rounded-xl"
        >
          Yeni Sınıf Ekle
        </Button>
      </div>

      {/* 2. Search & Grade Filter Controls */}
      <div className="flex flex-col sm:flex-row items-center justify-between gap-3 bg-white p-3.5 rounded-2xl border border-neutral-200/80 shadow-soft-xs">
        <div className="relative flex-1 w-full max-w-md">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Sınıf adı veya şube ara..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2 rounded-xl border border-neutral-200 bg-neutral-50/60 text-xs sm:text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all font-medium"
          />
        </div>

        <div className="flex items-center gap-2 w-full sm:w-auto">
          <select
            value={gradeFilter}
            onChange={(e) => setGradeFilter(e.target.value)}
            className="w-full sm:w-auto px-3.5 py-2 rounded-xl border border-neutral-200 bg-neutral-50/60 text-xs sm:text-sm font-medium text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all cursor-pointer"
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
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {filteredClasses.length === 0 ? (
          <div className="col-span-full py-16 text-center text-neutral-400 bg-white rounded-2xl border border-neutral-200/80 shadow-soft-xs">
            <Building2 className="h-10 w-10 mx-auto text-neutral-300 mb-2" />
            <p className="text-sm font-semibold text-neutral-700">Kayıtlı sınıf veya grup bulunamadı.</p>
            <p className="text-xs text-neutral-400 mt-1">Arama kriterlerinizi değiştirebilir veya yeni bir sınıf ekleyebilirsiniz.</p>
          </div>
        ) : (
          filteredClasses.map((cls) => (
            <div
              key={cls.id}
              className="bg-white rounded-2xl border border-neutral-200/80 p-5 shadow-soft-xs hover:border-neutral-300 hover:shadow-soft-sm transition-all flex flex-col justify-between"
            >
              <div className="space-y-3.5">
                {/* Header line */}
                <div className="flex items-start justify-between gap-2 pb-2.5 border-b border-neutral-100">
                  <div>
                    <h3 className="text-base font-bold text-navy-950 tracking-tight">{cls.name}</h3>
                    <p className="text-xs font-medium text-neutral-500 mt-0.5">
                      {cls.grade} · {cls.branch}
                    </p>
                  </div>
                  <span className="inline-flex items-center gap-1.5 text-xs font-bold bg-[#14233E] text-white px-2.5 py-1 rounded-xl shadow-soft-xs shrink-0">
                    <Users className="h-3.5 w-3.5" />
                    {cls.studentCount} Öğrenci
                  </span>
                </div>

                {/* Academic Year Info Banner */}
                <div className="flex items-center justify-between text-xs bg-[#F8F7F3] px-3 py-2 rounded-xl border border-neutral-200/70 text-neutral-700">
                  <span className="flex items-center gap-1.5 font-medium">
                    <Calendar className="h-3.5 w-3.5 text-neutral-500" />
                    Eğitim Yılı:
                  </span>
                  <span className="font-mono font-bold text-navy-900">{cls.academicYear}</span>
                </div>

                {/* Teachers List */}
                <div className="space-y-1.5">
                  <div className="flex items-center justify-between text-[11px] font-bold text-neutral-500 uppercase tracking-wider">
                    <span>Zümre Öğretmenleri</span>
                    <span>{cls.teacherCount} Öğretmen</span>
                  </div>
                  <div className="space-y-1 max-h-24 overflow-y-auto">
                    {cls.teachers.length === 0 ? (
                      <span className="text-xs text-neutral-400 italic block py-1">
                        Atanmış öğretmen bulunmuyor
                      </span>
                    ) : (
                      cls.teachers.map((t, idx) => (
                        <div
                          key={idx}
                          className="flex items-center justify-between gap-1.5 text-xs text-neutral-700 bg-neutral-50/70 px-2.5 py-1.5 rounded-lg border border-neutral-200/50"
                        >
                          <div className="flex items-center gap-1.5 truncate">
                            <GraduationCap className="h-3.5 w-3.5 text-[#315FE8] shrink-0" />
                            <span className="truncate font-semibold">{t.fullName}</span>
                          </div>
                          {t.title && (
                            <span className="text-[10px] text-neutral-400 truncate shrink-0">
                              {t.title}
                            </span>
                          )}
                        </div>
                      ))
                    )}
                  </div>
                </div>
              </div>

              {/* Action Button */}
              <div className="pt-4 mt-2 border-t border-neutral-100 flex items-center justify-between">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => handleOpenClassDetail(cls.id)}
                  className="w-full text-xs font-semibold rounded-xl border-neutral-200 hover:bg-neutral-50 text-navy-900"
                >
                  Sınıf Kadrosunu Yönet
                </Button>
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
            placeholder="Örn: 12-A"
            required
          />

          <div className="grid grid-cols-2 gap-3">
            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">
                Kademe
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
              label="Şube / Alan"
              value={newBranch}
              onChange={(e) => setNewBranch(e.target.value)}
              placeholder="Örn: Sayısal"
            />
          </div>

          <Input
            label="Akademik Yıl"
            value={newAcademicYear}
            onChange={(e) => setNewAcademicYear(e.target.value)}
            placeholder="Örn: 2026-2027"
          />

          {createError && (
            <p className="text-xs text-rose-600 font-medium bg-rose-50 p-2.5 rounded-lg border border-rose-200">
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
              className="bg-[#315FE8] hover:bg-blue-700"
            >
              {isCreating ? 'Kaydediliyor...' : 'Kaydet'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* Manage Class Roster & Teachers Modal */}
      {activeClassDetail && (
        <Modal
          isOpen={true}
          onClose={() => setActiveClassDetail(null)}
          title={`${activeClassDetail.name} — Kadro & Öğrenci Yönetimi`}
          maxWidth="lg"
        >
          <div className="space-y-5">
            {detailSuccess && (
              <div className="flex items-center gap-2 p-3 bg-emerald-50 text-emerald-800 border border-emerald-200 rounded-xl text-xs font-semibold">
                <CheckCircle2 className="h-4 w-4 text-emerald-600 shrink-0" />
                <span>{detailSuccess}</span>
              </div>
            )}

            {detailError && (
              <div className="flex items-center gap-2 p-3 bg-rose-50 text-rose-800 border border-rose-200 rounded-xl text-xs font-semibold">
                <AlertCircle className="h-4 w-4 text-rose-600 shrink-0" />
                <span>{detailError}</span>
              </div>
            )}

            {/* Class Info Pill */}
            <div className="flex items-center justify-between bg-[#F8F7F3] p-3 rounded-xl border border-neutral-200 text-xs">
              <span className="font-semibold text-navy-950">
                {activeClassDetail.gradeLevel}. Sınıf · {activeClassDetail.branch || 'Genel Şube'}
              </span>
              <span className="font-mono text-neutral-500 font-medium">
                {activeClassDetail.academicYear || '2026-2027'}
              </span>
            </div>

            {/* 1. Zümre Öğretmenleri Bölümü */}
            <div className="space-y-3">
              <h4 className="text-xs font-bold text-navy-950 uppercase tracking-wider flex items-center justify-between">
                <span>Zümre Öğretmenleri ({(activeClassDetail.teachers || []).length})</span>
              </h4>

              {/* Assign Teacher Dropdown */}
              <div className="flex items-center gap-2">
                <select
                  value={selectedTeacherId}
                  onChange={(e) => setSelectedTeacherId(e.target.value)}
                  className="flex-1 px-3 py-2 text-xs rounded-xl border border-neutral-200 bg-white text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500 font-medium"
                >
                  <option value="">Atanacak Öğretmeni Seçin...</option>
                  {availableTeachers.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.firstName} {t.lastName} {t.title ? `(${t.title})` : ''}
                    </option>
                  ))}
                </select>
                <label className="flex items-center gap-1.5 text-xs text-neutral-600 select-none cursor-pointer">
                  <input
                    type="checkbox"
                    checked={isTeacherPrimary}
                    onChange={(e) => setIsTeacherPrimary(e.target.checked)}
                    className="rounded text-primary-600 focus:ring-primary-500"
                  />
                  <span>Sınıf Rehber</span>
                </label>
                <Button
                  variant="primary"
                  size="sm"
                  onClick={handleAssignTeacher}
                  disabled={!selectedTeacherId || isAssigningTeacher}
                  className="bg-[#315FE8] text-xs h-9 px-3"
                >
                  Ata
                </Button>
              </div>

              {/* Current Teachers Table */}
              <div className="border border-neutral-200/80 rounded-xl overflow-hidden divide-y divide-neutral-100 max-h-40 overflow-y-auto">
                {(activeClassDetail.teachers || []).length === 0 ? (
                  <p className="p-3 text-xs text-neutral-400 italic text-center">
                    Henüz zümre öğretmeni atanmamış.
                  </p>
                ) : (
                  activeClassDetail.teachers.map((t) => (
                    <div
                      key={t.teacherId}
                      className="p-2.5 px-3 flex items-center justify-between bg-white text-xs"
                    >
                      <div className="flex items-center gap-2">
                        <GraduationCap className="h-4 w-4 text-[#315FE8]" />
                        <span className="font-semibold text-neutral-900">{t.fullName}</span>
                        {t.title && <span className="text-neutral-400">({t.title})</span>}
                        {t.isPrimary && (
                          <span className="px-1.5 py-0.5 rounded text-[10px] font-bold bg-blue-50 text-primary-700 border border-blue-200">
                            Rehber
                          </span>
                        )}
                      </div>
                      <button
                        type="button"
                        onClick={() => handleRemoveTeacher(t.teacherId)}
                        className="text-neutral-400 hover:text-rose-600 p-1 rounded transition-colors"
                        title="Öğretmeni Kaldır"
                      >
                        <UserMinus className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  ))
                )}
              </div>
            </div>

            {/* 2. Sınıf Öğrenci Kadrosu Bölümü */}
            <div className="space-y-3 pt-2 border-t border-neutral-100">
              <h4 className="text-xs font-bold text-navy-950 uppercase tracking-wider flex items-center justify-between">
                <span>Öğrenci Kadrosu ({(activeClassDetail.students || []).length})</span>
              </h4>

              {/* Assign Student Dropdown */}
              <div className="flex items-center gap-2">
                <select
                  value={selectedStudentId}
                  onChange={(e) => setSelectedStudentId(e.target.value)}
                  className="flex-1 px-3 py-2 text-xs rounded-xl border border-neutral-200 bg-white text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500 font-medium"
                >
                  <option value="">Sınıfa Eklenecek Öğrenciyi Seçin...</option>
                  {availableStudents.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.fullName} {s.studentNumber ? `(No: ${s.studentNumber})` : ''} · {s.gradeLevel}. Sınıf
                    </option>
                  ))}
                </select>
                <Button
                  variant="primary"
                  size="sm"
                  onClick={handleAssignStudent}
                  disabled={!selectedStudentId || isAssigningStudent}
                  className="bg-[#315FE8] text-xs h-9 px-3"
                >
                  Ekle
                </Button>
              </div>

              {/* Current Students Table */}
              <div className="border border-neutral-200/80 rounded-xl overflow-hidden divide-y divide-neutral-100 max-h-48 overflow-y-auto">
                {(activeClassDetail.students || []).length === 0 ? (
                  <p className="p-3 text-xs text-neutral-400 italic text-center">
                    Sınıfta henüz kayıtlı öğrenci bulunmuyor.
                  </p>
                ) : (
                  activeClassDetail.students.map((s) => (
                    <div
                      key={s.studentId}
                      className="p-2.5 px-3 flex items-center justify-between bg-white text-xs"
                    >
                      <div className="flex items-center gap-2">
                        <Users className="h-3.5 w-3.5 text-neutral-400" />
                        <span className="font-semibold text-neutral-900">{s.fullName}</span>
                        {s.studentNumber && (
                          <span className="font-mono text-neutral-400">#{s.studentNumber}</span>
                        )}
                        {s.gradeLevel && (
                          <span className="text-[10px] text-neutral-400">({s.gradeLevel}. Sınıf)</span>
                        )}
                      </div>
                      <button
                        type="button"
                        onClick={() => handleRemoveStudent(s.studentId)}
                        className="text-neutral-400 hover:text-rose-600 p-1 rounded transition-colors"
                        title="Öğrenciyi Sınıftan Çıkar"
                      >
                        <UserMinus className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  ))
                )}
              </div>
            </div>

            <div className="flex justify-end pt-3 border-t border-neutral-100">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setActiveClassDetail(null)}
              >
                Kapat
              </Button>
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};

export default AdminClassesView;
