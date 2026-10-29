import React from 'react';
import { Edit3, FileSpreadsheet, ArrowRight, UserCheck, Users, HelpCircle, Layers } from 'lucide-react';

export type EntryMode = 'manual_single' | 'manual_bulk' | 'excel_aggregate' | 'excel_question';

interface MethodSelectionStepProps {
  onSelectMode: (mode: EntryMode) => void;
}

export const MethodSelectionStep: React.FC<MethodSelectionStepProps> = ({ onSelectMode }) => {
  return (
    <div className="space-y-8 max-w-5xl mx-auto" data-testid="method-selection-container">
      <div className="text-center space-y-2">
        <h2 className="text-2xl font-bold text-[#14233E] dark:text-white">Sonuç Giriş Yöntemini Seçin</h2>
        <p className="text-sm text-navy-600 dark:text-navy-300 max-w-xl mx-auto">
          Sınav sonuçlarını tekil veya toplu olarak doğrudan ekrandan girebilir ya da Excel şablonu ile içe aktarabilirsiniz.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Manuel Giriş Seçenekleri */}
        <div className="bg-[#F8F7F3] dark:bg-navy-900/60 p-6 rounded-2xl border border-navy-200/70 dark:border-navy-800 shadow-xs flex flex-col justify-between space-y-6">
          <div className="space-y-4">
            <div className="flex items-center gap-3">
              <div className="h-12 w-12 rounded-2xl bg-blue-50 dark:bg-blue-950/50 text-[#315FE8] dark:text-blue-400 flex items-center justify-center shrink-0 border border-blue-100 dark:border-blue-900/60 shadow-xs">
                <Edit3 className="h-6 w-6" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-[#14233E] dark:text-white">Manuel Sonuç Girişi</h3>
                <span className="text-xs font-medium text-navy-500 dark:text-navy-400">Ekran Üzerinden Doğrudan Giriş</span>
              </div>
            </div>

            <p className="text-xs sm:text-sm text-navy-600 dark:text-navy-300">
              Öğrencilerin ders bazlı doğru, yanlış ve boş sayılarını doğrudan form veya hızlı tablo üzerinden kaydedin.
            </p>

            <div className="space-y-3 pt-2">
              <button
                type="button"
                data-testid="btn-mode-manual-single"
                onClick={() => onSelectMode('manual_single')}
                className="w-full text-left p-4 rounded-xl bg-white dark:bg-navy-950 border border-navy-200/80 dark:border-navy-700/80 hover:border-[#315FE8] hover:shadow-xs transition-all group flex items-center justify-between"
              >
                <div className="flex items-center gap-3.5 min-w-0 pr-2">
                  <div className="p-2.5 rounded-xl bg-emerald-50 text-emerald-600 dark:bg-emerald-950/60 dark:text-emerald-400 shrink-0 border border-emerald-100 dark:border-emerald-900/40">
                    <UserCheck className="h-5 w-5" />
                  </div>
                  <div className="min-w-0">
                    <div className="text-sm font-bold text-[#14233E] dark:text-white group-hover:text-[#315FE8] transition-colors truncate">
                      Tekil Öğrenci Girişi
                    </div>
                    <div className="text-xs text-navy-500 dark:text-navy-400 truncate mt-0.5">
                      Arama ile öğrenci seçip ders netlerini girin
                    </div>
                  </div>
                </div>
                <div className="h-8 w-8 rounded-lg bg-navy-50 dark:bg-navy-900 flex items-center justify-center text-navy-400 group-hover:text-[#315FE8] group-hover:bg-blue-50 dark:group-hover:bg-blue-950/50 transition-colors shrink-0">
                  <ArrowRight className="h-4 w-4" />
                </div>
              </button>

              <button
                type="button"
                data-testid="btn-mode-manual-bulk"
                onClick={() => onSelectMode('manual_bulk')}
                className="w-full text-left p-4 rounded-xl bg-white dark:bg-navy-950 border border-navy-200/80 dark:border-navy-700/80 hover:border-[#315FE8] hover:shadow-xs transition-all group flex items-center justify-between"
              >
                <div className="flex items-center gap-3.5 min-w-0 pr-2">
                  <div className="p-2.5 rounded-xl bg-blue-50 text-[#315FE8] dark:bg-blue-950/60 dark:text-blue-400 shrink-0 border border-blue-100 dark:border-blue-900/40">
                    <Users className="h-5 w-5" />
                  </div>
                  <div className="min-w-0">
                    <div className="text-sm font-bold text-[#14233E] dark:text-white group-hover:text-[#315FE8] transition-colors truncate">
                      Toplu Hızlı Tablo Girişi
                    </div>
                    <div className="text-xs text-navy-500 dark:text-navy-400 truncate mt-0.5">
                      Birden fazla öğrenci için serbest ızgara ortamı
                    </div>
                  </div>
                </div>
                <div className="h-8 w-8 rounded-lg bg-navy-50 dark:bg-navy-900 flex items-center justify-center text-navy-400 group-hover:text-[#315FE8] group-hover:bg-blue-50 dark:group-hover:bg-blue-950/50 transition-colors shrink-0">
                  <ArrowRight className="h-4 w-4" />
                </div>
              </button>
            </div>
          </div>
        </div>

        {/* Excel İle İçe Aktarma Seçenekleri */}
        <div className="bg-[#F8F7F3] dark:bg-navy-900/60 p-6 rounded-2xl border border-navy-200/70 dark:border-navy-800 shadow-xs flex flex-col justify-between space-y-6">
          <div className="space-y-4">
            <div className="flex items-center gap-3">
              <div className="h-12 w-12 rounded-2xl bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400 flex items-center justify-center shrink-0 border border-emerald-100 dark:border-emerald-900/60 shadow-xs">
                <FileSpreadsheet className="h-6 w-6" />
              </div>
              <div>
                <h3 className="text-lg font-bold text-[#14233E] dark:text-white">Excel ile Sonuç Yükle</h3>
                <span className="text-xs font-medium text-navy-500 dark:text-navy-400">Şablon ile Doğrulanmış Aktarım</span>
              </div>
            </div>

            <p className="text-xs sm:text-sm text-navy-600 dark:text-navy-300">
              Sistem şablonunu indirerek toplu sonuçları güvenli, hata denetimli ve hızlı biçimde içe aktarın.
            </p>

            <div className="space-y-3 pt-2">
              <button
                type="button"
                data-testid="btn-mode-excel-aggregate"
                onClick={() => onSelectMode('excel_aggregate')}
                className="w-full text-left p-4 rounded-xl bg-white dark:bg-navy-950 border border-navy-200/80 dark:border-navy-700/80 hover:border-emerald-500 hover:shadow-xs transition-all group flex items-center justify-between"
              >
                <div className="flex items-center gap-3.5 min-w-0 pr-2">
                  <div className="p-2.5 rounded-xl bg-teal-50 text-teal-600 dark:bg-teal-950/60 dark:text-teal-400 shrink-0 border border-teal-100 dark:border-teal-900/40">
                    <Layers className="h-5 w-5" />
                  </div>
                  <div className="min-w-0">
                    <div className="text-sm font-bold text-[#14233E] dark:text-white group-hover:text-emerald-600 dark:group-hover:text-emerald-400 transition-colors truncate">
                      Ders Bazlı Sonuç (Aggregate)
                    </div>
                    <div className="text-xs text-navy-500 dark:text-navy-400 truncate mt-0.5">
                      Ders bazlı toplam Doğru / Yanlış / Boş Excel yüklemesi
                    </div>
                  </div>
                </div>
                <div className="h-8 w-8 rounded-lg bg-navy-50 dark:bg-navy-900 flex items-center justify-center text-navy-400 group-hover:text-emerald-600 group-hover:bg-emerald-50 dark:group-hover:bg-emerald-950/50 transition-colors shrink-0">
                  <ArrowRight className="h-4 w-4" />
                </div>
              </button>

              <button
                type="button"
                data-testid="btn-mode-excel-question"
                onClick={() => onSelectMode('excel_question')}
                className="w-full text-left p-4 rounded-xl bg-white dark:bg-navy-950 border border-navy-200/80 dark:border-navy-700/80 hover:border-purple-500 hover:shadow-xs transition-all group flex items-center justify-between"
              >
                <div className="flex items-center gap-3.5 min-w-0 pr-2">
                  <div className="p-2.5 rounded-xl bg-purple-50 text-purple-600 dark:bg-purple-950/60 dark:text-purple-400 shrink-0 border border-purple-100 dark:border-purple-900/40">
                    <HelpCircle className="h-5 w-5" />
                  </div>
                  <div className="min-w-0">
                    <div className="text-sm font-bold text-[#14233E] dark:text-white group-hover:text-purple-600 dark:group-hover:text-purple-400 transition-colors truncate">
                      Soru Bazlı Sonuç (QuestionAnswers)
                    </div>
                    <div className="text-xs text-navy-500 dark:text-navy-400 truncate mt-0.5">
                      Her soru için A–E seçeneği bazlı optik cevap yüklemesi
                    </div>
                  </div>
                </div>
                <div className="h-8 w-8 rounded-lg bg-navy-50 dark:bg-navy-900 flex items-center justify-center text-navy-400 group-hover:text-purple-600 group-hover:bg-purple-50 dark:group-hover:bg-purple-950/50 transition-colors shrink-0">
                  <ArrowRight className="h-4 w-4" />
                </div>
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
