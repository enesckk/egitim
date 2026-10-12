import React, { useState, useEffect } from 'react';
import { Modal, Button, FormField, Input, Select, Alert } from '@/components/ui';
import {
  QuestionDetailDto,
  CreateQuestionPayload,
  UpdateQuestionPayload,
  DifficultyLevel,
  QuestionSourceType,
  QuestionStatus,
  QuestionType,
  TaxonomyDto,
} from '@/types/question';
import { academicService } from '@/services/academicService';
import { HelpCircle, CheckCircle2 } from 'lucide-react';

interface QuestionFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (payload: CreateQuestionPayload | UpdateQuestionPayload) => Promise<void>;
  initialData?: QuestionDetailDto | null;
  isSubmitting?: boolean;
}

const OPTION_LABELS = ['A', 'B', 'C', 'D', 'E'];

export const QuestionFormModal: React.FC<QuestionFormModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  initialData,
  isSubmitting = false,
}) => {
  const isEditing = Boolean(initialData);

  // Taxonomy states
  const [subjects, setSubjects] = useState<TaxonomyDto[]>([]);
  const [topics, setTopics] = useState<TaxonomyDto[]>([]);
  const [subTopics, setSubTopics] = useState<TaxonomyDto[]>([]);
  const [learningOutcomes, setLearningOutcomes] = useState<TaxonomyDto[]>([]);

  // Form states
  const [subjectId, setSubjectId] = useState<string>('');
  const [topicId, setTopicId] = useState<string>('');
  const [subTopicId, setSubTopicId] = useState<string>('');
  const [learningOutcomeId, setLearningOutcomeId] = useState<string>('');

  const [difficulty, setDifficulty] = useState<DifficultyLevel>(DifficultyLevel.Medium);
  const [sourceType, setSourceType] = useState<QuestionSourceType>(QuestionSourceType.InstitutionCustom);
  const [sourceYear, setSourceYear] = useState<string>('');
  const [status, setStatus] = useState<QuestionStatus>(QuestionStatus.Draft);
  const [content, setContent] = useState<string>('');
  const [mediaUrl, setMediaUrl] = useState<string>('');

  // Fixed 5 options
  const [optionContents, setOptionContents] = useState<Record<string, string>>({
    A: '',
    B: '',
    C: '',
    D: '',
    E: '',
  });
  const [correctOption, setCorrectOption] = useState<string>('A');

  const [error, setError] = useState<string | null>(null);

  // Fetch subjects on mount
  useEffect(() => {
    if (!isOpen) return;
    academicService
      .getTaxonomy('subjects')
      .then(setSubjects)
      .catch(() => setError('Ders listesi yüklenemedi.'));
  }, [isOpen]);

  // Fetch topics when subjectId changes
  useEffect(() => {
    if (!subjectId) {
      setTopics([]);
      setTopicId('');
      setSubTopics([]);
      setSubTopicId('');
      setLearningOutcomes([]);
      setLearningOutcomeId('');
      return;
    }

    academicService
      .getTaxonomy('topics', subjectId)
      .then(setTopics)
      .catch(() => setError('Konu listesi yüklenemedi.'));
  }, [subjectId]);

  // Fetch subTopics when topicId changes
  useEffect(() => {
    if (!topicId) {
      setSubTopics([]);
      setSubTopicId('');
      setLearningOutcomes([]);
      setLearningOutcomeId('');
      return;
    }

    academicService
      .getTaxonomy('sub-topics', topicId)
      .then(setSubTopics)
      .catch(() => setSubTopics([]));
  }, [topicId]);

  // Fetch learningOutcomes when subTopicId changes
  useEffect(() => {
    if (!subTopicId) {
      setLearningOutcomes([]);
      setLearningOutcomeId('');
      return;
    }

    academicService
      .getTaxonomy('learning-outcomes', subTopicId)
      .then(setLearningOutcomes)
      .catch(() => setLearningOutcomes([]));
  }, [subTopicId]);

  // Populate form for Edit
  useEffect(() => {
    if (!isOpen) return;

    if (initialData) {
      setSubjectId(initialData.subjectId || '');
      setTopicId(initialData.topicId || '');
      setSubTopicId(initialData.subTopicId || '');
      setLearningOutcomeId(initialData.learningOutcomeId || '');

      setDifficulty(initialData.difficulty);
      setSourceType(initialData.sourceType);
      setSourceYear(initialData.sourceYear ? String(initialData.sourceYear) : '');
      setStatus(initialData.status);
      setContent(initialData.content || '');
      setMediaUrl(initialData.mediaUrl || '');

      const initialOpts: Record<string, string> = { A: '', B: '', C: '', D: '', E: '' };
      let correct = 'A';

      initialData.options.forEach((opt) => {
        initialOpts[opt.optionLabel] = opt.content;
        if (opt.isCorrect) correct = opt.optionLabel;
      });

      setOptionContents(initialOpts);
      setCorrectOption(correct);
    } else {
      // Reset form
      setSubjectId('');
      setTopicId('');
      setSubTopicId('');
      setLearningOutcomeId('');
      setDifficulty(DifficultyLevel.Medium);
      setSourceType(QuestionSourceType.InstitutionCustom);
      setSourceYear('');
      setStatus(QuestionStatus.Draft);
      setContent('');
      setMediaUrl('');
      setOptionContents({ A: '', B: '', C: '', D: '', E: '' });
      setCorrectOption('A');
    }
    setError(null);
  }, [isOpen, initialData]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    // Validations
    if (!subjectId) {
      setError('Lütfen bir ders seçiniz.');
      return;
    }
    if (!topicId) {
      setError('Lütfen bir konu seçiniz.');
      return;
    }
    if (!content.trim()) {
      setError('Lütfen soru metnini giriniz.');
      return;
    }

    // Validate options
    for (const label of OPTION_LABELS) {
      if (!optionContents[label] || !optionContents[label].trim()) {
        setError(`Lütfen ${label} seçeneği içeriğini doldurunuz.`);
        return;
      }
    }

    const formattedOptions = OPTION_LABELS.map((label) => ({
      optionLabel: label,
      content: optionContents[label].trim(),
      isCorrect: label === correctOption,
    }));

    const parsedYear = sourceYear.trim() ? parseInt(sourceYear.trim(), 10) : null;

    const payload: CreateQuestionPayload = {
      subjectId,
      topicId,
      subTopicId: subTopicId || null,
      learningOutcomeId: learningOutcomeId || null,
      questionType: QuestionType.MultipleChoice,
      difficulty,
      sourceType,
      sourceYear: parsedYear,
      content: content.trim(),
      mediaUrl: mediaUrl.trim() || null,
      status,
      options: formattedOptions,
    };

    try {
      await onSubmit(payload);
      onClose();
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message);
      } else {
        setError('Soru kaydedilirken bir hata oluştu.');
      }
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={isEditing ? 'Soruyu Düzenle' : 'Yeni Soru Oluştur'}
      subtitle="Soru bankasına eklemek için gerekli bilgileri doldurunuz."
      maxWidth="xl"
      footer={
        <div className="flex items-center justify-end gap-2">
          <Button variant="outline" size="sm" onClick={onClose} disabled={isSubmitting}>
            İptal
          </Button>
          <Button variant="primary" size="sm" onClick={handleSubmit} isLoading={isSubmitting}>
            {isEditing ? 'Değişiklikleri Kaydet' : 'Soruyu Kaydet'}
          </Button>
        </div>
      }
    >
      <form onSubmit={handleSubmit} className="space-y-5">
        {error && <Alert variant="danger">{error}</Alert>}

        {/* 1. Academic Taxonomy Section */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 p-4 rounded-xl bg-neutral-50 border border-neutral-200">
          <FormField label="Ders *" required>
            <Select
              value={subjectId}
              onChange={(e) => setSubjectId(e.target.value)}
              disabled={isEditing}
            >
              <option value="">Ders Seçiniz</option>
              {subjects.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.name} ({s.code})
                </option>
              ))}
            </Select>
          </FormField>

          <FormField label="Konu *" required>
            <Select
              value={topicId}
              onChange={(e) => setTopicId(e.target.value)}
              disabled={!subjectId || isEditing}
            >
              <option value="">Konu Seçiniz</option>
              {topics.map((t) => (
                <option key={t.id} value={t.id}>
                  {t.name} ({t.code})
                </option>
              ))}
            </Select>
          </FormField>

          <FormField label="Alt Konu (Opsiyonel)">
            <Select
              value={subTopicId}
              onChange={(e) => setSubTopicId(e.target.value)}
              disabled={!topicId}
            >
              <option value="">Alt Konu Seçiniz</option>
              {subTopics.map((st) => (
                <option key={st.id} value={st.id}>
                  {st.name} ({st.code})
                </option>
              ))}
            </Select>
          </FormField>

          <FormField label="Kazanım (Opsiyonel)">
            <Select
              value={learningOutcomeId}
              onChange={(e) => setLearningOutcomeId(e.target.value)}
              disabled={!subTopicId && learningOutcomes.length === 0}
            >
              <option value="">Kazanım Seçiniz</option>
              {learningOutcomes.map((lo) => (
                <option key={lo.id} value={lo.id}>
                  {lo.name} ({lo.code})
                </option>
              ))}
            </Select>
          </FormField>
        </div>

        {/* 2. Metadata Section */}
        <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
          <FormField label="Zorluk Seviyesi *">
            <Select
              value={difficulty}
              onChange={(e) => setDifficulty(Number(e.target.value) as DifficultyLevel)}
            >
              <option value={DifficultyLevel.Easy}>Kolay</option>
              <option value={DifficultyLevel.Medium}>Orta</option>
              <option value={DifficultyLevel.Hard}>Zor</option>
            </Select>
          </FormField>

          <FormField label="Kaynak Türü *">
            <Select
              value={sourceType}
              onChange={(e) => setSourceType(Number(e.target.value) as QuestionSourceType)}
            >
              <option value={QuestionSourceType.InstitutionCustom}>Kurum İçi Özgün</option>
              <option value={QuestionSourceType.OsymPast}>ÖSYM Çıkmış Soru</option>
              <option value={QuestionSourceType.Publisher}>Yayıncı Soru</option>
            </Select>
          </FormField>

          <FormField label="Kaynak Yılı">
            <Input
              type="number"
              placeholder="Örn: 2024"
              value={sourceYear}
              onChange={(e) => setSourceYear(e.target.value)}
            />
          </FormField>

          <FormField label="Yayın Durumu *">
            <Select
              value={status}
              onChange={(e) => setStatus(Number(e.target.value) as QuestionStatus)}
            >
              <option value={QuestionStatus.Draft}>Taslak (Düzenlenebilir)</option>
              <option value={QuestionStatus.Published}>Yayımlandı (Kilitli)</option>
            </Select>
          </FormField>
        </div>

        {/* 3. Question Content */}
        <FormField label="Soru Metni (Markdown / LaTeX Destekli) *" required>
          <textarea
            rows={4}
            value={content}
            onChange={(e) => setContent(e.target.value)}
            placeholder="Soru metnini buraya yazınız... Formül ve semboller için standart LaTeX yazımı kullanılabilir."
            className="w-full px-3 py-2 text-sm border border-neutral-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-primary-500 font-sans"
          />
        </FormField>

        <FormField label="Görsel Bağlantısı (Media URL - Opsiyonel)">
          <Input
            type="url"
            placeholder="https://..."
            value={mediaUrl}
            onChange={(e) => setMediaUrl(e.target.value)}
          />
        </FormField>

        {/* 4. Options A-E */}
        <div className="space-y-3 pt-2">
          <div className="flex items-center justify-between">
            <h4 className="text-xs font-semibold text-neutral-600 uppercase tracking-wider flex items-center gap-1.5">
              <HelpCircle className="h-4 w-4 text-neutral-400" /> Şıklar (A–E) ve Doğru Cevap Seçimi
            </h4>
            <span className="text-xs text-neutral-500">
              Doğru şıkkı işaretlemek için radyo düğmesini seçiniz.
            </span>
          </div>

          <div className="space-y-2.5">
            {OPTION_LABELS.map((label) => (
              <div
                key={label}
                className={`flex items-center gap-3 p-2.5 rounded-lg border transition-colors ${
                  correctOption === label
                    ? 'bg-emerald-50/70 border-emerald-300'
                    : 'bg-white border-neutral-200'
                }`}
              >
                <label className="flex items-center gap-2 cursor-pointer shrink-0">
                  <input
                    type="radio"
                    name="correctOption"
                    value={label}
                    checked={correctOption === label}
                    onChange={() => setCorrectOption(label)}
                    className="h-4 w-4 text-emerald-600 focus:ring-emerald-500 border-neutral-300"
                  />
                  <span
                    className={`inline-flex items-center justify-center h-6 w-6 rounded font-bold text-xs ${
                      correctOption === label
                        ? 'bg-emerald-600 text-white'
                        : 'bg-neutral-100 text-neutral-700'
                    }`}
                  >
                    {label}
                  </span>
                </label>

                <input
                  type="text"
                  placeholder={`${label} seçeneği içeriği...`}
                  value={optionContents[label]}
                  onChange={(e) =>
                    setOptionContents((prev) => ({ ...prev, [label]: e.target.value }))
                  }
                  className="flex-1 px-3 py-1.5 text-sm border border-neutral-300 rounded-md focus:outline-none focus:ring-1 focus:ring-primary-500"
                />

                {correctOption === label && (
                  <span className="flex items-center gap-1 text-xs font-semibold text-emerald-700 shrink-0 px-2 py-0.5 bg-emerald-100 rounded-full">
                    <CheckCircle2 className="h-3.5 w-3.5" /> Doğru
                  </span>
                )}
              </div>
            ))}
          </div>
        </div>
      </form>
    </Modal>
  );
};
