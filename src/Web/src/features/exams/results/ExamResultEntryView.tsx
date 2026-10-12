import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, ShieldAlert } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Card } from '@/components/ui/Card';
import { LoadingState } from '@/components/ui/LoadingState';
import { ErrorState } from '@/components/ui/ErrorState';
import { ExamDetailDto } from '@/types/exam';
import {
  ImportType,
  SingleManualResultResponse,
  BulkManualResultResponse,
  ExamResultImportCommitResultDto,
} from '@/types/examResultImport';
import { examService } from '@/services/examService';
import { useAuth } from '@/auth';

import { MethodSelectionStep, EntryMode } from './MethodSelectionStep';
import { SingleManualEntryForm } from './SingleManualEntryForm';
import { BulkManualEntryTable } from './BulkManualEntryTable';
import { ExcelImportWizard } from './ExcelImportWizard';
import { ResultEntrySuccessSummary } from './ResultEntrySuccessSummary';

interface ExamResultEntryViewProps {
  portalRole?: 'admin' | 'coach' | 'teacher';
}

export const ExamResultEntryView: React.FC<ExamResultEntryViewProps> = ({ portalRole = 'admin' }) => {
  const { examId } = useParams<{ examId: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();

  const [exam, setExam] = useState<ExamDetailDto | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  const [activeMode, setActiveMode] = useState<EntryMode | null>(null);
  const [completedType, setCompletedType] = useState<'single_manual' | 'bulk_manual' | 'excel_commit' | null>(null);

  const [singleSuccess, setSingleSuccess] = useState<SingleManualResultResponse | undefined>();
  const [bulkSuccess, setBulkSuccess] = useState<BulkManualResultResponse | undefined>();
  const [commitSuccess, setCommitSuccess] = useState<ExamResultImportCommitResultDto | undefined>();

  useEffect(() => {
    if (!examId) return;

    const loadExam = async () => {
      try {
        setLoading(true);
        setError(null);
        const data = await examService.getExamDetail(examId);
        setExam(data);
      } catch (err: unknown) {
        setError(err instanceof Error ? err.message : 'Sınav bilgileri yüklenemedi.');
      } finally {
        setLoading(false);
      }
    };

    loadExam();
  }, [examId]);

  // Authorization Check — Backend CanRecordExamResults policy allows SuperAdmin, InstitutionAdmin, Coach. Denies Teacher & Student.
  const isTeacher = user?.role === 'teacher' || portalRole === 'teacher';
  if (isTeacher) {
    return (
      <div className="p-6 max-w-3xl mx-auto space-y-6">
        <Card className="p-8 text-center space-y-4 border-amber-200 bg-amber-50/50 dark:bg-amber-950/20">
          <div className="mx-auto h-12 w-12 rounded-full bg-amber-100 text-amber-600 flex items-center justify-center">
            <ShieldAlert className="h-6 w-6" />
          </div>
          <h2 className="text-xl font-bold text-gray-900 dark:text-white">Yetki Sınırlaması</h2>
          <p className="text-sm text-gray-600 dark:text-gray-400">
            Sınav sonucu girme ve Excel aktarım yetkisi yalnızca Kurum Yöneticileri ve Koçlar için geçerlidir. Öğretmen rolü için sonuç kaydetme izni tanımlanmamıştır.
          </p>
          <div className="pt-2">
            <Button variant="outline" onClick={() => navigate(-1)}>
              Geri Dön
            </Button>
          </div>
        </Card>
      </div>
    );
  }

  if (loading) {
    return <LoadingState message="Sınav bilgileri ve ders yapıları yükleniyor..." />;
  }

  if (error || !exam) {
    return (
      <ErrorState
        title="Sınav Bulunamadı"
        message={error || 'İstenen sınav detayına erişilemedi.'}
        onRetry={() => window.location.reload()}
      />
    );
  }

  const handleGoToExams = () => {
    if (portalRole === 'coach') {
      navigate('/coach/today');
    } else {
      navigate('/admin/overview');
    }
  };

  const handleNewEntry = () => {
    setActiveMode(null);
    setCompletedType(null);
    setSingleSuccess(undefined);
    setBulkSuccess(undefined);
    setCommitSuccess(undefined);
  };

  return (
    <div className="p-6 space-y-6">
      {/* Top Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <button
            type="button"
            onClick={handleGoToExams}
            className="p-2 rounded-lg hover:bg-gray-100 dark:hover:bg-navy-900 text-gray-500 transition-colors"
            title="Geri Dön"
          >
            <ArrowLeft className="h-5 w-5" />
          </button>
          <div>
            <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Sınav Sonuç İşlemleri</h1>
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {exam.title} ({exam.code}) — Soru Sayısı: {exam.sections.reduce((a, b) => a + b.questionCount, 0)} Soru ({exam.sections.length} Ders)
            </p>
          </div>
        </div>
      </div>

      {/* Render Active View State */}
      {completedType ? (
        <ResultEntrySuccessSummary
          type={completedType}
          singleResult={singleSuccess}
          bulkResult={bulkSuccess}
          commitResult={commitSuccess}
          onNewEntry={handleNewEntry}
          onGoToExams={handleGoToExams}
        />
      ) : activeMode === 'manual_single' ? (
        <SingleManualEntryForm
          exam={exam}
          onSuccess={(res) => {
            setSingleSuccess(res);
            setCompletedType('single_manual');
          }}
          onCancel={() => setActiveMode(null)}
        />
      ) : activeMode === 'manual_bulk' ? (
        <BulkManualEntryTable
          exam={exam}
          onSuccess={(res) => {
            setBulkSuccess(res);
            setCompletedType('bulk_manual');
          }}
          onCancel={() => setActiveMode(null)}
        />
      ) : activeMode === 'excel_aggregate' ? (
        <ExcelImportWizard
          exam={exam}
          initialImportType={ImportType.AggregateResults}
          onSuccess={(res) => {
            setCommitSuccess(res);
            setCompletedType('excel_commit');
          }}
          onCancel={() => setActiveMode(null)}
        />
      ) : activeMode === 'excel_question' ? (
        <ExcelImportWizard
          exam={exam}
          initialImportType={ImportType.QuestionAnswers}
          onSuccess={(res) => {
            setCommitSuccess(res);
            setCompletedType('excel_commit');
          }}
          onCancel={() => setActiveMode(null)}
        />
      ) : (
        <MethodSelectionStep onSelectMode={setActiveMode} />
      )}
    </div>
  );
};
