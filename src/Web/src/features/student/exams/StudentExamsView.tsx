import React, { useState, useEffect, useCallback } from 'react';
import { Award, FileText, AlertCircle, RefreshCw } from 'lucide-react';
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

  const fetchExamData = useCallback(async () => {
    if (initialData) return;

    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const realStudentId = await academicService.getCurrentStudentId();
      const studentId = realStudentId || userRef.current?.id || 'me';
      setResolvedStudentId(studentId);

      const res = await examService.getStudentExamResults(studentId);

      if (res && res.items && res.items.length > 0) {
        const mappedExams: StudentExamItem[] = res.items.map((item, idx) => {
          const typeStr = (item.examTypeName || 'TYT').toUpperCase();
          const type: ExamType = typeStr.includes('AYT') ? 'AYT' : typeStr.includes('BRANŞ') || typeStr.includes('BRANS') ? 'BRANS' : 'TYT';
          const prevItem = res.items.slice(idx + 1).find((prev) => {
            const pStr = (prev.examTypeName || 'TYT').toUpperCase();
            const pType: ExamType = pStr.includes('AYT') ? 'AYT' : pStr.includes('BRANŞ') || pStr.includes('BRANS') ? 'BRANS' : 'TYT';
            return pType === type;
          });
          const currentNet = item.totalNet ?? 0;
          const prevNet = prevItem ? (prevItem.totalNet ?? 0) : currentNet;
          const netChange = prevItem ? Math.round((currentNet - prevNet) * 100) / 100 : 0;

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

        // Enrich latest exam with subject breakdown if available
        if (mappedExams[0]?.attemptId) {
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

        setData({
          latestExam: mappedExams[0],
          exams: mappedExams,
          averageNetTYT: avgTYT,
          averageNetAYT: avgAYT,
          targetNetTYT: 100,
          targetNetAYT: 70,
        });
      } else {
        setData({
          latestExam: null as unknown as StudentExamItem,
          exams: [],
          averageNetTYT: 0,
          averageNetAYT: 0,
          targetNetTYT: 100,
          targetNetAYT: 70,
        });
      }
    } catch {
      // Gracefully fall back to honest empty state when backend is unreachable or unconfigured
      setData({
        latestExam: null as unknown as StudentExamItem,
        exams: [],
        averageNetTYT: 0,
        averageNetAYT: 0,
        targetNetTYT: 100,
        targetNetAYT: 70,
      });
    } finally {
      setIsLoading(false);
    }
  }, [initialData]);

  useEffect(() => {
    if (initialData) {
      setData(initialData);
      setIsLoading(false);
    } else {
      fetchExamData();
    }
  }, [initialData, fetchExamData]);

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
      fetchExamData();
    }
  };

  const activeData = data || initialData;
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="student-exams-loading">
        <div className="border-b border-neutral-100 pb-3 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Deneme Sınavları
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
        <div className="border-b border-neutral-100 pb-3 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Deneme Sınavları
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
        <div className="border-b border-neutral-100 pb-3 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Deneme Sınavları
          </h1>
          <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
            Sınav sonuçları, net dağılımları ve gelişim analizi
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-200/80 p-6 sm:p-8 shadow-soft-sm text-center">
          <div className="w-12 h-12 rounded-2xl bg-navy-50 text-navy-800 flex items-center justify-center mx-auto mb-4 border border-navy-100">
            <FileText className="h-6 w-6 text-primary-600" />
          </div>
          <h2 className="text-lg font-semibold text-neutral-900">
            Kayıtlı Deneme Sınavı Bulunmuyor
          </h2>
          <p className="text-xs sm:text-sm text-neutral-600 max-w-md mx-auto mt-2 leading-relaxed">
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

  const historyExams = activeData.latestExam
    ? filteredExams.filter((ex) => ex.id !== activeData.latestExam.id)
    : filteredExams;

  return (
    <div className="max-w-5xl mx-auto select-none space-y-3.5 sm:space-y-4" data-testid="student-exams-view">
      <div className="border-b border-neutral-100 pb-3 sm:pb-4">
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
          Deneme Sınavları
        </h1>
        <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
          Sınav sonuçları, net dağılımları ve gelişim analizi
        </p>
      </div>

      {/* 1. Latest Exam Summary Card */}
      {activeData.latestExam && (
        <LatestExamSummaryCard
          exam={activeData.latestExam}
          onViewAnalysis={handleOpenDetail}
        />
      )}

      {/* 2. Target & Average Stats Row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-2.5 sm:gap-3">
        <div className="bg-white rounded-2xl border border-neutral-100 p-3">
          <span className="text-xs text-neutral-400">TYT Ortalama</span>
          <p className="font-mono text-lg sm:text-xl font-bold text-neutral-900 mt-0.5">
            {activeData.averageNetTYT} Net
          </p>
          <span className="text-[10px] text-neutral-400 font-mono">Hedef: {activeData.targetNetTYT}</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-3">
          <span className="text-xs text-neutral-400">AYT Ortalama</span>
          <p className="font-mono text-lg sm:text-xl font-bold text-neutral-900 mt-0.5">
            {activeData.averageNetAYT} Net
          </p>
          <span className="text-[10px] text-neutral-400 font-mono">Hedef: {activeData.targetNetAYT}</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-3">
          <span className="text-xs text-neutral-400">Girilmiş Deneme</span>
          <p className="font-mono text-lg sm:text-xl font-bold text-primary-700 mt-0.5">
            {activeData.exams.length} Adet
          </p>
          <span className="text-[10px] text-neutral-400 font-medium">Toplam kayıt</span>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-3">
          <span className="text-xs text-neutral-400">Genel Net Artışı</span>
          {(() => {
            const overallDelta =
              activeData.exams.length > 1
                ? Math.round((activeData.exams[0].totalNet - activeData.exams[activeData.exams.length - 1].totalNet) * 100) / 100
                : activeData.latestExam?.netChange ?? 0;
            return (
              <p className={`font-mono text-lg sm:text-xl font-bold mt-0.5 ${overallDelta >= 0 ? 'text-success' : 'text-danger'}`}>
                {overallDelta >= 0 ? `+${overallDelta}` : overallDelta} Net
              </p>
            );
          })()}
          <span className="text-[10px] text-neutral-400">Başlangıçtan bu yana</span>
        </div>
      </div>

      {/* 3. Filter Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 bg-white rounded-2xl border border-neutral-100 p-3 sm:px-4">
        <div className="flex items-center gap-2">
          <Award className="h-4 w-4 text-primary-600" />
          <h3 className="text-sm font-semibold text-neutral-900">
            Geçmiş Deneme Sınavları ({historyExams.length})
          </h3>
        </div>

        <div className="flex items-center gap-2">
          <Select
            options={[
              { value: 'TYT', label: 'Yalnızca TYT' },
              { value: 'AYT', label: 'Yalnızca AYT' },
            ]}
            value={examTypeFilter}
            onChange={(e) => setExamTypeFilter(e.target.value)}
            placeholder="Tüm Sınav Türleri"
            className="text-xs min-h-[34px] py-1 pl-2.5 pr-7"
          />
        </div>
      </div>

      {/* 4. Exams History List */}
      {historyExams.length > 0 ? (
        <div className="space-y-2.5">
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
            filteredExams.length === 1 && activeData.latestExam
              ? 'Son denemeniz yukarıda özetlenmiştir. Daha eski bir deneme kaydı bulunmuyor.'
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

      {/* Exam Detail Modal */}
      <ExamDetailModal
        exam={selectedExam}
        isOpen={isDetailOpen}
        onClose={() => setIsDetailOpen(false)}
      />
    </div>
  );
};
