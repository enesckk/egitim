import React from 'react';
import { AlertTriangle, ChevronRight, UserCheck } from 'lucide-react';
import { AdminCoachPortfolioItem } from '../types';

export interface CoachPortfolioCardProps {
  coaches: AdminCoachPortfolioItem[];
  onSelectCoach?: (coachId: string) => void;
}

export const CoachPortfolioCard: React.FC<CoachPortfolioCardProps> = ({
  coaches,
  onSelectCoach,
}) => {
  return (
    <div className="bg-white rounded-2xl border border-neutral-200/80 overflow-hidden shadow-soft-xs select-none">
      <div className="flex items-center justify-between px-5 py-4 border-b border-neutral-100 bg-[#F8F7F3]/70">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-xl bg-[#14233E] text-white flex items-center justify-center font-bold text-xs shadow-soft-xs">
            <UserCheck className="h-4 w-4" />
          </div>
          <div>
            <h2 className="font-bold text-navy-950 text-sm">Koçluk Portföy Durumu</h2>
            <p className="text-xs text-neutral-500 mt-0.5">
              Atanmış öğrenci sayıları ve aktif çalışma planı uyum ortalamaları
            </p>
          </div>
        </div>
        <span className="text-xs font-mono font-semibold text-neutral-500 bg-white px-2.5 py-1 rounded-full border border-neutral-200">
          {coaches.length} Koç
        </span>
      </div>

      {coaches.length === 0 ? (
        <div className="p-8 text-center text-xs text-neutral-400">
          Kurumunuzda kayıtlı koç veya aktif portföy verisi bulunamadı.
        </div>
      ) : (
        <div className="divide-y divide-neutral-100/80">
          {coaches.map((coach) => (
            <div
              key={coach.id}
              onClick={() => onSelectCoach?.(coach.id)}
              className="flex items-center gap-4 px-5 py-3.5 hover:bg-neutral-50/80 transition-colors cursor-pointer group"
            >
              <div className="w-10 h-10 rounded-xl bg-[#14233E] text-white text-xs font-bold flex items-center justify-center shrink-0 shadow-soft-xs">
                {coach.initials}
              </div>

              <div className="flex-1 min-w-0">
                <div className="flex items-center gap-2 mb-0.5">
                  <p className="text-sm font-bold text-neutral-900 group-hover:text-primary-600 transition-colors truncate">
                    {coach.name}
                  </p>
                  {coach.status === 'attention' && (
                    <span className="inline-flex items-center gap-1 text-[10px] font-semibold text-amber-800 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded-full">
                      <AlertTriangle className="h-3 w-3 text-amber-600" />
                      Plan Takibi Gerekli
                    </span>
                  )}
                </div>
                <p className="text-xs text-neutral-500 truncate">
                  Portföy:{' '}
                  <span className="font-semibold text-neutral-700">{coach.students} Öğrenci</span>
                  {' · '}
                  Ort. Plan Uyumu:{' '}
                  <strong
                    className={`font-mono font-bold ${
                      coach.adherence >= 80
                        ? 'text-emerald-600'
                        : coach.adherence >= 70
                        ? 'text-amber-600'
                        : 'text-rose-600'
                    }`}
                  >
                    %{coach.adherence}
                  </strong>
                  {' · '}
                  Haftalık Çalışma: <span className="font-mono">{coach.avgStudy} sa</span>
                </p>
              </div>

              <div className="flex items-center gap-3 shrink-0">
                {coach.attention > 0 && (
                  <div className="text-right hidden sm:block">
                    <p className="text-xs font-bold text-rose-600 font-mono">
                      {coach.attention} dikkat
                    </p>
                    <p className="text-[10px] text-neutral-400">öğrenci</p>
                  </div>
                )}
                <ChevronRight className="h-4 w-4 text-neutral-400 group-hover:text-primary-600 group-hover:translate-x-0.5 transition-all" />
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};
