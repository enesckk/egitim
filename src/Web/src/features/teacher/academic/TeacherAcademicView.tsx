import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  BookOpen,
  AlertTriangle,
  AlertCircle,
  RefreshCw,
  CheckCircle2,
  XCircle,
  ChevronRight,
  GraduationCap,
  Sparkles,
  HelpCircle,
  Layers,
  ArrowRight,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { EmptyState } from '@/components/ui/EmptyState';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { TeacherAcademicViewModel, TeacherAcademicGap, TeacherAcademicTopicItem } from './types';
import { teacherService } from '@/services/teacherService';
import { academicService } from '@/services/academicService';

export interface TeacherAcademicViewProps {
  initialData?: TeacherAcademicViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherAcademicView: React.FC<TeacherAcademicViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();

  const [data, setData] = useState<TeacherAcademicViewModel | undefined>(initialData);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  // Filters
  const [selectedClassId, setSelectedClassId] = useState<string>('all');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [selectedSubject, setSelectedSubject] = useState<string>('all');

  const fetchData = useCallback(async () => {
    if (initialData) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      // 1. Fetch teacher profile, classrooms and dashboard gaps in parallel
      const [teacherMe, classrooms, dashboard] = await Promise.all([
        teacherService.getMe().catch(() => null),
        teacherService.listClassrooms().catch(() => []),
        teacherService.getDashboard().catch(() => null),
      ]);

      const teacherSubjects = (teacherMe?.subjects || []).map((s) => s.subjectName).filter(Boolean);
      const assignedSubjects = teacherSubjects.length > 0 ? teacherSubjects : (dashboard?.assignedSubjects || ['Matematik']);

      const rawGaps: TeacherAcademicGap[] = (dashboard?.academicGaps || []).map((g: any) => ({
        id: g.id,
        studentId: g.studentId,
        studentName: g.studentName,
        grade: g.grade,
        subject: g.subject,
        topic: g.topic,
        detail: g.detail,
        severity: g.severity,
        initials: g.initials || 'ÖĞ',
        lastExam: g.lastExam,
      }));

      // 2. Fetch classroom students to get topic performances for assigned students
      const topicItems: TeacherAcademicTopicItem[] = [];
      const studentClassroomMap = new Map<string, string>();

      if (classrooms.length > 0) {
        // Collect students from classrooms
        const studentFetchPromises = classrooms.slice(0, 3).map(async (cls) => {
          try {
            const detail = await teacherService.getClassroomDetail(cls.id);
            return (detail.students || []).map((s) => ({
              studentId: s.studentId,
              studentName: s.fullName,
              classId: cls.id,
            }));
          } catch {
            return [];
          }
        });

        const studentsByClass = await Promise.all(studentFetchPromises);
        studentsByClass.flat().forEach((s) => {
          if (!studentClassroomMap.has(s.studentId)) {
            studentClassroomMap.set(s.studentId, s.classId);
          }
        });

        const uniqueStudents = Array.from(
          new Map(studentsByClass.flat().map((s) => [s.studentId, s])).values()
        );

        // Fetch classifications for up to first 5 students to respect rate limits & load times
        const topicPromises = uniqueStudents.slice(0, 5).map(async (st) => {
          try {
            const classifications = await academicService.getTopicClassifications(st.studentId);
            return (classifications || []).map((tc) => {
              const accuracyPercent = Math.round((tc.accuracy || 0) * 100);
              const stateStr = String(tc.state).toLowerCase();
              const status: 'mastered' | 'improving' | 'needs_work' =
                stateStr === 'strong' || stateStr === '4' || accuracyPercent >= 75
                  ? 'mastered'
                  : stateStr === 'critical' || stateStr === '1' || accuracyPercent < 50
                  ? 'needs_work'
                  : 'improving';

              return {
                id: `${st.studentId}-${tc.topicId}`,
                topicId: tc.topicId,
                topicName: tc.topicName,
                subjectId: tc.subjectId,
                subjectName: tc.subjectName,
                accuracy: accuracyPercent,
                totalQuestions: tc.totalQuestions || 0,
                totalCorrect: tc.totalCorrect || 0,
                totalWrong: tc.totalWrong || 0,
                totalBlank: tc.totalBlank || 0,
                status,
                studentName: st.studentName,
                studentId: st.studentId,
                classroomId: st.classId,
              };
            });
          } catch {
            return [];
          }
        });

        const allTopics = await Promise.all(topicPromises);
        topicItems.push(...allTopics.flat());
      }

      // Associate classroomId with rawGaps if known from studentClassroomMap
      rawGaps.forEach((g) => {
        if (studentClassroomMap.has(g.studentId)) {
          g.classroomId = studentClassroomMap.get(g.studentId);
        }
      });

      setData({
        teacherName: teacherMe?.fullName || dashboard?.teacherName || 'Öğretmen',
        assignedSubjects,
        totalAssignedClasses: teacherMe?.assignedClassroomCount || classrooms.length,
        totalAssignedStudents: teacherMe?.assignedStudentCount || dashboard?.totalAssignedStudents || 0,
        classrooms: classrooms.map((c) => ({
          id: c.id,
          name: c.name,
          branch: c.branch || undefined,
          gradeLevel: c.gradeLevel || undefined,
          studentCount: c.studentCount,
        })),
        gaps: rawGaps,
        topicItems,
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Akademik takip verisi yüklenemedi.';
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
      fetchData();
    }
  }, [initialData, fetchData]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchData();
    }
  };

  const activeData = data || initialData;
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Filtered Gaps
  const filteredGaps = useMemo(() => {
    if (!activeData) return [];
    return activeData.gaps.filter((gap) => {
      const matchesSearch =
        searchQuery === '' ||
        gap.studentName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        gap.topic.toLowerCase().includes(searchQuery.toLowerCase()) ||
        gap.subject.toLowerCase().includes(searchQuery.toLowerCase());

      const matchesSubject =
        selectedSubject === 'all' || gap.subject.trim().toLowerCase() === selectedSubject.trim().toLowerCase();

      const matchesClass =
        selectedClassId === 'all' || (gap.classroomId && gap.classroomId === selectedClassId);

      return matchesSearch && matchesSubject && matchesClass;
    });
  }, [activeData, searchQuery, selectedSubject, selectedClassId]);

  // Grouped Gaps by Student
  const studentGroupedGaps = useMemo(() => {
    const map = new Map<string, { studentId: string; studentName: string; grade: string; initials: string; gaps: TeacherAcademicGap[] }>();
    filteredGaps.forEach((gap) => {
      if (!map.has(gap.studentId)) {
        map.set(gap.studentId, {
          studentId: gap.studentId,
          studentName: gap.studentName,
          grade: gap.grade,
          initials: gap.initials,
          gaps: [],
        });
      }
      map.get(gap.studentId)!.gaps.push(gap);
    });
    return Array.from(map.values());
  }, [filteredGaps]);

  // Filtered Topic Items
  const filteredTopics = useMemo(() => {
    if (!activeData) return [];
    return activeData.topicItems.filter((item) => {
      const matchesSearch =
        searchQuery === '' ||
        item.topicName.toLowerCase().includes(searchQuery.toLowerCase()) ||
        (item.studentName && item.studentName.toLowerCase().includes(searchQuery.toLowerCase()));

      const matchesSubject =
        selectedSubject === 'all' || item.subjectName.trim().toLowerCase() === selectedSubject.trim().toLowerCase();

      const matchesClass =
        selectedClassId === 'all' || (item.classroomId && item.classroomId === selectedClassId);

      return matchesSearch && matchesSubject && matchesClass;
    });
  }, [activeData, searchQuery, selectedSubject, selectedClassId]);

  // Loading State
  if (activeLoading) {
    return (
      <div className="w-full max-w-6xl mx-auto space-y-5 select-none" data-testid="teacher-academic-loading">
        <Skeleton className="h-10 w-48 rounded-xl" />
        <Skeleton className="h-32 w-full rounded-2xl" />
        <Skeleton className="h-14 w-full rounded-2xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-64 w-full rounded-2xl" />
          <Skeleton className="h-64 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError || !activeData) {
    return (
      <div className="w-full max-w-6xl mx-auto py-8 space-y-4 select-none" data-testid="teacher-academic-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Akademik Takip Yüklenemedi">
          {activeError || 'Öğretmen yetki alanınıza ait akademik verilere ulaşılamadı.'}
        </Alert>
        <div className="flex justify-center pt-2">
          <Button variant="primary" size="sm" onClick={handleRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  const criticalGapsCount = activeData.gaps.filter((g) => g.severity === 'critical').length;
  const totalGapsCount = activeData.gaps.length;

  return (
    <div className="w-full max-w-6xl mx-auto select-none space-y-5 sm:space-y-6" data-testid="teacher-academic-view">
      {/* 1. Header with Real Teacher Scope */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-5 sm:p-6 shadow-soft-sm">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-5">
          <div className="space-y-2 max-w-2xl">
            <div className="flex items-center gap-2.5 flex-wrap">
              <h1 className="font-serif text-2xl sm:text-3xl font-bold text-neutral-900 tracking-tight">
                Akademik Takip & Kazanım Analizi
              </h1>
              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-primary-50 text-primary-700 border border-primary-200">
                <BookOpen className="h-3.5 w-3.5" />
                {activeData.assignedSubjects.join(', ')}
              </span>
            </div>
            <p className="text-xs sm:text-sm text-neutral-500 leading-relaxed">
              Yetkili sınıflarınızdaki öğrencilerin deneme sınavı sonuçları, konu kazanım oranları ve öncelikli akademik eksikleri
            </p>
          </div>

          <div className="grid grid-cols-2 gap-3 sm:gap-4 border-t lg:border-t-0 lg:border-l border-neutral-200/80 pt-4 lg:pt-0 lg:pl-6 w-full lg:w-auto">
            <div className="bg-[#F8F7F3] p-3.5 sm:p-4 rounded-xl border border-neutral-200/80 min-w-0">
              <span className="text-[10px] uppercase font-bold text-neutral-500 tracking-wider block">
                Yetkili Şube
              </span>
              <div className="flex items-center gap-1.5 mt-1">
                <Layers className="h-4 w-4 text-primary-600 flex-shrink-0" />
                <span className="font-mono text-lg sm:text-xl font-bold text-neutral-900">
                  {activeData.totalAssignedClasses}
                </span>
                <span className="text-xs text-neutral-500">Sınıf</span>
              </div>
            </div>

            <div className="bg-[#F8F7F3] p-3.5 sm:p-4 rounded-xl border border-neutral-200/80 min-w-0">
              <span className="text-[10px] uppercase font-bold text-neutral-500 tracking-wider block">
                Kayıtlı Öğrenci
              </span>
              <div className="flex items-center gap-1.5 mt-1">
                <GraduationCap className="h-4 w-4 text-primary-600 flex-shrink-0" />
                <span className="font-mono text-lg sm:text-xl font-bold text-neutral-900">
                  {activeData.totalAssignedStudents}
                </span>
                <span className="text-xs text-neutral-500">Öğrenci</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      {/* 2. Filters Bar */}
      <div className="bg-white p-3.5 sm:p-4 rounded-2xl border border-neutral-200/80 shadow-soft-sm">
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div className="sm:col-span-1">
            <Input
              placeholder="Öğrenci adı veya konu ara..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="bg-[#F8F7F3] border-neutral-200"
            />
          </div>

          <Select
            options={[
              { value: 'all', label: 'Tüm Sınıflarım' },
              ...activeData.classrooms.map((c) => ({
                value: c.id,
                label: `${c.name} ${c.branch ? `(${c.branch})` : ''}`,
              })),
            ]}
            value={selectedClassId}
            onChange={(e) => setSelectedClassId(e.target.value)}
            className="bg-[#F8F7F3] border-neutral-200"
          />

          <Select
            options={[
              { value: 'all', label: 'Tüm Branşlar' },
              ...activeData.assignedSubjects.map((s) => ({
                value: s,
                label: s,
              })),
            ]}
            value={selectedSubject}
            onChange={(e) => setSelectedSubject(e.target.value)}
            className="bg-[#F8F7F3] border-neutral-200"
          />
        </div>
      </div>

      {/* 3. Critical Academic Gaps */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-5 sm:p-6 shadow-soft-sm space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-neutral-100 pb-4">
          <div>
            <div className="flex items-center gap-2.5">
              <h2 className="text-base sm:text-lg font-bold text-neutral-900">
                Öncelikli Konu Açıkları
              </h2>
              {totalGapsCount > 0 && (
                <span className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full text-xs font-bold bg-rose-50 text-rose-700 border border-rose-200">
                  <AlertTriangle className="h-3.5 w-3.5" />
                  {criticalGapsCount} Kritik / {totalGapsCount} Toplam Açık
                </span>
              )}
            </div>
            <p className="text-xs text-neutral-500 mt-1">
              Öğrencilerin deneme sınavlarında ardışık hata veya kavram boşluğu yaşadığı tespit edilen konular
            </p>
          </div>
        </div>

        {studentGroupedGaps.length === 0 ? (
          <EmptyState
            icon={<CheckCircle2 className="h-8 w-8 text-emerald-500" />}
            title="Kritik Akademik Açık Yok"
            description="Seçilen arama kriterlerinde veya şubede öncelikli akademik eksik bulunmuyor."
          />
        ) : (
          <div
            className={cn(
              'grid gap-4',
              studentGroupedGaps.length === 1 ? 'grid-cols-1' : 'grid-cols-1 lg:grid-cols-2'
            )}
          >
            {studentGroupedGaps.map((group) => (
              <div
                key={group.studentId}
                className="bg-[#F8F7F3] rounded-2xl border border-neutral-200/80 p-4 sm:p-5 flex flex-col justify-between space-y-3.5 hover:border-primary-300 transition-colors"
              >
                <div className="space-y-3">
                  <div className="flex items-center justify-between gap-2 border-b border-neutral-200/60 pb-3">
                    <div className="flex items-center gap-3 min-w-0">
                      <div className="w-10 h-10 rounded-full bg-[#14233E] text-white text-xs font-bold flex items-center justify-center flex-shrink-0 shadow-sm">
                        {group.initials}
                      </div>
                      <div className="min-w-0">
                        <h3 className="font-bold text-sm text-neutral-900 truncate">
                          {group.studentName}
                        </h3>
                        <span className="text-xs text-neutral-500">{group.grade}</span>
                      </div>
                    </div>

                    <span className="text-[11px] font-bold px-2 py-0.5 rounded-full bg-white text-neutral-700 border border-neutral-200 shadow-2xs">
                      {group.gaps.length} Eksik Konu
                    </span>
                  </div>

                  <div
                    className={cn(
                      'space-y-2',
                      studentGroupedGaps.length === 1 && group.gaps.length > 1
                        ? 'grid grid-cols-1 sm:grid-cols-2 gap-2 space-y-0'
                        : ''
                    )}
                  >
                    {group.gaps.map((gap) => (
                      <div
                        key={gap.id}
                        className="bg-white p-3 rounded-xl border border-neutral-200/70 space-y-1 shadow-2xs"
                      >
                        <div className="flex items-center justify-between gap-2">
                          <span className="font-bold text-xs text-neutral-900">{gap.topic}</span>
                          <span
                            className={cn(
                              'text-[10px] font-bold px-2 py-0.5 rounded-full border',
                              gap.severity === 'critical'
                                ? 'bg-rose-50 text-rose-700 border-rose-200'
                                : 'bg-amber-50 text-amber-700 border-amber-200'
                            )}
                          >
                            {gap.severity === 'critical' ? 'Kritik' : 'Öncelikli'}
                          </span>
                        </div>
                        <p className="text-[11px] text-neutral-600 line-clamp-2">{gap.detail}</p>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="pt-2">
                  <button
                    type="button"
                    onClick={() => navigate(`/teacher/students/${group.studentId}`)}
                    className="w-full inline-flex items-center justify-center gap-1.5 py-2 px-3 text-xs font-semibold text-primary-700 hover:text-primary-800 bg-white rounded-xl border border-primary-200 hover:bg-primary-50 transition-colors shadow-2xs"
                  >
                    <span>Öğrenci Karnesi & Detay</span>
                    <ChevronRight className="h-3.5 w-3.5" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* 4. Topic / Outcome Performance from Backend Intelligence */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-5 sm:p-6 shadow-soft-sm space-y-4">
        <div className="border-b border-neutral-100 pb-3.5">
          <h2 className="text-base sm:text-lg font-bold text-neutral-900">
            Soru Bazlı Konu & Kazanım İlerlemesi
          </h2>
          <p className="text-xs text-neutral-500 mt-0.5">
            Öğrencilerin çözdüğü sorular üzerinden hesaplanan ve deneme sınavlarında ölçülen kazanım oranları
          </p>
        </div>

        {filteredTopics.length === 0 ? (
          <div className="p-8 text-center space-y-2 bg-[#F8F7F3] rounded-xl border border-neutral-200/70">
            <Sparkles className="h-7 w-7 text-neutral-400 mx-auto" />
            <p className="text-xs font-semibold text-neutral-700">
              Bu kapsamda kayıtlı konu kazanım verisi bulunmuyor.
            </p>
            <p className="text-[11px] text-neutral-500 max-w-sm mx-auto">
              Öğrenciler branş denemelerine katıldıkça veya soru bankası üzerinden ödevleri çözdükçe başarı oranları listelenecektir.
            </p>
          </div>
        ) : (
          <div className="grid grid-cols-1 lg:grid-cols-2 xl:grid-cols-3 gap-3.5">
            {filteredTopics.map((item) => {
              const isLow = item.status === 'needs_work';
              const isMastered = item.status === 'mastered';

              return (
                <div
                  key={item.id}
                  className="bg-[#F8F7F3] rounded-xl border border-neutral-200/70 p-4 space-y-3 flex flex-col justify-between"
                >
                  <div className="space-y-1.5">
                    <div className="flex items-start justify-between gap-2">
                      <div className="flex items-center gap-1.5 min-w-0">
                        {isMastered && <CheckCircle2 className="h-4 w-4 text-emerald-600 flex-shrink-0" />}
                        {isLow && <XCircle className="h-4 w-4 text-rose-600 flex-shrink-0" />}
                        {!isMastered && !isLow && <AlertTriangle className="h-4 w-4 text-amber-500 flex-shrink-0" />}
                        <span className="font-bold text-xs sm:text-sm text-neutral-900 truncate">
                          {item.topicName}
                        </span>
                      </div>
                      <span
                        className={cn(
                          'text-[10px] font-bold px-2 py-0.5 rounded-full border flex-shrink-0',
                          isLow
                            ? 'bg-rose-50 text-rose-700 border-rose-200'
                            : isMastered
                            ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                            : 'bg-amber-50 text-amber-700 border-amber-200'
                        )}
                      >
                        %{item.accuracy}
                      </span>
                    </div>

                    <div className="text-[11px] text-neutral-500 flex items-center justify-between">
                      <span>Ders: <strong className="text-neutral-700">{item.subjectName}</strong></span>
                      {item.studentName && <span className="truncate max-w-[120px]">{item.studentName}</span>}
                    </div>
                  </div>

                  <div className="space-y-1.5 pt-2 border-t border-neutral-200/60">
                    <div className="flex items-center justify-between text-[11px] text-neutral-600">
                      <span>{item.totalQuestions} Soru Analizi</span>
                      <span className="font-mono text-neutral-700">D:{item.totalCorrect} Y:{item.totalWrong} B:{item.totalBlank}</span>
                    </div>
                    <div className="h-1.5 w-full bg-neutral-200 rounded-full overflow-hidden">
                      <div
                        className={cn(
                          'h-full rounded-full transition-all duration-500',
                          isLow ? 'bg-rose-500' : isMastered ? 'bg-emerald-500' : 'bg-amber-500'
                        )}
                        style={{ width: `${item.accuracy}%` }}
                      />
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* 5. Quick Actions */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-5 shadow-soft-sm">
        <h2 className="text-sm font-bold text-neutral-900 mb-3">Hızlı Akademik İşlemler</h2>
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <button
            type="button"
            onClick={() => navigate('/teacher/classes')}
            className="flex items-center justify-between p-3.5 rounded-xl border border-neutral-200/80 hover:border-primary-300 hover:bg-neutral-50/80 transition-all text-left group"
          >
            <div className="flex items-center gap-3">
              <div className="w-9 h-9 rounded-xl bg-primary-50 text-primary-700 flex items-center justify-center">
                <Layers className="h-4 w-4" />
              </div>
              <div>
                <span className="text-xs font-bold text-neutral-900 block group-hover:text-primary-700">Sınıflarım</span>
                <span className="text-[11px] text-neutral-500">Şube listesi ve mevcut</span>
              </div>
            </div>
            <ArrowRight className="h-4 w-4 text-neutral-400 group-hover:text-primary-600 transform group-hover:translate-x-1 transition-transform" />
          </button>

          <button
            type="button"
            onClick={() => navigate('/teacher/questions')}
            className="flex items-center justify-between p-3.5 rounded-xl border border-neutral-200/80 hover:border-primary-300 hover:bg-neutral-50/80 transition-all text-left group"
          >
            <div className="flex items-center gap-3">
              <div className="w-9 h-9 rounded-xl bg-primary-50 text-primary-700 flex items-center justify-center">
                <HelpCircle className="h-4 w-4" />
              </div>
              <div>
                <span className="text-xs font-bold text-neutral-900 block group-hover:text-primary-700">Soru Bankası</span>
                <span className="text-[11px] text-neutral-500">Ödev ve test hazırlığı</span>
              </div>
            </div>
            <ArrowRight className="h-4 w-4 text-neutral-400 group-hover:text-primary-600 transform group-hover:translate-x-1 transition-transform" />
          </button>

          <button
            type="button"
            onClick={() => navigate('/teacher/messages')}
            className="flex items-center justify-between p-3.5 rounded-xl border border-neutral-200/80 hover:border-primary-300 hover:bg-neutral-50/80 transition-all text-left group"
          >
            <div className="flex items-center gap-3">
              <div className="w-9 h-9 rounded-xl bg-primary-50 text-primary-700 flex items-center justify-center">
                <BookOpen className="h-4 w-4" />
              </div>
              <div>
                <span className="text-xs font-bold text-neutral-900 block group-hover:text-primary-700">Öğrenci İletişimi</span>
                <span className="text-[11px] text-neutral-500">Bireysel akademik mesajlar</span>
              </div>
            </div>
            <ArrowRight className="h-4 w-4 text-neutral-400 group-hover:text-primary-600 transform group-hover:translate-x-1 transition-transform" />
          </button>
        </div>
      </div>
    </div>
  );
};
