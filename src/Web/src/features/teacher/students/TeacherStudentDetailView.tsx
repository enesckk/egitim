import React, { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, AlertCircle, RefreshCw, Send, CheckCircle2, AlertTriangle, XCircle, Plus } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Tabs } from '@/components/ui/Tabs';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { initialTeacherStudentsData } from './mockData';
import { TeacherStudentDetailModel } from './types';

export interface TeacherStudentDetailViewProps {
  initialStudents?: TeacherStudentDetailModel[];
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherStudentDetailView: React.FC<TeacherStudentDetailViewProps> = ({
  initialStudents = initialTeacherStudentsData.students,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const { studentId } = useParams<{ studentId: string }>();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState('topics');
  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [assignTitle, setAssignTitle] = useState('Limit & Süreklilik Odak Testi (20 Soru)');
  const [assignDueDate, setAssignDueDate] = useState('3 gün sonra');

  const student = initialStudents.find((s) => s.id === studentId) || initialStudents[0];

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
        <Skeleton className="h-10 w-32 rounded-xl" />
        <Skeleton className="h-32 w-full rounded-2xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (errorMessage || !student) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4 select-none">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Öğrenci Bulunamadı">
          {errorMessage || 'İstenen öğrencinin branşınızla ilgili akademik kaydına ulaşılamadı.'}
        </Alert>
        <div className="flex justify-center gap-2 pt-2">
          {onRetry && (
            <Button variant="secondary" size="sm" onClick={onRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
              Tekrar Dene
            </Button>
          )}
          <Button variant="primary" size="sm" onClick={() => navigate('/teacher/classes')} leftIcon={<ArrowLeft className="h-4 w-4" />}>
            Sınıflara Dön
          </Button>
        </div>
      </div>
    );
  }

  const tabs = [
    { id: 'topics', label: 'Konu Hakimiyeti', count: student.topicPerformances.length },
    { id: 'exams', label: 'Deneme Analizleri', count: student.recentExamResults.length },
  ];

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* Back Button */}
      <div>
        <button
          type="button"
          onClick={() => navigate(-1)}
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-neutral-500 hover:text-primary-600 transition-colors py-1 px-2 rounded-lg hover:bg-neutral-100"
        >
          <ArrowLeft className="h-3.5 w-3.5" />
          Geri Dön
        </button>
      </div>

      {/* 1. Student Academic Header Card */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-6 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-soft-sm">
        <div className="flex items-center gap-4 min-w-0">
          <div className="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl bg-navy-900 text-white font-semibold text-xl flex items-center justify-center flex-shrink-0">
            {student.initials}
          </div>
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h1 className="font-serif text-xl sm:text-2xl font-bold text-neutral-900 leading-tight truncate">
                {student.name}
              </h1>
              <span className="text-xs font-semibold bg-neutral-100 text-neutral-700 px-2.5 py-0.5 rounded-full">
                {student.classBranch}
              </span>
            </div>
            <p className="text-xs sm:text-sm text-neutral-500 truncate mt-0.5">
              Dersleriniz: <strong className="text-neutral-700">{student.assignedSubjects.join(', ')}</strong>
            </p>
            <div className="flex items-center gap-3 font-mono text-xs text-neutral-600 mt-1.5">
              <span>Matematik Ort: <strong className="text-primary-700">{student.mathAverageNet} net</strong></span>
              {student.physicsAverageNet > 0 && (
                <>
                  <span>•</span>
                  <span>Fizik Ort: <strong className="text-primary-700">{student.physicsAverageNet} net</strong></span>
                </>
              )}
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2 self-start sm:self-center">
          <Button
            variant="secondary"
            size="sm"
            onClick={() => navigate(`/teacher/messages?studentId=${student.id}`)}
            leftIcon={<Send className="h-3.5 w-3.5" />}
          >
            Soru Sor / Mesaj
          </Button>
          <Button
            variant="primary"
            size="sm"
            onClick={() => setIsAssignModalOpen(true)}
            leftIcon={<Plus className="h-3.5 w-3.5" />}
          >
            Ödev Ata
          </Button>
        </div>
      </div>

      {/* 2. Detail Tabs */}
      <div className="bg-white rounded-2xl border border-neutral-100 px-3 sm:px-4 pt-1 shadow-soft-sm">
        <Tabs
          tabs={tabs}
          activeId={activeTab}
          onChange={setActiveTab}
        />
      </div>

      {/* 3. Tab Content */}
      <div>
        {activeTab === 'topics' && (
          <div className="space-y-3">
            {student.topicPerformances.map((tp) => {
              const isLow = tp.status === 'needs_work';
              const isMastered = tp.status === 'mastered';

              return (
                <div
                  key={tp.id}
                  className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm space-y-2.5"
                >
                  <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
                    <div className="flex items-center gap-2">
                      {isMastered && <CheckCircle2 className="h-4 w-4 text-success" />}
                      {isLow && <XCircle className="h-4 w-4 text-danger" />}
                      {!isMastered && !isLow && <AlertTriangle className="h-4 w-4 text-warning" />}
                      <span className="font-semibold text-sm text-neutral-900">{tp.topic}</span>
                      <span className="text-xs text-neutral-400">({tp.subject})</span>
                    </div>

                    <div className="flex items-center gap-3 font-mono text-xs text-neutral-500">
                      <span>{tp.solvedQuestionCount} Soru Çözüldü</span>
                      <span>Doğruluk: %{tp.accuracyRate}</span>
                      <span
                        className={cn(
                          'font-bold px-2 py-0.5 rounded-full text-[11px]',
                          isLow
                            ? 'bg-danger-light text-danger-dark'
                            : isMastered
                            ? 'bg-success-light text-success-dark'
                            : 'bg-warning-light text-warning-dark'
                        )}
                      >
                        %{tp.masteryPercentage} Hakimiyet
                      </span>
                    </div>
                  </div>

                  <div className="h-2 w-full bg-neutral-100 rounded-full overflow-hidden">
                    <div
                      className={cn(
                        'h-full rounded-full transition-all duration-500',
                        isLow ? 'bg-danger' : isMastered ? 'bg-success' : 'bg-warning'
                      )}
                      style={{ width: `${tp.masteryPercentage}%` }}
                    />
                  </div>

                  <div className="flex items-center justify-between text-[11px] text-neutral-400">
                    <span>Son çalışma: {tp.lastPracticedDate}</span>
                    {isLow && (
                      <span className="text-danger font-medium">
                        Kazanım tekrarı ve ek soru çözümü önerilir
                      </span>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}

        {activeTab === 'exams' && (
          <div className="space-y-3">
            {student.recentExamResults.map((exam) => (
              <div
                key={exam.id}
                className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm space-y-3"
              >
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-1 pb-2 border-b border-neutral-100">
                  <div>
                    <h3 className="text-sm font-semibold text-neutral-900">{exam.examTitle}</h3>
                    <p className="text-xs text-neutral-400">{exam.date} • {exam.subject}</p>
                  </div>
                  <div className="text-right font-mono text-xs">
                    <span className="text-sm font-bold text-primary-700">{exam.net} Net</span>
                    <span className="text-success text-[11px] ml-1.5">(+{exam.netChange})</span>
                  </div>
                </div>

                <div className="grid grid-cols-3 gap-2 bg-surface-alt p-2.5 rounded-xl text-center text-xs font-mono">
                  <div>
                    <span className="text-neutral-400 block text-[10px]">Doğru</span>
                    <span className="font-bold text-success text-sm">{exam.correctCount}</span>
                  </div>
                  <div>
                    <span className="text-neutral-400 block text-[10px]">Yanlış</span>
                    <span className="font-bold text-danger text-sm">{exam.incorrectCount}</span>
                  </div>
                  <div>
                    <span className="text-neutral-400 block text-[10px]">Boş</span>
                    <span className="font-bold text-neutral-600 text-sm">{exam.emptyCount}</span>
                  </div>
                </div>

                {exam.incorrectTopics.length > 0 && (
                  <div className="text-xs space-y-1">
                    <span className="text-neutral-500 font-medium">Hatalı Soru Dağılımı:</span>
                    <div className="flex flex-wrap gap-1.5">
                      {exam.incorrectTopics.map((topic, idx) => (
                        <span
                          key={idx}
                          className="bg-red-50 text-danger border border-red-200 px-2 py-0.5 rounded-lg text-[11px]"
                        >
                          {topic}
                        </span>
                      ))}
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        )}
      </div>

      {/* Assign Test Modal */}
      <Modal
        isOpen={isAssignModalOpen}
        onClose={() => setIsAssignModalOpen(false)}
        title="Akademik Ödev / Test Ata"
        subtitle={`${student.name} için kişiselleştirilmiş konu pekiştirme testi`}
        headerVariant="dark"
        footer={
          <div className="flex justify-end gap-2 w-full">
            <Button variant="secondary" size="sm" onClick={() => setIsAssignModalOpen(false)}>
              İptal
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={() => {
                setIsAssignModalOpen(false);
              }}
            >
              Ödevi Gönder
            </Button>
          </div>
        }
      >
        <div className="space-y-3 select-none">
          <Input
            label="Ödev / Materyal Başlığı"
            value={assignTitle}
            onChange={(e) => setAssignTitle(e.target.value)}
          />
          <Input
            label="Teslim Tarihi"
            value={assignDueDate}
            onChange={(e) => setAssignDueDate(e.target.value)}
          />
          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1">
              Öğretmen Notu
            </label>
            <textarea
              rows={2}
              placeholder="Öğrencinin dikkat etmesi gereken noktalar..."
              className="w-full text-xs bg-surface-alt border border-neutral-200 rounded-xl p-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
          </div>
        </div>
      </Modal>
    </div>
  );
};
