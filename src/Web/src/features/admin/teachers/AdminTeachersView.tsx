import React, { useState, useMemo, useEffect, useCallback } from 'react';
import {
  Search,
  GraduationCap,
  AlertCircle,
  RefreshCw,
  UserPlus,
  BookOpen,
  Plus,
  CheckCircle2,
  Trash2,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import {
  adminService,
  TeacherListDto,
  ClassroomDto,
  ClassroomDetailDto,
} from '@/services/adminService';
import { academicService } from '@/services/academicService';
import { TaxonomyDto } from '@/types/question';
import { useAuth } from '@/auth';
import { AdminTeacherDirectoryItem, AdminTeachersViewModel } from './types';

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
  const { user } = useAuth();
  const [data, setData] = useState<AdminTeachersViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const [searchTerm, setSearchTerm] = useState('');
  const [allClassrooms, setAllClassrooms] = useState<ClassroomDto[]>([]);
  const [allSubjects, setAllSubjects] = useState<TaxonomyDto[]>([]);
  const [selectedTeacher, setSelectedTeacher] = useState<AdminTeacherDirectoryItem | null>(null);

  // Add Teacher Modal
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [newFirstName, setNewFirstName] = useState('');
  const [newLastName, setNewLastName] = useState('');
  const [newEmail, setNewEmail] = useState('');
  const [newPassword, setNewPassword] = useState('Ogretmen@123!');
  const [newTitle, setNewTitle] = useState('Matematik Öğretmeni');
  const [isCreating, setIsCreating] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  // Classroom Assignment Modal
  const [isAssignClassModalOpen, setIsAssignClassModalOpen] = useState(false);
  const [targetTeacherForClass, setTargetTeacherForClass] = useState<AdminTeacherDirectoryItem | null>(null);
  const [selectedClassroomId, setSelectedClassroomId] = useState('');
  const [isPrimaryTeacher, setIsPrimaryTeacher] = useState(false);
  const [isAssigningClass, setIsAssigningClass] = useState(false);
  const [assignClassError, setAssignClassError] = useState<string | null>(null);
  const [assignClassSuccess, setAssignClassSuccess] = useState(false);

  // Subject Assignment Modal
  const [isAssignSubjectModalOpen, setIsAssignSubjectModalOpen] = useState(false);
  const [targetTeacherForSubject, setTargetTeacherForSubject] = useState<AdminTeacherDirectoryItem | null>(null);
  const [selectedSubjectId, setSelectedSubjectId] = useState('');
  const [isAssigningSubject, setIsAssigningSubject] = useState(false);
  const [assignSubjectError, setAssignSubjectError] = useState<string | null>(null);
  const [assignSubjectSuccess, setAssignSubjectSuccess] = useState(false);

  const fetchTeachers = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const [teachersRes, classesRes, subjectsRes] = await Promise.all([
        adminService.listTeachers({ page: 1, pageSize: 100 }),
        adminService.listClassrooms().catch(() => [] as ClassroomDto[]),
        academicService.getTaxonomy('subjects').catch(() => [] as TaxonomyDto[]),
      ]);

      const teachersList: TeacherListDto[] = Array.isArray(teachersRes)
        ? teachersRes
        : (teachersRes?.items || []);

      setAllClassrooms(classesRes || []);
      setAllSubjects(subjectsRes || []);

      // For each classroom, fetch details to get assigned teachers
      const classroomDetailPromises = (classesRes || []).map((c) =>
        adminService.getClassroom(c.id).catch(() => null as ClassroomDetailDto | null)
      );
      const classroomDetails = await Promise.all(classroomDetailPromises);

      // Map teacher classroom assignments from real classroom details
      const teacherClassroomsMap = new Map<
        string,
        Array<{ id: string; name: string; isPrimary: boolean; studentCount: number }>
      >();

      for (const cd of classroomDetails) {
        if (!cd) continue;
        const studentCount = cd.students?.length ?? 0;
        for (const t of cd.teachers || []) {
          const list = teacherClassroomsMap.get(t.teacherId) || [];
          list.push({
            id: cd.id,
            name: cd.name,
            isPrimary: t.isPrimary,
            studentCount,
          });
          teacherClassroomsMap.set(t.teacherId, list);
        }
      }

      // Fetch subject assignments for each teacher
      const subjectPromises = teachersList.map(async (t) => {
        try {
          const subs = await adminService.getTeacherSubjects(t.id);
          return { teacherId: t.id, subjectIds: (subs || []).map((s) => s.subjectId) };
        } catch {
          return { teacherId: t.id, subjectIds: [] };
        }
      });
      const subjectResults = await Promise.all(subjectPromises);
      const teacherSubjectsMap = new Map<string, string[]>();
      for (const sr of subjectResults) {
        teacherSubjectsMap.set(sr.teacherId, sr.subjectIds);
      }

      const items: AdminTeacherDirectoryItem[] = teachersList.map((t) => {
        const assignedClasses = teacherClassroomsMap.get(t.id) || [];
        const assignedSubjectIds = teacherSubjectsMap.get(t.id) || [];
        const totalStudentCount = assignedClasses.reduce((acc, c) => acc + c.studentCount, 0);
        const fullName = `${t.firstName || ''} ${t.lastName || ''}`.trim() || 'Öğretmen';
        const initials =
          `${t.firstName?.[0] || ''}${t.lastName?.[0] || ''}`.toUpperCase() || 'ÖĞ';

        return {
          id: t.id,
          name: fullName,
          firstName: t.firstName,
          lastName: t.lastName,
          initials,
          title: t.title || 'Branş Öğretmeni',
          assignedClassrooms: assignedClasses,
          assignedSubjectIds,
          totalStudentCount,
          status: 'active',
        };
      });

      setData({ teachers: items });

      // Keep selectedTeacher reference fresh
      setSelectedTeacher((prev) => {
        if (!prev) return null;
        return items.find((x) => x.id === prev.id) || null;
      });
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
    if (!newFirstName.trim() || !newLastName.trim() || !newEmail.trim() || !newPassword) {
      setCreateError('Lütfen tüm zorunlu alanları doldurunuz.');
      return;
    }

    setIsCreating(true);
    setCreateError(null);

    try {
      await adminService.createTeacher({
        institutionId: user?.institutionId || '00000000-0000-0000-0000-000000000000',
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
      setNewTitle('Matematik Öğretmeni');
      await fetchTeachers();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : 'Öğretmen kaydı oluşturulamadı.');
    } finally {
      setIsCreating(false);
    }
  };

  const handleOpenAssignClass = (teacher: AdminTeacherDirectoryItem) => {
    setTargetTeacherForClass(teacher);
    setSelectedClassroomId('');
    setIsPrimaryTeacher(false);
    setAssignClassError(null);
    setAssignClassSuccess(false);
    setIsAssignClassModalOpen(true);
  };

  const handleAssignClassroom = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetTeacherForClass || !selectedClassroomId) {
      setAssignClassError('Lütfen bir sınıf seçiniz.');
      return;
    }

    setIsAssigningClass(true);
    setAssignClassError(null);
    setAssignClassSuccess(false);

    try {
      await adminService.assignTeacherToClassroom(
        selectedClassroomId,
        targetTeacherForClass.id,
        isPrimaryTeacher
      );
      setAssignClassSuccess(true);
      await fetchTeachers();
      setTimeout(() => {
        setIsAssignClassModalOpen(false);
        setAssignClassSuccess(false);
      }, 1000);
    } catch (err: unknown) {
      setAssignClassError(err instanceof Error ? err.message : 'Sınıf ataması yapılamadı.');
    } finally {
      setIsAssigningClass(false);
    }
  };

  const handleRemoveClassroom = async (classroomId: string, teacherId: string) => {
    try {
      await adminService.removeTeacherFromClassroom(classroomId, teacherId);
      await fetchTeachers();
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : 'Sınıf ataması kaldırılamadı.');
    }
  };

  const handleOpenAssignSubject = (teacher: AdminTeacherDirectoryItem) => {
    setTargetTeacherForSubject(teacher);
    setSelectedSubjectId('');
    setAssignSubjectError(null);
    setAssignSubjectSuccess(false);
    setIsAssignSubjectModalOpen(true);
  };

  const handleAssignSubject = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!targetTeacherForSubject || !selectedSubjectId) {
      setAssignSubjectError('Lütfen atanacak ders branşını seçiniz.');
      return;
    }

    setIsAssigningSubject(true);
    setAssignSubjectError(null);
    setAssignSubjectSuccess(false);

    try {
      await adminService.assignTeacherSubject(targetTeacherForSubject.id, selectedSubjectId);
      setAssignSubjectSuccess(true);
      await fetchTeachers();
      setTimeout(() => {
        setIsAssignSubjectModalOpen(false);
        setAssignSubjectSuccess(false);
      }, 1000);
    } catch (err: unknown) {
      setAssignSubjectError(err instanceof Error ? err.message : 'Branş ders ataması yapılamadı.');
    } finally {
      setIsAssigningSubject(false);
    }
  };

  const handleRemoveSubject = async (teacherId: string, subjectId: string) => {
    try {
      await adminService.removeTeacherSubject(teacherId, subjectId);
      await fetchTeachers();
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : 'Branş ders ataması kaldırılamadı.');
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
        !teacher.title?.toLowerCase().includes(searchTerm.toLowerCase())
      ) {
        return false;
      }
      return true;
    });
  }, [activeData.teachers, searchTerm]);

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="admin-teachers-loading">
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
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none p-4" data-testid="admin-teachers-error">
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
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6 p-4" data-testid="admin-teachers-view">
      {/* 1. Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-neutral-200/70 pb-4">
        <div>
          <div className="flex items-center gap-2.5 flex-wrap">
            <h1 className="font-serif text-2xl sm:text-3xl font-bold text-navy-950 tracking-tight leading-tight">
              Öğretmenler & Zümre Yönetimi
            </h1>
            <span className="text-xs font-bold text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-full border border-primary-100">
              {filteredTeachers.length !== activeData.teachers.length
                ? `${filteredTeachers.length} / ${activeData.teachers.length} Öğretmen`
                : `${activeData.teachers.length} Aktif Öğretmen`}
            </span>
          </div>
          <p className="text-xs sm:text-sm text-neutral-500 mt-1">
            Kuruma kayıtlı öğretmenlerin zümre branşları, sınıf atamaları ve ders yükü yönetimi
          </p>
        </div>

        <Button
          variant="primary"
          size="sm"
          onClick={() => setIsAddModalOpen(true)}
          leftIcon={<UserPlus className="h-4 w-4" />}
          className="bg-[#315FE8] hover:bg-blue-700 rounded-xl shadow-soft-xs font-semibold w-full md:w-auto shrink-0 py-2.5 px-4"
        >
          Yeni Öğretmen Ekle
        </Button>
      </div>

      {/* 2. Search Bar */}
      <div className="bg-white p-3.5 sm:p-4 rounded-2xl border border-neutral-200/80 shadow-soft-xs flex flex-col md:flex-row items-stretch md:items-center justify-between gap-3">
        <div className="relative flex-1 w-full min-w-0 md:max-w-md">
          <Search className="h-4 w-4 absolute left-3.5 top-1/2 -translate-y-1/2 text-neutral-400 pointer-events-none" />
          <input
            type="search"
            placeholder="Öğretmen adı veya unvan ara..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-neutral-200 bg-neutral-50/60 text-xs sm:text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all font-medium"
          />
        </div>
      </div>

      {/* 3. Teachers Cards Grid */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-2 xl:grid-cols-3 gap-4 sm:gap-5">
        {filteredTeachers.length === 0 ? (
          <div className="col-span-full py-16 text-center text-neutral-400 bg-white rounded-2xl border border-neutral-200/80 shadow-soft-xs p-4">
            <GraduationCap className="h-10 w-10 mx-auto text-neutral-300 mb-2" />
            <p className="text-sm font-semibold text-neutral-700">Kayıtlı öğretmen bulunamadı.</p>
            <p className="text-xs text-neutral-400 mt-1">
              Kurumunuza yeni branş öğretmeni ekleyebilir veya arama filtresini temizleyebilirsiniz.
            </p>
          </div>
        ) : (
          filteredTeachers.map((teacher) => {
            const teacherSubjectNames = teacher.assignedSubjectIds
              .map((id) => allSubjects.find((s) => s.id === id)?.name)
              .filter(Boolean);

            return (
              <div
                key={teacher.id}
                className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-xs hover:border-primary-200 hover:shadow-soft-sm transition-all flex flex-col justify-between space-y-4"
              >
                {/* Top: Identity */}
                <div>
                  <div className="flex items-start justify-between gap-3">
                    <div className="flex items-center gap-3 min-w-0 flex-1">
                      <div className="w-11 h-11 rounded-2xl bg-[#14233E] text-white text-sm font-bold flex items-center justify-center shrink-0 shadow-soft-xs">
                        {teacher.initials}
                      </div>
                      <div className="min-w-0 flex-1">
                        <h3 className="text-sm sm:text-base font-bold text-navy-950 break-words leading-snug">
                          {teacher.name}
                        </h3>
                        <p className="text-xs text-neutral-500 font-medium truncate mt-0.5">
                          {teacher.title || 'Branş Öğretmeni'}
                        </p>
                      </div>
                    </div>

                    <span className="inline-flex items-center gap-1 text-[11px] font-bold text-emerald-700 bg-emerald-50 px-2.5 py-0.5 rounded-full border border-emerald-200 shrink-0">
                      <CheckCircle2 className="h-3 w-3" />
                      Aktif
                    </span>
                  </div>

                  {/* Real Stats Banner */}
                  <div className="mt-4 p-3 rounded-xl bg-neutral-50 border border-neutral-100 flex items-center justify-between text-xs">
                    <span className="text-neutral-500 font-medium">Sınıf & Öğrenci Yükü</span>
                    <span className="font-bold text-navy-950 font-mono">
                      {teacher.assignedClassrooms.length} Sınıf · {teacher.totalStudentCount} Öğrenci
                    </span>
                  </div>

                  {/* Real Subject Tags */}
                  <div className="mt-3.5">
                    <div className="flex items-center justify-between gap-2 mb-1.5 flex-wrap">
                      <span className="text-[11px] font-semibold text-neutral-400 uppercase tracking-wider">
                        Branş Dersleri ({teacherSubjectNames.length})
                      </span>
                      <button
                        type="button"
                        onClick={() => handleOpenAssignSubject(teacher)}
                        className="text-[11px] font-semibold text-primary-600 hover:text-primary-700 flex items-center gap-1 px-1.5 py-0.5 rounded hover:bg-primary-50 transition-colors"
                      >
                        <Plus className="h-3 w-3" /> Ders Ata
                      </button>
                    </div>

                    {teacherSubjectNames.length === 0 ? (
                      <p className="text-xs text-neutral-400 italic py-0.5">
                        Ders branşı tanımlanmamış.
                      </p>
                    ) : (
                      <div className="flex flex-wrap gap-1.5">
                        {teacherSubjectNames.map((subName, i) => (
                          <span
                            key={i}
                            className="inline-flex items-center gap-1 text-[11px] font-medium bg-blue-50 text-blue-800 border border-blue-100 px-2.5 py-0.5 rounded-md"
                          >
                            <BookOpen className="h-2.5 w-2.5 text-blue-600" />
                            {subName}
                          </span>
                        ))}
                      </div>
                    )}
                  </div>

                  {/* Real Classroom Assignments */}
                  <div className="mt-3.5">
                    <div className="flex items-center justify-between gap-2 mb-1.5 flex-wrap">
                      <span className="text-[11px] font-semibold text-neutral-400 uppercase tracking-wider">
                        Atanmış Sınıflar ({teacher.assignedClassrooms.length})
                      </span>
                      <button
                        type="button"
                        onClick={() => handleOpenAssignClass(teacher)}
                        className="text-[11px] font-semibold text-primary-600 hover:text-primary-700 flex items-center gap-1 px-1.5 py-0.5 rounded hover:bg-primary-50 transition-colors"
                      >
                        <Plus className="h-3 w-3" /> Sınıf Ata
                      </button>
                    </div>

                    {teacher.assignedClassrooms.length === 0 ? (
                      <p className="text-xs text-neutral-400 italic py-0.5">
                        Henüz sınıf atanmamış.
                      </p>
                    ) : (
                      <div className="max-h-32 overflow-y-auto space-y-1 pr-1">
                        {teacher.assignedClassrooms.map((cls) => (
                          <div
                            key={cls.id}
                            className="flex items-center justify-between text-xs py-1.5 px-2.5 rounded-lg bg-neutral-50/80 hover:bg-neutral-100/70 transition-colors"
                          >
                            <div className="flex items-center gap-1.5 min-w-0 flex-1">
                              <GraduationCap className="h-3.5 w-3.5 text-neutral-400 shrink-0" />
                              <span className="font-medium text-neutral-800 truncate">
                                {cls.name}
                              </span>
                              {cls.isPrimary && (
                                <span className="text-[10px] font-semibold text-amber-700 bg-amber-50 px-1.5 py-0.2 rounded border border-amber-200 shrink-0">
                                  Rehber
                                </span>
                              )}
                            </div>
                            <span className="text-[11px] text-neutral-400 shrink-0 ml-2 font-mono">
                              {cls.studentCount} Öğr.
                            </span>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>

                {/* Footer Action */}
                <div className="pt-3 border-t border-neutral-100 flex items-center justify-between gap-2">
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => setSelectedTeacher(teacher)}
                    className="text-xs h-8 px-3 rounded-lg text-neutral-700 font-medium w-full"
                  >
                    Detaylı Yönetim
                  </Button>
                </div>
              </div>
            );
          })
        )}
      </div>

      {/* 4. Teacher Detail Management Modal */}
      {selectedTeacher && !isAssignClassModalOpen && !isAssignSubjectModalOpen && (
        <Modal
          isOpen={Boolean(selectedTeacher)}
          onClose={() => setSelectedTeacher(null)}
          title="Öğretmen Zümre & Sınıf Yönetimi"
        >
          <div className="space-y-4">
            <div className="flex items-center gap-3.5 p-3.5 rounded-2xl bg-neutral-50 border border-neutral-200/80">
              <div className="w-12 h-12 rounded-2xl bg-[#14233E] text-white text-base font-bold flex items-center justify-center shrink-0 shadow-soft-xs">
                {selectedTeacher.initials}
              </div>
              <div className="min-w-0 flex-1">
                <h3 className="text-base font-bold text-navy-950 truncate">
                  {selectedTeacher.name}
                </h3>
                <p className="text-xs text-neutral-500 font-medium mt-0.5">
                  {selectedTeacher.title || 'Branş Öğretmeni'} · {selectedTeacher.assignedClassrooms.length} Sınıf · {selectedTeacher.totalStudentCount} Öğrenci
                </p>
              </div>
            </div>

            {/* Subjects Management */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <h4 className="text-xs font-bold uppercase tracking-wider text-neutral-500">
                  Ders Branşları ({selectedTeacher.assignedSubjectIds.length})
                </h4>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleOpenAssignSubject(selectedTeacher)}
                  leftIcon={<Plus className="h-3.5 w-3.5 text-primary-600" />}
                  className="text-primary-600 hover:text-primary-700 text-xs h-7 px-2 font-semibold"
                >
                  Branş Ekle
                </Button>
              </div>

              {selectedTeacher.assignedSubjectIds.length === 0 ? (
                <div className="p-4 text-center rounded-xl bg-neutral-50 border border-neutral-100 text-neutral-400 text-xs">
                  Tanımlı ders branşı bulunmuyor.
                </div>
              ) : (
                <div className="divide-y divide-neutral-100 rounded-xl border border-neutral-200/80 bg-white">
                  {selectedTeacher.assignedSubjectIds.map((subId) => {
                    const subName = allSubjects.find((s) => s.id === subId)?.name || 'Ders';
                    return (
                      <div
                        key={subId}
                        className="p-2.5 px-3 flex items-center justify-between text-xs hover:bg-[#F8F7F3] transition-colors"
                      >
                        <div className="flex items-center gap-2">
                          <BookOpen className="h-3.5 w-3.5 text-blue-600" />
                          <span className="font-semibold text-neutral-900">{subName}</span>
                        </div>
                        <button
                          type="button"
                          onClick={() => handleRemoveSubject(selectedTeacher.id, subId)}
                          className="text-neutral-400 hover:text-rose-600 transition-colors p-1"
                          title="Branşı Kaldır"
                        >
                          <Trash2 className="h-3.5 w-3.5" />
                        </button>
                      </div>
                    );
                  })}
                </div>
              )}
            </div>

            {/* Classrooms Management */}
            <div>
              <div className="flex items-center justify-between mb-2">
                <h4 className="text-xs font-bold uppercase tracking-wider text-neutral-500">
                  Atanmış Sınıflar ({selectedTeacher.assignedClassrooms.length})
                </h4>
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={() => handleOpenAssignClass(selectedTeacher)}
                  leftIcon={<Plus className="h-3.5 w-3.5 text-primary-600" />}
                  className="text-primary-600 hover:text-primary-700 text-xs h-7 px-2 font-semibold"
                >
                  Sınıf Ata
                </Button>
              </div>

              {selectedTeacher.assignedClassrooms.length === 0 ? (
                <div className="p-4 text-center rounded-xl bg-neutral-50 border border-neutral-100 text-neutral-400 text-xs">
                  Atanmış sınıf bulunmuyor.
                </div>
              ) : (
                <div className="divide-y divide-neutral-100 rounded-xl border border-neutral-200/80 bg-white max-h-48 overflow-y-auto">
                  {selectedTeacher.assignedClassrooms.map((cls) => (
                    <div
                      key={cls.id}
                      className="p-2.5 px-3 flex items-center justify-between text-xs hover:bg-[#F8F7F3] transition-colors"
                    >
                      <div>
                        <span className="font-semibold text-neutral-900 block">
                          {cls.name}
                        </span>
                        <span className="text-[11px] text-neutral-400">
                          {cls.studentCount} Kayıtlı Öğrenci {cls.isPrimary ? '· Rehber Öğretmen' : ''}
                        </span>
                      </div>
                      <button
                        type="button"
                        onClick={() => handleRemoveClassroom(cls.id, selectedTeacher.id)}
                        className="text-neutral-400 hover:text-rose-600 transition-colors p-1"
                        title="Sınıf Atamasını Kaldır"
                      >
                        <Trash2 className="h-3.5 w-3.5" />
                      </button>
                    </div>
                  ))}
                </div>
              )}
            </div>

            <div className="flex justify-end pt-2 border-t border-neutral-100">
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setSelectedTeacher(null)}
              >
                Kapat
              </Button>
            </div>
          </div>
        </Modal>
      )}

      {/* 5. Classroom Assignment Modal */}
      <Modal
        isOpen={isAssignClassModalOpen}
        onClose={() => {
          setIsAssignClassModalOpen(false);
          setAssignClassError(null);
          setAssignClassSuccess(false);
        }}
        title={`Sınıf Ata — ${targetTeacherForClass?.name || 'Öğretmen'}`}
      >
        <form onSubmit={handleAssignClassroom} className="space-y-4">
          <p className="text-xs text-neutral-500">
            Kurum bünyesindeki aktif sınıflardan birini seçerek öğretmeni sınıfa ders veya rehberlik sorumlusu olarak atayabilirsiniz.
          </p>

          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1.5">
              Sınıf Seçin
            </label>
            <select
              value={selectedClassroomId}
              onChange={(e) => setSelectedClassroomId(e.target.value)}
              className="w-full px-3 py-2 text-xs sm:text-sm rounded-xl border border-neutral-200 bg-white text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500 font-medium"
              required
            >
              <option value="">Bir sınıf seçiniz...</option>
              {allClassrooms.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name} ({c.gradeLevel}. Sınıf)
                </option>
              ))}
            </select>
          </div>

          <div className="flex items-center gap-2 pt-1">
            <input
              type="checkbox"
              id="isPrimaryTeacher"
              checked={isPrimaryTeacher}
              onChange={(e) => setIsPrimaryTeacher(e.target.checked)}
              className="rounded border-neutral-300 text-primary-600 focus:ring-primary-500"
            />
            <label htmlFor="isPrimaryTeacher" className="text-xs text-neutral-700 font-medium cursor-pointer">
              Sınıfın rehber / ana branş öğretmeni olarak ata
            </label>
          </div>

          {assignClassError && (
            <p className="text-xs text-rose-700 bg-rose-50 border border-rose-200 p-2.5 rounded-xl font-medium">
              {assignClassError}
            </p>
          )}

          {assignClassSuccess && (
            <p className="text-xs text-emerald-700 bg-emerald-50 border border-emerald-200 p-2.5 rounded-xl font-medium">
              Öğretmen sınıfa başarıyla atandı.
            </p>
          )}

          <div className="flex justify-end gap-2 pt-2 border-t border-neutral-100">
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setIsAssignClassModalOpen(false);
                setAssignClassError(null);
                setAssignClassSuccess(false);
              }}
            >
              İptal
            </Button>
            <Button
              type="submit"
              variant="primary"
              disabled={isAssigningClass || !selectedClassroomId || assignClassSuccess}
              className="bg-[#315FE8] hover:bg-blue-700 font-semibold"
            >
              {isAssigningClass ? 'Atanıyor...' : 'Sınıfa Ata'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* 6. Subject Assignment Modal */}
      <Modal
        isOpen={isAssignSubjectModalOpen}
        onClose={() => {
          setIsAssignSubjectModalOpen(false);
          setAssignSubjectError(null);
          setAssignSubjectSuccess(false);
        }}
        title={`Branş Dersi Ata — ${targetTeacherForSubject?.name || 'Öğretmen'}`}
      >
        <form onSubmit={handleAssignSubject} className="space-y-4">
          <p className="text-xs text-neutral-500">
            Öğretmenin kurumsal zümre ve sınav analizlerinde sorumlu olacağı ders branşını tanımlayınız.
          </p>

          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1.5">
              Ders Branşı Seçin
            </label>
            <select
              value={selectedSubjectId}
              onChange={(e) => setSelectedSubjectId(e.target.value)}
              className="w-full px-3 py-2 text-xs sm:text-sm rounded-xl border border-neutral-200 bg-white text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500 font-medium"
              required
            >
              <option value="">Bir ders seçiniz...</option>
              {allSubjects.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.code})
                </option>
              ))}
            </select>
          </div>

          {assignSubjectError && (
            <p className="text-xs text-rose-700 bg-rose-50 border border-rose-200 p-2.5 rounded-xl font-medium">
              {assignSubjectError}
            </p>
          )}

          {assignSubjectSuccess && (
            <p className="text-xs text-emerald-700 bg-emerald-50 border border-emerald-200 p-2.5 rounded-xl font-medium">
              Branş dersi başarıyla atandı.
            </p>
          )}

          <div className="flex justify-end gap-2 pt-2 border-t border-neutral-100">
            <Button
              type="button"
              variant="secondary"
              onClick={() => {
                setIsAssignSubjectModalOpen(false);
                setAssignSubjectError(null);
                setAssignSubjectSuccess(false);
              }}
            >
              İptal
            </Button>
            <Button
              type="submit"
              variant="primary"
              disabled={isAssigningSubject || !selectedSubjectId || assignSubjectSuccess}
              className="bg-[#315FE8] hover:bg-blue-700 font-semibold"
            >
              {isAssigningSubject ? 'Atanıyor...' : 'Branşı Kaydet'}
            </Button>
          </div>
        </form>
      </Modal>

      {/* 7. Add Teacher Modal */}
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

export default AdminTeachersView;
