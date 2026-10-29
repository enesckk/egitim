import React, { useState, useEffect, useMemo, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  FileText,
  Plus,
  Search,
  Calendar,
  Layers,
  ArrowRight,
  ClipboardList,
  CheckCircle2,
  Clock,
  Archive,
  RefreshCw,
  AlertCircle,
  HelpCircle,
  Trash2,
  BookOpen,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Modal } from '@/components/ui/Modal';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { examService } from '@/services/examService';
import { academicService } from '@/services/academicService';
import { ExamDto, ExamStatus, ScoreSource } from '@/types/exam';
import { TaxonomyDto } from '@/types/question';

export interface AdminExamsViewProps {
  initialExams?: ExamDto[];
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

interface NewSectionRow {
  subjectId: string;
  questionCount: number;
  wrongAnswerPenalty: number;
}

export const AdminExamsView: React.FC<AdminExamsViewProps> = ({
  initialExams,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry,
}) => {
  const navigate = useNavigate();

  const [exams, setExams] = useState<ExamDto[]>(initialExams || []);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!initialExams && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | undefined>(propErrorMessage);

  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedExamType, setSelectedExamType] = useState<string>('all');
  const [selectedStatus, setSelectedStatus] = useState<string>('all');

  // Taxonomy for creation & filtering
  const [examTypes, setExamTypes] = useState<TaxonomyDto[]>([]);
  const [subjects, setSubjects] = useState<TaxonomyDto[]>([]);

  // Create Exam Modal State
  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [newTitle, setNewTitle] = useState('');
  const [newCode, setNewCode] = useState('');
  const [newExamTypeId, setNewExamTypeId] = useState('');
  const [newExamDate, setNewExamDate] = useState(new Date().toISOString().split('T')[0]);
  const [newWrongPenalty, setNewWrongPenalty] = useState<number>(0.25);
  const [newSections, setNewSections] = useState<NewSectionRow[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);

  // Load Exam Types and Subjects
  useEffect(() => {
    let mounted = true;
    Promise.all([
      academicService.getTaxonomy('exam-types').catch(() => []),
      academicService.getTaxonomy('subjects').catch(() => []),
    ]).then(([types, subs]) => {
      if (mounted) {
        setExamTypes(types || []);
        setSubjects(subs || []);
      }
    });
    return () => {
      mounted = false;
    };
  }, []);

  const fetchExams = useCallback(async () => {
    if (initialExams) return;
    setIsLoading(true);
    setErrorMessage(undefined);

    try {
      const res = await examService.getExams({ page: 1, pageSize: 100 });
      setExams(res.items || []);
    } catch (err: unknown) {
      setErrorMessage(err instanceof Error ? err.message : 'Sınavlar yüklenirken bir hata oluştu.');
    } finally {
      setIsLoading(false);
    }
  }, [initialExams]);

  useEffect(() => {
    if (!initialExams) {
      fetchExams();
    }
  }, [initialExams, fetchExams]);

  const handleRetry = () => {
    if (onRetry) {
      onRetry();
    } else {
      fetchExams();
    }
  };

  // Filtered Exams
  const filteredExams = useMemo(() => {
    return exams.filter((exam) => {
      const matchesSearch =
        exam.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
        exam.code.toLowerCase().includes(searchTerm.toLowerCase());
      const matchesType =
        selectedExamType === 'all' || exam.examTypeId === selectedExamType;
      const matchesStatus =
        selectedStatus === 'all' ||
        (selectedStatus === 'draft' && exam.status === ExamStatus.Draft) ||
        (selectedStatus === 'published' && exam.status === ExamStatus.Published) ||
        (selectedStatus === 'closed' && exam.status === ExamStatus.Closed);

      return matchesSearch && matchesType && matchesStatus;
    });
  }, [exams, searchTerm, selectedExamType, selectedStatus]);

  // Open Create Modal with default section
  const handleOpenCreateModal = () => {
    setNewTitle('');
    setNewCode(`DENEME-${new Date().getFullYear()}-${Math.floor(100 + Math.random() * 900)}`);
    setNewExamTypeId(examTypes[0]?.id || '');
    setNewExamDate(new Date().toISOString().split('T')[0]);
    setNewWrongPenalty(0.25);
    setNewSections(
      subjects.length > 0
        ? [{ subjectId: subjects[0].id, questionCount: 40, wrongAnswerPenalty: 0.25 }]
        : []
    );
    setCreateError(null);
    setIsCreateModalOpen(true);
  };

  const handleAddSection = () => {
    const unusedSubject = subjects.find((s) => !newSections.some((sec) => sec.subjectId === s.id));
    const subjectId = unusedSubject ? unusedSubject.id : (subjects[0]?.id || '');
    setNewSections((prev) => [
      ...prev,
      { subjectId, questionCount: 20, wrongAnswerPenalty: newWrongPenalty },
    ]);
  };

  const handleRemoveSection = (index: number) => {
    setNewSections((prev) => prev.filter((_, idx) => idx !== index));
  };

  const handleSectionChange = (index: number, field: keyof NewSectionRow, value: string | number) => {
    setNewSections((prev) => {
      const copy = [...prev];
      copy[index] = { ...copy[index], [field]: value };
      return copy;
    });
  };

  const handleCreateExam = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newTitle.trim() || !newCode.trim() || !newExamTypeId) {
      setCreateError('Lütfen sınav adı, kodu ve sınav türünü doldurunuz.');
      return;
    }

    if (newSections.length === 0) {
      setCreateError('En az 1 ders bölümü eklemelisiniz.');
      return;
    }

    const uniqueSubjects = new Set(newSections.map((s) => s.subjectId));
    if (uniqueSubjects.size !== newSections.length) {
      setCreateError('Aynı dersi birden fazla bölüm olarak ekleyemezsiniz.');
      return;
    }

    setIsSubmitting(true);
    setCreateError(null);

    try {
      await examService.createExam({
        code: newCode.trim(),
        title: newTitle.trim(),
        examTypeId: newExamTypeId,
        examDate: new Date(newExamDate).toISOString(),
        wrongAnswerPenalty: newWrongPenalty,
        scoreSource: ScoreSource.Institution,
        sections: newSections.map((s, idx) => ({
          subjectId: s.subjectId,
          displayOrder: idx,
          questionCount: Number(s.questionCount) || 1,
          wrongAnswerPenalty: s.wrongAnswerPenalty || newWrongPenalty,
        })),
      });

      setIsCreateModalOpen(false);
      await fetchExams();
    } catch (err: unknown) {
      setCreateError(err instanceof Error ? err.message : 'Sınav oluşturulamadı.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const getStatusBadge = (status: ExamStatus) => {
    switch (status) {
      case ExamStatus.Published:
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200/80 dark:bg-emerald-950/40 dark:text-emerald-300 dark:border-emerald-800">
            <CheckCircle2 className="w-3.5 h-3.5 text-emerald-600 dark:text-emerald-400" /> Yayında
          </span>
        );
      case ExamStatus.Closed:
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-slate-100 text-slate-700 border border-slate-200 dark:bg-slate-800/60 dark:text-slate-300 dark:border-slate-700">
            <Archive className="w-3.5 h-3.5 text-slate-500" /> Kapandı
          </span>
        );
      case ExamStatus.Draft:
      default:
        return (
          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200/80 dark:bg-amber-950/40 dark:text-amber-300 dark:border-amber-800">
            <Clock className="w-3.5 h-3.5 text-amber-600 dark:text-amber-400" /> Taslak
          </span>
        );
    }
  };

  return (
    <div className="space-y-6 max-w-7xl mx-auto" data-testid="admin-exams-page">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 border-b border-navy-100/80 dark:border-navy-800/80 pb-5">
        <div>
          <div className="flex items-center gap-2">
            <div className="p-2 rounded-xl bg-blue-50 text-[#315FE8] dark:bg-blue-950/40 dark:text-blue-400">
              <FileText className="h-6 w-6" />
            </div>
            <h1 className="text-2xl font-bold text-[#14233E] dark:text-white">
              Deneme Sınavları
            </h1>
          </div>
          <p className="text-sm text-navy-600 dark:text-navy-300 mt-1 pl-1">
            Kurumsal deneme sınavlarını oluşturun, soru dizilimlerini yönetin ve manuel/Excel sonuç girişlerini gerçekleştirin.
          </p>
        </div>
        <Button
          onClick={handleOpenCreateModal}
          data-testid="create-exam-btn"
          className="flex items-center gap-2 bg-[#315FE8] hover:bg-blue-700 text-white font-medium shadow-sm transition-all shrink-0 rounded-xl px-4 py-2.5"
        >
          <Plus className="h-4 w-4" />
          Yeni Sınav Oluştur
        </Button>
      </div>

      {/* Error State */}
      {errorMessage && (
        <Alert variant="danger" className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertCircle className="h-4 w-4" />
            <span>{errorMessage}</span>
          </div>
          <Button variant="outline" size="sm" onClick={handleRetry} className="flex items-center gap-1">
            <RefreshCw className="h-3 w-3" /> Tekrar Dene
          </Button>
        </Alert>
      )}

      {/* Filters Toolbar */}
      <div className="bg-[#F8F7F3] dark:bg-navy-900/60 p-4 rounded-2xl border border-navy-200/70 dark:border-navy-800 shadow-xs flex flex-col lg:flex-row gap-3 items-stretch lg:items-center justify-between">
        <div className="relative flex-1 max-w-md">
          <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-navy-400" />
          <Input
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Sınav adı veya kodu ile ara..."
            className="pl-10 bg-white dark:bg-navy-950 border-navy-200/80 dark:border-navy-700 text-sm rounded-xl focus:ring-2 focus:ring-[#315FE8]/20 focus:border-[#315FE8]"
            data-testid="exam-search-input"
          />
        </div>

        <div className="flex flex-wrap items-center gap-2.5">
          <select
            value={selectedExamType}
            onChange={(e) => setSelectedExamType(e.target.value)}
            aria-label="Sınav Türü Filtresi"
            className="h-10 px-3.5 rounded-xl border border-navy-200/80 dark:border-navy-700 bg-white dark:bg-navy-950 text-xs sm:text-sm text-[#14233E] dark:text-white focus:outline-none focus:ring-2 focus:ring-[#315FE8]/20"
            data-testid="exam-type-filter"
          >
            <option value="all">Tüm Sınav Türleri</option>
            {examTypes.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </select>

          <select
            value={selectedStatus}
            onChange={(e) => setSelectedStatus(e.target.value)}
            aria-label="Durum Filtresi"
            className="h-10 px-3.5 rounded-xl border border-navy-200/80 dark:border-navy-700 bg-white dark:bg-navy-950 text-xs sm:text-sm text-[#14233E] dark:text-white focus:outline-none focus:ring-2 focus:ring-[#315FE8]/20"
            data-testid="exam-status-filter"
          >
            <option value="all">Tüm Durumlar</option>
            <option value="draft">Taslak</option>
            <option value="published">Yayında</option>
            <option value="closed">Kapandı</option>
          </select>

          {(searchTerm || selectedExamType !== 'all' || selectedStatus !== 'all') && (
            <button
              type="button"
              onClick={() => {
                setSearchTerm('');
                setSelectedExamType('all');
                setSelectedStatus('all');
              }}
              className="text-xs font-semibold text-[#315FE8] hover:underline px-2 py-1"
            >
              Temizle
            </button>
          )}
        </div>
      </div>

      {/* Loading Skeleton */}
      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="p-5 bg-[#F8F7F3] dark:bg-navy-900 rounded-2xl border border-navy-200/60 dark:border-navy-800 animate-pulse flex items-center justify-between">
              <div className="space-y-2">
                <Skeleton className="h-5 w-48 bg-navy-200/60" />
                <Skeleton className="h-4 w-32 bg-navy-100" />
              </div>
              <div className="flex gap-2">
                <Skeleton className="h-9 w-28 bg-navy-200/60 rounded-xl" />
                <Skeleton className="h-9 w-28 bg-navy-200/60 rounded-xl" />
              </div>
            </div>
          ))}
        </div>
      ) : filteredExams.length === 0 ? (
        /* Empty State */
        <div
          data-testid="admin-exams-empty"
          className="bg-[#F8F7F3] dark:bg-navy-900/40 rounded-2xl border border-navy-200/70 dark:border-navy-800 p-12 text-center flex flex-col items-center justify-center space-y-4 shadow-xs"
        >
          <div className="h-14 w-14 rounded-2xl bg-blue-50 dark:bg-blue-950/50 flex items-center justify-center text-[#315FE8]">
            <ClipboardList className="h-7 w-7" />
          </div>
          <div className="max-w-md">
            <h3 className="text-lg font-bold text-[#14233E] dark:text-white">Henüz Deneme Sınavı Bulunmuyor</h3>
            <p className="text-sm text-navy-600 dark:text-navy-300 mt-1">
              {searchTerm || selectedExamType !== 'all' || selectedStatus !== 'all'
                ? 'Arama kriterlerinize uygun deneme sınavı bulunamadı. Filtreleri temizleyebilirsiniz.'
                : 'Kurumunuz için ilk deneme sınavını oluşturarak soru bankasından soru dizilimine veya sonuç girişine başlayın.'}
            </p>
          </div>
          {searchTerm || selectedExamType !== 'all' || selectedStatus !== 'all' ? (
            <Button
              variant="outline"
              onClick={() => {
                setSearchTerm('');
                setSelectedExamType('all');
                setSelectedStatus('all');
              }}
              className="rounded-xl"
            >
              Filtreleri Sıfırla
            </Button>
          ) : (
            <Button
              onClick={handleOpenCreateModal}
              className="flex items-center gap-2 bg-[#315FE8] hover:bg-blue-700 text-white rounded-xl"
            >
              <Plus className="h-4 w-4" />
              Yeni Sınav Oluştur
            </Button>
          )}
        </div>
      ) : (
        /* Dual Presentation: Responsive Cards (< 1024px) and Desktop Table (>= 1024px) */
        <div className="space-y-4">
          {/* Responsive Cards View (< 1024px, covers mobile 390px and tablet 768px) */}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:hidden gap-3.5">
            {filteredExams.map((exam) => (
              <div
                key={exam.id}
                data-testid={`exam-card-${exam.id}`}
                className="bg-[#F8F7F3] dark:bg-navy-900/60 p-4.5 rounded-2xl border border-navy-200/70 dark:border-navy-800 shadow-xs flex flex-col justify-between gap-3.5"
              >
                <div className="space-y-2">
                  <div className="flex items-start justify-between gap-2">
                    <div className="min-w-0 flex-1">
                      <h3 className="font-bold text-sm sm:text-base text-[#14233E] dark:text-white truncate">
                        {exam.title}
                      </h3>
                      <div className="text-xs font-mono text-navy-500 dark:text-navy-400 mt-0.5">
                        {exam.code}
                      </div>
                    </div>
                    <div className="shrink-0">{getStatusBadge(exam.status)}</div>
                  </div>

                  <div className="flex flex-wrap items-center gap-2 text-xs pt-1 border-t border-navy-200/50 dark:border-navy-800">
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-white dark:bg-navy-950 text-[#14233E] dark:text-white font-medium border border-navy-200/60 dark:border-navy-800">
                      <Layers className="h-3 w-3 text-[#315FE8]" />
                      {exam.examTypeName || 'Belirtilmedi'}
                    </span>
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-white dark:bg-navy-950 text-navy-600 dark:text-navy-300 border border-navy-200/60 dark:border-navy-800">
                      <BookOpen className="h-3 w-3 text-navy-400" />
                      {exam.sectionCount > 0 ? `${exam.sectionCount} Ders` : 'Bölüm Yok'}
                    </span>
                    <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-lg bg-white dark:bg-navy-950 text-navy-600 dark:text-navy-300 border border-navy-200/60 dark:border-navy-800">
                      <Calendar className="h-3 w-3 text-navy-400" />
                      {new Date(exam.examDate).toLocaleDateString('tr-TR', {
                        day: 'numeric',
                        month: 'short',
                        year: 'numeric',
                      })}
                    </span>
                  </div>
                </div>

                <div className="grid grid-cols-2 gap-2 pt-2 border-t border-navy-200/40 dark:border-navy-800/80">
                  <Button
                    variant="outline"
                    size="sm"
                    data-testid={`navigate-questions-m-${exam.id}`}
                    onClick={() => navigate(`/admin/exams/${exam.id}/questions`)}
                    className="w-full flex items-center justify-center gap-1.5 text-xs rounded-xl border-navy-200/80 text-[#14233E] hover:bg-white"
                  >
                    <HelpCircle className="h-3.5 w-3.5 text-navy-500" />
                    Soru Dizilimi
                  </Button>
                  <Button
                    size="sm"
                    data-testid={`navigate-results-m-${exam.id}`}
                    onClick={() => navigate(`/admin/exams/${exam.id}/results/entry`)}
                    className="w-full flex items-center justify-center gap-1.5 text-xs rounded-xl bg-[#14233E] hover:bg-navy-800 text-white shadow-xs"
                  >
                    <ClipboardList className="h-3.5 w-3.5" />
                    Sonuç Girişi
                    <ArrowRight className="h-3 w-3 ml-0.5" />
                  </Button>
                </div>
              </div>
            ))}
          </div>

          {/* Desktop Table (>= 1024px) */}
          <div className="hidden lg:block bg-white dark:bg-navy-900 rounded-2xl border border-navy-200/70 dark:border-navy-800 overflow-hidden shadow-xs">
            <div className="overflow-x-auto">
              <table className="w-full text-left border-collapse">
                <thead>
                  <tr className="bg-[#F8F7F3] dark:bg-navy-950/70 border-b border-navy-200/70 dark:border-navy-800 text-xs font-semibold text-navy-600 dark:text-navy-300 uppercase tracking-wider">
                    <th className="py-4 px-5">Sınav Bilgisi</th>
                    <th className="py-4 px-4">Tür & Bölümler</th>
                    <th className="py-4 px-4">Tarih</th>
                    <th className="py-4 px-4">Durum</th>
                    <th className="py-4 px-5 text-right">İşlemler</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-navy-100 dark:divide-navy-800/80 text-sm">
                  {filteredExams.map((exam) => (
                    <tr
                      key={exam.id}
                      data-testid={`exam-row-${exam.id}`}
                      className="hover:bg-[#F8F7F3]/60 dark:hover:bg-navy-800/40 transition-colors"
                    >
                      <td className="py-4 px-5">
                        <div className="font-bold text-[#14233E] dark:text-white">{exam.title}</div>
                        <div className="text-xs font-mono text-navy-500 dark:text-navy-400 mt-0.5">{exam.code}</div>
                      </td>
                      <td className="py-4 px-4">
                        <div className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg bg-blue-50 dark:bg-blue-950/40 text-[#14233E] dark:text-blue-300 font-semibold text-xs border border-blue-100 dark:border-blue-900">
                          <Layers className="h-3.5 w-3.5 text-[#315FE8]" />
                          {exam.examTypeName || 'Belirtilmedi'}
                        </div>
                        <div className="text-xs text-navy-500 dark:text-navy-400 mt-1 pl-0.5">
                          {exam.sectionCount > 0 ? `${exam.sectionCount} Ders / Bölüm` : 'Bölüm Yok'}
                        </div>
                      </td>
                      <td className="py-4 px-4 text-navy-700 dark:text-navy-300">
                        <div className="flex items-center gap-1.5 text-xs font-medium">
                          <Calendar className="h-3.5 w-3.5 text-navy-400" />
                          {new Date(exam.examDate).toLocaleDateString('tr-TR', {
                            year: 'numeric',
                            month: 'short',
                            day: 'numeric',
                          })}
                        </div>
                      </td>
                      <td className="py-4 px-4">
                        {getStatusBadge(exam.status)}
                      </td>
                      <td className="py-4 px-5 text-right">
                        <div className="flex items-center justify-end gap-2">
                          <Button
                            variant="outline"
                            size="sm"
                            data-testid={`navigate-questions-${exam.id}`}
                            onClick={() => navigate(`/admin/exams/${exam.id}/questions`)}
                            className="flex items-center gap-1 text-xs border-navy-200/80 text-[#14233E] hover:bg-[#F8F7F3] rounded-xl"
                          >
                            <HelpCircle className="h-3.5 w-3.5 text-navy-500" />
                            Soru Dizilimi
                          </Button>
                          <Button
                            size="sm"
                            data-testid={`navigate-results-${exam.id}`}
                            onClick={() => navigate(`/admin/exams/${exam.id}/results/entry`)}
                            className="flex items-center gap-1 text-xs bg-[#14233E] hover:bg-navy-800 text-white rounded-xl shadow-xs"
                          >
                            <ClipboardList className="h-3.5 w-3.5" />
                            Sonuç Girişi
                            <ArrowRight className="h-3 w-3 ml-0.5" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      )}

      {/* Create Exam Modal */}
      <Modal
        isOpen={isCreateModalOpen}
        onClose={() => !isSubmitting && setIsCreateModalOpen(false)}
        title="Yeni Deneme Sınavı Oluştur"
      >
        <form onSubmit={handleCreateExam} className="space-y-4 pt-2" data-testid="create-exam-form">
          {createError && (
            <Alert variant="danger">
              <AlertCircle className="h-4 w-4" />
              <span>{createError}</span>
            </Alert>
          )}

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <label className="block text-xs font-semibold text-[#14233E] dark:text-white mb-1.5">
                Sınav Başlığı <span className="text-red-500">*</span>
              </label>
              <Input
                value={newTitle}
                onChange={(e) => setNewTitle(e.target.value)}
                placeholder="Örn: 2026 TYT Genel Deneme - 1"
                required
                className="rounded-xl border-navy-200/80 focus:border-[#315FE8]"
                data-testid="input-exam-title"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[#14233E] dark:text-white mb-1.5">
                Sınav Kodu <span className="text-red-500">*</span>
              </label>
              <Input
                value={newCode}
                onChange={(e) => setNewCode(e.target.value)}
                placeholder="Örn: TYT-2026-01"
                required
                className="rounded-xl border-navy-200/80 focus:border-[#315FE8]"
                data-testid="input-exam-code"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label className="block text-xs font-semibold text-[#14233E] dark:text-white mb-1.5">
                Sınav Türü <span className="text-red-500">*</span>
              </label>
              <select
                value={newExamTypeId}
                onChange={(e) => setNewExamTypeId(e.target.value)}
                required
                aria-label="Sınav Türü Seçiniz"
                className="w-full h-10 px-3 rounded-xl border border-navy-200/80 dark:border-navy-700 bg-white dark:bg-navy-950 text-sm text-[#14233E] dark:text-white focus:outline-none focus:ring-2 focus:ring-[#315FE8]/20 focus:border-[#315FE8]"
                data-testid="select-exam-type"
              >
                <option value="">Seçiniz</option>
                {examTypes.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-[#14233E] dark:text-white mb-1.5">
                Sınav Tarihi <span className="text-red-500">*</span>
              </label>
              <Input
                type="date"
                value={newExamDate}
                onChange={(e) => setNewExamDate(e.target.value)}
                required
                className="rounded-xl border-navy-200/80 focus:border-[#315FE8]"
                data-testid="input-exam-date"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[#14233E] dark:text-white mb-1.5">
                Yanlış Cezası (Örn: 0.25)
              </label>
              <Input
                type="number"
                step="0.05"
                min="0"
                max="1"
                value={newWrongPenalty}
                onChange={(e) => setNewWrongPenalty(parseFloat(e.target.value) || 0)}
                className="rounded-xl border-navy-200/80 focus:border-[#315FE8]"
                data-testid="input-exam-penalty"
              />
            </div>
          </div>

          {/* Responsive Section Setup */}
          <div className="pt-3 border-t border-navy-100 dark:border-navy-800">
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-bold text-[#14233E] dark:text-white uppercase tracking-wider">
                Ders / Bölüm Yapısı ({newSections.length})
              </span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleAddSection}
                className="text-xs flex items-center gap-1 rounded-xl border-navy-200/80"
                data-testid="btn-add-section"
              >
                <Plus className="h-3 w-3 text-[#315FE8]" /> Bölüm Ekle
              </Button>
            </div>

            {newSections.length === 0 ? (
              <p className="text-xs text-navy-500 italic py-3 text-center bg-[#F8F7F3] rounded-xl border border-dashed border-navy-200">
                Sınavda yer alacak en az bir ders bölümü eklemelisiniz.
              </p>
            ) : (
              <div className="space-y-2.5 max-h-60 overflow-y-auto pr-1">
                {newSections.map((sec, idx) => (
                  <div
                    key={idx}
                    className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2 bg-[#F8F7F3] dark:bg-navy-900/60 p-2.5 rounded-xl border border-navy-200/70 dark:border-navy-800 text-xs"
                  >
                    <div className="flex items-center gap-2 flex-1">
                      <span className="font-bold text-navy-400 w-5 shrink-0 text-center">
                        {idx + 1}.
                      </span>
                      <select
                        value={sec.subjectId}
                        onChange={(e) => handleSectionChange(idx, 'subjectId', e.target.value)}
                        aria-label="Ders Seçiniz"
                        className="flex-1 h-9 px-2.5 rounded-lg border border-navy-200/80 bg-white dark:bg-navy-950 text-[#14233E] dark:text-white text-xs focus:ring-1 focus:ring-[#315FE8]"
                      >
                        {subjects.map((sub) => (
                          <option key={sub.id} value={sub.id}>
                            {sub.name}
                          </option>
                        ))}
                      </select>
                    </div>

                    <div className="flex items-center justify-between sm:justify-end gap-2 pl-7 sm:pl-0">
                      <div className="flex items-center gap-1.5">
                        <span className="text-navy-600 dark:text-navy-300 font-medium">Soru:</span>
                        <input
                          type="number"
                          min="1"
                          max="200"
                          value={sec.questionCount}
                          onChange={(e) =>
                            handleSectionChange(idx, 'questionCount', parseInt(e.target.value, 10) || 1)
                          }
                          aria-label="Soru Sayısı"
                          className="w-16 h-9 px-2 rounded-lg border border-navy-200/80 bg-white dark:bg-navy-950 text-center font-bold text-xs"
                        />
                      </div>

                      <button
                        type="button"
                        onClick={() => handleRemoveSection(idx)}
                        className="p-1.5 rounded-lg text-red-500 hover:text-red-700 hover:bg-red-50 dark:hover:bg-red-950/40 transition-colors shrink-0"
                        title="Bölümü Kaldır"
                        aria-label="Bölümü Kaldır"
                      >
                        <Trash2 className="h-4 w-4" />
                      </button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Modal Actions */}
          <div className="flex justify-end gap-2.5 pt-4 border-t border-navy-100 dark:border-navy-800">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsCreateModalOpen(false)}
              disabled={isSubmitting}
              className="rounded-xl border-navy-200/80"
            >
              İptal
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting}
              data-testid="submit-create-exam"
              className="bg-[#315FE8] hover:bg-blue-700 text-white rounded-xl shadow-xs"
            >
              {isSubmitting ? 'Oluşturuluyor...' : 'Sınavı Oluştur'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
