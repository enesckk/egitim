import React from 'react';
import { Award, FileText } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { StudentExamItem } from '../types';
import { StudentQuestionResultReview } from './StudentQuestionResultReview';

export interface ExamDetailModalProps {
  exam: StudentExamItem | null;
  isOpen: boolean;
  onClose: () => void;
}

export const ExamDetailModal: React.FC<ExamDetailModalProps> = ({ exam, isOpen, onClose }) => {
  if (!exam) return null;

  const shortId = exam.attemptId ? exam.attemptId.slice(-4).toUpperCase() : null;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={exam.title}
      subtitle={`${exam.type} Deneme Sınavı • ${exam.date}${shortId ? ` • #${shortId}` : ''}`}
      headerVariant="dark"
      maxWidth="lg"
      footer={
        <div className="flex justify-end w-full">
          <Button variant="primary" size="sm" onClick={onClose}>
            Kapat
          </Button>
        </div>
      }
    >
      <div className="space-y-4 select-none">
        {/* Score and Result Cards */}
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-2.5 sm:gap-3">
          <div className="bg-neutral-50 rounded-2xl p-3.5 border border-neutral-100">
            <span className="text-[11px] font-semibold text-neutral-400 uppercase tracking-wider">Toplam Net</span>
            <p className="font-mono text-2xl font-bold text-neutral-900 mt-1">
              {exam.totalNet.toFixed(2)}
            </p>
          </div>

          <div className="bg-neutral-50 rounded-2xl p-3.5 border border-neutral-100">
            <span className="text-[11px] font-semibold text-neutral-400 uppercase tracking-wider">Sınav Türü</span>
            <div className="mt-1 flex items-center gap-1.5">
              <span className="font-semibold text-neutral-800 text-base">{exam.type}</span>
              {exam.maxTotalNet && (
                <span className="text-xs text-neutral-400 font-mono">({exam.maxTotalNet} Soru)</span>
              )}
            </div>
          </div>

          {exam.rankInInstitution ? (
            <div className="bg-neutral-50 rounded-2xl p-3.5 border border-neutral-100 col-span-2 sm:col-span-1">
              <span className="text-[11px] font-semibold text-neutral-400 uppercase tracking-wider flex items-center gap-1">
                <Award className="h-3.5 w-3.5 text-amber-500" />
                Kurum Sırası
              </span>
              <p className="font-mono text-xl font-bold text-neutral-900 mt-1">
                {exam.rankInInstitution}. / {exam.totalStudents || '—'}
              </p>
            </div>
          ) : (
            <div className="bg-neutral-50 rounded-2xl p-3.5 border border-neutral-100 col-span-2 sm:col-span-1">
              <span className="text-[11px] font-semibold text-neutral-400 uppercase tracking-wider">Tarih</span>
              <p className="text-sm font-semibold text-neutral-800 mt-1">
                {exam.date}
              </p>
            </div>
          )}
        </div>

        {/* Subject Breakdown Table */}
        <div className="bg-white rounded-2xl border border-neutral-200/80 overflow-hidden shadow-soft-sm">
          <div className="px-4 py-3 bg-neutral-50/80 border-b border-neutral-100 flex justify-between items-center">
            <div className="flex items-center gap-2">
              <FileText className="h-4 w-4 text-primary-600" />
              <span className="text-xs font-bold text-neutral-800 uppercase tracking-wider">Ders Dağılım Karnesi</span>
            </div>
            <span className="text-[11px] font-mono text-neutral-500">D • Y • B → Net</span>
          </div>

          {exam.subjects && exam.subjects.length > 0 ? (
            <div className="divide-y divide-neutral-100">
              {exam.subjects.map((sub) => (
                <div key={sub.subjectName} className="px-4 py-3 sm:py-3.5 flex items-center justify-between hover:bg-neutral-50/50 transition-colors">
                  <div>
                    <p className="text-sm font-semibold text-neutral-900">{sub.subjectName}</p>
                    <div className="flex items-center gap-2 mt-0.5 text-xs font-mono text-neutral-500">
                      <span className="text-emerald-700 font-semibold">{sub.correct} D</span>
                      <span>•</span>
                      <span className="text-rose-700 font-semibold">{sub.wrong} Y</span>
                      <span>•</span>
                      <span className="text-neutral-500">{sub.empty} B</span>
                    </div>
                  </div>

                  <div className="text-right">
                    <span className="font-mono text-base sm:text-lg font-bold text-primary-700">
                      {sub.net.toFixed(2)}
                    </span>
                    <span className="text-xs font-medium text-neutral-400 ml-1">Net</span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="p-6 text-center text-xs text-neutral-500">
              Ders bazlı net dağılımı bu deneme için kaydedilmemiş.
            </div>
          )}
        </div>

        {/* Question-by-Question Result Review or Aggregate Notice */}
        <div className="border-t border-neutral-100 pt-3">
          <StudentQuestionResultReview
            attemptId={exam.attemptId || exam.id}
            isRawAvailable={exam.isRawAvailable ?? false}
          />
        </div>
      </div>
    </Modal>
  );
};
