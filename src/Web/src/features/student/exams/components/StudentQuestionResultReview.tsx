import React, { useState, useEffect } from 'react';
import { Card, Button, LoadingState, Alert } from '@/components/ui';
import { StudentQuestionResultDto } from '@/types/question';
import { examService } from '@/services/examService';
import { CheckCircle2, XCircle, MinusCircle, HelpCircle, ChevronDown, ChevronUp } from 'lucide-react';

interface StudentQuestionResultReviewProps {
  attemptId: string;
  isRawAvailable: boolean;
}

export const StudentQuestionResultReview: React.FC<StudentQuestionResultReviewProps> = ({
  attemptId,
  isRawAvailable,
}) => {
  const [isOpen, setIsOpen] = useState<boolean>(false);
  const [questionResults, setQuestionResults] = useState<StudentQuestionResultDto[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen || !isRawAvailable || questionResults.length > 0) return;

    setIsLoading(true);
    setError(null);
    examService
      .getAttemptQuestionResults(attemptId)
      .then((data) => setQuestionResults(data))
      .catch((err: unknown) => {
        setError(err instanceof Error ? err.message : 'Soru detayları yüklenemedi.');
      })
      .finally(() => setIsLoading(false));
  }, [isOpen, isRawAvailable, attemptId, questionResults.length]);

  if (!isRawAvailable) {
    return (
      <div className="p-4 bg-neutral-50 rounded-xl border border-neutral-200 text-xs text-neutral-500 flex items-center gap-2">
        <HelpCircle className="h-4 w-4 text-neutral-400 shrink-0" />
        <span>Bu sınav denemesi için soru bazlı detaylı inceleme mevcut değildir (Özet mod).</span>
      </div>
    );
  }

  return (
    <div className="space-y-3 pt-2">
      {/* Toggle Button */}
      <Button
        variant="outline"
        size="sm"
        onClick={() => setIsOpen((prev) => !prev)}
        className="w-full flex items-center justify-between bg-white hover:bg-neutral-50 border-neutral-200 text-neutral-800"
      >
        <span className="flex items-center gap-2 font-semibold text-xs">
          <HelpCircle className="h-4 w-4 text-primary-600" />
          Soru Bazlı Detaylı İnceleme ({isOpen ? 'Gizle' : 'Göster'})
        </span>
        {isOpen ? <ChevronUp className="h-4 w-4" /> : <ChevronDown className="h-4 w-4" />}
      </Button>

      {/* Accordion Content */}
      {isOpen && (
        <div className="space-y-4 pt-2">
          {isLoading ? (
            <LoadingState message="Soru cevapları ve değerlendirmeler yükleniyor..." />
          ) : error ? (
            <Alert variant="danger">{error}</Alert>
          ) : questionResults.length === 0 ? (
            <p className="text-xs text-neutral-500 text-center py-4">Soru detayı bulunamadı.</p>
          ) : (
            <div className="space-y-3">
              {questionResults.map((q) => {
                const isCorrect = q.isCorrect === true;
                const isWrong = q.isCorrect === false;
                const isBlank = q.isBlank || (!q.selectedOption && !isCorrect && !isWrong);

                return (
                  <Card
                    key={q.examQuestionId}
                    className={`p-4 border transition-colors space-y-3 ${
                      isCorrect
                        ? 'bg-emerald-50/40 border-emerald-200'
                        : isWrong
                        ? 'bg-rose-50/40 border-rose-200'
                        : 'bg-white border-neutral-200'
                    }`}
                  >
                    {/* Header: Question Number & Result Badge */}
                    <div className="flex items-center justify-between text-xs">
                      <div className="flex items-center gap-2">
                        <span className="inline-flex items-center justify-center h-6 w-6 rounded-md bg-navy-900 text-white font-bold text-xs">
                          {q.questionNumber}
                        </span>
                        <span className="font-semibold text-neutral-700">Soru {q.questionNumber}</span>
                      </div>

                      <div className="flex items-center gap-2">
                        {isCorrect && (
                          <span className="flex items-center gap-1 font-bold text-emerald-700 bg-emerald-100 px-2.5 py-1 rounded-full text-xs">
                            <CheckCircle2 className="h-3.5 w-3.5" /> Doğru (+{q.netContribution} Net)
                          </span>
                        )}
                        {isWrong && (
                          <span className="flex items-center gap-1 font-bold text-rose-700 bg-rose-100 px-2.5 py-1 rounded-full text-xs">
                            <XCircle className="h-3.5 w-3.5" /> Yanlış ({q.netContribution} Net)
                          </span>
                        )}
                        {isBlank && (
                          <span className="flex items-center gap-1 font-bold text-neutral-600 bg-neutral-100 px-2.5 py-1 rounded-full text-xs">
                            <MinusCircle className="h-3.5 w-3.5" /> Boş (0 Net)
                          </span>
                        )}
                      </div>
                    </div>

                    {/* Question Content */}
                    <div className="text-sm font-medium text-neutral-900 leading-relaxed whitespace-pre-wrap">
                      {q.content}
                    </div>

                    {/* Answer Comparison Banner */}
                    <div className="p-3 rounded-lg bg-white border border-neutral-200 text-xs flex flex-wrap items-center justify-between gap-2">
                      <div className="flex items-center gap-3">
                        <span className="text-neutral-500">
                          Senin Cevabın:{' '}
                          <strong className={isCorrect ? 'text-emerald-700 font-bold' : isWrong ? 'text-rose-700 font-bold' : 'text-neutral-500'}>
                            {q.selectedOption || 'Boş'}
                          </strong>
                        </span>

                        <span className="text-neutral-300">|</span>

                        <span className="text-neutral-500">
                          Doğru Cevap: <strong className="text-emerald-700 font-bold">{q.correctOption}</strong>
                        </span>
                      </div>

                      {isWrong && q.selectedOption && (
                        <span className="text-rose-600 font-medium text-[11px]">
                          Cevabın yanlış. Doğru şık <strong>{q.correctOption}</strong> seçeneğidir.
                        </span>
                      )}
                    </div>
                  </Card>
                );
              })}
            </div>
          )}
        </div>
      )}
    </div>
  );
};
