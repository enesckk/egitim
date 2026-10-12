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
        return <Badge variant="success">Kolay</Badge>;
      case DifficultyLevel.Medium:
        return <Badge variant="warning">Orta</Badge>;
      case DifficultyLevel.Hard:
        return <Badge variant="danger">Zor</Badge>;
      default:
        return <Badge variant="secondary">Bilinmiyor</Badge>;
    }
  };

  const getStatusBadge = (status: QuestionStatus) => {
    switch (status) {
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
      maxWidth="lg"
      footer={
        <div className="flex items-center justify-between w-full">
          <div className="flex items-center gap-2">
            {canArchive && onArchive && (
              <Button
                variant="outline"
                size="sm"
                className="text-amber-700 border-amber-300 hover:bg-amber-50"
                onClick={() => onArchive(question)}
              >
                Soru Arşivle
              </Button>
            )}
          </div>
          <div className="flex items-center gap-2">
            <Button variant="outline" size="sm" onClick={onClose}>
              Kapat
            </Button>
            {onEdit && (
              <Button
                variant="primary"
                size="sm"
                disabled={!canEdit}
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
          <div className="flex items-center gap-2.5 p-3 rounded-lg bg-amber-50 border border-amber-200 text-amber-800 text-xs">
            <Lock className="h-4 w-4 shrink-0 text-amber-600" />
            <span>{cannotEditReason}</span>
          </div>
        )}

        {/* Metadata Badges & Tags */}
        <div className="flex flex-wrap items-center gap-2">
          {question.institutionId === null ? (
            <Badge variant="info" className="flex items-center gap-1">
              <Globe className="h-3 w-3" /> Global Soru
            </Badge>
          ) : (
            <Badge variant="secondary" className="flex items-center gap-1">
              <Building2 className="h-3 w-3" /> Kurum Sorusu
            </Badge>
          )}

          {getStatusBadge(question.status)}
          {getDifficultyBadge(question.difficulty)}

          <Badge variant="secondary">
            {getSourceLabel(question.sourceType, question.sourceYear)}
          </Badge>

          {subjectName && <Badge variant="secondary">Ders: {subjectName}</Badge>}
          {topicName && <Badge variant="secondary">Konu: {topicName}</Badge>}
        </div>

        {/* Question Content */}
        <div className="p-4 rounded-xl bg-neutral-50 border border-neutral-200">
          <h4 className="text-xs font-semibold text-neutral-400 uppercase tracking-wider mb-2">
            Soru Metni
          </h4>
          <div className="text-sm font-medium text-neutral-900 leading-relaxed whitespace-pre-wrap">
            {question.content}
          </div>
          {question.mediaUrl && (
            <div className="mt-3">
              <img
                src={question.mediaUrl}
                alt="Soru görseli"
                className="max-h-60 rounded-lg border border-neutral-200 object-contain"
              />
            </div>
          )}
        </div>

        {/* Options */}
        <div className="space-y-3">
          <h4 className="text-xs font-semibold text-neutral-400 uppercase tracking-wider flex items-center gap-1.5">
            <HelpCircle className="h-3.5 w-3.5" /> Seçenekler (A–E)
          </h4>

          <div className="space-y-2">
            {question.options
              .slice()
              .sort((a, b) => a.optionLabel.localeCompare(b.optionLabel))
              .map((option) => (
                <div
                  key={option.id}
                  className={`flex items-start gap-3 p-3 rounded-lg border text-sm transition-colors ${
                    option.isCorrect
                      ? 'bg-emerald-50/70 border-emerald-300 text-emerald-950 font-medium'
                      : 'bg-white border-neutral-200 text-neutral-800'
                  }`}
                >
                  <span
                    className={`inline-flex items-center justify-center h-6 w-6 rounded-md font-bold text-xs shrink-0 ${
                      option.isCorrect
                        ? 'bg-emerald-600 text-white'
                        : 'bg-neutral-100 text-neutral-600'
                    }`}
                  >
                    {option.optionLabel}
                  </span>
                  <div className="flex-1 pt-0.5 whitespace-pre-wrap">{option.content}</div>
                  {option.isCorrect && (
                    <span className="flex items-center gap-1 text-xs font-semibold text-emerald-700 bg-emerald-100 px-2 py-0.5 rounded-full shrink-0">
                      <CheckCircle2 className="h-3.5 w-3.5" /> Doğru Cevap
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
