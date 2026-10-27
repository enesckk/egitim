import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  ArrowLeft,
  AlertCircle,
  RefreshCw,
  Send,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  Plus,
  BookOpen,
  FileText,
  Sparkles,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Tabs } from '@/components/ui/Tabs';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { Modal } from '@/components/ui/Modal';
import { Input } from '@/components/ui/Input';
import {
  TeacherStudentDetailModel,
  TeacherStudentTopicPerformance,
  TeacherStudentExamDetail,
  TeacherStudentAcademicGap,
} from './types';
import { teacherService } from '@/services/teacherService';
import { academicService, Student360Dto } from '@/services/academicService';

function mapStudent360ToTeacherModel(
  dto: Student360Dto,
  teacherSubjects: string[] = ['Matematik'],
  topicPerformancesFromAi: TeacherStudentTopicPerformance[] = [],
  academicGaps: TeacherStudentAcademicGap[] = []
): TeacherStudentDetailModel {
  const name = dto.student ? `${dto.student.firstName} ${dto.student.lastName}`.trim() : 'Öğrenci';
  const initials =
    name
      .split(' ')
      .filter(Boolean)
      .map((p) => p[0])
      .join('')
      .slice(0, 2)
      .toUpperCase() || 'ÖĞ';

  // If topicPerformances came from real academic intelligence API, use them;
  // otherwise fallback to goals if available
  const topics: TeacherStudentTopicPerformance[] =
    topicPerformancesFromAi.length > 0
      ? topicPerformancesFromAi
      : (dto.currentGoals || []).map((g) => ({
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
    studentNumber: dto.student.studentNumber || null,
    assignedSubjects: teacherSubjects.length > 0 ? teacherSubjects : ['Branş Dersi'],
    mathAverageNet: null,
    physicsAverageNet: null,
    overallAcademicStatus: academicGaps.some((g) => g.severity === 'critical')
      ? 'kritik'
      : academicGaps.length > 0
      ? 'dikkat'
      : 'takipte',
    topicPerformances: topics,
    recentExamResults: exams,
    academicGaps,
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
  const [assignTitle, setAssignTitle] = useState('Hedef Pekiştirme Testi (20 Soru)');
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
      // 1. Fetch Teacher info for assigned subjects & student 360
      const [res360, teacherMe, dashboardData] = await Promise.all([
        teacherService.getStudent360(studentId),
        teacherService.getMe().catch(() => null),
        teacherService.getDashboard().catch(() => null),
      ]);

      const teacherSubjects = (teacherMe?.subjects || []).map((s) => s.subjectName).filter(Boolean);

      // 2. Fetch Topic Performances from Academic Intelligence API
      let topicPerformancesFromAi: TeacherStudentTopicPerformance[] = [];
      try {
        const topicClassifications = await academicService.getTopicClassifications(studentId);
        if (Array.isArray(topicClassifications) && topicClassifications.length > 0) {
          topicPerformancesFromAi = topicClassifications.map((tc) => {
            const accuracyPercent = Math.round((tc.accuracy || 0) * 100);
            const stateStr = String(tc.state).toLowerCase();
            const status: 'mastered' | 'improving' | 'needs_work' =
              stateStr === 'strong' || stateStr === '4' || accuracyPercent >= 75
                ? 'mastered'
                : stateStr === 'critical' || stateStr === '1' || accuracyPercent < 50
                ? 'needs_work'
                : 'improving';

            return {
              id: tc.topicId,
              subject: tc.subjectName,
              topic: tc.topicName,
              masteryPercentage: accuracyPercent,
              solvedQuestionCount: tc.totalQuestions || 0,
              accuracyRate: accuracyPercent,
              status,
              lastPracticedDate: 'Son Sınavlar',
            };
          });
        }
      } catch {
        // Non-blocking
      }

      // 3. Extract real academic gaps for this student
      const gapsForStudent: TeacherStudentAcademicGap[] = (dashboardData?.academicGaps || [])
        .filter((g: any) => g.studentId === studentId)
        .map((g: any) => ({
          id: g.id,
          subject: g.subject,
          topic: g.topic,
          severity: g.severity,
          detail: g.detail,
          lastExam: g.lastExam,
        }));

      setStudent(
        mapStudent360ToTeacherModel(
          res360,
          teacherSubjects,
          topicPerformancesFromAi,
          gapsForStudent
        )
      );
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
      <div className="w-full max-w-6xl mx-auto space-y-5 select-none" data-testid="teacher-student-detail-loading">
        <Skeleton className="h-9 w-32 rounded-xl" />
        <Skeleton className="h-36 w-full rounded-2xl" />
        <Skeleton className="h-12 w-full rounded-xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-64 w-full rounded-2xl" />
          <Skeleton className="h-64 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (activeError || !activeStudent) {
    return (
      <div className="w-full max-w-6xl mx-auto py-8 space-y-4 select-none" data-testid="teacher-student-detail-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Öğrenci Bulunamadı">
          {activeError || 'İstenen öğrencinin branşınızla ilgili akademik kaydına ulaşılamadı.'}
        </Alert>
        <div className="flex justify-center gap-3 pt-2">
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
    { id: 'gaps', label: 'Öncelikli Eksikler', count: activeStudent.academicGaps?.length || 0 },
    { id: 'exams', label: 'Deneme Analizleri', count: activeStudent.recentExamResults.length },
  ];

  return (
    <div className="w-full max-w-6xl mx-auto select-none space-y-4 sm:space-y-6" data-testid="teacher-student-detail-view">
      {/* Back Button */}
      <div>
        <Button
          variant="ghost"
          size="sm"
          onClick={() => navigate(-1)}
          leftIcon={<ArrowLeft className="h-4 w-4 text-neutral-600" />}
          className="text-neutral-600 hover:text-neutral-900 -ml-2 font-medium"
        >
          Geri Dön
        </Button>
      </div>

      {/* 1. Student Academic Header Card - Premium Navy / Ivory */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-5 sm:p-6 shadow-soft-sm">
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-5">
          <div className="flex items-center gap-4 min-w-0">
            <div className="w-14 h-14 sm:w-16 sm:h-16 rounded-2xl bg-[#14233E] text-white font-bold text-xl flex items-center justify-center flex-shrink-0 shadow-md">
              {activeStudent.initials}
            </div>
            <div className="min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <h1 className="font-serif text-xl sm:text-2xl font-bold text-neutral-900 tracking-tight truncate">
                  {activeStudent.name}
                </h1>
                <span className="text-xs font-bold px-2.5 py-0.5 rounded-full bg-primary-50 text-primary-700 border border-primary-200">
                  {activeStudent.grade}
                </span>
                {activeStudent.studentNumber && (
                  <span className="text-xs font-mono font-medium px-2.5 py-0.5 rounded-full bg-neutral-100 text-neutral-600">
                    No: {activeStudent.studentNumber}
                  </span>
                )}
              </div>
              <div className="flex items-center gap-2 text-xs sm:text-sm text-neutral-600 mt-1">
                <BookOpen className="h-4 w-4 text-primary-600 flex-shrink-0" />
                <span>Branşınız: <strong className="text-neutral-800 font-medium">{activeStudent.assignedSubjects.join(', ')}</strong></span>
              </div>
              <div className="flex items-center gap-2 font-mono text-xs text-neutral-600 mt-1.5 flex-wrap">
                {activeStudent.mathAverageNet != null ? (
                  <span>Branş Sınav Ortalaması: <strong className="text-primary-700 font-bold">{activeStudent.mathAverageNet} net</strong></span>
                ) : (
                  <span>Branş Sınav Ortalaması: <strong className="font-sans text-neutral-500 font-normal">Bu branşta henüz sınav verisi yok</strong></span>
                )}
                {activeStudent.physicsAverageNet != null && (
                  <>
                    <span className="text-neutral-300">•</span>
                    <span>Fizik Ort: <strong className="text-primary-700 font-bold">{activeStudent.physicsAverageNet} net</strong></span>
                  </>
                )}
              </div>
            </div>
          </div>

          {/* Quick Actions */}
          <div className="flex items-center gap-2.5 self-start md:self-center border-t md:border-t-0 pt-3 md:pt-0 border-neutral-100 w-full md:w-auto justify-end">
            <Button
              variant="secondary"
              size="sm"
              onClick={() => navigate(`/teacher/messages?studentId=${activeStudent.id}`)}
              leftIcon={<Send className="h-3.5 w-3.5" />}
              className="bg-[#F8F7F3] border-neutral-200 hover:bg-neutral-100"
            >
              Mesaj Gönder
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
      </div>

      {/* 2. Detail Tabs */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 px-3 sm:px-4 pt-1 shadow-soft-sm">
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
              <div className="bg-white rounded-2xl border border-neutral-200/80 p-8 sm:p-12 text-center space-y-2 shadow-soft-sm">
                <Sparkles className="h-8 w-8 text-neutral-400 mx-auto" />
                <p className="text-sm font-semibold text-neutral-700">
                  Konu performansı verisi bulunmuyor.
                </p>
                <p className="text-xs text-neutral-500 max-w-md mx-auto">
                  Öğrenci konu bazlı deneme sınavı çözdükçe veya soru bankası testlerini tamamladıkça kazanım oranları burada listelenecektir.
                </p>
              </div>
            ) : (
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-3.5 sm:gap-4">
                {activeStudent.topicPerformances.map((tp) => {
                  const isLow = tp.status === 'needs_work';
                  const isMastered = tp.status === 'mastered';

                  return (
                    <div
                      key={tp.id}
                      className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-sm space-y-3 flex flex-col justify-between"
                    >
                      <div className="space-y-1.5">
                        <div className="flex items-start justify-between gap-2">
                          <div className="flex items-center gap-2 min-w-0">
                            {isMastered && <CheckCircle2 className="h-4 w-4 text-emerald-600 flex-shrink-0" />}
                            {isLow && <XCircle className="h-4 w-4 text-rose-600 flex-shrink-0" />}
                            {!isMastered && !isLow && <AlertTriangle className="h-4 w-4 text-amber-500 flex-shrink-0" />}
                            <span className="font-bold text-sm text-neutral-900 truncate">{tp.topic}</span>
                          </div>
                          <span
                            className={cn(
                              'font-bold px-2.5 py-0.5 rounded-full text-[11px] flex-shrink-0 border',
                              isLow
                                ? 'bg-rose-50 text-rose-700 border-rose-200'
                                : isMastered
                                ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                                : 'bg-amber-50 text-amber-700 border-amber-200'
                            )}
                          >
                            {isMastered ? 'Kazanıldı' : isLow ? 'Kritik Eksik' : 'Geliştirilmeli'}
                          </span>
                        </div>
                        <p className="text-xs text-neutral-500">
                          Ders: <strong className="text-neutral-700">{tp.subject}</strong>
                        </p>
                      </div>

                      <div className="space-y-1.5 pt-2 border-t border-neutral-100">
                        <div className="flex items-center justify-between text-xs text-neutral-600">
                          <span>{tp.solvedQuestionCount} Soru İncelendi</span>
                          <span className="font-mono font-bold text-neutral-800">Doğruluk: %{tp.accuracyRate}</span>
                        </div>
                        <div className="h-2 w-full bg-neutral-100 rounded-full overflow-hidden">
                          <div
                            className={cn(
                              'h-full rounded-full transition-all duration-500',
                              isLow ? 'bg-rose-500' : isMastered ? 'bg-emerald-500' : 'bg-amber-500'
                            )}
                            style={{ width: `${tp.accuracyRate}%` }}
                          />
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </div>
        )}

        {activeTab === 'gaps' && (
          <div className="space-y-3">
            {(!activeStudent.academicGaps || activeStudent.academicGaps.length === 0) ? (
              <div className="bg-white rounded-2xl border border-neutral-200/80 p-8 sm:p-12 text-center space-y-2 shadow-soft-sm">
                <CheckCircle2 className="h-8 w-8 text-emerald-500 mx-auto" />
                <p className="text-sm font-semibold text-neutral-700">
                  Öncelikli akademik eksik tespit edilmedi.
                </p>
                <p className="text-xs text-neutral-500 max-w-md mx-auto">
                  Öğrencinin branş sınavlarındaki yanlış ve boş dağılımları normal seviyededir.
                </p>
              </div>
            ) : (
              <div className="space-y-2.5">
                {activeStudent.academicGaps.map((gap) => (
                  <div
                    key={gap.id}
                    className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-sm flex flex-col sm:flex-row sm:items-center justify-between gap-3"
                  >
                    <div className="space-y-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span
                          className={cn(
                            'text-[10px] font-bold uppercase tracking-wider px-2 py-0.5 rounded-full border',
                            gap.severity === 'critical'
                              ? 'bg-rose-50 text-rose-700 border-rose-200'
                              : 'bg-amber-50 text-amber-700 border-amber-200'
                          )}
                        >
                          {gap.severity === 'critical' ? 'Kritik Eksik' : 'Öncelikli'}
                        </span>
                        <h4 className="font-bold text-sm text-neutral-900">{gap.topic}</h4>
                        <span className="text-xs text-neutral-500">({gap.subject})</span>
                      </div>
                      <p className="text-xs text-neutral-600">{gap.detail}</p>
                    </div>

                    {gap.lastExam && (
                      <span className="text-xs font-mono text-neutral-500 bg-[#F8F7F3] px-3 py-1.5 rounded-xl border border-neutral-200/70 self-start sm:self-auto flex-shrink-0">
                        Son Sınav: {gap.lastExam}
                      </span>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}

        {activeTab === 'exams' && (
          <div className="space-y-3">
            {activeStudent.recentExamResults.length === 0 ? (
              <div className="bg-white rounded-2xl border border-neutral-200/80 p-8 sm:p-12 text-center space-y-2 shadow-soft-sm">
                <FileText className="h-8 w-8 text-neutral-400 mx-auto" />
                <p className="text-sm font-semibold text-neutral-700">
                  Sınav sonucu verisi bulunmuyor.
                </p>
                <p className="text-xs text-neutral-500 max-w-md mx-auto">
                  Öğrencinin katıldığı deneme sınavları ve optik form sonuçları girildikçe bu alanda listelenecektir.
                </p>
              </div>
            ) : (
              activeStudent.recentExamResults.map((exam) => (
                <div
                  key={exam.id}
                  className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-sm flex flex-col sm:flex-row sm:items-center justify-between gap-4"
                >
                  <div>
                    <h4 className="font-bold text-base text-neutral-900">{exam.examTitle}</h4>
                    <p className="text-xs text-neutral-500 mt-0.5">{exam.date} • {exam.subject}</p>
                  </div>

                  <div className="flex items-center gap-4 text-xs font-mono bg-[#F8F7F3] px-3.5 py-2 rounded-xl border border-neutral-200/70 self-start sm:self-auto">
                    <span className="text-neutral-600">
                      Toplam Net: <strong className="text-neutral-900 font-bold">{exam.net}</strong>
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
            className="bg-[#F8F7F3]"
          />

          <Input
            label="Teslim Tarihi"
            value={assignDueDate}
            onChange={(e) => setAssignDueDate(e.target.value)}
            className="bg-[#F8F7F3]"
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
