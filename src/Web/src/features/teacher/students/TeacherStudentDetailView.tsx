import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, AlertCircle, RefreshCw, Send, CheckCircle2, AlertTriangle, XCircle, Plus } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Tabs } from '@/components/ui/Tabs';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import { TeacherStudentDetailModel, TeacherStudentTopicPerformance, TeacherStudentExamDetail } from './types';
import { teacherService } from '@/services/teacherService';
import { Student360Dto } from '@/services/academicService';

function mapStudent360ToTeacherModel(dto: Student360Dto): TeacherStudentDetailModel {
  const name = dto.student ? `${dto.student.firstName} ${dto.student.lastName}`.trim() : 'Öğrenci';
  const initials =
    name
      .split(' ')
      .filter(Boolean)
      .map((p) => p[0])
      .join('')
      .slice(0, 2)
      .toUpperCase() || 'ÖĞ';

  const topics: TeacherStudentTopicPerformance[] = (dto.currentGoals || []).map((g) => ({
    id: g.id,
    subject: g.targetSchoolName || 'Akademik Hedef',
    topic: g.title,
    masteryPercentage: g.targetScore ? Math.min(100, Math.round(g.targetScore)) : 0,
    solvedQuestionCount: 0,
    accuracyRate: g.targetScore ? Math.min(100, Math.round(g.targetScore)) : 0,
    status: 'improving',
    lastPracticedDate: g.effectiveDate ? new Date(g.effectiveDate).toLocaleDateString('tr-TR') : 'Güncel',
  }));

  const exams: TeacherStudentExamDetail[] = dto.recentExam
    ? [
        {
          id: dto.recentExam.latestExamId,
          examTitle: dto.recentExam.title,
          date: dto.recentExam.takenAt
            ? new Date(dto.recentExam.takenAt).toLocaleDateString('tr-TR')
            : 'Güncel',
          subject: dto.recentExam.examType || 'Genel',
          correctCount: 0,
          incorrectCount: 0,
          emptyCount: 0,
          net: dto.recentExam.totalNet,
          netChange: dto.recentExam.delta,
          incorrectTopics: [],
        },
      ]
    : [];

  return {
    id: dto.student.id,
    name,
    initials,
    grade: dto.student.gradeLevel ? `${dto.student.gradeLevel}. Sınıf` : '12. Sınıf',
    classBranch: dto.student.gradeLevel ? `${dto.student.gradeLevel}. Sınıf` : '12. Sınıf',
    assignedSubjects: ['Matematik', 'Fizik'],
    mathAverageNet: null,
    physicsAverageNet: null,
    overallAcademicStatus: 'takipte',
    topicPerformances: topics,
    recentExamResults: exams,
  };
}

export interface TeacherStudentDetailViewProps {
  initialStudents?: TeacherStudentDetailModel[];
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherStudentDetailView: React.FC<TeacherStudentDetailViewProps> = ({
  initialStudents,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const { studentId } = useParams<{ studentId: string }>();
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState('topics');
  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [assignTitle, setAssignTitle] = useState('Limit & Süreklilik Odak Testi (20 Soru)');
  const [assignDueDate, setAssignDueDate] = useState('3 gün sonra');

  const [student, setStudent] = useState<TeacherStudentDetailModel | undefined>(() => {
    if (initialStudents) {
      return initialStudents.find((s) => s.id === studentId) || initialStudents[0];
    }
    return undefined;
  });
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialStudents && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  const fetchStudent = useCallback(async () => {
    if (initialStudents || !studentId) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const res = await teacherService.getStudent360(studentId);
      setStudent(mapStudent360ToTeacherModel(res));
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Öğrenci karnesi yüklenemedi.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [initialStudents, studentId]);

  useEffect(() => {
    if (initialStudents) {
      setStudent(initialStudents.find((s) => s.id === studentId) || initialStudents[0]);
      setIsLoading(false);
    } else {
      fetchStudent();
    }
  }, [initialStudents, studentId, fetchStudent]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchStudent();
    }
  };

  const activeStudent = student || (initialStudents ? initialStudents[0] : undefined);
  const activeLoading = propIsLoading || isLoading;
  const activeError = propErrorMessage || errorMessage;

  // Loading State
  if (activeLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="teacher-student-detail-loading">
        <Skeleton className="h-10 w-32 rounded-xl" />
        <Skeleton className="h-32 w-full rounded-2xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <Skeleton className="h-64 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (activeError || !activeStudent) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4 select-none" data-testid="teacher-student-detail-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Öğrenci Bulunamadı">
          {activeError || 'İstenen öğrencinin branşınızla ilgili akademik kaydına ulaşılamadı.'}
        </Alert>
        <div className="flex justify-center gap-2 pt-2">
          <Button variant="secondary" size="sm" onClick={handleRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
          <Button variant="primary" size="sm" onClick={() => navigate('/teacher/classes')} leftIcon={<ArrowLeft className="h-4 w-4" />}>
            Sınıflara Dön
          </Button>
        </div>
      </div>
    );
  }

  const tabs = [
    { id: 'topics', label: 'Konu Hakimiyeti', count: activeStudent.topicPerformances.length },
    { id: 'exams', label: 'Deneme Analizleri', count: activeStudent.recentExamResults.length },
  ];

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="teacher-student-detail-view">
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
            {activeStudent.initials}
          </div>
          <div className="min-w-0">
            <div className="flex items-center gap-2">
              <h1 className="font-serif text-xl sm:text-2xl font-bold text-neutral-900 leading-tight truncate">
                {activeStudent.name}
              </h1>
              <span className="text-xs font-semibold bg-neutral-100 text-neutral-700 px-2.5 py-0.5 rounded-full">
                {activeStudent.classBranch}
              </span>
            </div>
            <p className="text-xs sm:text-sm text-neutral-500 truncate mt-0.5">
              Dersleriniz: <strong className="text-neutral-700">{activeStudent.assignedSubjects.join(', ')}</strong>
            </p>
            <div className="flex items-center gap-3 font-mono text-xs text-neutral-600 mt-1.5">
              {activeStudent.mathAverageNet != null ? (
                <span>Matematik Ort: <strong className="text-primary-700">{activeStudent.mathAverageNet} net</strong></span>
              ) : (
                <span>Branş Sınav Verisi: <span className="text-neutral-400">Henüz sınav verisi yok</span></span>
              )}
              {activeStudent.physicsAverageNet != null && (
                <>
                  <span>•</span>
                  <span>Fizik Ort: <strong className="text-primary-700">{activeStudent.physicsAverageNet} net</strong></span>
                </>
              )}
            </div>
          </div>
        </div>

        <div className="flex items-center gap-2 self-start sm:self-center">
          <Button
            variant="secondary"
            size="sm"
            onClick={() => navigate(`/teacher/messages?studentId=${activeStudent.id}`)}
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
            {activeStudent.topicPerformances.length === 0 ? (
              <div className="bg-white rounded-2xl border border-neutral-100 p-8 text-center text-neutral-400">
                Konu performansı verisi bulunmuyor.
              </div>
            ) : (
              activeStudent.topicPerformances.map((tp) => {
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
                          {isMastered ? 'Kazanıldı' : isLow ? 'Kritik Eksik' : 'Geliştirilmeli'}
                        </span>
                      </div>
                    </div>

                    <div className="h-2 w-full bg-neutral-100 rounded-full overflow-hidden">
                      <div
                        className={cn(
                          'h-full rounded-full transition-all duration-500',
                          isLow ? 'bg-danger' : isMastered ? 'bg-success' : 'bg-warning'
                        )}
                        style={{ width: `${tp.accuracyRate}%` }}
                      />
                    </div>
                  </div>
                );
              })
            )}
          </div>
        )}

        {activeTab === 'exams' && (
          <div className="space-y-3">
            {activeStudent.recentExamResults.length === 0 ? (
              <div className="bg-white rounded-2xl border border-neutral-100 p-8 text-center text-neutral-400">
                Sınav sonucu verisi bulunmuyor.
              </div>
            ) : (
              activeStudent.recentExamResults.map((exam) => (
                <div
                  key={exam.id}
                  className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm flex items-center justify-between gap-4"
                >
                  <div>
                    <h4 className="font-semibold text-sm text-neutral-900">{exam.examTitle}</h4>
                    <p className="text-xs text-neutral-400 mt-0.5">{exam.date} • {exam.subject}</p>
                  </div>

                  <div className="flex items-center gap-4 text-xs font-mono">
                    <span className="text-neutral-500">
                      Net: <strong className="text-neutral-800">{exam.net}</strong>
                    </span>
                    <span className="text-primary-700 font-bold">
                      Değişim: {exam.netChange > 0 ? `+${exam.netChange}` : exam.netChange}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        )}
      </div>

      {/* Assign Homework Modal */}
      <Modal
        isOpen={isAssignModalOpen}
        onClose={() => setIsAssignModalOpen(false)}
        title="Öğrenciye Ödev / Test Ata"
      >
        <div className="space-y-4">
          <Input
            label="Ödev Başlığı"
            value={assignTitle}
            onChange={(e) => setAssignTitle(e.target.value)}
          />

          <Input
            label="Teslim Tarihi"
            value={assignDueDate}
            onChange={(e) => setAssignDueDate(e.target.value)}
          />

          <div className="flex justify-end gap-2 pt-2">
            <Button
              variant="secondary"
              onClick={() => setIsAssignModalOpen(false)}
            >
              İptal
            </Button>
            <Button
              variant="primary"
              onClick={() => {
                setIsAssignModalOpen(false);
              }}
            >
              Ödevi Gönder
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
