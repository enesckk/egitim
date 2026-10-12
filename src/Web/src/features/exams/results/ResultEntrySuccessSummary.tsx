import React from 'react';
import { CheckCircle2, UserCheck, Users, FileSpreadsheet, ArrowRight, RotateCcw } from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import {
  SingleManualResultResponse,
  BulkManualResultResponse,
  ExamResultImportCommitResultDto,
} from '@/types/examResultImport';

interface ResultEntrySuccessSummaryProps {
  type: 'single_manual' | 'bulk_manual' | 'excel_commit';
  singleResult?: SingleManualResultResponse;
  bulkResult?: BulkManualResultResponse;
  commitResult?: ExamResultImportCommitResultDto;
  onNewEntry: () => void;
  onGoToExams: () => void;
}

export const ResultEntrySuccessSummary: React.FC<ResultEntrySuccessSummaryProps> = ({
  type,
  singleResult,
  bulkResult,
  commitResult,
  onNewEntry,
  onGoToExams,
}) => {
  return (
    <Card className="p-8 max-w-2xl mx-auto text-center space-y-6">
      <div className="mx-auto h-16 w-16 rounded-full bg-emerald-100 dark:bg-emerald-950/60 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
        <CheckCircle2 className="h-10 w-10" />
      </div>

      <div className="space-y-2">
        <h2 className="text-2xl font-bold text-gray-900 dark:text-white">
          {type === 'single_manual' && 'Tekil Öğrenci Sonucu Başarıyla Kaydedildi'}
          {type === 'bulk_manual' && 'Toplu Manuel Sonuçlar Başarıyla Kaydedildi'}
          {type === 'excel_commit' && 'Excel Dosyası Başarıyla İçe Aktarıldı'}
        </h2>
        <p className="text-sm text-gray-500 dark:text-gray-400">
          Sınav sonuçları başarıyla sisteme aktarıldı ve netler hesaplandı.
        </p>
      </div>

      {/* Summary Details Box */}
      <div className="p-6 rounded-2xl bg-gray-50 dark:bg-navy-900 border border-gray-200 dark:border-gray-800 text-left space-y-4">
        {type === 'single_manual' && singleResult && (
          <div className="space-y-3 text-sm">
            <div className="flex items-center gap-2 font-semibold text-gray-900 dark:text-white border-b pb-2 dark:border-gray-800">
              <UserCheck className="h-4 w-4 text-emerald-600" />
              Deneme #{singleResult.attemptNumber} Özeti
            </div>
            <div className="grid grid-cols-2 gap-3 text-xs">
              <div>
                <span className="text-gray-500">Doğru Sayısı:</span>{' '}
                <strong className="text-emerald-600 font-bold">{singleResult.totalCorrect}</strong>
              </div>
              <div>
                <span className="text-gray-500">Yanlış Sayısı:</span>{' '}
                <strong className="text-red-600 font-bold">{singleResult.totalWrong}</strong>
              </div>
              <div>
                <span className="text-gray-500">Boş Sayısı:</span>{' '}
                <strong className="text-gray-700 dark:text-gray-300 font-bold">{singleResult.totalBlank}</strong>
              </div>
              <div>
                <span className="text-gray-500">Hesaplanan Toplam Net:</span>{' '}
                <strong className="text-navy-600 dark:text-navy-400 font-bold text-sm">
                  {singleResult.totalNet.toFixed(2)} Net
                </strong>
              </div>
            </div>
          </div>
        )}

        {type === 'bulk_manual' && bulkResult && (
          <div className="space-y-3 text-sm">
            <div className="flex items-center gap-2 font-semibold text-gray-900 dark:text-white border-b pb-2 dark:border-gray-800">
              <Users className="h-4 w-4 text-blue-600" />
              Toplu Manuel Giriş İşlem Özeti
            </div>
            <div className="grid grid-cols-2 gap-3 text-xs">
              <div>
                <span className="text-gray-500">İşlenen Öğrenci Sayısı:</span>{' '}
                <strong className="text-gray-900 dark:text-white font-bold">{bulkResult.studentsProcessed}</strong>
              </div>
              <div>
                <span className="text-gray-500">Oluşturulan Deneme Sayısı:</span>{' '}
                <strong className="text-emerald-600 font-bold">{bulkResult.attemptsCreated}</strong>
              </div>
              <div>
                <span className="text-gray-500">Oluşturulan Ders Sonucu:</span>{' '}
                <strong className="text-navy-600 font-bold">{bulkResult.subjectResultsCreated}</strong>
              </div>
            </div>
          </div>
        )}

        {type === 'excel_commit' && commitResult && (
          <div className="space-y-3 text-sm">
            <div className="flex items-center gap-2 font-semibold text-gray-900 dark:text-white border-b pb-2 dark:border-gray-800">
              <FileSpreadsheet className="h-4 w-4 text-emerald-600" />
              Excel Aktarım İşlem Özeti
            </div>
            <div className="grid grid-cols-2 gap-3 text-xs">
              <div>
                <span className="text-gray-500">Aktarılan Öğrenci Sayısı:</span>{' '}
                <strong className="text-gray-900 dark:text-white font-bold">{commitResult.importedStudentsCount}</strong>
              </div>
              <div>
                <span className="text-gray-500">Oluşturulan Deneme Sayısı:</span>{' '}
                <strong className="text-emerald-600 font-bold">{commitResult.createdAttemptsCount}</strong>
              </div>
              <div>
                <span className="text-gray-500">İşlem Tarihi:</span>{' '}
                <strong className="text-gray-700 dark:text-gray-300 font-medium">
                  {new Date(commitResult.importedAt).toLocaleString('tr-TR')}
                </strong>
              </div>
            </div>
          </div>
        )}
      </div>

      {/* Navigation Actions */}
      <div className="flex items-center justify-center gap-4 pt-2">
        <Button
          type="button"
          variant="outline"
          onClick={onNewEntry}
          className="flex items-center gap-2"
        >
          <RotateCcw className="h-4 w-4" /> Yeni Sonuç Girişi
        </Button>

        <Button
          type="button"
          variant="primary"
          onClick={onGoToExams}
          className="flex items-center gap-2 bg-navy-600 hover:bg-navy-700 text-white"
        >
          Sınav Sonuçlarına Git <ArrowRight className="h-4 w-4" />
        </Button>
      </div>
    </Card>
  );
};
