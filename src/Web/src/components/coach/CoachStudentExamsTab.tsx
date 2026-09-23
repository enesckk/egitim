import React, { useCallback, useEffect, useState } from 'react';
import { Award, AlertTriangle, FileText, RefreshCw } from 'lucide-react';
import { examService } from '@/services/examService';
import { StudentExamResultSummaryDto, StudentExamAnalysisDto } from '@/types/exam';
import { Alert } from '@/components/ui/Alert';
import { Button } from '@/components/ui/Button';
import { Skeleton } from '@/components/ui/Skeleton';

interface CoachStudentExamsTabProps {
  studentId: string;
}

export const CoachStudentExamsTab: React.FC<CoachStudentExamsTabProps> = ({ studentId }) => {
  const [results, setResults] = useState<StudentExamResultSummaryDto[]>([]);
  const [analysis, setAnalysis] = useState<StudentExamAnalysisDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const [resList, analysisData] = await Promise.all([
        examService.getStudentExamResults(studentId),
        examService.getStudentExamAnalysis(studentId),
      ]);
      setResults(resList.items || []);
      setAnalysis(analysisData);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Öğrenci sınav verileri yüklenirken hata oluştu.');
    } finally {
      setLoading(false);
    }
  }, [studentId]);

  useEffect(() => {
    if (studentId) {
      loadData();
    }
  }, [studentId, loadData]);

  if (loading) {
    return (
      <div className="space-y-4" data-testid="coach-student-exams-loading">
        <Skeleton className="h-28 w-full rounded-2xl" />
        <Skeleton className="h-20 w-full rounded-xl" />
        <Skeleton className="h-20 w-full rounded-xl" />
      </div>
    );
  }

  if (error) {
    return (
      <div className="py-6 space-y-3" data-testid="coach-student-exams-error">
        <Alert variant="danger" title="Sınav Verileri Alınamadı">
          {error}
        </Alert>
        <Button size="sm" variant="secondary" onClick={loadData} leftIcon={<RefreshCw className="h-4 w-4" />}>
          Yeniden Dene
        </Button>
      </div>
    );
  }

  if (results.length === 0) {
    return (
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-8 text-center" data-testid="coach-student-exams-empty">
        <FileText className="h-10 w-10 text-neutral-400 mx-auto mb-3" />
        <h3 className="text-base font-semibold text-neutral-900">Sonuçlanmış Sınav Yok</h3>
        <p className="text-xs text-neutral-500 max-w-sm mx-auto mt-1">
          Öğrencinin henüz onaylanmış veya tamamlanmış deneme sınavı kaydı bulunmuyor.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-4 select-none" data-testid="coach-student-exams-tab">
      {/* Overview Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
        <div className="bg-white rounded-xl border border-neutral-200/80 p-3 shadow-soft-sm">
          <span className="text-xs text-neutral-500">Ortalama Net</span>
          <p className="font-mono text-lg font-bold text-neutral-900 mt-1">{analysis?.averageTotalNet ?? 0} Net</p>
        </div>
        <div className="bg-white rounded-xl border border-neutral-200/80 p-3 shadow-soft-sm">
          <span className="text-xs text-neutral-500">Son Deneme Neti</span>
          <p className="font-mono text-lg font-bold text-primary-700 mt-1">{analysis?.latestTotalNet ?? 0} Net</p>
        </div>
        <div className="bg-white rounded-xl border border-neutral-200/80 p-3 shadow-soft-sm">
          <span className="text-xs text-neutral-500">Net Değişimi (Delta)</span>
          <p className={`font-mono text-lg font-bold mt-1 ${(analysis?.delta ?? 0) >= 0 ? 'text-success' : 'text-danger'}`}>
            {(analysis?.delta ?? 0) >= 0 ? `+${analysis?.delta}` : analysis?.delta} Net
          </p>
        </div>
        <div className="bg-white rounded-xl border border-neutral-200/80 p-3 shadow-soft-sm">
          <span className="text-xs text-neutral-500">Toplam Sınav</span>
          <p className="font-mono text-lg font-bold text-neutral-900 mt-1">{analysis?.attemptCount ?? 0} Adet</p>
        </div>
      </div>

      {/* Weak Topics Section */}
      {analysis?.weakestTopics && analysis.weakestTopics.length > 0 && (
        <div className="bg-amber-50/50 border border-amber-200/60 rounded-xl p-4">
          <div className="flex items-center gap-2 mb-2">
            <AlertTriangle className="h-4 w-4 text-amber-600" />
            <h4 className="text-xs font-semibold text-amber-900">Geliştirilmesi Gereken Konular</h4>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 text-xs">
            {analysis.weakestTopics.slice(0, 4).map((t) => (
              <div key={t.topicId} className="flex items-center justify-between bg-white rounded-lg p-2 border border-amber-100">
                <div>
                  <span className="font-medium text-neutral-900">{t.topicName}</span>
                  <span className="text-[10px] text-neutral-400 block">{t.subjectName}</span>
                </div>
                <span className="font-mono font-bold text-amber-700">{t.averageNet} Net</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Attempts History List */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-sm">
        <h4 className="text-xs font-semibold text-neutral-900 mb-3 flex items-center gap-2">
          <Award className="h-4 w-4 text-primary-600" /> Deneme Geçmişi
        </h4>
        <div className="space-y-2">
          {results.map((res) => (
            <div key={res.attemptId} className="flex items-center justify-between p-3 rounded-xl border border-neutral-100 hover:bg-neutral-50 transition-colors">
              <div>
                <h5 className="text-xs font-semibold text-neutral-900">{res.examTitle}</h5>
                <span className="text-[10px] text-neutral-400">
                  {new Date(res.takenAt).toLocaleDateString('tr-TR')} • {res.examTypeName}
                </span>
              </div>
              <div className="text-right">
                <span className="font-mono text-sm font-bold text-neutral-900 block">{res.totalNet} Net</span>
                <span className="text-[10px] text-neutral-400">
                  D: {res.totalCorrect} | Y: {res.totalWrong} | B: {res.totalBlank}
                </span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
