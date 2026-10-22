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
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
            <CheckCircle2 className="w-3.5 h-3.5" /> Yayında
          </span>
        );
      case ExamStatus.Closed:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-slate-100 text-slate-700 border border-slate-200">
            <Archive className="w-3.5 h-3.5" /> Kapandı
          </span>
        );
      case ExamStatus.Draft:
      default:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200">
            <Clock className="w-3.5 h-3.5" /> Taslak
          </span>
        );
    }
  };

  return (
    <div className="space-y-6" data-testid="admin-exams-page">
      {/* Header */}
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 border-b border-navy-100 pb-5">
        <div>
          <h1 className="text-2xl font-bold text-navy-950 flex items-center gap-2">
            <FileText className="h-6 w-6 text-primary" />
            Deneme Sınavları
          </h1>
          <p className="text-sm text-navy-600 mt-1">
            Kurumsal deneme sınavlarını oluşturun, soru dizilimlerini yönetin ve manuel/Excel sonuç girişlerini yapın.
          </p>
        </div>
        <Button
          onClick={handleOpenCreateModal}
          data-testid="create-exam-btn"
          className="flex items-center gap-2 bg-primary text-white hover:bg-primary-hover shrink-0"
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

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3 items-center justify-between bg-white p-4 rounded-xl border border-navy-100 shadow-sm">
        <div className="relative w-full sm:w-80">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-navy-400" />
          <Input
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Sınav adı veya kodu ile ara..."
            className="pl-9 bg-navy-50/50 border-navy-200"
            data-testid="exam-search-input"
          />
        </div>

        <div className="flex flex-wrap items-center gap-3 w-full sm:w-auto">
          <select
            value={selectedExamType}
            onChange={(e) => setSelectedExamType(e.target.value)}
            aria-label="Sınav Türü Filtresi"
            className="h-10 px-3 rounded-lg border border-navy-200 bg-white text-sm text-navy-800 focus:outline-none focus:ring-2 focus:ring-primary/20"
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
            className="h-10 px-3 rounded-lg border border-navy-200 bg-white text-sm text-navy-800 focus:outline-none focus:ring-2 focus:ring-primary/20"
            data-testid="exam-status-filter"
          >
            <option value="all">Tüm Durumlar</option>
            <option value="draft">Taslak</option>
            <option value="published">Yayında</option>
            <option value="closed">Kapandı</option>
          </select>
        </div>
      </div>

      {/* Loading Skeleton */}
      {isLoading ? (
        <div className="space-y-3">
          {[1, 2, 3, 4].map((i) => (
            <div key={i} className="p-5 bg-white rounded-xl border border-navy-100 animate-pulse flex items-center justify-between">
              <div className="space-y-2">
                <Skeleton className="h-5 w-48 bg-navy-100" />
                <Skeleton className="h-4 w-32 bg-navy-50" />
              </div>
              <div className="flex gap-2">
                <Skeleton className="h-9 w-28 bg-navy-100 rounded-lg" />
                <Skeleton className="h-9 w-28 bg-navy-100 rounded-lg" />
              </div>
            </div>
          ))}
        </div>
      ) : filteredExams.length === 0 ? (
        /* Empty State */
        <div
          data-testid="admin-exams-empty"
          className="bg-white rounded-xl border border-navy-100 p-12 text-center flex flex-col items-center justify-center space-y-4"
        >
          <div className="h-14 w-14 rounded-2xl bg-navy-50 flex items-center justify-center text-navy-400">
            <ClipboardList className="h-7 w-7" />
          </div>
          <div className="max-w-md">
            <h3 className="text-lg font-bold text-navy-900">Henüz Deneme Sınavı Bulunmuyor</h3>
            <p className="text-sm text-navy-500 mt-1">
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
            >
              Filtreleri Sıfırla
            </Button>
          ) : (
            <Button onClick={handleOpenCreateModal} className="flex items-center gap-2">
              <Plus className="h-4 w-4" />
              Yeni Sınav Oluştur
            </Button>
          )}
        </div>
      ) : (
        /* Exam List */
        <div className="bg-white rounded-xl border border-navy-100 overflow-hidden shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="bg-navy-50/70 border-b border-navy-100 text-xs font-semibold text-navy-600 uppercase tracking-wider">
                  <th className="py-3.5 px-4">Sınav Bilgisi</th>
                  <th className="py-3.5 px-4">Tür & Bölümler</th>
                  <th className="py-3.5 px-4">Tarih</th>
                  <th className="py-3.5 px-4">Durum</th>
                  <th className="py-3.5 px-4 text-right">İşlemler</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-navy-100 text-sm">
                {filteredExams.map((exam) => (
                  <tr
                    key={exam.id}
                    data-testid={`exam-row-${exam.id}`}
                    className="hover:bg-navy-50/40 transition-colors"
                  >
                    <td className="py-4 px-4">
                      <div className="font-semibold text-navy-950">{exam.title}</div>
                      <div className="text-xs font-mono text-navy-500 mt-0.5">{exam.code}</div>
                    </td>
                    <td className="py-4 px-4">
                      <div className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md bg-navy-50 text-navy-800 font-medium text-xs">
                        <Layers className="h-3.5 w-3.5 text-navy-500" />
                        {exam.examTypeName || 'Genel Sınav'}
                      </div>
                      <div className="text-xs text-navy-500 mt-1">
                        {exam.sectionCount > 0 ? `${exam.sectionCount} Bölüm / Ders` : 'Bölüm Yok'}
                      </div>
                    </td>
                    <td className="py-4 px-4 text-navy-600">
                      <div className="flex items-center gap-1.5 text-xs">
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
                    <td className="py-4 px-4 text-right">
                      <div className="flex items-center justify-end gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          data-testid={`navigate-questions-${exam.id}`}
                          onClick={() => navigate(`/admin/exams/${exam.id}/questions`)}
                          className="flex items-center gap-1 text-xs border-navy-200 text-navy-700 hover:bg-navy-50"
                        >
                          <HelpCircle className="h-3.5 w-3.5 text-navy-500" />
                          Soru Dizilimi
                        </Button>
                        <Button
                          size="sm"
                          data-testid={`navigate-results-${exam.id}`}
                          onClick={() => navigate(`/admin/exams/${exam.id}/results/entry`)}
                          className="flex items-center gap-1 text-xs bg-navy-900 text-white hover:bg-navy-800"
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
              <label className="block text-xs font-semibold text-navy-700 mb-1">
                Sınav Başlığı *
              </label>
              <Input
                value={newTitle}
                onChange={(e) => setNewTitle(e.target.value)}
                placeholder="Örn: 2026 TYT Genel Deneme - 1"
                required
                data-testid="input-exam-title"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-navy-700 mb-1">
                Sınav Kodu *
              </label>
              <Input
                value={newCode}
                onChange={(e) => setNewCode(e.target.value)}
                placeholder="Örn: TYT-2026-01"
                required
                data-testid="input-exam-code"
              />
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div>
              <label className="block text-xs font-semibold text-navy-700 mb-1">
                Sınav Türü *
              </label>
              <select
                value={newExamTypeId}
                onChange={(e) => setNewExamTypeId(e.target.value)}
                required
                aria-label="Sınav Türü Seçiniz"
                className="w-full h-10 px-3 rounded-lg border border-navy-200 bg-white text-sm text-navy-800 focus:outline-none focus:ring-2 focus:ring-primary/20"
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
              <label className="block text-xs font-semibold text-navy-700 mb-1">
                Sınav Tarihi *
              </label>
              <Input
                type="date"
                value={newExamDate}
                onChange={(e) => setNewExamDate(e.target.value)}
                required
                data-testid="input-exam-date"
              />
            </div>
            <div>
              <label className="block text-xs font-semibold text-navy-700 mb-1">
                Yanlış Cezası (Örn: 0.25)
              </label>
              <Input
                type="number"
                step="0.05"
                min="0"
                max="1"
                value={newWrongPenalty}
                onChange={(e) => setNewWrongPenalty(parseFloat(e.target.value) || 0)}
                data-testid="input-exam-penalty"
              />
            </div>
          </div>

          {/* Section Setup */}
          <div className="pt-2 border-t border-navy-100">
            <div className="flex items-center justify-between mb-2">
              <span className="text-xs font-bold text-navy-900 uppercase tracking-wider">
                Ders / Bölüm Yapısı ({newSections.length})
              </span>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleAddSection}
                className="text-xs flex items-center gap-1"
                data-testid="btn-add-section"
              >
                <Plus className="h-3 w-3" /> Bölüm Ekle
              </Button>
            </div>

            {newSections.length === 0 ? (
              <p className="text-xs text-navy-500 italic py-2">
                Sınavda yer alacak en az bir ders bölümü eklemelisiniz.
              </p>
            ) : (
              <div className="space-y-2 max-h-48 overflow-y-auto pr-1">
                {newSections.map((sec, idx) => (
                  <div
                    key={idx}
                    className="flex items-center gap-2 bg-navy-50/60 p-2 rounded-lg border border-navy-100 text-xs"
                  >
                    <span className="font-semibold text-navy-500 w-5">{idx + 1}.</span>
                    <select
                      value={sec.subjectId}
                      onChange={(e) => handleSectionChange(idx, 'subjectId', e.target.value)}
                      aria-label="Ders Seçiniz"
                      className="flex-1 h-8 px-2 rounded border border-navy-200 bg-white text-navy-800 text-xs"
                    >
                      {subjects.map((sub) => (
                        <option key={sub.id} value={sub.id}>
                          {sub.name}
                        </option>
                      ))}
                    </select>
                    <div className="flex items-center gap-1">
                      <span className="text-navy-500">Soru:</span>
                      <input
                        type="number"
                        min="1"
                        max="200"
                        value={sec.questionCount}
                        onChange={(e) =>
                          handleSectionChange(idx, 'questionCount', parseInt(e.target.value, 10) || 1)
                        }
                        className="w-16 h-8 px-2 rounded border border-navy-200 bg-white text-center text-xs"
                      />
                    </div>
                    <button
                      type="button"
                      onClick={() => handleRemoveSection(idx)}
                      className="text-red-500 hover:text-red-700 px-1 font-bold text-sm"
                      title="Bölümü Kaldır"
                    >
                      &times;
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* Modal Actions */}
          <div className="flex justify-end gap-2 pt-4 border-t border-navy-100">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsCreateModalOpen(false)}
              disabled={isSubmitting}
            >
              İptal
            </Button>
            <Button
              type="submit"
              disabled={isSubmitting}
              data-testid="submit-create-exam"
              className="bg-primary text-white hover:bg-primary-hover"
            >
              {isSubmitting ? 'Oluşturuluyor...' : 'Sınavı Oluştur'}
            </Button>
          </div>
        </form>
      </Modal>
    </div>
  );
};
