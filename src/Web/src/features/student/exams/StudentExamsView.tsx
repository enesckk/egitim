import React, { useState, useEffect, useCallback } from 'react';
import { Award, FileText, AlertCircle, RefreshCw, ChevronLeft, ChevronRight } from 'lucide-react';
import { useAuth } from '@/auth';
import { academicService } from '@/services/academicService';
import { examService } from '@/services/examService';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { LatestExamSummaryCard } from './components/LatestExamSummaryCard';
import { ExamListItemCard } from './components/ExamListItemCard';
import { ExamDetailModal } from './components/ExamDetailModal';
import { ExamType, StudentExamItem, StudentExamsViewModel, SubjectExamResult } from './types';

export interface StudentExamsViewProps {
  initialData?: StudentExamsViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

const PAGE_SIZE = 10;

export const StudentExamsView: React.FC<StudentExamsViewProps> = ({
  initialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const { user } = useAuth();
  const userRef = React.useRef(user);
  userRef.current = user;

  const [data, setData] = useState<StudentExamsViewModel | undefined>(initialData);
  const [resolvedStudentId, setResolvedStudentId] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const [examTypeFilter, setExamTypeFilter] = useState<string>('');
  const [selectedExam, setSelectedExam] = useState<StudentExamItem | null>(null);
  const [isDetailOpen, setIsDetailOpen] = useState<boolean>(false);

  // Pagination state
  const [currentPage, setCurrentPage] = useState<number>(1);
  const [totalCount, setTotalCount] = useState<number>(0);

  const fetchExamData = useCallback(async (page: number = 1) => {
    if (initialData) return;

    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const realStudentId = await academicService.getCurrentStudentId();
      const studentId = realStudentId || userRef.current?.id || 'me';
      setResolvedStudentId(studentId);

      // Fetch paginated attempts from backend
      const res = await examService.getStudentExamResults(studentId, {
        page,
        pageSize: PAGE_SIZE,
      });

      if (res && res.items && res.items.length > 0) {
        setTotalCount(res.totalCount ?? res.items.length);

        // Sort items deterministically: takenAt descending, then attemptId tie-breaker
        const sortedItems = [...res.items].sort((a, b) => {
          const timeA = a.takenAt ? new Date(a.takenAt).getTime() : 0;
          const timeB = b.takenAt ? new Date(b.takenAt).getTime() : 0;
          if (timeB !== timeA) return timeB - timeA;
          return (b.attemptId || '').localeCompare(a.attemptId || '');
        });

        const mappedExams: StudentExamItem[] = sortedItems.map((item, idx) => {
          const typeStr = (item.examTypeName || 'TYT').toUpperCase();
          const type: ExamType = typeStr.includes('AYT') ? 'AYT' : typeStr.includes('BRANŞ') || typeStr.includes('BRANS') ? 'BRANS' : 'TYT';
          
          // Compute netChange only against a chronologically older, genuinely earlier attempt of the same exam type
          const currentItemTime = item.takenAt ? new Date(item.takenAt).getTime() : 0;
          const prevItem = sortedItems.slice(idx + 1).find((prev) => {
            const pStr = (prev.examTypeName || 'TYT').toUpperCase();
            const pType: ExamType = pStr.includes('AYT') ? 'AYT' : pStr.includes('BRANŞ') || pStr.includes('BRANS') ? 'BRANS' : 'TYT';
            const prevItemTime = prev.takenAt ? new Date(prev.takenAt).getTime() : 0;
            // Only compare if it's the same type AND strictly earlier in time
            return pType === type && prevItemTime < currentItemTime;
          });

          const currentNet = item.totalNet ?? 0;
          let netChange = 0;
          if (prevItem && prevItem.totalNet !== undefined) {
            netChange = Math.round((currentNet - prevItem.totalNet) * 100) / 100;
          }

          return {
            id: item.attemptId || item.examId,
            attemptId: item.attemptId,
            title: item.examTitle,
            type,
            date: item.takenAt ? new Date(item.takenAt).toLocaleDateString('tr-TR') : '',
            totalNet: currentNet,
            maxTotalNet: type === 'AYT' ? 80 : 120,
            netChange,
            subjects: [],
          };
        });

        // Enrich latest exam with subject breakdown if on page 1
        if (page === 1 && mappedExams[0]?.attemptId) {
          try {
            const detail = await examService.getStudentExamResultDetail(studentId, mappedExams[0].attemptId);
            if (detail?.subjectResults) {
              const subjects: SubjectExamResult[] = detail.subjectResults.map((sr) => ({
                subjectName: sr.subjectName,
                correct: sr.correct,
                wrong: sr.wrong,
                empty: sr.blank,
                net: sr.net,
                maxNet: sr.correct + sr.wrong + sr.blank || 40,
              }));
              const weakTopics = detail.subjectResults
                .flatMap((sr) => sr.topics || [])
                .filter((t) => t.wrong > t.correct)
                .map((t) => t.topicName)
                .slice(0, 4);
              mappedExams[0] = {
                ...mappedExams[0],
                subjects,
                focusWeakTopics: weakTopics.length > 0 ? weakTopics : undefined,
                isRawAvailable: detail.isRawAvailable,
              };
            }
          } catch {
            // Detail enrichment is optional
          }
        }

        const tytExams = mappedExams.filter((e) => e.type === 'TYT');
        const aytExams = mappedExams.filter((e) => e.type === 'AYT');
        const avgTYT = tytExams.length ? Math.round((tytExams.reduce((s, e) => s + e.totalNet, 0) / tytExams.length) * 100) / 100 : 0;
        const avgAYT = aytExams.length ? Math.round((aytExams.reduce((s, e) => s + e.totalNet, 0) / aytExams.length) * 100) / 100 : 0;

        setData((prev) => ({
          latestExam: page === 1 ? mappedExams[0] : (prev?.latestExam || mappedExams[0]),
          exams: mappedExams,
          averageNetTYT: avgTYT,
          averageNetAYT: avgAYT,
          targetNetTYT: 100,
          targetNetAYT: 70,
        }));
      } else {
        setData({
          latestExam: null as unknown as StudentExamItem,
          exams: [],
          averageNetTYT: 0,
          averageNetAYT: 0,
          targetNetTYT: 100,
          targetNetAYT: 70,
        });
        setTotalCount(0);
      }
    } catch {
      setData({
        latestExam: null as unknown as StudentExamItem,
        exams: [],
        averageNetTYT: 0,
        averageNetAYT: 0,
        targetNetTYT: 100,
        targetNetAYT: 70,
      });
      setTotalCount(0);
    } finally {
      setIsLoading(false);
    }
  }, [initialData]);

  useEffect(() => {
    if (initialData) {
      setData(initialData);
      setIsLoading(false);
    } else {
      fetchExamData(currentPage);
    }
  }, [initialData, fetchExamData, currentPage]);

  const handleOpenDetail = async (exam: StudentExamItem) => {
    setSelectedExam(exam);
    setIsDetailOpen(true);

    if (exam.subjects.length === 0 && resolvedStudentId && (exam.attemptId || exam.id)) {
      try {
        const detail = await examService.getStudentExamResultDetail(resolvedStudentId, exam.attemptId || exam.id);
        if (detail?.subjectResults) {
          const subjects: SubjectExamResult[] = detail.subjectResults.map((sr) => ({
            subjectName: sr.subjectName,
            correct: sr.correct,
            wrong: sr.wrong,
            empty: sr.blank,
            net: sr.net,
            maxNet: sr.correct + sr.wrong + sr.blank || 40,
          }));
          const weakTopics = detail.subjectResults
            .flatMap((sr) => sr.topics || [])
            .filter((t) => t.wrong > t.correct)
            .map((t) => t.topicName)
            .slice(0, 4);
          const enriched: StudentExamItem = {
            ...exam,
            subjects,
            focusWeakTopics: weakTopics.length > 0 ? weakTopics : exam.focusWeakTopics,
            isRawAvailable: detail.isRawAvailable,
          };
          setSelectedExam(enriched);
          setData((prev) =>
            prev
              ? {
                  ...prev,
                  latestExam: prev.latestExam?.id === exam.id ? enriched : prev.latestExam,
                  exams: prev.exams.map((e) => (e.id === exam.id ? enriched : e)),
                }
              : prev
          );
        }
      } catch {
        // Ignore detail fetch error
      }
    }
  };

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchExamData(currentPage);
    }
  };

  const activeData = data || initialData;
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="student-exams-loading">
        <div className="border-b border-neutral-200/60 pb-3 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Sınavlarım ve Karnelerim
          </h1>
          <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
            Sınav sonuçları, net dağılımları ve gelişim analizi
          </p>
        </div>
        <Skeleton className="h-56 w-full rounded-2xl" />
        <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
          <Skeleton className="h-20 w-full rounded-xl" />
          <Skeleton className="h-20 w-full rounded-xl" />
          <Skeleton className="h-20 w-full rounded-xl" />
          <Skeleton className="h-20 w-full rounded-xl" />
        </div>
        <Skeleton className="h-24 w-full rounded-2xl" />
        <Skeleton className="h-24 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (activeError) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4" data-testid="student-exams-error">
        <div className="border-b border-neutral-200/60 pb-3 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Sınavlarım ve Karnelerim
          </h1>
          <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
            Sınav sonuçları, net dağılımları ve gelişim analizi
          </p>
        </div>
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Deneme Sonuçları Yüklenemedi">
          {activeError}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={handleRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  const hasExams = activeData && activeData.exams && activeData.exams.length > 0;

  if (!hasExams) {
    return (
      <div className="max-w-5xl mx-auto select-none space-y-4" data-testid="student-exams-view">
        <div className="border-b border-neutral-200/60 pb-3 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Sınavlarım ve Karnelerim
          </h1>
          <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
            Sınav sonuçları, net dağılımları ve gelişim analizi
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-200/80 p-8 sm:p-12 shadow-soft-sm text-center">
          <div className="w-14 h-14 rounded-2xl bg-neutral-100 text-navy-900 flex items-center justify-center mx-auto mb-4 border border-neutral-200">
            <FileText className="h-7 w-7 text-primary-600" />
          </div>
          <h2 className="text-lg font-bold text-neutral-900">
            Kayıtlı Deneme Sınavı Bulunmuyor
          </h2>
          <p className="text-xs sm:text-sm text-neutral-500 max-w-md mx-auto mt-2 leading-relaxed">
            Sisteme henüz işlenmiş bir deneme sınavı sonucu bulunmuyor. Kurum denemelerine katıldıkça veya sınav sonuçlarınız girildikçe TYT/AYT netleriniz ve konu analizleriniz burada görüntülenecektir.
          </p>
        </div>
      </div>
    );
  }

  const filteredExams = activeData.exams.filter((ex) => {
    if (!examTypeFilter) return true;
    return ex.type === examTypeFilter;
  });

  const historyExams = activeData.latestExam && currentPage === 1
    ? filteredExams.filter((ex) => ex.id !== activeData.latestExam.id)
    : filteredExams;

  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-5" data-testid="student-exams-view">
      {/* Page Header */}
      <div className="border-b border-neutral-200/60 pb-3.5 sm:pb-4">
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
          Sınavlarım ve Karnelerim
        </h1>
        <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
          Sınav sonuçları, net dağılımları ve gelişim analizi
        </p>
      </div>

      {/* 1. Latest Exam Hero Card (Shown on page 1) */}
      {activeData.latestExam && currentPage === 1 && (
        <LatestExamSummaryCard
          exam={activeData.latestExam}
          onViewAnalysis={handleOpenDetail}
        />
      )}

      {/* 2. Target & Average Stats Row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 sm:gap-3">
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-3.5 sm:p-4 shadow-soft-xs">
          <span className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">TYT Ortalama</span>
          <p className="font-mono text-xl sm:text-2xl font-bold text-neutral-900 mt-1">
            {activeData.averageNetTYT > 0 ? `${activeData.averageNetTYT} Net` : '—'}
          </p>
          <span className="text-[11px] text-neutral-400 font-mono">Hedef: {activeData.targetNetTYT}</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-200/80 p-3.5 sm:p-4 shadow-soft-xs">
          <span className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">AYT Ortalama</span>
          <p className="font-mono text-xl sm:text-2xl font-bold text-neutral-900 mt-1">
            {activeData.averageNetAYT > 0 ? `${activeData.averageNetAYT} Net` : '—'}
          </p>
          <span className="text-[11px] text-neutral-400 font-mono">Hedef: {activeData.targetNetAYT}</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-200/80 p-3.5 sm:p-4 shadow-soft-xs">
          <span className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">Toplam Deneme</span>
          <p className="font-mono text-xl sm:text-2xl font-bold text-primary-700 mt-1">
            {totalCount || activeData.exams.length} Adet
          </p>
          <span className="text-[11px] text-neutral-400 font-medium">Kayıtlı sınav</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-200/80 p-3.5 sm:p-4 shadow-soft-xs">
          <span className="text-xs font-semibold text-neutral-500 uppercase tracking-wider">Son Net Durumu</span>
          {(() => {
            const latestNet = activeData.latestExam?.totalNet;
            return (
              <p className="font-mono text-xl sm:text-2xl font-bold text-neutral-900 mt-1">
                {latestNet !== undefined ? `${latestNet.toFixed(2)} Net` : '—'}
              </p>
            );
          })()}
          <span className="text-[11px] text-neutral-400">Son değerlendirilen</span>
        </div>
      </div>

      {/* 3. Filter Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2.5 bg-white rounded-2xl border border-neutral-200/80 p-3.5 sm:px-4 shadow-soft-xs">
        <div className="flex items-center gap-2">
          <Award className="h-4 w-4 text-primary-600" />
          <h3 className="text-sm font-bold text-neutral-900">
            Geçmiş Deneme Sınavları ({totalCount || filteredExams.length})
          </h3>
        </div>

        <div className="flex items-center gap-2">
          <Select
            options={[
              { value: '', label: 'Tüm Sınav Türleri' },
              { value: 'TYT', label: 'Yalnızca TYT' },
              { value: 'AYT', label: 'Yalnızca AYT' },
            ]}
            value={examTypeFilter}
            onChange={(e) => setExamTypeFilter(e.target.value)}
            className="text-xs min-h-[36px] py-1 pl-2.5 pr-7 font-medium"
          />
        </div>
      </div>

      {/* 4. Exams History List */}
      {historyExams.length > 0 ? (
        <div className="space-y-2.5 sm:space-y-3">
          {historyExams.map((exam) => (
            <ExamListItemCard
              key={exam.id}
              exam={exam}
              onClick={handleOpenDetail}
            />
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<FileText className="h-6 w-6 text-neutral-400" />}
          title="Geçmiş Deneme Sınavı Bulunamadı"
          description={
            filteredExams.length === 1 && activeData.latestExam && currentPage === 1
              ? 'Son denemeniz yukarıda özetlenmiştir. Bu sayfada daha eski bir deneme kaydı bulunmuyor.'
              : 'Seçilen sınav türü filtresine uygun sonuçlanmış deneme kaydı yok.'
          }
          action={
            examTypeFilter ? (
              <Button size="sm" variant="secondary" onClick={() => setExamTypeFilter('')}>
                Filtreyi Temizle
              </Button>
            ) : undefined
          }
        />
      )}

      {/* 5. Pagination Bar */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between bg-white rounded-2xl border border-neutral-200/80 px-4 py-3 shadow-soft-xs">
          <span className="text-xs text-neutral-500 font-medium">
            Sayfa {currentPage} / {totalPages}
          </span>
          <div className="flex items-center gap-2">
            <Button
              variant="outline"
              size="sm"
              disabled={currentPage <= 1 || isLoading}
              onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
              leftIcon={<ChevronLeft className="h-4 w-4" />}
            >
              Önceki
            </Button>
            <Button
              variant="outline"
              size="sm"
              disabled={currentPage >= totalPages || isLoading}
              onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
              rightIcon={<ChevronRight className="h-4 w-4" />}
            >
              Sonraki
            </Button>
          </div>
        </div>
      )}

      {/* Exam Detail Modal */}
      <ExamDetailModal
        exam={selectedExam}
        isOpen={isDetailOpen}
        onClose={() => setIsDetailOpen(false)}
      />
    </div>
  );
};
