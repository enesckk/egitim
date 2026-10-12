import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Card,
  Button,
  Input,
  Select,
  Badge,
  Modal,
  Alert,
  LoadingState,
  FormField,
} from '@/components/ui';
import {
  ExamDetailDto,
  ExamSectionDetailDto,
} from '@/types/exam';
import {
  QuestionDto,
  ExamQuestionTeacherDto,
  QuestionStatus,
  DifficultyLevel,
} from '@/types/question';
import { examService } from '@/services/examService';
import { questionService } from '@/services/questionService';
import {
  ArrowLeft,
  Plus,
  Search,
  BookOpen,
  CheckCircle2,
  Globe,
  Building2,
  FileSpreadsheet,
} from 'lucide-react';

export const ExamQuestionAssemblyView: React.FC = () => {
  const { examId } = useParams<{ examId: string }>();
  const navigate = useNavigate();

  const [exam, setExam] = useState<ExamDetailDto | null>(null);
  const [examQuestions, setExamQuestions] = useState<ExamQuestionTeacherDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Available Published Question Bank Search for Assembly
  const [bankQuestions, setBankQuestions] = useState<QuestionDto[]>([]);
  const [bankSearch, setBankSearch] = useState<string>('');
  const [bankSubjectId, setBankSubjectId] = useState<string>('');
  const [isBankLoading, setIsBankLoading] = useState<boolean>(false);

  // Add Question Modal State
  const [selectedQuestion, setSelectedQuestion] = useState<QuestionDto | null>(null);
  const [selectedSectionId, setSelectedSectionId] = useState<string>('');
  const [questionNumber, setQuestionNumber] = useState<number>(1);
  const [orderNumber, setOrderNumber] = useState<number>(1);
  const [isAdding, setIsAdding] = useState<boolean>(false);
  const [addError, setAddError] = useState<string | null>(null);

  // Load Exam and Linked Questions
  const loadExamData = useCallback(async () => {
    if (!examId) return;
    setIsLoading(true);
    setError(null);
    try {
      const [examData, questionsData] = await Promise.all([
        examService.getExamDetail(examId),
        questionService.getExamQuestions(examId),
      ]);
      setExam(examData);
      setExamQuestions(questionsData);
      if (examData.sections.length > 0) {
        setSelectedSectionId(examData.sections[0].id);
      }
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Sınav bilgileri yüklenemedi.');
    } finally {
      setIsLoading(false);
    }
  }, [examId]);

  useEffect(() => {
    loadExamData();
  }, [loadExamData]);

  // Search Question Bank (only Published questions allowed)
  const searchQuestionBank = useCallback(async () => {
    setIsBankLoading(true);
    try {
      const res = await questionService.getQuestions({
        status: QuestionStatus.Published, // Only Published questions
        subjectId: bankSubjectId || undefined,
        searchText: bankSearch.trim() || undefined,
        pageSize: 10,
      });
      setBankQuestions(res.items);
    } catch {
      // Ignore bank search errors silently
    } finally {
      setIsBankLoading(false);
    }
  }, [bankSearch, bankSubjectId]);

  useEffect(() => {
    const handler = setTimeout(() => {
      searchQuestionBank();
    }, 300);
    return () => clearTimeout(handler);
  }, [searchQuestionBank]);

  // Handle Add Question to Exam
  const handleOpenAddModal = (q: QuestionDto) => {
    setSelectedQuestion(q);
    setAddError(null);

    // Auto calculate next QuestionNumber & Order in currently selected section
    const sectionQuestions = examQuestions.filter((eq) => eq.examSectionId === selectedSectionId);
    const maxNum = sectionQuestions.reduce((max, x) => Math.max(max, x.questionNumber), 0);
    const maxOrd = sectionQuestions.reduce((max, x) => Math.max(max, x.order), 0);

    setQuestionNumber(maxNum + 1);
    setOrderNumber(maxOrd + 1);
  };

  const handleConfirmAddQuestion = async () => {
    if (!examId || !selectedQuestion || !selectedSectionId) return;
    setIsAdding(true);
    setAddError(null);

    try {
      await questionService.addExamQuestion(examId, {
        examSectionId: selectedSectionId,
        questionId: selectedQuestion.id,
        questionNumber: Number(questionNumber),
        order: Number(orderNumber),
      });

      setSelectedQuestion(null);
      await loadExamData();
    } catch (err: unknown) {
      if (err instanceof Error) {
        setAddError(err.message);
      } else {
        setAddError('Soru sınava eklenirken bir hata oluştu.');
      }
    } finally {
      setIsAdding(false);
    }
  };

  if (isLoading) {
    return <LoadingState message="Sınav ve soru eşleşmeleri yükleniyor..." />;
  }

  if (error || !exam) {
    return (
      <div className="space-y-4">
        <Button variant="ghost" size="sm" onClick={() => navigate(-1)}>
          <ArrowLeft className="h-4 w-4 mr-1" /> Geri Dön
        </Button>
        <Alert variant="danger">{error || 'Sınav bulunamadı.'}</Alert>
      </div>
    );
  }

  return (
    <div className="space-y-6 pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-neutral-200 pb-4">
        <div className="flex items-center gap-3">
          <Button variant="outline" size="sm" onClick={() => navigate(-1)}>
            <ArrowLeft className="h-4 w-4" />
          </Button>
          <div>
            <div className="flex items-center gap-2">
              <h1 className="text-xl font-bold text-neutral-900">{exam.title}</h1>
              <Badge variant="secondary">{exam.code}</Badge>
              <Badge variant="info">{exam.examTypeName}</Badge>
            </div>
            <p className="text-xs text-neutral-500 mt-0.5">
              Sınav Soru Eşleştirme ve Sıralama Yönetimi
            </p>
          </div>
        </div>
      </div>

      {/* Grid Assembly Workspace */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Side: Question Bank Picker */}
        <div className="lg:col-span-5 space-y-4">
          <Card className="p-4 space-y-3">
            <h3 className="text-sm font-bold text-neutral-900 flex items-center gap-1.5">
              <BookOpen className="h-4 w-4 text-primary-600" /> Soru Bankası Seçici
            </h3>
            <p className="text-xs text-neutral-500">
              Yayımlanmış havuz sorularından sınava eklemek istediklerinizi seçiniz.
            </p>

            <div className="space-y-2 pt-1">
              <div className="relative">
                <Search className="absolute left-3 top-2.5 h-3.5 w-3.5 text-neutral-400" />
                <Input
                  placeholder="Soru arama..."
                  value={bankSearch}
                  onChange={(e) => setBankSearch(e.target.value)}
                  className="pl-8 text-xs"
                />
              </div>

              {exam.sections.length > 0 && (
                <Select
                  value={bankSubjectId}
                  onChange={(e) => setBankSubjectId(e.target.value)}
                  className="text-xs"
                >
                  <option value="">Tüm Dersler</option>
                  {exam.sections.map((s) => (
                    <option key={s.id} value={s.subjectId}>
                      {s.subjectName}
                    </option>
                  ))}
                </Select>
              )}
            </div>

            {/* Bank Questions List */}
            <div className="space-y-2 max-h-[500px] overflow-y-auto pr-1">
              {isBankLoading ? (
                <p className="text-xs text-neutral-400 py-4 text-center">Sorular aranıyor...</p>
              ) : bankQuestions.length === 0 ? (
                <p className="text-xs text-neutral-400 py-4 text-center">Uygun yayımlanmış soru bulunamadı.</p>
              ) : (
                bankQuestions.map((q) => {
                  const isAlreadyAdded = examQuestions.some((eq) => eq.questionId === q.id);

                  return (
                    <div
                      key={q.id}
                      className="p-3 rounded-lg border border-neutral-200 bg-white hover:border-primary-300 transition-colors space-y-2"
                    >
                      <div className="flex items-center justify-between text-[11px]">
                        {q.institutionId === null ? (
                          <span className="text-blue-600 font-semibold flex items-center gap-1">
                            <Globe className="h-3 w-3" /> Global
                          </span>
                        ) : (
                          <span className="text-neutral-600 font-semibold flex items-center gap-1">
                            <Building2 className="h-3 w-3" /> Kurumsal
                          </span>
                        )}
                        <span className="text-neutral-400">{q.optionCount} Şık</span>
                      </div>

                      <p className="text-xs text-neutral-800 font-medium line-clamp-2 leading-snug">
                        {q.content}
                      </p>

                      <div className="flex items-center justify-between pt-1">
                        <span className="text-[11px] text-neutral-400">
                          Zorluk: {q.difficulty === DifficultyLevel.Easy ? 'Kolay' : q.difficulty === DifficultyLevel.Hard ? 'Zor' : 'Orta'}
                        </span>

                        <Button
                          variant={isAlreadyAdded ? 'outline' : 'primary'}
                          size="sm"
                          disabled={isAlreadyAdded}
                          onClick={() => handleOpenAddModal(q)}
                          className="h-7 text-xs px-2.5"
                        >
                          {isAlreadyAdded ? (
                            <span className="flex items-center gap-1 text-emerald-700">
                              <CheckCircle2 className="h-3 w-3" /> Ekli
                            </span>
                          ) : (
                            <span className="flex items-center gap-1">
                              <Plus className="h-3 w-3" /> Sınava Ekle
                            </span>
                          )}
                        </Button>
                      </div>
                    </div>
                  );
                })
              )}
            </div>
          </Card>
        </div>

        {/* Right Side: Linked Exam Questions */}
        <div className="lg:col-span-7 space-y-4">
          <Card className="p-4 space-y-4">
            <div className="flex items-center justify-between border-b border-neutral-100 pb-3">
              <div>
                <h3 className="text-sm font-bold text-neutral-900 flex items-center gap-1.5">
                  <FileSpreadsheet className="h-4 w-4 text-emerald-600" /> Sınavdaki Sorular ({examQuestions.length})
                </h3>
                <p className="text-xs text-neutral-500 mt-0.5">
                  Bölümlere göre bağlı olan soruları inceleyin.
                </p>
              </div>

              {exam.sections.length > 0 && (
                <Select
                  value={selectedSectionId}
                  onChange={(e) => setSelectedSectionId(e.target.value)}
                  className="text-xs w-48"
                >
                  {exam.sections.map((s) => (
                    <option key={s.id} value={s.id}>
                      {s.subjectName} ({s.questionCount} Soru)
                    </option>
                  ))}
                </Select>
              )}
            </div>

            {/* Questions Table */}
            {examQuestions.length === 0 ? (
              <p className="text-xs text-neutral-400 py-8 text-center">
                Bu sınava henüz hiç soru eklenmemiş. Soldaki havuzdan soru ekleyebilirsiniz.
              </p>
            ) : (
              <div className="space-y-3">
                {exam.sections.map((section: ExamSectionDetailDto) => {
                  const sectionQuestions = examQuestions
                    .filter((eq) => eq.examSectionId === section.id)
                    .sort((a, b) => a.order - b.order || a.questionNumber - b.questionNumber);

                  return (
                    <div key={section.id} className="space-y-2">
                      <div className="flex items-center justify-between bg-neutral-100 px-3 py-1.5 rounded-md text-xs font-semibold text-neutral-700">
                        <span>{section.subjectName} Bölümü</span>
                        <span>{sectionQuestions.length} / {section.questionCount} Soru Eşleşti</span>
                      </div>

                      {sectionQuestions.length === 0 ? (
                        <p className="text-xs text-neutral-400 p-3 italic">Bu bölümde soru bulunmuyor.</p>
                      ) : (
                        <div className="space-y-2">
                          {sectionQuestions.map((eq) => (
                            <div
                              key={eq.id}
                              className="p-3 rounded-lg border border-neutral-200 bg-white flex items-start justify-between gap-3 text-xs"
                            >
                              <div className="flex items-center gap-2 shrink-0">
                                <span className="inline-flex items-center justify-center h-6 w-6 rounded bg-neutral-900 text-white font-bold text-xs">
                                  S{eq.questionNumber}
                                </span>
                                <span className="text-neutral-400 text-[11px]">Sıra: #{eq.order}</span>
                              </div>

                              <div className="flex-1 min-w-0 space-y-1">
                                <p className="font-medium text-neutral-900 line-clamp-2">
                                  {eq.content}
                                </p>
                                <div className="flex items-center gap-2 text-[11px] text-neutral-500">
                                  <span className="font-semibold text-emerald-700 bg-emerald-50 px-1.5 py-0.5 rounded border border-emerald-200">
                                    Doğru Şık: {eq.correctOption}
                                  </span>
                                  <span>{eq.options.length} Seçenek</span>
                                </div>
                              </div>
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  );
                })}
              </div>
            )}
          </Card>
        </div>
      </div>

      {/* Add Question Dialog */}
      <Modal
        isOpen={Boolean(selectedQuestion)}
        onClose={() => setSelectedQuestion(null)}
        title="Sınava Soru Ekle"
        subtitle="Soru numarasını ve sıralamasını belirleyiniz."
        footer={
          <div className="flex items-center justify-end gap-2">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setSelectedQuestion(null)}
              disabled={isAdding}
            >
              Vazgeç
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={handleConfirmAddQuestion}
              isLoading={isAdding}
            >
              Ekle
            </Button>
          </div>
        }
      >
        <div className="space-y-4">
          {addError && <Alert variant="danger">{addError}</Alert>}

          {selectedQuestion && (
            <div className="p-3 bg-neutral-50 rounded-lg border border-neutral-200 text-xs space-y-1">
              <span className="font-semibold text-neutral-700">Seçilen Soru Metni:</span>
              <p className="text-neutral-900 line-clamp-3">{selectedQuestion.content}</p>
            </div>
          )}

          <FormField label="Sınav Bölümü (Ders) *" required>
            <Select
              value={selectedSectionId}
              onChange={(e) => setSelectedSectionId(e.target.value)}
            >
              {exam.sections.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.subjectName}
                </option>
              ))}
            </Select>
          </FormField>

          <div className="grid grid-cols-2 gap-3">
            <FormField label="Soru Numarası *" required>
              <Input
                type="number"
                min={1}
                value={questionNumber}
                onChange={(e) => setQuestionNumber(Number(e.target.value))}
              />
            </FormField>

            <FormField label="Sıralama (Order) *" required>
              <Input
                type="number"
                min={1}
                value={orderNumber}
                onChange={(e) => setOrderNumber(Number(e.target.value))}
              />
            </FormField>
          </div>
        </div>
      </Modal>
    </div>
  );
};
