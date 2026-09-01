import React from 'react';
import { Award, UserCheck, AlertTriangle } from 'lucide-react';
import { Modal } from '@/components/ui/Modal';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { StudentExamItem } from '../types';

export interface ExamDetailModalProps {
  exam: StudentExamItem | null;
  isOpen: boolean;
  onClose: () => void;
}

export const ExamDetailModal: React.FC<ExamDetailModalProps> = ({ exam, isOpen, onClose }) => {
  if (!exam) return null;

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={exam.title}
      subtitle={`${exam.type} Deneme Sınavı Sonuç Karnesi • ${exam.date}`}
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
        {/* Total Net and Rank Header */}
        <div className="grid grid-cols-2 sm:grid-cols-3 gap-3">
          <div className="bg-surface-alt rounded-xl p-3">
            <span className="text-xs text-neutral-400">Toplam Net</span>
            <p className="font-mono text-2xl font-bold text-neutral-900 mt-0.5">
              {exam.totalNet.toFixed(2)}
            </p>
          </div>

          <div className="bg-surface-alt rounded-xl p-3">
            <span className="text-xs text-neutral-400">Net Değişimi</span>
            <p className="font-mono text-xl font-bold text-success mt-0.5">
              {exam.netChange > 0 ? `+${exam.netChange.toFixed(2)}` : exam.netChange.toFixed(2)} Net
            </p>
          </div>

          {exam.rankInInstitution && (
            <div className="bg-surface-alt rounded-xl p-3 col-span-2 sm:col-span-1">
              <span className="text-xs text-neutral-400 flex items-center gap-1">
                <Award className="h-3.5 w-3.5 text-warning" />
                Kurum Derecesi
              </span>
              <p className="font-mono text-xl font-bold text-neutral-900 mt-0.5">
                {exam.rankInInstitution}. / {exam.totalStudents}
              </p>
            </div>
          )}
        </div>

        {/* Subject Breakdown Table */}
        <div className="bg-white rounded-xl border border-neutral-100 overflow-hidden">
          <div className="px-4 py-2.5 bg-neutral-50 border-b border-neutral-100 flex justify-between items-center">
            <span className="text-xs font-semibold text-neutral-600">Ders Bazlı Sonuçlar</span>
            <span className="text-[11px] font-mono text-neutral-400">D / Y / B / Net</span>
          </div>

          <div className="divide-y divide-neutral-50">
            {exam.subjects.map((sub) => (
              <div key={sub.subjectName} className="px-4 py-3 flex items-center justify-between">
                <div>
                  <p className="text-sm font-semibold text-neutral-800">{sub.subjectName}</p>
                  <p className="text-xs font-mono text-neutral-400 mt-0.5">
                    {sub.correct} D • {sub.wrong} Y • {sub.empty} B
                  </p>
                </div>

                <div className="text-right">
                  <span className="font-mono text-base font-bold text-primary-700">
                    {sub.net.toFixed(2)} Net
                  </span>
                  {sub.changeFromPrevious !== undefined && (
                    <p
                      className={`text-[10px] font-mono font-semibold ${
                        sub.changeFromPrevious >= 0 ? 'text-success' : 'text-danger'
                      }`}
                    >
                      {sub.changeFromPrevious >= 0 ? `+${sub.changeFromPrevious.toFixed(2)}` : sub.changeFromPrevious.toFixed(2)}
                    </p>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Coach Note */}
        {exam.coachReviewNote && (
          <div className="bg-attention-light border border-purple-200 rounded-xl p-3.5">
            <p className="text-xs font-semibold text-attention-dark flex items-center gap-1 mb-1">
              <UserCheck className="h-3.5 w-3.5" />
              Koç Hasan Yılmaz&apos;ın Değerlendirmesi
            </p>
            <p className="text-xs text-purple-900 leading-relaxed">
              {exam.coachReviewNote}
            </p>
          </div>
        )}

        {/* Weak Topics */}
        {exam.focusWeakTopics && exam.focusWeakTopics.length > 0 && (
          <div className="bg-warning-light border border-amber-200 rounded-xl p-3.5">
            <p className="text-xs font-semibold text-warning-dark flex items-center gap-1 mb-1.5">
              <AlertTriangle className="h-3.5 w-3.5" />
              Geliştirilmesi Gereken Odak Konular
            </p>
            <div className="flex flex-wrap gap-1.5">
              {exam.focusWeakTopics.map((topic) => (
                <Badge key={topic} variant="warning" size="sm">
                  {topic}
                </Badge>
              ))}
            </div>
          </div>
        )}
      </div>
    </Modal>
  );
};
