import React, { useState, useEffect } from 'react';
import { Sparkles, RefreshCw, ChevronRight } from 'lucide-react';
import { academicService } from '@/services/academicService';
import { AcademicAiSummaryDto } from '../types';

interface AcademicAiSummaryCardProps {
  studentId: string;
  audience?: 'student' | 'coach' | 'parent';
  onNavigateDetails?: () => void;
}

export const AcademicAiSummaryCard: React.FC<AcademicAiSummaryCardProps> = ({
  studentId,
  audience = 'student',
  onNavigateDetails,
}) => {
  const [data, setData] = useState<AcademicAiSummaryDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(false);

  const fetchSummary = async () => {
    if (!studentId) return;
    setLoading(true);
    setError(false);
    try {
      const res = await academicService.getAiSummary(studentId, audience);
      setData(res);
    } catch {
      setError(true);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchSummary();
  }, [studentId, audience]);

  return (
    <div
      onClick={onNavigateDetails}
      className="bg-white rounded-2xl p-4 sm:p-5 shadow-xs border border-neutral-200/70 hover:border-blue-300 transition-all cursor-pointer group flex items-start gap-3 sm:gap-4 relative"
    >
      <div className="w-10 h-10 rounded-xl bg-blue-50 text-[#315FE8] flex items-center justify-center shrink-0 group-hover:scale-105 transition-transform mt-0.5">
        <Sparkles className="w-5 h-5 fill-blue-100/50" />
      </div>

      <div className="flex-1 min-w-0 pr-6">
        <div className="flex items-center gap-2">
          <h4 className="text-sm font-bold text-[#14233E] group-hover:text-[#315FE8] transition-colors">
            Akademik Değerlendirmem
          </h4>
          <span className="text-[10px] font-semibold text-neutral-400 bg-neutral-100 px-2 py-0.5 rounded-md hidden xs:inline">
            Otomatik Analiz
          </span>
        </div>

        {loading && !data && (
          <p className="text-xs text-neutral-400 mt-1 animate-pulse">
            Sınav sonuçları ve akademik eğilimler analiz ediliyor...
          </p>
        )}

        {error && !data && (
          <p className="text-xs text-neutral-500 mt-1">
            Akademik değerlendirme şu anda oluşturulamadı.
          </p>
        )}

        {data && (
          <p className="text-xs text-neutral-600 mt-1 leading-relaxed line-clamp-2">
            {data.summary || 'Genel olarak iyi bir seviyedesin. Düzenli soru çözümü ve zayıf konulara odaklanma ile netlerini daha da artırabilirsin.'}
          </p>
        )}
      </div>

      <div className="absolute right-4 top-1/2 -translate-y-1/2 flex items-center gap-1">
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            fetchSummary();
          }}
          disabled={loading}
          className="p-1.5 rounded-full text-neutral-400 hover:text-neutral-700 hover:bg-neutral-100 transition-colors disabled:opacity-50"
          title="Yenile"
        >
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
        </button>
        {onNavigateDetails && (
          <ChevronRight className="w-4 h-4 text-neutral-400 group-hover:translate-x-0.5 transition-transform" />
        )}
      </div>
    </div>
  );
};
