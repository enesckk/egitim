import React, { useState, useEffect, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Button,
  Input,
  Select,
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
  CheckCircle2,
  Clock,
  Archive as ArchiveIcon,
  AlertCircle,
  RefreshCw,
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
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
            Kolay
          </span>
        );
      case DifficultyLevel.Medium:
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-amber-50 text-amber-700 border border-amber-200">
            Orta
          </span>
        );
      case DifficultyLevel.Hard:
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-[11px] font-semibold bg-rose-50 text-rose-700 border border-rose-200">
            Zor
          </span>
        );
      default:
        return null;
    }
  };

  const getStatusBadge = (s: QuestionStatus) => {
    switch (s) {
      case QuestionStatus.Draft:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-amber-50 text-amber-700 border border-amber-200">
            <Clock className="h-3 w-3 text-amber-600" /> Taslak
          </span>
        );
      case QuestionStatus.Published:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
            <CheckCircle2 className="h-3 w-3 text-emerald-600" /> Yayımlandı
          </span>
        );
      case QuestionStatus.Archived:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-slate-100 text-slate-700 border border-slate-200">
            <ArchiveIcon className="h-3 w-3 text-slate-500" /> Arşivlendi
          </span>
        );
      default:
        return null;
    }
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const currentCountOnPage = questions.length;
  const startItemIndex = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const endItemIndex = Math.min(page * pageSize, totalCount);

  // Authoritative subjects for teacher display
  const teacherSubjectsDisplay = useMemo(() => {
    if (teacherProfile && teacherProfile.subjects && teacherProfile.subjects.length > 0) {
      return teacherProfile.subjects.map((s) => s.subjectName).join(', ');
    }
    return 'Matematik';
  }, [teacherProfile]);

  const hasActiveFilters = Boolean(
    selectedSubjectId ||
    selectedTopicId ||
    difficulty !== 'all' ||
    status !== 'all' ||
    sourceType !== 'all' ||
    sourceYear ||
    debouncedSearch ||
    poolType !== 'all'
  );

  return (
    <div
      className="w-full max-w-7xl mx-auto select-none space-y-5 sm:space-y-6 pb-12"
      data-testid={role === 'admin' ? 'admin-questions-page' : 'teacher-questions-page'}
    >
      {/* Hidden marker for testid compatibility */}
      <span className="sr-only" data-testid="question-bank-view">Soru Bankası</span>

      {/* 1. Header with Ivory/Navy/Blue Design System */}
      {role === 'teacher' ? (
        <div className="bg-[#F8F7F3] rounded-2xl border border-navy-200/80 p-5 sm:p-6 shadow-soft-xs">
          <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-5">
            <div className="space-y-2 max-w-2xl">
              <div className="flex items-center gap-2.5 flex-wrap">
                <div className="p-2 rounded-xl bg-blue-50 text-[#315FE8]">
                  <BookOpen className="h-6 w-6" />
                </div>
                <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#14233E] tracking-tight">
                  Soru Bankası & Deneme Montajı
                </h1>
                <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-blue-50 text-[#315FE8] border border-blue-200">
                  <BookOpen className="h-3.5 w-3.5" />
                  {teacherSubjectsDisplay}
                </span>
              </div>
              <p className="text-xs sm:text-sm text-navy-600 leading-relaxed pl-1">
                Kurumsal ve genel soru havuzundan branşınıza uygun soruları filtreleyin, yeni sorular ekleyin ve sınav montajı yapın.
              </p>
            </div>

            <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 border-t lg:border-t-0 lg:border-l border-navy-200/80 pt-4 lg:pt-0 lg:pl-6">
              <div className="bg-white p-3 rounded-xl border border-navy-200/80 min-w-[130px] shadow-2xs">
                <span className="text-[10px] uppercase font-bold text-navy-500 tracking-wider block">
                  Havuz Sorusu
                </span>
                <div className="flex items-center gap-1.5 mt-0.5">
                  <Layers className="h-4 w-4 text-[#315FE8] flex-shrink-0" />
                  <span className="font-mono text-lg font-bold text-[#14233E]" data-testid="pool-question-count">
                    {totalCount}
                  </span>
                  <span className="text-xs text-navy-500">Soru</span>
                </div>
              </div>

              <div className="flex items-center gap-2">
                {availableExams.length > 0 && (
                  <Button
                    variant="secondary"
                    size="sm"
                    onClick={() => setIsAssemblyModalOpen(true)}
                    leftIcon={<FileSpreadsheet className="h-4 w-4 text-[#315FE8]" />}
                    className="h-11 px-3 text-xs font-semibold bg-white border-navy-200/80 hover:bg-blue-50 hover:text-[#315FE8] text-[#14233E] rounded-xl shadow-2xs"
                    id="btn-exam-assembly"
                    data-testid="btn-exam-assembly"
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
                  className="h-11 px-4 text-xs font-semibold bg-[#315FE8] hover:bg-blue-700 text-white rounded-xl shadow-soft-xs"
                  id="btn-create-question"
                  data-testid="btn-create-question"
                >
                  Yeni Soru Ekle
                </Button>
              </div>
            </div>
          </div>
        </div>
      ) : (
        /* Admin Default Header */
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-navy-200/70 pb-5">
          <div>
            <div className="flex items-center gap-2.5 flex-wrap">
              <div className="p-2 rounded-xl bg-blue-50 text-[#315FE8]">
                <BookOpen className="h-6 w-6" />
              </div>
              <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#14233E] tracking-tight">
                Soru Bankası
              </h1>
              <span className="text-xs font-bold text-[#315FE8] bg-blue-50 px-2.5 py-0.5 rounded-full border border-blue-100">
                {totalCount} Havuz Sorusu
              </span>
            </div>
            <p className="text-xs sm:text-sm text-navy-600 mt-1 pl-1">
              Kurumsal ve global soru havuzunu yönetin, zorluk ve kazanım etiketlerini denetleyin.
            </p>
          </div>

          <Button
            variant="primary"
            size="sm"
            onClick={() => {
              setEditingQuestion(null);
              setIsFormOpen(true);
            }}
            data-testid="create-question-btn"
            className="flex items-center gap-2 bg-[#315FE8] hover:bg-blue-700 text-white font-medium rounded-xl shadow-soft-xs px-4 py-2.5 self-start sm:self-auto shrink-0"
          >
            <Plus className="h-4 w-4" />
            <span>Yeni Soru Ekle</span>
          </Button>
        </div>
      )}

      {error && (
        <Alert variant="danger" className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <AlertCircle className="h-4 w-4" />
            <span>{error}</span>
          </div>
          <Button variant="outline" size="sm" onClick={fetchQuestions} className="flex items-center gap-1">
            <RefreshCw className="h-3 w-3" /> Tekrar Dene
          </Button>
        </Alert>
      )}

      {/* 2. Filters Toolbar */}
      <div className="bg-[#F8F7F3] p-4 sm:p-5 rounded-2xl border border-navy-200/80 shadow-soft-xs space-y-3.5">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-3">
          {/* Search Box */}
          <div className="relative flex-1 min-w-0">
            <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-navy-400 pointer-events-none" />
            <Input
              placeholder="Soru metninde veya kazanımda ara..."
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              className="pl-10 text-xs sm:text-sm bg-white border-navy-200/80 rounded-xl w-full text-[#14233E] placeholder:text-navy-400 focus:ring-2 focus:ring-[#315FE8]/20 focus:border-[#315FE8]"
              data-testid="question-search-input"
            />
          </div>

          <div className="flex items-center justify-between sm:justify-start gap-2 flex-wrap">
            {/* Pool Filter Chips */}
            <div className="inline-flex rounded-xl bg-white p-1 border border-navy-200/80 flex-wrap shadow-2xs">
              <button
                type="button"
                onClick={() => {
                  setPoolType('all');
                  setPage(1);
                }}
                data-testid="pool-tab-all"
                className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                  poolType === 'all'
                    ? 'bg-[#14233E] text-white shadow-2xs'
                    : 'text-navy-600 hover:text-[#14233E]'
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
                data-testid="pool-tab-institution"
                className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                  poolType === 'institution'
                    ? 'bg-[#14233E] text-white shadow-2xs'
                    : 'text-navy-600 hover:text-[#14233E]'
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
                data-testid="pool-tab-global"
                className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                  poolType === 'global'
                    ? 'bg-[#14233E] text-white shadow-2xs'
                    : 'text-navy-600 hover:text-[#14233E]'
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
                data-testid="toggle-filters-btn"
                className="text-xs text-[#14233E] bg-white border-navy-200/80 hover:bg-neutral-50 rounded-xl"
              >
                <Filter className="h-3.5 w-3.5 mr-1 text-[#315FE8]" />
                <span>Detaylı Filtreler</span>
                {showAdvancedFilters ? (
                  <ChevronUp className="h-3.5 w-3.5 ml-1 text-navy-400" />
                ) : (
                  <ChevronDown className="h-3.5 w-3.5 ml-1 text-navy-400" />
                )}
              </Button>

              {hasActiveFilters && (
                <Button
                  variant="ghost"
                  size="sm"
                  onClick={handleResetFilters}
                  data-testid="reset-filters-btn"
                  className="text-xs text-navy-500 hover:text-[#14233E]"
                >
                  <RotateCcw className="h-3.5 w-3.5 mr-1" /> Temizle
                </Button>
              )}
            </div>
          </div>
        </div>

        {/* Collapsible Advanced Filters */}
        {showAdvancedFilters && (
          <div
            className="pt-3 border-t border-navy-200/60 grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-3 animate-fadeIn"
            data-testid="advanced-filters-panel"
          >
            {/* Subject Selector */}
            <div>
              <label className="text-[11px] font-bold text-navy-700 block mb-1">Ders / Branş</label>
              <Select
                value={selectedSubjectId}
                onChange={(e) => {
                  setSelectedSubjectId(e.target.value);
                  setPage(1);
                }}
                className="text-xs bg-white border-navy-200/80 rounded-xl text-[#14233E]"
                data-testid="filter-subject-select"
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
              <label className="text-[11px] font-bold text-navy-700 block mb-1">Konu</label>
              <Select
                value={selectedTopicId}
                onChange={(e) => {
                  setSelectedTopicId(e.target.value);
                  setPage(1);
                }}
                disabled={!selectedSubjectId}
                className="text-xs bg-white border-navy-200/80 rounded-xl text-[#14233E]"
                data-testid="filter-topic-select"
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
              <label className="text-[11px] font-bold text-navy-700 block mb-1">Zorluk Seviyesi</label>
              <Select
                value={difficulty}
                onChange={(e) => {
                  setDifficulty(e.target.value);
                  setPage(1);
                }}
                className="text-xs bg-white border-navy-200/80 rounded-xl text-[#14233E]"
                data-testid="filter-difficulty-select"
              >
                <option value="all">Tüm Zorluklar</option>
                <option value={DifficultyLevel.Easy}>Kolay</option>
                <option value={DifficultyLevel.Medium}>Orta</option>
                <option value={DifficultyLevel.Hard}>Zor</option>
              </Select>
            </div>

            {/* Status Selector */}
            <div>
              <label className="text-[11px] font-bold text-navy-700 block mb-1">Yayın Durumu</label>
              <Select
                value={status}
                onChange={(e) => {
                  setStatus(e.target.value);
                  setPage(1);
                }}
                className="text-xs bg-white border-navy-200/80 rounded-xl text-[#14233E]"
                data-testid="filter-status-select"
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
          icon={<BookOpen className="h-8 w-8 text-navy-400" />}
          title={hasActiveFilters ? 'Filtrelere uygun soru bulunamadı' : 'Henüz soru bulunmuyor'}
          description={
            hasActiveFilters
              ? 'Arama terimlerinizi veya filtrelerinizi değiştirerek tekrar deneyiniz.'
              : 'Yeni soru ekle butonunu kullanarak kurum havuzuna ilk sorunuzu kaydedebilirsiniz.'
          }
          action={
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setEditingQuestion(null);
                setIsFormOpen(true);
              }}
              className="bg-white border-navy-200/80 text-[#14233E] hover:bg-neutral-50 rounded-xl"
            >
              <Plus className="h-4 w-4 mr-1 text-[#315FE8]" /> Soru Ekle
            </Button>
          }
        />
      ) : (
        <div className="space-y-4">
          {/* Results Summary with honest distinction */}
          <div className="flex flex-col sm:flex-row sm:items-center justify-between text-xs text-navy-600 px-1 gap-1">
            <span data-testid="pagination-count-label">
              Toplam <strong className="text-[#14233E] font-bold">{totalCount}</strong> sorudan{' '}
              <strong className="text-[#14233E] font-semibold">{startItemIndex}–{endItemIndex}</strong> arası listeleniyor
              {' '}(bu sayfada {currentCountOnPage} soru).
            </span>
            <span className="font-medium text-navy-500">
              Sayfa {page} / {totalPages}
            </span>
          </div>

          {/* Question List Cards - Responsive below 1024px & Clean compact layout at 1024px+ */}
          <div className="space-y-3.5" data-testid="question-list-container">
            {questions.map((q) => {
              const canEdit = canEditQuestion(q);
              const reason = getCannotEditReason(q);

              return (
                <div
                  key={q.id}
                  data-testid={`question-card-${q.id}`}
                  className="bg-white rounded-2xl border border-navy-200/80 p-4 sm:p-5 shadow-soft-xs hover:border-[#315FE8]/40 transition-colors flex flex-col lg:flex-row lg:items-center justify-between gap-4"
                >
                  <div className="space-y-2 flex-1 min-w-0">
                    {/* Metadata Badges Header */}
                    <div className="flex flex-wrap items-center gap-2 text-xs">
                      {q.institutionId === null ? (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-sky-50 text-sky-700 border border-sky-200">
                          <Globe className="h-3 w-3" /> Genel Havuz
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-[11px] font-semibold bg-blue-50 text-[#315FE8] border border-blue-200">
                          <Building2 className="h-3 w-3" /> Kurumsal
                        </span>
                      )}

                      {getStatusBadge(q.status)}
                      {getDifficultyBadge(q.difficulty)}

                      <span className="text-navy-600 font-medium">
                        {subjectNames[q.subjectId] || 'Ders'} • {topicNames[q.topicId] || 'Konu'}
                      </span>
                    </div>

                    {/* Question Content Preview */}
                    <p className="text-sm font-semibold text-[#14233E] line-clamp-2 leading-relaxed">
                      {q.content}
                    </p>

                    {/* Question Style Tags */}
                    {q.styles && q.styles.length > 0 && (
                      <div className="flex flex-wrap items-center gap-1.5 pt-0.5">
                        {q.styles.map((s) => (
                          <span
                            key={s.id}
                            className="inline-flex items-center px-2 py-0.5 rounded-md text-[11px] font-medium bg-[#F8F7F3] text-navy-800 border border-navy-200/80"
                          >
                            {s.name}
                          </span>
                        ))}
                      </div>
                    )}

                    {/* Sub-info metadata */}
                    <div className="flex flex-wrap items-center gap-4 text-[11px] text-navy-400 font-mono">
                      <span>{q.optionCount} Şık</span>
                      <span>Kayıt: {new Date(q.createdAt).toLocaleDateString('tr-TR')}</span>
                      {!canEdit && reason && (
                        <span className="flex items-center gap-1 text-amber-800 bg-amber-50 px-2 py-0.5 rounded-md text-[11px] font-sans border border-amber-200/80">
                          <Lock className="h-3 w-3 text-amber-600" /> {reason}
                        </span>
                      )}
                    </div>
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-2 shrink-0 border-t lg:border-t-0 pt-3 lg:pt-0 border-navy-100 flex-wrap">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => handleOpenDetail(q.id)}
                      data-testid={`question-detail-btn-${q.id}`}
                      className="text-navy-700 hover:text-[#14233E] hover:bg-navy-50 text-xs rounded-xl"
                    >
                      <Eye className="h-3.5 w-3.5 mr-1 text-[#315FE8]" /> İncele
                    </Button>

                    <Button
                      variant="outline"
                      size="sm"
                      disabled={!canEdit}
                      onClick={() => handleOpenEdit(q.id)}
                      data-testid={`question-edit-btn-${q.id}`}
                      className="text-xs bg-white border-navy-200/80 hover:bg-neutral-50 text-[#14233E] rounded-xl"
                    >
                      <Edit2 className="h-3.5 w-3.5 mr-1 text-navy-600" /> Düzenle
                    </Button>

                    {q.status !== QuestionStatus.Archived && (
                      <Button
                        variant="ghost"
                        size="sm"
                        disabled={q.institutionId === null && !isSuperAdmin}
                        onClick={() => setArchivingQuestion(q)}
                        data-testid={`question-archive-btn-${q.id}`}
                        className="text-amber-700 hover:bg-amber-50 hover:text-amber-800 text-xs rounded-xl"
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
            <div
              className="bg-[#F8F7F3] rounded-2xl border border-navy-200/80 p-3.5 sm:p-4 shadow-soft-xs flex items-center justify-between"
              data-testid="question-pagination-controls"
            >
              <Button
                variant="outline"
                size="sm"
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                data-testid="pagination-prev-btn"
                className="text-xs bg-white border-navy-200/80 text-[#14233E] hover:bg-neutral-50 rounded-xl"
              >
                <ChevronLeft className="h-4 w-4 mr-1" /> Önceki
              </Button>

              <span className="text-xs font-semibold text-[#14233E]">
                Sayfa {page} / {totalPages}
              </span>

              <Button
                variant="outline"
                size="sm"
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                data-testid="pagination-next-btn"
                className="text-xs bg-white border-navy-200/80 text-[#14233E] hover:bg-neutral-50 rounded-xl"
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
              <Button
                variant="secondary"
                size="sm"
                onClick={() => setIsAssemblyModalOpen(false)}
                className="rounded-xl"
              >
                Kapat
              </Button>
            </div>
          }
        >
          <div className="space-y-3 py-2">
            {availableExams.length === 0 ? (
              <p className="text-xs text-navy-500 text-center py-4">
                Montaj yapılabilecek kayıtlı sınav bulunmuyor.
              </p>
            ) : (
              <div className="space-y-2 max-h-72 overflow-y-auto pr-1">
                {availableExams.map((exam) => (
                  <div
                    key={exam.id}
                    className="p-3 bg-[#F8F7F3] rounded-xl border border-navy-200/80 flex items-center justify-between gap-3 hover:border-[#315FE8]/40 transition-colors"
                  >
                    <div>
                      <h4 className="text-xs font-bold text-[#14233E]">{exam.title}</h4>
                      <p className="text-[11px] text-navy-500 font-mono">
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
                      className="text-xs shrink-0 bg-[#315FE8] hover:bg-blue-700 text-white rounded-xl"
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
              className="rounded-xl border-navy-200"
            >
              Vazgeç
            </Button>
            <Button
              variant="primary"
              size="sm"
              className="bg-amber-600 hover:bg-amber-700 text-white rounded-xl"
              onClick={handleConfirmArchive}
              isLoading={isArchiving}
            >
              Evet, Arşivle
            </Button>
          </div>
        }
      >
        <div className="space-y-3 py-2 text-xs text-navy-700">
          <p className="font-medium text-[#14233E]">
            Soru arşivlenecektir. Geçmiş sınav ve sonuç kayıtları etkilenmeyecektir.
          </p>
          {archivingQuestion && (
            <div className="p-3 bg-[#F8F7F3] rounded-xl text-xs text-navy-600 border border-navy-200/80 line-clamp-3">
              "{archivingQuestion.content}"
            </div>
          )}
        </div>
      </Modal>
    </div>
  );
};
