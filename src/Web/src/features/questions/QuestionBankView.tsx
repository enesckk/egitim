import React, { useState, useEffect, useCallback } from 'react';
import {
  Card,
  Button,
  Input,
  Select,
  Badge,
  Modal,
  Alert,
  LoadingState,
  EmptyState,
} from '@/components/ui';
import {
  QuestionDto,
  QuestionDetailDto,
  GetQuestionsParams,
  QuestionStatus,
  DifficultyLevel,
  QuestionSourceType,
  TaxonomyDto,
  CreateQuestionPayload,
  UpdateQuestionPayload,
} from '@/types/question';
import { questionService } from '@/services/questionService';
import { academicService } from '@/services/academicService';
import { useAuth } from '@/auth';
import { QuestionFormModal } from './components/QuestionFormModal';
import { QuestionDetailModal } from './components/QuestionDetailModal';
import {
  Search,
  Plus,
  Filter,
  Globe,
  Building2,
  Lock,
  Edit2,
  Archive,
  Eye,
  ChevronLeft,
  ChevronRight,
  BookOpen,
  RotateCcw,
} from 'lucide-react';

interface QuestionBankViewProps {
  role: 'teacher' | 'admin';
}

export const QuestionBankView: React.FC<QuestionBankViewProps> = ({ role }) => {
  const { user } = useAuth();
  const isSuperAdmin = user?.role === 'admin';

  // Data states
  const [questions, setQuestions] = useState<QuestionDto[]>([]);
  const [totalCount, setTotalCount] = useState<number>(0);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Pagination states
  const [page, setPage] = useState<number>(1);
  const pageSize = 15;

  // Filter states
  const [searchText, setSearchText] = useState<string>('');
  const [debouncedSearch, setDebouncedSearch] = useState<string>('');
  const [poolType, setPoolType] = useState<'all' | 'global' | 'institution'>('all');
  const [status, setStatus] = useState<string>('all');
  const [difficulty, setDifficulty] = useState<string>('all');
  const [sourceType, setSourceType] = useState<string>('all');
  const [sourceYear, setSourceYear] = useState<string>('');

  // Taxonomy Filter states
  const [subjects, setSubjects] = useState<TaxonomyDto[]>([]);
  const [topics, setTopics] = useState<TaxonomyDto[]>([]);
  const [subTopics, setSubTopics] = useState<TaxonomyDto[]>([]);
  const [selectedSubjectId, setSelectedSubjectId] = useState<string>('');
  const [selectedTopicId, setSelectedTopicId] = useState<string>('');
  const [selectedSubTopicId, setSelectedSubTopicId] = useState<string>('');

  // Names dictionary for list rendering
  const [subjectNames, setSubjectNames] = useState<Record<string, string>>({});
  const [topicNames, setTopicNames] = useState<Record<string, string>>({});

  // Modal states
  const [isFormOpen, setIsFormOpen] = useState<boolean>(false);
  const [editingQuestion, setEditingQuestion] = useState<QuestionDetailDto | null>(null);
  const [isSubmittingForm, setIsSubmittingForm] = useState<boolean>(false);

  const [detailQuestion, setDetailQuestion] = useState<QuestionDetailDto | null>(null);
  const [isDetailOpen, setIsDetailOpen] = useState<boolean>(false);

  const [archivingQuestion, setArchivingQuestion] = useState<QuestionDto | null>(null);
  const [isArchiving, setIsArchiving] = useState<boolean>(false);

  // Debounce search text
  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearch(searchText);
      setPage(1);
    }, 300);
    return () => clearTimeout(handler);
  }, [searchText]);

  // Load initial subjects
  useEffect(() => {
    academicService
      .getTaxonomy('subjects')
      .then((data) => {
        setSubjects(data);
        const map: Record<string, string> = {};
        data.forEach((s) => (map[s.id] = s.name));
        setSubjectNames(map);
      })
      .catch(() => {});
  }, []);

  // Load topics when subject filter changes
  useEffect(() => {
    if (!selectedSubjectId) {
      setTopics([]);
      setSelectedTopicId('');
      setSubTopics([]);
      setSelectedSubTopicId('');
      return;
    }

    academicService
      .getTaxonomy('topics', selectedSubjectId)
      .then((data) => {
        setTopics(data);
        setTopicNames((prev) => {
          const map = { ...prev };
          data.forEach((t) => (map[t.id] = t.name));
          return map;
        });
      })
      .catch(() => {});
  }, [selectedSubjectId]);

  // Load subTopics when topic filter changes
  useEffect(() => {
    if (!selectedTopicId) {
      setSubTopics([]);
      setSelectedSubTopicId('');
      return;
    }

    academicService
      .getTaxonomy('sub-topics', selectedTopicId)
      .then(setSubTopics)
      .catch(() => setSubTopics([]));
  }, [selectedTopicId]);

  // Fetch Questions
  const fetchQuestions = useCallback(async () => {
    setIsLoading(true);
    setError(null);

    const params: GetQuestionsParams = {
      page,
      pageSize,
      poolType: poolType !== 'all' ? poolType : undefined,
      status: status !== 'all' ? (Number(status) as QuestionStatus) : undefined,
      difficulty: difficulty !== 'all' ? (Number(difficulty) as DifficultyLevel) : undefined,
      sourceType: sourceType !== 'all' ? (Number(sourceType) as QuestionSourceType) : undefined,
      sourceYear: sourceYear.trim() ? parseInt(sourceYear.trim(), 10) : undefined,
      subjectId: selectedSubjectId || undefined,
      topicId: selectedTopicId || undefined,
      subTopicId: selectedSubTopicId || undefined,
      searchText: debouncedSearch.trim() || undefined,
    };

    try {
      const res = await questionService.getQuestions(params);
      setQuestions(res.items);
      setTotalCount(res.totalCount);
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Soru bankası yüklenirken bir hata oluştu.');
      }
    } finally {
      setIsLoading(false);
    }
  }, [
    page,
    pageSize,
    poolType,
    status,
    difficulty,
    sourceType,
    sourceYear,
    selectedSubjectId,
    selectedTopicId,
    selectedSubTopicId,
    debouncedSearch,
  ]);

  useEffect(() => {
    fetchQuestions();
  }, [fetchQuestions]);

  const handleResetFilters = () => {
    setSearchText('');
    setDebouncedSearch('');
    setPoolType('all');
    setStatus('all');
    setDifficulty('all');
    setSourceType('all');
    setSourceYear('');
    setSelectedSubjectId('');
    setSelectedTopicId('');
    setSelectedSubTopicId('');
    setPage(1);
  };

  // Open Edit
  const handleOpenEdit = async (questionId: string) => {
    try {
      const detail = await questionService.getQuestion(questionId);
      setEditingQuestion(detail);
      setIsFormOpen(true);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Soru detayları yüklenemedi.');
    }
  };

  // Open Detail
  const handleOpenDetail = async (questionId: string) => {
    try {
      const detail = await questionService.getQuestion(questionId);
      setDetailQuestion(detail);
      setIsDetailOpen(true);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Soru detayları yüklenemedi.');
    }
  };

  // Submit Create / Edit Form
  const handleFormSubmit = async (payload: CreateQuestionPayload | UpdateQuestionPayload) => {
    setIsSubmittingForm(true);
    try {
      if (editingQuestion) {
        await questionService.updateQuestion(editingQuestion.id, payload as UpdateQuestionPayload);
      } else {
        await questionService.createQuestion(payload as CreateQuestionPayload);
      }
      setIsFormOpen(false);
      setEditingQuestion(null);
      fetchQuestions();
    } finally {
      setIsSubmittingForm(false);
    }
  };

  // Archive Question
  const handleConfirmArchive = async () => {
    if (!archivingQuestion) return;
    setIsArchiving(true);
    try {
      await questionService.archiveQuestion(archivingQuestion.id);
      setArchivingQuestion(null);
      fetchQuestions();
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Soru arşivlenirken bir hata oluştu.');
    } finally {
      setIsArchiving(false);
    }
  };

  // Security & Immutability Helpers
  const canEditQuestion = (q: QuestionDto | QuestionDetailDto): boolean => {
    // Global questions cannot be edited by normal institution users
    if (q.institutionId === null && !isSuperAdmin) return false;
    // Published & Archived questions are immutable
    if (q.status !== QuestionStatus.Draft) return false;
    return true;
  };

  const getCannotEditReason = (q: QuestionDto | QuestionDetailDto): string | undefined => {
    if (q.institutionId === null && !isSuperAdmin) {
      return 'Global sorular kurumsal kullanıcılar tarafından düzenlenemez.';
    }
    if (q.status === QuestionStatus.Published) {
      return 'Bu soru yayımlandığı veya bir sınavda kullanıldığı için düzenlenemez.';
    }
    if (q.status === QuestionStatus.Archived) {
      return 'Arşivlenmiş sorular düzenlenemez.';
    }
    return undefined;
  };

  const getDifficultyBadge = (d: DifficultyLevel) => {
    switch (d) {
      case DifficultyLevel.Easy:
        return <Badge variant="success">Kolay</Badge>;
      case DifficultyLevel.Medium:
        return <Badge variant="warning">Orta</Badge>;
      case DifficultyLevel.Hard:
        return <Badge variant="danger">Zor</Badge>;
      default:
        return null;
    }
  };

  const getStatusBadge = (s: QuestionStatus) => {
    switch (s) {
      case QuestionStatus.Draft:
        return <Badge variant="warning">Taslak</Badge>;
      case QuestionStatus.Published:
        return <Badge variant="success">Yayımlandı</Badge>;
      case QuestionStatus.Archived:
        return <Badge variant="secondary">Arşivlendi</Badge>;
      default:
        return null;
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div className="space-y-6 pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-neutral-900 tracking-tight flex items-center gap-2">
            <BookOpen className="h-6 w-6 text-primary-600" />
            Soru Bankası
          </h1>
          <p className="text-xs text-neutral-500 mt-1">
            {role === 'admin' ? 'Kurumsal ve global soru havuzunu yönetin.' : 'Sınavlar için soruları inceleyin ve oluşturun.'}
          </p>
        </div>

        <Button
          variant="primary"
          size="sm"
          onClick={() => {
            setEditingQuestion(null);
            setIsFormOpen(true);
          }}
          className="flex items-center gap-1.5 self-start sm:self-auto"
        >
          <Plus className="h-4 w-4" />
          <span>Yeni Soru Oluştur</span>
        </Button>
      </div>

      {error && <Alert variant="danger">{error}</Alert>}

      {/* Filter Bar Card */}
      <Card className="p-4 space-y-4">
        <div className="flex items-center justify-between gap-2 border-b border-neutral-100 pb-3">
          <div className="flex items-center gap-2 text-sm font-semibold text-neutral-700">
            <Filter className="h-4 w-4 text-primary-600" />
            <span>Arama ve Filtreler</span>
          </div>
          <Button variant="ghost" size="sm" onClick={handleResetFilters} className="text-xs text-neutral-500 hover:text-neutral-800">
            <RotateCcw className="h-3.5 w-3.5 mr-1" /> Filtreleri Sıfırla
          </Button>
        </div>

        {/* Row 1: Search & Pool & Status & Difficulty */}
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3">
          <div className="relative">
            <Search className="absolute left-3 top-2.5 h-4 w-4 text-neutral-400" />
            <Input
              placeholder="Soru metninde ara..."
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              className="pl-9 text-xs"
            />
          </div>

          <Select
            value={poolType}
            onChange={(e) => {
              setPoolType(e.target.value as 'all' | 'global' | 'institution');
              setPage(1);
            }}
            className="text-xs"
          >
            <option value="all">Tüm Havuzlar (Global + Kurum)</option>
            <option value="global">Sadece Global Havuz</option>
            <option value="institution">Sadece Kurum Havuzu</option>
          </Select>

          <Select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
            className="text-xs"
          >
            <option value="all">Tüm Durumlar</option>
            <option value={QuestionStatus.Draft}>Taslak</option>
            <option value={QuestionStatus.Published}>Yayımlandı</option>
            <option value={QuestionStatus.Archived}>Arşivlendi</option>
          </Select>

          <Select
            value={difficulty}
            onChange={(e) => {
              setDifficulty(e.target.value);
              setPage(1);
            }}
            className="text-xs"
          >
            <option value="all">Tüm Zorluklar</option>
            <option value={DifficultyLevel.Easy}>Kolay</option>
            <option value={DifficultyLevel.Medium}>Orta</option>
            <option value={DifficultyLevel.Hard}>Zor</option>
          </Select>
        </div>

        {/* Row 2: Taxonomy Filters (Subject, Topic, SubTopic) */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <Select
            value={selectedSubjectId}
            onChange={(e) => {
              setSelectedSubjectId(e.target.value);
              setPage(1);
            }}
            className="text-xs"
          >
            <option value="">Tüm Dersler</option>
            {subjects.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </Select>

          <Select
            value={selectedTopicId}
            onChange={(e) => {
              setSelectedTopicId(e.target.value);
              setPage(1);
            }}
            disabled={!selectedSubjectId}
            className="text-xs"
          >
            <option value="">Tüm Konular</option>
            {topics.map((t) => (
              <option key={t.id} value={t.id}>
                {t.name}
              </option>
            ))}
          </Select>

          <Select
            value={selectedSubTopicId}
            onChange={(e) => {
              setSelectedSubTopicId(e.target.value);
              setPage(1);
            }}
            disabled={!selectedTopicId}
            className="text-xs"
          >
            <option value="">Tüm Alt Konular</option>
            {subTopics.map((st) => (
              <option key={st.id} value={st.id}>
                {st.name}
              </option>
            ))}
          </Select>
        </div>
      </Card>

      {/* Main Content Area */}
      {isLoading ? (
        <LoadingState message="Sorular yükleniyor..." />
      ) : questions.length === 0 ? (
        <EmptyState
          icon={<BookOpen className="h-6 w-6 text-neutral-400" />}
          title={debouncedSearch || selectedSubjectId ? 'Filtrelere uygun soru bulunamadı' : 'Henüz soru bulunmuyor'}
          description={
            debouncedSearch || selectedSubjectId
              ? 'Arama terimlerinizi veya filtrelerinizi değiştirerek tekrar deneyiniz.'
              : 'Yeni soru oluştur butonunu kullanarak ilk sorunuzu ekleyebilirsiniz.'
          }
          action={
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setEditingQuestion(null);
                setIsFormOpen(true);
              }}
            >
              <Plus className="h-4 w-4 mr-1" /> Soru Ekle
            </Button>
          }
        />
      ) : (
        <div className="space-y-3">
          {/* Results Summary */}
          <div className="flex items-center justify-between text-xs text-neutral-500 px-1">
            <span>Toplam <strong>{totalCount}</strong> soru bulundu.</span>
            <span>Sayfa {page} / {totalPages}</span>
          </div>

          {/* Question List Cards */}
          <div className="space-y-3">
            {questions.map((q) => {
              const canEdit = canEditQuestion(q);
              const reason = getCannotEditReason(q);

              return (
                <Card
                  key={q.id}
                  className="p-4 hover:border-neutral-300 transition-colors flex flex-col md:flex-row md:items-center justify-between gap-4"
                >
                  <div className="space-y-2 flex-1 min-w-0">
                    {/* Metadata Badges Header */}
                    <div className="flex flex-wrap items-center gap-2 text-xs">
                      {q.institutionId === null ? (
                        <Badge variant="info" className="flex items-center gap-1">
                          <Globe className="h-3 w-3" /> Global
                        </Badge>
                      ) : (
                        <Badge variant="secondary" className="flex items-center gap-1">
                          <Building2 className="h-3 w-3" /> Kurumsal
                        </Badge>
                      )}

                      {getStatusBadge(q.status)}
                      {getDifficultyBadge(q.difficulty)}

                      <span className="text-neutral-500 font-medium">
                        {subjectNames[q.subjectId] || 'Ders'} • {topicNames[q.topicId] || 'Konu'}
                      </span>
                    </div>

                    {/* Question Content Preview */}
                    <p className="text-sm font-medium text-neutral-900 line-clamp-2 leading-relaxed">
                      {q.content}
                    </p>

                    {/* Sub-info */}
                    <div className="flex items-center gap-4 text-xs text-neutral-400">
                      <span>{q.optionCount} Şık (A–E)</span>
                      <span>Oluşturulma: {new Date(q.createdAt).toLocaleDateString('tr-TR')}</span>
                      {!canEdit && reason && (
                        <span className="flex items-center gap-1 text-amber-700 bg-amber-50 px-2 py-0.5 rounded text-[11px]">
                          <Lock className="h-3 w-3" /> {reason}
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-2 shrink-0 border-t md:border-t-0 pt-3 md:pt-0 border-neutral-100">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleOpenDetail(q.id)}
                      className="text-neutral-600 hover:text-neutral-900"
                    >
                      <Eye className="h-4 w-4 mr-1" /> Detay
                    </Button>

                    <Button
                      variant="outline"
                      size="sm"
                      disabled={!canEdit}
                      onClick={() => handleOpenEdit(q.id)}
                    >
                      <Edit2 className="h-3.5 w-3.5 mr-1" /> Düzenle
                    </Button>

                    {q.status !== QuestionStatus.Archived && (
                      <Button
                        variant="ghost"
                        size="sm"
                        disabled={q.institutionId === null && !isSuperAdmin}
                        onClick={() => setArchivingQuestion(q)}
                        className="text-amber-700 hover:bg-amber-50 hover:text-amber-800"
                      >
                        <Archive className="h-3.5 w-3.5 mr-1" /> Arşivle
                      </Button>
                    )}
                  </div>
                </Card>
              );
            })}
          </div>

          {/* Pagination Controls */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between border-t border-neutral-200 pt-4 px-1">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
              >
                <ChevronLeft className="h-4 w-4 mr-1" /> Önceki
              </Button>

              <span className="text-xs text-neutral-600">
                Sayfa {page} / {totalPages}
              </span>

              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
              >
                Sonraki <ChevronRight className="h-4 w-4 ml-1" />
              </Button>
            </div>
          )}
        </div>
      )}

      {/* Form Modal (Create / Edit) */}
      <QuestionFormModal
        isOpen={isFormOpen}
        onClose={() => {
          setIsFormOpen(false);
          setEditingQuestion(null);
        }}
        onSubmit={handleFormSubmit}
        initialData={editingQuestion}
        isSubmitting={isSubmittingForm}
      />

      {/* Detail Modal */}
      <QuestionDetailModal
        isOpen={isDetailOpen}
        onClose={() => {
          setIsDetailOpen(false);
          setDetailQuestion(null);
        }}
        question={detailQuestion}
        subjectName={detailQuestion ? subjectNames[detailQuestion.subjectId] : undefined}
        topicName={detailQuestion ? topicNames[detailQuestion.topicId] : undefined}
        onEdit={(q) => {
          setIsDetailOpen(false);
          handleOpenEdit(q.id);
        }}
        onArchive={(q) => {
          setIsDetailOpen(false);
          const found = questions.find((x) => x.id === q.id);
          if (found) setArchivingQuestion(found);
        }}
        canEdit={detailQuestion ? canEditQuestion(detailQuestion) : false}
        canArchive={detailQuestion ? detailQuestion.status !== QuestionStatus.Archived : false}
        cannotEditReason={detailQuestion ? getCannotEditReason(detailQuestion) : undefined}
      />

      {/* Archive Confirmation Modal */}
      <Modal
        isOpen={Boolean(archivingQuestion)}
        onClose={() => setArchivingQuestion(null)}
        title="Soruyu Arşivle"
        subtitle="Bu işlem soruyu aktif aramadan kaldıracaktır."
        footer={
          <div className="flex items-center justify-end gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setArchivingQuestion(null)}
              disabled={isArchiving}
            >
              Vazgeç
            </Button>
            <Button
              variant="primary"
              size="sm"
              className="bg-amber-600 hover:bg-amber-700 text-white"
              onClick={handleConfirmArchive}
              isLoading={isArchiving}
            >
              Evet, Arşivle
            </Button>
          </div>
        }
      >
        <div className="space-y-3">
          <p className="text-sm text-neutral-800 font-medium">
            Soru arşivlenecektir. Geçmiş sınav ve sonuç kayıtları etkilenmeyecektir.
          </p>
          {archivingQuestion && (
            <div className="p-3 bg-neutral-50 rounded-lg text-xs text-neutral-600 border border-neutral-200 line-clamp-3">
              "{archivingQuestion.content}"
            </div>
          )}
        </div>
      </Modal>
    </div>
  );
};
