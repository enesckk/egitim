import React from 'react';
import { Modal, Button, Badge } from '@/components/ui';
import { QuestionDetailDto, QuestionStatus, DifficultyLevel, QuestionSourceType } from '@/types/question';
import { CheckCircle2, Lock, Globe, Building2, HelpCircle } from 'lucide-react';

interface QuestionDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
  question: QuestionDetailDto | null;
  subjectName?: string;
  topicName?: string;
  onEdit?: (question: QuestionDetailDto) => void;
  onArchive?: (question: QuestionDetailDto) => void;
  canEdit: boolean;
  canArchive: boolean;
  cannotEditReason?: string;
}

export const QuestionDetailModal: React.FC<QuestionDetailModalProps> = ({
  isOpen,
  onClose,
  question,
  subjectName,
  topicName,
  onEdit,
  onArchive,
  canEdit,
  canArchive,
  cannotEditReason,
}) => {
  if (!question) return null;

  const getDifficultyBadge = (difficulty: DifficultyLevel) => {
    switch (difficulty) {
      case DifficultyLevel.Easy:
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
            Kolay
          </span>
        );
      case DifficultyLevel.Medium:
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200">
            Orta
          </span>
        );
      case DifficultyLevel.Hard:
        return (
          <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-semibold bg-rose-50 text-rose-700 border border-rose-200">
            Zor
          </span>
        );
      default:
        return <Badge variant="secondary">Bilinmiyor</Badge>;
    }
  };

  const getStatusBadge = (status: QuestionStatus) => {
    switch (status) {
      case QuestionStatus.Draft:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-amber-50 text-amber-700 border border-amber-200">
            Taslak
          </span>
        );
      case QuestionStatus.Published:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-50 text-emerald-700 border border-emerald-200">
            Yayımlandı
          </span>
        );
      case QuestionStatus.Archived:
        return (
          <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-slate-100 text-slate-700 border border-slate-200">
            Arşivlendi
          </span>
        );
      default:
        return null;
    }
  };

  const getSourceLabel = (sourceType: QuestionSourceType, year: number | null) => {
    switch (sourceType) {
      case QuestionSourceType.OsymPast:
        return `ÖSYM Çıkmış ${year ? `(${year})` : ''}`;
      case QuestionSourceType.Publisher:
        return 'Yayıncı';
      case QuestionSourceType.InstitutionCustom:
      default:
        return 'Kurum İçi Özgün';
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title="Soru Detayı"
      subtitle={`Soru ID: ${question.id.substring(0, 8)}...`}
      headerVariant="dark"
      maxWidth="lg"
      footer={
        <div className="flex items-center justify-between w-full">
          <div className="flex items-center gap-2">
            {canArchive && onArchive && (
              <Button
                variant="outline"
                size="sm"
                className="text-amber-700 border-amber-300 hover:bg-amber-50 rounded-xl"
                onClick={() => onArchive(question)}
              >
                Soru Arşivle
              </Button>
            )}
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={onClose} className="rounded-xl border-navy-200">
              Kapat
            </Button>
            {onEdit && (
              <Button
                variant="primary"
                size="sm"
                disabled={!canEdit}
                className="bg-[#315FE8] hover:bg-blue-700 text-white rounded-xl"
                onClick={() => onEdit(question)}
              >
                Düzenle
              </Button>
            )}
          </div>
        </div>
      }
    >
      <div className="space-y-6">
        {/* Immutability Banner */}
        {!canEdit && cannotEditReason && (
          <div className="flex items-center gap-2.5 p-3 rounded-xl bg-amber-50 border border-amber-200 text-amber-850 text-xs">
            <Lock className="h-4 w-4 shrink-0 text-amber-600" />
            <span className="font-medium text-amber-900">{cannotEditReason}</span>
          </div>
        )}

        {/* Metadata Badges & Tags */}
        <div className="flex flex-wrap items-center gap-2">
          {question.institutionId === null ? (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-sky-50 text-sky-700 border border-sky-200">
              <Globe className="h-3 w-3" /> Global Soru
            </span>
          ) : (
            <span className="inline-flex items-center gap-1 px-2.5 py-0.5 rounded-full text-xs font-semibold bg-blue-50 text-[#315FE8] border border-blue-200">
              <Building2 className="h-3 w-3" /> Kurum Sorusu
            </span>
          )}

          {getStatusBadge(question.status)}
          {getDifficultyBadge(question.difficulty)}

          <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-[#F8F7F3] text-navy-800 border border-navy-200/80">
            {getSourceLabel(question.sourceType, question.sourceYear)}
          </span>

          {subjectName && (
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-[#F8F7F3] text-navy-800 border border-navy-200/80">
              Ders: {subjectName}
            </span>
          )}
          {topicName && (
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-[#F8F7F3] text-navy-800 border border-navy-200/80">
              Konu: {topicName}
            </span>
          )}
        </div>

        {/* Question Styles */}
        {question.styles && question.styles.length > 0 && (
          <div className="flex flex-wrap items-center gap-1.5 p-3 rounded-xl bg-[#F8F7F3] border border-navy-200/80">
            <span className="text-xs font-bold text-[#14233E] mr-1">Soru Tarzları:</span>
            {question.styles.map((s) => (
              <span
                key={s.id}
                title={s.description || s.name}
                className="inline-flex items-center px-2.5 py-0.5 rounded-md text-xs font-semibold bg-white text-navy-800 border border-navy-200/80 shadow-2xs"
              >
                {s.name}
              </span>
            ))}
          </div>
        )}

        {/* Question Content */}
        <div className="p-4 sm:p-5 rounded-2xl bg-[#F8F7F3] border border-navy-200/80">
          <h4 className="text-[11px] font-bold text-navy-500 uppercase tracking-wider mb-2">
            Soru Metni
          </h4>
          <div className="text-sm font-semibold text-[#14233E] leading-relaxed whitespace-pre-wrap">
            {question.content}
          </div>
          {question.mediaUrl && (
            <div className="mt-3">
              <img
                src={question.mediaUrl}
                alt="Soru görseli"
                className="max-h-60 rounded-xl border border-navy-200/80 object-contain bg-white p-2"
              />
            </div>
          )}
        </div>

        {/* Options */}
        <div className="space-y-3">
          <h4 className="text-[11px] font-bold text-navy-500 uppercase tracking-wider flex items-center gap-1.5">
            <HelpCircle className="h-3.5 w-3.5 text-[#315FE8]" /> Seçenekler (A–E)
          </h4>

          <div className="space-y-2">
            {question.options
              .slice()
              .sort((a, b) => a.optionLabel.localeCompare(b.optionLabel))
              .map((option) => (
                <div
                  key={option.id}
                  className={`flex items-start gap-3 p-3.5 rounded-xl border text-sm transition-colors ${
                    option.isCorrect
                      ? 'bg-emerald-50/80 border-emerald-300 text-emerald-950 font-medium'
                      : 'bg-white border-navy-200/80 text-navy-900'
                  }`}
                >
                  <span
                    className={`inline-flex items-center justify-center h-6 w-6 rounded-lg font-bold text-xs shrink-0 shadow-2xs ${
                      option.isCorrect
                        ? 'bg-emerald-600 text-white'
                        : 'bg-[#F8F7F3] text-navy-700 border border-navy-200/80'
                    }`}
                  >
                    {option.optionLabel}
                  </span>
                  <div className="flex-1 pt-0.5 whitespace-pre-wrap text-xs sm:text-sm font-medium">{option.content}</div>
                  {option.isCorrect && (
                    <span className="flex items-center gap-1 text-xs font-bold text-emerald-700 bg-emerald-100/80 px-2.5 py-0.5 rounded-full shrink-0">
                      <CheckCircle2 className="h-3.5 w-3.5 text-emerald-600" /> Doğru Cevap
                    </span>
                  )}
                </div>
              ))}
          </div>
        </div>
      </div>
    </Modal>
  );
};
