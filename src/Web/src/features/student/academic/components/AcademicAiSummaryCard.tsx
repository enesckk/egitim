import React, { useState, useEffect } from 'react';
import { Sparkles, RefreshCw, CheckCircle2, AlertTriangle, Lightbulb } from 'lucide-react';
import { academicService } from '@/services/academicService';
import { AcademicAiSummaryDto } from '../types';

interface AcademicAiSummaryCardProps {
  studentId: string;
  audience?: 'student' | 'coach' | 'parent';
}

export const AcademicAiSummaryCard: React.FC<AcademicAiSummaryCardProps> = ({
  studentId,
  audience = 'student',
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
    <div className="rounded-xl border border-primary-100 bg-gradient-to-br from-primary-50/50 via-white to-neutral-50 p-5 shadow-sm">
      <div className="flex items-center justify-between gap-3 mb-3">
        <div className="flex items-center gap-2">
          <div className="p-2 rounded-lg bg-primary-100 text-primary-700">
            <Sparkles className="h-5 w-5" />
          </div>
          <div>
            <h3 className="text-sm font-semibold text-neutral-900">AI Akademik Yorum</h3>
            <span className="text-[10px] font-medium text-primary-600 bg-primary-100/60 px-2 py-0.5 rounded-md">
              Yapay Zeka Destekli Değerlendirme
            </span>
          </div>
        </div>
        <button
          type="button"
          onClick={fetchSummary}
          disabled={loading}
          className="p-1.5 rounded-lg text-neutral-400 hover:text-neutral-600 hover:bg-white transition-colors disabled:opacity-50"
          title="Yorumu Yenile"
        >
          <RefreshCw className={`h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
        </button>
      </div>

      {loading && !data && (
        <div className="py-4 text-center text-xs text-neutral-500 animate-pulse">
          Akademik veriler değerlendiriliyor...
        </div>
      )}

      {error && !data && (
        <div className="py-2 text-xs text-neutral-500">
          Değerlendirme şu anda yüklenemedi.
        </div>
      )}

      {data && (
        <div className="space-y-3">
          <p className="text-xs text-neutral-700 leading-relaxed font-medium">
            {data.summary}
          </p>

          {data.strengths.length > 0 && (
            <div>
              <span className="text-[11px] font-semibold text-emerald-700 flex items-center gap-1 mb-1">
                <CheckCircle2 className="h-3.5 w-3.5" /> Güçlü Yanlar
              </span>
              <div className="flex flex-wrap gap-1.5">
                {data.strengths.map((s, i) => (
                  <span key={i} className="text-[11px] bg-emerald-50 text-emerald-700 border border-emerald-100 px-2 py-0.5 rounded-md">
                    {s}
                  </span>
                ))}
              </div>
            </div>
          )}

          {data.improvementAreas.length > 0 && (
            <div>
              <span className="text-[11px] font-semibold text-amber-700 flex items-center gap-1 mb-1">
                <AlertTriangle className="h-3.5 w-3.5" /> Geliştirilmesi Gereken Alanlar
              </span>
              <div className="flex flex-wrap gap-1.5">
                {data.improvementAreas.map((a, i) => (
                  <span key={i} className="text-[11px] bg-amber-50 text-amber-700 border border-amber-100 px-2 py-0.5 rounded-md">
                    {a}
                  </span>
                ))}
              </div>
            </div>
          )}

          {data.suggestedFocus.length > 0 && (
            <div>
              <span className="text-[11px] font-semibold text-primary-700 flex items-center gap-1 mb-1">
                <Lightbulb className="h-3.5 w-3.5" /> Önerilen Odak Konuları
              </span>
              <ul className="text-xs text-neutral-600 list-disc list-inside space-y-0.5">
                {data.suggestedFocus.slice(0, 3).map((f, i) => (
                  <li key={i}>{f}</li>
                ))}
              </ul>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
