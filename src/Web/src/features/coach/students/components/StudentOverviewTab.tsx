import React from 'react';
import { Clock, Lock, CheckCircle2, AlertTriangle, Calendar } from 'lucide-react';
import { AssignedStudentDetail } from '../types';
import { AcademicAiSummaryCard } from '@/features/student/academic/components/AcademicAiSummaryCard';
import { QuestionStyleAnalyticsSection } from '@/features/student/academic/components/QuestionStyleAnalyticsSection';

export interface StudentOverviewTabProps {
  student: AssignedStudentDetail;
}

export const StudentOverviewTab: React.FC<StudentOverviewTabProps> = ({ student }) => {
  return (
    <div className="space-y-4 select-none">
      <AcademicAiSummaryCard studentId={student.id} audience="coach" />
      <QuestionStyleAnalyticsSection studentId={student.id} isReadOnly />

      {/* 1. Academic Metrics */}
      <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
        {/* Plan Compliance */}
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-xs">
          <div className="flex items-center gap-1.5 text-xs text-neutral-500 mb-1.5 font-medium">
            <Calendar className="h-4 w-4 text-primary-600" />
            <span>Aktif Plan Uyumu</span>
          </div>
          {student.hasActivePlan && typeof student.planAdherence === 'number' ? (
            <div>
              <p className="font-mono text-xl sm:text-2xl font-bold text-neutral-900 leading-tight">
                %{student.planAdherence}
              </p>
              <div className="w-full bg-neutral-100 h-1.5 rounded-full mt-2 overflow-hidden">
                <div
                  className={`h-full rounded-full ${
                    student.planAdherence < 40
                      ? 'bg-rose-500'
                      : student.planAdherence < 70
                      ? 'bg-amber-500'
                      : 'bg-emerald-500'
                  }`}
                  style={{ width: `${Math.min(student.planAdherence, 100)}%` }}
                />
              </div>
            </div>
          ) : (
            <p className="text-sm font-semibold text-neutral-500 mt-1">Aktif Plan Yok</p>
          )}
        </div>

        {/* Weekly Study Hours */}
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-xs">
          <div className="flex items-center gap-1.5 text-xs text-neutral-500 mb-1.5 font-medium">
            <Clock className="h-4 w-4 text-emerald-600" />
            <span>Tamamlanan Çalışma</span>
          </div>
          <p className="font-mono text-xl sm:text-2xl font-bold text-neutral-900 leading-tight">
            {student.weeklyStudyHours || 0} saat
          </p>
          <p className="text-[11px] text-neutral-400 mt-1">
            Plana kayıtlı tamamlanan süre
          </p>
        </div>

        {/* Private Coach Notes Count */}
        <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-xs">
          <div className="flex items-center gap-1.5 text-xs text-neutral-500 mb-1.5 font-medium">
            <Lock className="h-4 w-4 text-navy-800" />
            <span>Özel Koçluk Notları</span>
          </div>
          <p className="font-mono text-xl sm:text-2xl font-bold text-navy-900 leading-tight">
            {student.privateNotes.length} Not
          </p>
          <p className="text-[11px] text-neutral-400 mt-1">
            Yalnızca size özel kayıtlar
          </p>
        </div>
      </div>

      {/* 2. Critical Follow-up Summary based on real data */}
      <div className="bg-[#F8F7F3] rounded-2xl border border-neutral-200/80 p-4 sm:p-5 space-y-2 shadow-soft-xs">
        <h4 className="text-xs font-bold uppercase tracking-wider text-neutral-700">
          Koçluk Takip Durumu
        </h4>
        <div className="text-xs sm:text-sm text-neutral-700 leading-relaxed">
          {student.hasActivePlan && typeof student.planAdherence === 'number' ? (
            <div className="flex items-start gap-2">
              {student.status === 'kritik' ? (
                <AlertTriangle className="h-4 w-4 text-rose-600 shrink-0 mt-0.5" />
              ) : student.status === 'dikkat' ? (
                <Clock className="h-4 w-4 text-amber-600 shrink-0 mt-0.5" />
              ) : (
                <CheckCircle2 className="h-4 w-4 text-emerald-600 shrink-0 mt-0.5" />
              )}
              <p>
                {student.name}, aktif çalışma planında <strong>%{student.planAdherence}</strong> tamamlama oranına sahiptir.
                {student.status === 'kritik'
                  ? ' Görev tamamlama oranında aksamalar gözlenmektedir; haftalık görüşmede plan revizyonu yapılması önerilir.'
                  : student.status === 'dikkat'
                  ? ' Kalan görevlerin vaktinde tamamlanması için yakın takip tavsiye edilir.'
                  : ' Çalışma temposu ve görev uyumu planlanan hedeflerle uyumlu şekilde devam etmektedir.'}
              </p>
            </div>
          ) : (
            <p className="text-neutral-500">
              Bu öğrenci için henüz onaylanmış aktif bir çalışma planı bulunmamaktadır. Plan İnceleme sekmesinden veya görev atayarak çalışma sürecini başlatabilirsiniz.
            </p>
          )}
        </div>
      </div>
    </div>
  );
};

export default StudentOverviewTab;
