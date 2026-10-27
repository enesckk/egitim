import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
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
import { teacherService, TeacherMeDto } from '@/services/teacherService';
import { examService } from '@/services/examService';
import { ExamDto } from '@/types/exam';
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
  Layers,
  ChevronDown,
  ChevronUp,
  FileSpreadsheet,
} from 'lucide-react';

interface QuestionBankViewProps {
  role: 'teacher' | 'admin';
}

export const QuestionBankView: React.FC<QuestionBankViewProps> = ({ role }) => {
  const navigate = useNavigate();
  const { user } = useAuth();
  const isSuperAdmin = user?.role === 'admin';

  // Teacher specific state
  const [teacherProfile, setTeacherProfile] = useState<TeacherMeDto | null>(null);
  const [availableExams, setAvailableExams] = useState<ExamDto[]>([]);
  const [isAssemblyModalOpen, setIsAssemblyModalOpen] = useState(false);

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
  const [showAdvancedFilters, setShowAdvancedFilters] = useState<boolean>(false);

  // Taxonomy Filter states
  const [subjects, setSubjects] = useState<TaxonomyDto[]>([]);
  const [topics, setTopics] = useState<TaxonomyDto[]>([]);
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

  // Load teacher profile & authorized exams when role is teacher
  useEffect(() => {
    if (role === 'teacher') {
      teacherService
        .getMe()
        .then((me) => {
          setTeacherProfile(me);
        })
        .catch(() => {});

      examService
        .getExams({ page: 1, pageSize: 20 })
        .then((res) => {
          setAvailableExams(res.items || []);
        })
        .catch(() => {});
    }
  }, [role]);

  // Load initial subjects & set default teacher assigned subject if applicable
  useEffect(() => {
    academicService
      .getTaxonomy('subjects')
      .then((data) => {
        setSubjects(data);
        const map: Record<string, string> = {};
        data.forEach((s) => (map[s.id] = s.name));
        setSubjectNames(map);

        // If teacher has assigned subjects, find matching subject and select it by default once
        if (role === 'teacher' && teacherProfile && teacherProfile.subjects && teacherProfile.subjects.length > 0) {
          const teacherSubjectNames = teacherProfile.subjects.map((ts) => ts.subjectName.toLowerCase());
          const matched = data.find((s) => teacherSubjectNames.includes(s.name.toLowerCase()));
          if (matched && !selectedSubjectId) {
            setSelectedSubjectId(matched.id);
          }
        }
      })
      .catch(() => {});
  }, [role, teacherProfile, selectedSubjectId]);

  // Load topics when subject filter changes
  useEffect(() => {
    if (!selectedSubjectId) {
      setTopics([]);
      setSelectedTopicId('');
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
      setSelectedSubTopicId('');
      return;
    }

    academicService
      .getTaxonomy('sub-topics', selectedTopicId)
      .catch(() => {});
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
    if (q.institutionId === null && !isSuperAdmin) return false;
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

  // Authoritative subjects for teacher display
  const teacherSubjectsDisplay = useMemo(() => {
    if (teacherProfile && teacherProfile.subjects && teacherProfile.subjects.length > 0) {
      return teacherProfile.subjects.map((s) => s.subjectName).join(', ');
    }
    return 'Matematik';
  }, [teacherProfile]);

  return (
    <div className="w-full max-w-6xl mx-auto select-none space-y-5 sm:space-y-6 pb-12" data-testid="question-bank-view">
      {/* 1. Header with Real Scope & Statistics */}
      {role === 'teacher' ? (
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-5 sm:p-6 shadow-soft-sm">
          <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-5">
            <div className="space-y-2 max-w-2xl">
              <div className="flex items-center gap-2.5 flex-wrap">
                <h1 className="font-serif text-2xl sm:text-3xl font-bold text-neutral-900 tracking-tight">
                  Soru Bankası & Deneme Montajı
                </h1>
                <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-primary-50 text-primary-700 border border-primary-200">
                  <BookOpen className="h-3.5 w-3.5" />
                  {teacherSubjectsDisplay}
                </span>
              </div>
              <p className="text-xs sm:text-sm text-neutral-500 leading-relaxed">
                Kurumsal ve genel soru havuzundan branşınıza uygun soruları filtreleyin, yeni sorular ekleyin ve sınav montajı yapın
              </p>
            </div>

            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 border-t lg:border-t-0 lg:border-l border-neutral-200/80 pt-4 lg:pt-0 lg:pl-6">
              <div className="bg-[#F8F7F3] p-3 rounded-xl border border-neutral-200/80 min-w-[120px]">
                <span className="text-[10px] uppercase font-bold text-neutral-500 tracking-wider block">
                  Havuz Sorusu
                </span>
                <div className="flex items-center gap-1.5 mt-0.5">
                  <Layers className="h-4 w-4 text-primary-600 flex-shrink-0" />
                  <span className="font-mono text-lg font-bold text-neutral-900">
                    {totalCount}
                  </span>
                  <span className="text-xs text-neutral-500">Soru</span>
                </div>
              </div>

              <div className="flex items-center gap-2">
                {availableExams.length > 0 && (
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => setIsAssemblyModalOpen(true)}
                    leftIcon={<FileSpreadsheet className="h-4 w-4 text-primary-600" />}
                    className="h-11 px-3 text-xs font-semibold bg-[#F8F7F3] border-neutral-200 hover:bg-primary-50 hover:text-primary-700"
                    id="btn-exam-assembly"
                  >
                    Sınav Montajı
                  </Button>
                )}

                <Button
                  variant="primary"
                  size="sm"
                  onClick={() => {
                    setEditingQuestion(null);
                    setIsFormOpen(true);
                  }}
                  leftIcon={<Plus className="h-4 w-4" />}
                  className="h-11 px-4 text-xs font-semibold shadow-soft-sm"
                  id="btn-create-question"
                >
                  Yeni Soru Ekle
                </Button>
              </div>
            </div>
          </div>
        </div>
      ) : (
        /* Admin Default Header */
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div>
            <h1 className="text-2xl font-bold text-neutral-900 tracking-tight flex items-center gap-2">
              <BookOpen className="h-6 w-6 text-primary-600" />
              Soru Bankası
            </h1>
            <p className="text-xs text-neutral-500 mt-1">
              Kurumsal ve global soru havuzunu yönetin.
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
      )}

      {error && <Alert variant="danger">{error}</Alert>}

      {/* 2. Simplified Filters Bar */}
      <div className="bg-white p-4 sm:p-5 rounded-2xl border border-neutral-200/80 shadow-soft-sm space-y-3.5">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-3">
          {/* Search Box */}
          <div className="relative flex-1 min-w-0">
            <Search className="absolute left-3.5 top-3 h-4 w-4 text-neutral-400" />
            <Input
              placeholder="Soru metninde veya kazanımda ara..."
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              className="pl-10 text-xs bg-[#F8F7F3] border-neutral-200 w-full"
            />
          </div>

          <div className="flex items-center justify-between sm:justify-start gap-2 flex-wrap">
            {/* Pool Filter Chips */}
            <div className="inline-flex rounded-xl bg-[#F8F7F3] p-1 border border-neutral-200/80 flex-wrap">
              <button
                type="button"
                onClick={() => {
                  setPoolType('all');
                  setPage(1);
                }}
                className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all ${
                  poolType === 'all'
                    ? 'bg-white text-neutral-900 shadow-2xs font-semibold'
                    : 'text-neutral-600 hover:text-neutral-900'
                }`}
              >
                Tüm Havuzlar
              </button>
              <button
                type="button"
                onClick={() => {
                  setPoolType('institution');
                  setPage(1);
                }}
                className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all ${
                  poolType === 'institution'
                    ? 'bg-white text-neutral-900 shadow-2xs font-semibold'
                    : 'text-neutral-600 hover:text-neutral-900'
                }`}
              >
                Kurum Havuzu
              </button>
              <button
                type="button"
                onClick={() => {
                  setPoolType('global');
                  setPage(1);
                }}
                className={`px-3 py-1.5 rounded-lg text-xs font-medium transition-all ${
                  poolType === 'global'
                    ? 'bg-white text-neutral-900 shadow-2xs font-semibold'
                    : 'text-neutral-600 hover:text-neutral-900'
                }`}
              >
                Genel Havuz
              </button>
            </div>

            {/* Toggle Advanced Filters */}
            <div className="flex items-center gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setShowAdvancedFilters(!showAdvancedFilters)}
                className="text-xs text-neutral-700 bg-white border-neutral-200 hover:bg-neutral-50"
              >
                <Filter className="h-3.5 w-3.5 mr-1 text-primary-600" />
                <span>Filtreler</span>
                {showAdvancedFilters ? (
                  <ChevronUp className="h-3.5 w-3.5 ml-1 text-neutral-400" />
                ) : (
                  <ChevronDown className="h-3.5 w-3.5 ml-1 text-neutral-400" />
                )}
              </Button>

              {(selectedSubjectId || selectedTopicId || difficulty !== 'all' || status !== 'all' || debouncedSearch) && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleResetFilters}
                  className="text-xs text-neutral-500 hover:text-neutral-800"
                >
                  <RotateCcw className="h-3.5 w-3.5 mr-1" /> Temizle
                </Button>
              )}
            </div>
          </div>
        </div>

        {/* Collapsible Advanced Filters */}
        {showAdvancedFilters && (
          <div className="pt-3 border-t border-neutral-100 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 animate-fadeIn">
            {/* Subject Selector */}
            <div>
              <label className="text-[11px] font-semibold text-neutral-600 block mb-1">Ders / Branş</label>
              <Select
                value={selectedSubjectId}
                onChange={(e) => {
                  setSelectedSubjectId(e.target.value);
                  setPage(1);
                }}
                className="text-xs bg-[#F8F7F3] border-neutral-200"
              >
                <option value="">Tüm Dersler</option>
                {subjects.map((s) => (
                  <option key={s.id} value={s.id}>
                    {s.name}
                  </option>
                ))}
              </Select>
            </div>

            {/* Topic Selector */}
            <div>
              <label className="text-[11px] font-semibold text-neutral-600 block mb-1">Konu</label>
              <Select
                value={selectedTopicId}
                onChange={(e) => {
                  setSelectedTopicId(e.target.value);
                  setPage(1);
                }}
                disabled={!selectedSubjectId}
                className="text-xs bg-[#F8F7F3] border-neutral-200"
              >
                <option value="">Tüm Konular</option>
                {topics.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
              </Select>
            </div>

            {/* Difficulty Selector */}
            <div>
              <label className="text-[11px] font-semibold text-neutral-600 block mb-1">Zorluk Seviyesi</label>
              <Select
                value={difficulty}
                onChange={(e) => {
                  setDifficulty(e.target.value);
                  setPage(1);
                }}
                className="text-xs bg-[#F8F7F3] border-neutral-200"
              >
                <option value="all">Tüm Zorluklar</option>
                <option value={DifficultyLevel.Easy}>Kolay</option>
                <option value={DifficultyLevel.Medium}>Orta</option>
                <option value={DifficultyLevel.Hard}>Zor</option>
              </Select>
            </div>

            {/* Status Selector */}
            <div>
              <label className="text-[11px] font-semibold text-neutral-600 block mb-1">Durum</label>
              <Select
                value={status}
                onChange={(e) => {
                  setStatus(e.target.value);
                  setPage(1);
                }}
                className="text-xs bg-[#F8F7F3] border-neutral-200"
              >
                <option value="all">Tüm Durumlar</option>
                <option value={QuestionStatus.Draft}>Taslak</option>
                <option value={QuestionStatus.Published}>Yayımlandı</option>
                <option value={QuestionStatus.Archived}>Arşivlendi</option>
              </Select>
            </div>
          </div>
        )}
      </div>

      {/* 3. Main Content Area */}
      {isLoading ? (
        <LoadingState message="Sorular yükleniyor..." />
      ) : questions.length === 0 ? (
        <EmptyState
          icon={<BookOpen className="h-8 w-8 text-neutral-400" />}
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
        <div className="space-y-4">
          {/* Results Summary */}
          <div className="flex items-center justify-between text-xs text-neutral-500 px-1">
            <span>
              Toplam <strong className="text-neutral-800">{totalCount}</strong> soru listeleniyor.
            </span>
            <span>
              Sayfa {page} / {totalPages}
            </span>
          </div>

          {/* Question List Cards */}
          <div className="space-y-3.5">
            {questions.map((q) => {
              const canEdit = canEditQuestion(q);
              const reason = getCannotEditReason(q);

              return (
                <div
                  key={q.id}
                  className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-sm hover:border-primary-300 transition-colors flex flex-col md:flex-row md:items-center justify-between gap-4"
                >
                  <div className="space-y-2 flex-1 min-w-0">
                    {/* Metadata Header */}
                    <div className="flex flex-wrap items-center gap-2 text-xs">
                      {q.institutionId === null ? (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-sky-50 text-sky-700 border border-sky-200">
                          <Globe className="h-3 w-3" /> Genel Havuz
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-primary-50 text-primary-700 border border-primary-200">
                          <Building2 className="h-3 w-3" /> Kurumsal
                        </span>
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

                    {/* Style Badges */}
                    {q.styles && q.styles.length > 0 && (
                      <div className="flex flex-wrap items-center gap-1.5 pt-0.5">
                        {q.styles.map((s) => (
                          <span
                            key={s.id}
                            className="inline-flex items-center px-2 py-0.5 rounded text-[11px] font-medium bg-[#F8F7F3] text-neutral-700 border border-neutral-200"
                          >
                            {s.name}
                          </span>
                        ))}
                      </div>
                    )}

                    {/* Sub-info */}
                    <div className="flex flex-wrap items-center gap-4 text-[11px] text-neutral-400 font-mono">
                      <span>{q.optionCount} Şık</span>
                      <span>Kayıt: {new Date(q.createdAt).toLocaleDateString('tr-TR')}</span>
                      {!canEdit && reason && (
                        <span className="flex items-center gap-1 text-amber-700 bg-amber-50 px-2 py-0.5 rounded text-[11px] font-sans">
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
                      className="text-neutral-600 hover:text-neutral-900 text-xs"
                    >
                      <Eye className="h-3.5 w-3.5 mr-1 text-primary-600" /> İncele
                    </Button>

                    <Button
                      variant="outline"
                      size="sm"
                      disabled={!canEdit}
                      onClick={() => handleOpenEdit(q.id)}
                      className="text-xs"
                    >
                      <Edit2 className="h-3.5 w-3.5 mr-1" /> Düzenle
                    </Button>

                    {q.status !== QuestionStatus.Archived && (
                      <Button
                        variant="ghost"
                        size="sm"
                        disabled={q.institutionId === null && !isSuperAdmin}
                        onClick={() => setArchivingQuestion(q)}
                        className="text-amber-700 hover:bg-amber-50 hover:text-amber-800 text-xs"
                      >
                        <Archive className="h-3.5 w-3.5 mr-1" /> Arşivle
                      </Button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>

          {/* 4. Pagination Controls */}
          {totalPages > 1 && (
            <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-sm flex items-center justify-between">
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="text-xs"
              >
                <ChevronLeft className="h-4 w-4 mr-1" /> Önceki
              </Button>

              <span className="text-xs font-semibold text-neutral-700">
                Sayfa {page} / {totalPages}
              </span>

              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                className="text-xs"
              >
                Sonraki <ChevronRight className="h-4 w-4 ml-1" />
              </Button>
            </div>
          )}
        </div>
      )}

      {/* 5. Exam Assembly Target Selection Modal */}
      {isAssemblyModalOpen && (
        <Modal
          isOpen={isAssemblyModalOpen}
          onClose={() => setIsAssemblyModalOpen(false)}
          title="Deneme Sınavı Montajı"
          subtitle="Soru eklemek istediğiniz aktif sınavı seçiniz"
          headerVariant="dark"
          footer={
            <div className="flex justify-end gap-2 w-full">
              <Button variant="secondary" size="sm" onClick={() => setIsAssemblyModalOpen(false)}>
                Kapat
              </Button>
            </div>
          }
        >
          <div className="space-y-3 py-2">
            {availableExams.length === 0 ? (
              <p className="text-xs text-neutral-500 text-center py-4">
                Montaj yapılabilecek kayıtlı sınav bulunmuyor.
              </p>
            ) : (
              <div className="space-y-2 max-h-72 overflow-y-auto pr-1">
                {availableExams.map((exam) => (
                  <div
                    key={exam.id}
                    className="p-3 bg-[#F8F7F3] rounded-xl border border-neutral-200/80 flex items-center justify-between gap-3 hover:border-primary-300 transition-colors"
                  >
                    <div>
                      <h4 className="text-xs font-bold text-neutral-900">{exam.title}</h4>
                      <p className="text-[11px] text-neutral-500 font-mono">
                        {exam.examTypeName || 'Sınav'} • {new Date(exam.examDate).toLocaleDateString('tr-TR')}
                      </p>
                    </div>

                    <Button
                      variant="primary"
                      size="sm"
                      onClick={() => {
                        setIsAssemblyModalOpen(false);
                        navigate(`/exams/${exam.id}/questions`);
                      }}
                      className="text-xs shrink-0"
                    >
                      Soruları Yönet
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </Modal>
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
        headerVariant="dark"
        footer={
          <div className="flex items-center justify-end gap-2 w-full">
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
        <div className="space-y-3 py-2 text-xs text-neutral-700">
          <p className="font-medium text-neutral-900">
            Soru arşivlenecektir. Geçmiş sınav ve sonuç kayıtları etkilenmeyecektir.
          </p>
          {archivingQuestion && (
            <div className="p-3 bg-[#F8F7F3] rounded-xl text-xs text-neutral-600 border border-neutral-200 line-clamp-3">
              "{archivingQuestion.content}"
            </div>
          )}
        </div>
      </Modal>
    </div>
  );
};
