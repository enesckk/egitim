import React, { useState } from 'react';
import { BookOpen, AlertTriangle, AlertCircle, RefreshCw, CheckCircle2, XCircle } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Tabs } from '@/components/ui/Tabs';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { EmptyState } from '@/components/ui/EmptyState';
import { initialTeacherAcademicData } from './mockData';
import { TeacherAcademicViewModel } from './types';

export interface TeacherAcademicViewProps {
  initialData?: TeacherAcademicViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherAcademicView: React.FC<TeacherAcademicViewProps> = ({
  initialData = initialTeacherAcademicData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const [selectedSubject, setSelectedSubject] = useState(initialData.subjects[0] || 'Matematik');

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <Skeleton className="h-28 w-full rounded-2xl" />
        <div className="space-y-3">
          <Skeleton className="h-44 w-full rounded-2xl" />
          <Skeleton className="h-44 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Akademik Takip Yüklenemedi">
          {errorMessage}
        </Alert>
        {onRetry && (
          <div className="text-center pt-2">
            <Button variant="primary" size="sm" onClick={onRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
              Tekrar Dene
            </Button>
          </div>
        )}
      </div>
    );
  }

  const subjectTabs = initialData.subjects.map((sub) => ({
    id: sub,
    label: sub,
  }));

  const filteredTopics = initialData.topics.filter((t) => t.subject === selectedSubject);
  const relevantAlerts = initialData.alerts.filter((a) => a.subject === selectedSubject);

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* Header */}
      <div>
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
          Akademik Takip & Kazanım Hakimiyeti
        </h1>
        <p className="text-xs text-neutral-400 mt-0.5">
          Branş bazında müfredat kazanımları, şube başarı oranları ve kritik konu açıkları
        </p>
      </div>

      {/* Subject Tabs */}
      <div className="bg-white rounded-2xl border border-neutral-100 px-4 pt-1 shadow-soft-sm">
        <Tabs
          tabs={subjectTabs}
          activeId={selectedSubject}
          onChange={setSelectedSubject}
        />
      </div>

      {/* Academic Alerts */}
      {relevantAlerts.length > 0 && (
        <div className="space-y-2.5">
          {relevantAlerts.map((alert) => (
            <div
              key={alert.id}
              className="bg-danger-light border border-red-200 rounded-2xl p-4 flex items-start gap-3 shadow-soft-sm"
            >
              <AlertTriangle className="h-4 w-4 text-danger flex-shrink-0 mt-0.5" />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2 flex-wrap">
                  <h4 className="text-sm font-semibold text-neutral-900">{alert.title}</h4>
                  <span className="text-[10px] font-bold uppercase bg-danger text-white px-2 py-0.5 rounded-full font-mono">
                    {alert.affectedStudentsCount} Öğrenci Etkilendi
                  </span>
                </div>
                <p className="text-xs text-neutral-600 mt-1 leading-relaxed">{alert.description}</p>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Topics List */}
      {filteredTopics.length > 0 ? (
        <div className="space-y-4">
          {filteredTopics.map((topic) => {
            const isLow = topic.overallMastery < 60;
            const isMastered = topic.overallMastery >= 80;

            return (
              <div
                key={topic.id}
                className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm space-y-4"
              >
                {/* Topic Header */}
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 border-b border-neutral-100 pb-3">
                  <div>
                    <div className="flex items-center gap-2">
                      <BookOpen className="h-4 w-4 text-primary-600" />
                      <h3 className="font-semibold text-base text-neutral-900">{topic.topicName}</h3>
                      <span className="text-xs text-neutral-400">({topic.grade})</span>
                    </div>
                  </div>

                  <div className="flex items-center gap-3 font-mono text-xs text-neutral-500">
                    <span>Toplam {topic.totalQuestionsSolved} Soru</span>
                    <span>Ort. %{topic.averageAccuracy} Doğruluk</span>
                    <span
                      className={cn(
                        'font-bold px-2.5 py-0.5 rounded-full text-xs',
                        isLow
                          ? 'bg-danger-light text-danger-dark'
                          : isMastered
                          ? 'bg-success-light text-success-dark'
                          : 'bg-warning-light text-warning-dark'
                      )}
                    >
                      %{topic.overallMastery} Hakimiyet
                    </span>
                  </div>
                </div>

                {/* Progress Bar */}
                <div className="h-2 w-full bg-neutral-100 rounded-full overflow-hidden">
                  <div
                    className={cn(
                      'h-full rounded-full transition-all duration-500',
                      isLow ? 'bg-danger' : isMastered ? 'bg-success' : 'bg-warning'
                    )}
                    style={{ width: `${topic.overallMastery}%` }}
                  />
                </div>

                {/* Kazanımlar Table / List */}
                <div className="space-y-2 pt-1">
                  <span className="text-[11px] font-semibold uppercase tracking-wider text-neutral-400 block">
                    Alt Kazanım Dağılımı ({topic.kazanımlar.length})
                  </span>

                  <div className="divide-y divide-neutral-50 bg-surface-alt rounded-xl border border-neutral-100 overflow-hidden">
                    {topic.kazanımlar.map((k) => (
                      <div
                        key={k.code}
                        className="p-3 flex flex-col sm:flex-row sm:items-center justify-between gap-2 text-xs"
                      >
                        <div className="flex items-start gap-2 min-w-0">
                          {k.status === 'mastered' && (
                            <CheckCircle2 className="h-3.5 w-3.5 text-success flex-shrink-0 mt-0.5" />
                          )}
                          {k.status === 'critical' && (
                            <XCircle className="h-3.5 w-3.5 text-danger flex-shrink-0 mt-0.5" />
                          )}
                          {k.status === 'in_progress' && (
                            <AlertTriangle className="h-3.5 w-3.5 text-warning flex-shrink-0 mt-0.5" />
                          )}

                          <div className="min-w-0">
                            <span className="font-mono font-bold text-neutral-800 mr-2">{k.code}</span>
                            <span className="text-neutral-600">{k.description}</span>
                          </div>
                        </div>

                        <div className="flex items-center gap-3 font-mono text-[11px] self-end sm:self-center flex-shrink-0">
                          {k.atRiskStudentCount > 0 && (
                            <span className="text-danger font-semibold">
                              {k.atRiskStudentCount} Öğrenci Riskli
                            </span>
                          )}
                          <span className="font-bold text-neutral-800">
                            %{k.masteryPercentage}
                          </span>
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <EmptyState
          icon={<BookOpen className="h-6 w-6 text-neutral-400" />}
          title="Kazanım Verisi Bulunamadı"
          description="Seçili branş için henüz kayıtlı kazanım performansı bulunmamaktadır."
        />
      )}
    </div>
  );
};
