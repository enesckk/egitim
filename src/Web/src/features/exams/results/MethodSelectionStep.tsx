import React from 'react';
import { Edit3, FileSpreadsheet, ArrowRight, UserCheck, Users, HelpCircle, Layers } from 'lucide-react';
import { Card } from '@/components/ui/Card';

export type EntryMode = 'manual_single' | 'manual_bulk' | 'excel_aggregate' | 'excel_question';

interface MethodSelectionStepProps {
  onSelectMode: (mode: EntryMode) => void;
}

export const MethodSelectionStep: React.FC<MethodSelectionStepProps> = ({ onSelectMode }) => {
  return (
    <div className="space-y-8 max-w-4xl mx-auto">
      <div className="text-center space-y-2">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">Sonuç Giriş Yöntemini Seçin</h2>
        <p className="text-sm text-gray-600 dark:text-gray-400">
          Sınav sonuçlarını tekil veya toplu manuel olarak girebilir ya da Excel şablonu ile içe aktarabilirsiniz.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        {/* Manuel Giriş Seçenekleri */}
        <Card className="p-6 border-2 border-navy-100 hover:border-navy-500 transition-all dark:border-navy-800 dark:hover:border-navy-500 space-y-6 flex flex-col justify-between">
          <div className="space-y-4">
            <div className="h-12 w-12 rounded-xl bg-navy-50 dark:bg-navy-900/50 text-navy-600 dark:text-navy-400 flex items-center justify-center">
              <Edit3 className="h-6 w-6" />
            </div>

            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Manuel Sonuç Girişi</h3>
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Öğrencilerin ders bazlı doğru, yanlış ve boş sayılarını doğrudan ekrandan girin.
              </p>
            </div>

            <div className="space-y-3 pt-2">
              <button
                type="button"
                onClick={() => onSelectMode('manual_single')}
                className="w-full text-left p-4 rounded-xl border border-gray-200 dark:border-gray-700 hover:bg-navy-50 dark:hover:bg-navy-900/40 transition-colors group flex items-center justify-between"
              >
                <div className="flex items-center gap-3">
                  <div className="p-2 rounded-lg bg-emerald-50 text-emerald-600 dark:bg-emerald-950 dark:text-emerald-400">
                    <UserCheck className="h-5 w-5" />
                  </div>
                  <div>
                    <div className="text-sm font-medium text-gray-900 dark:text-white">Tekil Öğrenci Girişi</div>
                    <div className="text-xs text-gray-500 dark:text-gray-400">Tek öğrenci seçerek ders bazlı sonuç girin</div>
                  </div>
                </div>
                <ArrowRight className="h-4 w-4 text-gray-400 group-hover:text-navy-600 dark:group-hover:text-navy-400 transition-colors" />
              </button>

              <button
                type="button"
                onClick={() => onSelectMode('manual_bulk')}
                className="w-full text-left p-4 rounded-xl border border-gray-200 dark:border-gray-700 hover:bg-navy-50 dark:hover:bg-navy-900/40 transition-colors group flex items-center justify-between"
              >
                <div className="flex items-center gap-3">
                  <div className="p-2 rounded-lg bg-blue-50 text-blue-600 dark:bg-blue-950 dark:text-blue-400">
                    <Users className="h-5 w-5" />
                  </div>
                  <div>
                    <div className="text-sm font-medium text-gray-900 dark:text-white">Toplu Hızlı Tablo Girişi</div>
                    <div className="text-xs text-gray-500 dark:text-gray-400">Birden fazla öğrenci için serbest tablo ortamı</div>
                  </div>
                </div>
                <ArrowRight className="h-4 w-4 text-gray-400 group-hover:text-navy-600 dark:group-hover:text-navy-400 transition-colors" />
              </button>
            </div>
          </div>
        </Card>

        {/* Excel İle İçe Aktarma Seçenekleri */}
        <Card className="p-6 border-2 border-emerald-100 hover:border-emerald-500 transition-all dark:border-emerald-950 dark:hover:border-emerald-500 space-y-6 flex flex-col justify-between">
          <div className="space-y-4">
            <div className="h-12 w-12 rounded-xl bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
              <FileSpreadsheet className="h-6 w-6" />
            </div>

            <div>
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">Excel ile Sonuç Yükle</h3>
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                Sistem şablonunu indirerek toplu sonuçları güvenli ve hızlı şekilde yükleyin.
              </p>
            </div>

            <div className="space-y-3 pt-2">
              <button
                type="button"
                onClick={() => onSelectMode('excel_aggregate')}
                className="w-full text-left p-4 rounded-xl border border-gray-200 dark:border-gray-700 hover:bg-emerald-50 dark:hover:bg-emerald-950/40 transition-colors group flex items-center justify-between"
              >
                <div className="flex items-center gap-3">
                  <div className="p-2 rounded-lg bg-teal-50 text-teal-600 dark:bg-teal-950 dark:text-teal-400">
                    <Layers className="h-5 w-5" />
                  </div>
                  <div>
                    <div className="text-sm font-medium text-gray-900 dark:text-white">Ders Bazlı Sonuç (Aggregate)</div>
                    <div className="text-xs text-gray-500 dark:text-gray-400">Ders bazlı toplam Doğru / Yanlış / Boş Excel yüklemesi</div>
                  </div>
                </div>
                <ArrowRight className="h-4 w-4 text-gray-400 group-hover:text-emerald-600 dark:group-hover:text-emerald-400 transition-colors" />
              </button>

              <button
                type="button"
                onClick={() => onSelectMode('excel_question')}
                className="w-full text-left p-4 rounded-xl border border-gray-200 dark:border-gray-700 hover:bg-emerald-50 dark:hover:bg-emerald-950/40 transition-colors group flex items-center justify-between"
              >
                <div className="flex items-center gap-3">
                  <div className="p-2 rounded-lg bg-purple-50 text-purple-600 dark:bg-purple-950 dark:text-purple-400">
                    <HelpCircle className="h-5 w-5" />
                  </div>
                  <div>
                    <div className="text-sm font-medium text-gray-900 dark:text-white">Soru Bazlı Sonuç (QuestionAnswers)</div>
                    <div className="text-xs text-gray-500 dark:text-gray-400">Her soru için A–E seçeneği bazlı Excel cevap yüklemesi</div>
                  </div>
                </div>
                <ArrowRight className="h-4 w-4 text-gray-400 group-hover:text-emerald-600 dark:group-hover:text-emerald-400 transition-colors" />
              </button>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
};
