import React from 'react';
import { Clock, AlertCircle } from 'lucide-react';
import { AssignedStudentDetail } from '../types';

export interface StudentOverviewTabProps {
  student: AssignedStudentDetail;
}

export const StudentOverviewTab: React.FC<StudentOverviewTabProps> = ({ student }) => {
  return (
    <div className="space-y-4 select-none">
      {/* 1. Academic Metrics */}
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
        <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm">
          <div className="flex items-center gap-1.5 text-xs text-neutral-400 mb-1">
            <Clock className="h-3.5 w-3.5 text-success" />
            <span>Haftalık Çalışma Süresi</span>
          </div>
          <p className="font-mono text-xl sm:text-2xl font-bold text-neutral-900 leading-tight">
            {student.weeklyStudyHours} saat
          </p>
          <p className="text-xs text-neutral-400 mt-0.5">
            Son 7 günlük toplam çalışma
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm">
          <div className="flex items-center gap-1.5 text-xs text-neutral-400 mb-1">
            <AlertCircle className="h-3.5 w-3.5 text-attention-dark" />
            <span>Özel Koçluk Notu Sayısı</span>
          </div>
          <p className="font-mono text-xl sm:text-2xl font-bold text-attention-dark leading-tight">
            {student.privateNotes.length} Not
          </p>
          <p className="text-xs text-neutral-400 mt-0.5">
            Yalnızca koça özel kayıt
          </p>
        </div>
      </div>

      {/* 2. Critical Follow-up Summary */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 space-y-2.5 shadow-soft-sm">
        <h4 className="text-xs font-semibold uppercase tracking-wider text-neutral-400">
          Koçluk Değerlendirme Özeti
        </h4>
        <p className="text-sm text-neutral-700 leading-relaxed">
          {student.name}, son 30 günlük süreçte %{student.planAdherence} plan uyumu ile çalışmalarını sürdürmektedir.
          {student.status === 'kritik'
            ? ' Öğrencinin çalışma disiplininde kritik aksamalar tespit edilmiş olup acil görüşme önerilmektedir.'
            : student.status === 'dikkat'
            ? ' Belirli ders ve konu bazlı eksiklerin giderilmesi için yakın takip gerekmektedir.'
            : ' Genel çalışma performansı ve deneme sonuçları hedefleriyle uyumlu ilerlemektedir.'}
        </p>
      </div>
    </div>
  );
};
