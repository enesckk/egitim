import React, { useState } from 'react';
import {
  Download,
  Upload,
  FileSpreadsheet,
  CheckCircle2,
  XCircle,
  ChevronDown,
  ChevronRight,
  ArrowLeft,
  FileCheck,
} from 'lucide-react';
import { Card } from '@/components/ui/Card';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/Badge';
import { ExamDetailDto } from '@/types/exam';
import {
  ImportType,
  ExamResultImportPreviewDto,
  ExamResultImportCommitResultDto,
  ImportRowPreviewDto,
} from '@/types/examResultImport';
import { examService } from '@/services/examService';
import { translateIssueCode } from './issueTranslations';

interface ExcelImportWizardProps {
  exam: ExamDetailDto;
  initialImportType: ImportType;
  onSuccess: (commitResult: ExamResultImportCommitResultDto) => void;
  onCancel: () => void;
}

export const ExcelImportWizard: React.FC<ExcelImportWizardProps> = ({
  exam,
  initialImportType,
  onSuccess,
  onCancel,
}) => {
  const [importType, setImportType] = useState<ImportType>(initialImportType);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [isDownloadingTemplate, setIsDownloadingTemplate] = useState<boolean>(false);
  const [isPreviewing, setIsPreviewing] = useState<boolean>(false);
  const [isCommitting, setIsCommitting] = useState<boolean>(false);

  const [previewDto, setPreviewDto] = useState<ExamResultImportPreviewDto | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [expandedRows, setExpandedRows] = useState<Record<number, boolean>>({});

  // 1. Template Download Handler
  const handleDownloadTemplate = async () => {
    setErrorMessage(null);
    setIsDownloadingTemplate(true);
    try {
      const blob =
        importType === ImportType.AggregateResults
          ? await examService.downloadAggregateTemplate(exam.id)
          : await examService.downloadQuestionAnswersTemplate(exam.id);

      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `Sinav_Sablonu_${exam.code}_${importType === ImportType.AggregateResults ? 'Aggregate' : 'QuestionAnswers'}.xlsx`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (err: unknown) {
      setErrorMessage(
        err instanceof Error ? err.message : 'Excel şablon dosyası indirilirken bir hata oluştu.'
      );
    } finally {
      setIsDownloadingTemplate(false);
    }
  };

  // 2. File Selection Handler
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    setErrorMessage(null);
    setPreviewDto(null);

    if (!file) {
      setSelectedFile(null);
      return;
    }

    if (!file.name.endsWith('.xlsx')) {
      setErrorMessage('Lütfen sadece geçerli bir Excel (.xlsx) dosyası yükleyin.');
      setSelectedFile(null);
      return;
    }

    setSelectedFile(file);
  };

  // 3. Preview Request Handler
  const handleGeneratePreview = async () => {
    if (!selectedFile) return;

    setErrorMessage(null);
    setIsPreviewing(true);

    try {
      const preview = await examService.previewResultImport(exam.id, importType, selectedFile);
      setPreviewDto(preview);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Önizleme oluşturulurken bir hata oluştu.';
      setErrorMessage(translateIssueCode(msg, msg));
      setPreviewDto(null);
    } finally {
      setIsPreviewing(false);
    }
  };

  // 4. Final Commit Handler (Sends EXACT SAME original File object + fileHash)
  const handleCommit = async () => {
    if (!selectedFile || !previewDto || !previewDto.canImport) return;

    setErrorMessage(null);
    setIsCommitting(true);

    try {
      const result = await examService.commitResultImport(
        exam.id,
        importType,
        previewDto.fileHash,
        selectedFile
      );
      onSuccess(result);
    } catch (err: unknown) {
      const rawMsg = err instanceof Error ? err.message : 'İçe aktarma sırasında hata oluştu.';

      if (rawMsg.includes('IMPORT_FILE_CHANGED')) {
        setErrorMessage('Dosya önizlemeden sonra değişmiş. Lütfen dosyayı yeniden yükleyip tekrar kontrol edin.');
        setPreviewDto(null);
      } else if (rawMsg.includes('IMPORT_ALREADY_COMPLETED')) {
        setErrorMessage('Bu Excel dosyası bu sınav için daha önce içe aktarılmış.');
      } else {
        setErrorMessage(translateIssueCode(rawMsg, rawMsg));
      }
    } finally {
      setIsCommitting(false);
    }
  };

  const toggleRowExpansion = (rowNumber: number) => {
    setExpandedRows((prev) => ({ ...prev, [rowNumber]: !prev[rowNumber] }));
  };

  return (
    <div className="space-y-8 max-w-4xl mx-auto">
      {/* Top Bar */}
      <div className="flex items-center justify-between border-b pb-4 dark:border-gray-800">
        <div>
          <h2 className="text-xl font-bold text-gray-900 dark:text-white flex items-center gap-2">
            <FileSpreadsheet className="h-5 w-5 text-emerald-600" />
            Excel İle Sonuç Yükleme Sihirbazı
          </h2>
          <p className="text-xs text-gray-500 dark:text-gray-400 mt-1">
            {exam.title} ({exam.code}) — {importType === ImportType.AggregateResults ? 'Ders Bazlı Sonuç' : 'Soru Bazlı Sonuç'}
          </p>
        </div>
        <Button type="button" variant="outline" size="sm" onClick={onCancel}>
          Vazgeç
        </Button>
      </div>

      {errorMessage && (
        <div className="p-4 rounded-xl bg-red-50 dark:bg-red-950/40 border border-red-200 dark:border-red-900 text-red-700 dark:text-red-300 text-xs flex items-center gap-2">
          <XCircle className="h-5 w-5 shrink-0 text-red-500" />
          <span>{errorMessage}</span>
        </div>
      )}

      {/* Step A: Import Type Switcher & Template Download */}
      {!previewDto && (
        <div className="space-y-6">
          <Card className="p-6 space-y-4">
            <h3 className="text-sm font-semibold text-gray-900 dark:text-white">1. Yükleme Türü ve Şablon</h3>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <button
                type="button"
                onClick={() => { setImportType(ImportType.AggregateResults); setPreviewDto(null); }}
                className={`p-4 rounded-xl border text-left transition-all ${
                  importType === ImportType.AggregateResults
                    ? 'border-emerald-500 bg-emerald-50/50 dark:bg-emerald-950/30 text-emerald-900 dark:text-emerald-200 ring-2 ring-emerald-500'
                    : 'border-gray-200 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-navy-900'
                }`}
              >
                <div className="font-semibold text-sm">Ders Bazlı Sonuç (AggregateResults)</div>
                <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                  Her ders için Doğru, Yanlış ve Boş sayılarını içeren şablon.
                </div>
              </button>

              <button
                type="button"
                onClick={() => { setImportType(ImportType.QuestionAnswers); setPreviewDto(null); }}
                className={`p-4 rounded-xl border text-left transition-all ${
                  importType === ImportType.QuestionAnswers
                    ? 'border-purple-500 bg-purple-50/50 dark:bg-purple-950/30 text-purple-900 dark:text-purple-200 ring-2 ring-purple-500'
                    : 'border-gray-200 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-navy-900'
                }`}
              >
                <div className="font-semibold text-sm">Soru Bazlı Sonuç (QuestionAnswers)</div>
                <div className="text-xs text-gray-500 dark:text-gray-400 mt-1">
                  Her soru için A–E seçeneklerini veya boş yanıtları içeren şablon.
                </div>
              </button>
            </div>

            {/* Template Download Prompt */}
            <div className="pt-2 flex items-center justify-between border-t border-gray-100 dark:border-gray-800">
              <div className="text-xs text-gray-500">
                Sisteme tam uyumlu Excel formatını kullanmak için güncel şablonu indirin.
              </div>
              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={handleDownloadTemplate}
                isLoading={isDownloadingTemplate}
                className="flex items-center gap-2 text-emerald-600 border-emerald-300 hover:bg-emerald-50 dark:text-emerald-400 dark:border-emerald-800 dark:hover:bg-emerald-950/50"
              >
                <Download className="h-4 w-4" /> Excel Şablonunu İndir
              </Button>
            </div>
          </Card>

          {/* Template Instructions */}
          <Card className="p-6 bg-gray-50/50 dark:bg-navy-900/40 space-y-3">
            <h4 className="text-xs font-semibold text-gray-700 dark:text-gray-300 uppercase tracking-wider">
              Şablon Doldurma Yönergeleri
            </h4>
            {importType === ImportType.AggregateResults ? (
              <ul className="text-xs text-gray-600 dark:text-gray-400 space-y-1.5 list-disc list-inside">
                <li>Excel sayfasındaki öğrenci bilgilerini (StudentId / StudentNumber) değiştirmeden kullanın.</li>
                <li>Her ders bölümü için <strong>Doğru</strong>, <strong>Yanlış</strong> ve <strong>Boş</strong> kolonlarına tam sayı değerler girin.</li>
                <li>Sütun başlıklarını ve sistem tanımlayıcı hücreleri değiştirmeyin.</li>
              </ul>
            ) : (
              <ul className="text-xs text-gray-600 dark:text-gray-400 space-y-1.5 list-disc list-inside">
                <li>Her soru için A, B, C, D veya E seçeneğini büyük harfle girin.</li>
                <li>Boş bırakılan hücreler sistem tarafından otomatik olarak <strong>Boş Yanıt</strong> kabul edilir.</li>
                <li>Soru sütun yapılarını veya öğrenci ID başlıklarını değiştirmeyin.</li>
              </ul>
            )}
          </Card>

          {/* Step B: File Upload & Preview Action */}
          <Card className="p-6 space-y-4 border-2 border-dashed border-gray-300 dark:border-gray-700 text-center">
            <div className="mx-auto h-12 w-12 rounded-full bg-emerald-50 dark:bg-emerald-950/50 text-emerald-600 dark:text-emerald-400 flex items-center justify-center">
              <Upload className="h-6 w-6" />
            </div>

            <div>
              <label htmlFor="excel-file-input" className="cursor-pointer text-sm font-semibold text-navy-600 dark:text-navy-400 hover:underline">
                Doldurulmuş Excel Dosyasını Seçin (.xlsx)
              </label>
              <input
                id="excel-file-input"
                type="file"
                accept=".xlsx"
                onChange={handleFileChange}
                className="hidden"
              />
              <p className="text-xs text-gray-400 mt-1">Yalnızca .xlsx formatı desteklenir.</p>
            </div>

            {selectedFile && (
              <div className="inline-flex items-center gap-2 px-3 py-1.5 rounded-lg bg-emerald-50 dark:bg-emerald-950/60 border border-emerald-200 dark:border-emerald-800 text-emerald-800 dark:text-emerald-200 text-xs font-medium">
                <FileCheck className="h-4 w-4 text-emerald-600" />
                <span>{selectedFile.name}</span> ({(selectedFile.size / 1024).toFixed(1)} KB)
              </div>
            )}

            <div className="pt-4 flex justify-center">
              <Button
                type="button"
                variant="primary"
                onClick={handleGeneratePreview}
                disabled={!selectedFile || isPreviewing}
                isLoading={isPreviewing}
                className="flex items-center gap-2"
              >
                Dosyayı Önizle ve Kontrol Et
              </Button>
            </div>
          </Card>
        </div>
      )}

      {/* Step C: Preview Results & Confirmation */}
      {previewDto && (
        <div className="space-y-6">
          {/* Top Summary Banner */}
          <Card className="p-6 space-y-4 border-l-4 border-l-navy-600">
            <div className="flex items-center justify-between">
              <div>
                <h3 className="text-lg font-bold text-gray-900 dark:text-white flex items-center gap-2">
                  Önizleme Raporu
                  {previewDto.canImport ? (
                    <Badge variant="success" className="text-xs">İçe Aktarmaya Uygum</Badge>
                  ) : (
                    <Badge variant="danger" className="text-xs">Hatalar Mevcut (Engellendi)</Badge>
                  )}
                </h3>
                <p className="text-xs text-gray-500 mt-1">
                  Dosya Hash: <code className="bg-gray-100 dark:bg-navy-950 px-1.5 py-0.5 rounded text-navy-600">{previewDto.fileHash.slice(0, 12)}...</code>
                </p>
              </div>

              <Button
                type="button"
                variant="outline"
                size="sm"
                onClick={() => setPreviewDto(null)}
                className="flex items-center gap-1.5 text-xs"
              >
                <ArrowLeft className="h-4 w-4" /> Yeni Dosya Seç
              </Button>
            </div>

            {/* Metric Counters Grid */}
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4 pt-2 text-center">
              <div className="p-3 rounded-xl bg-gray-50 dark:bg-navy-900">
                <div className="text-xs text-gray-500">Toplam Satır</div>
                <div className="text-xl font-bold text-gray-900 dark:text-white">{previewDto.totalRows}</div>
              </div>
              <div className="p-3 rounded-xl bg-emerald-50 dark:bg-emerald-950/40">
                <div className="text-xs text-emerald-700 dark:text-emerald-400">Geçerli Öğrenci</div>
                <div className="text-xl font-bold text-emerald-600 dark:text-emerald-400">{previewDto.validRowsCount}</div>
              </div>
              <div className="p-3 rounded-xl bg-red-50 dark:bg-red-950/40">
                <div className="text-xs text-red-700 dark:text-red-400">Hatalı Satır</div>
                <div className="text-xl font-bold text-red-600 dark:text-red-400">{previewDto.invalidRowsCount}</div>
              </div>
              <div className="p-3 rounded-xl bg-amber-50 dark:bg-amber-950/40">
                <div className="text-xs text-amber-700 dark:text-amber-400">Uyarı Sayısı</div>
                <div className="text-xl font-bold text-amber-600 dark:text-amber-400">{previewDto.warningCount}</div>
              </div>
            </div>
          </Card>

          {/* Issues Table (if any issues exist) */}
          {previewDto.issues && previewDto.issues.length > 0 && (
            <Card className="overflow-hidden border border-gray-200 dark:border-gray-800">
              <div className="p-4 bg-gray-50 dark:bg-navy-900 border-b border-gray-200 dark:border-gray-800 font-semibold text-xs text-gray-700 dark:text-gray-300">
                Tespit Edilen Hata ve Uyarılar ({previewDto.issues.length})
              </div>

              <div className="max-h-64 overflow-y-auto">
                <table className="w-full text-xs text-left">
                  <thead className="bg-gray-100 dark:bg-navy-950 text-gray-600 dark:text-gray-400 uppercase">
                    <tr>
                      <th className="px-3 py-2">Satır</th>
                      <th className="px-3 py-2">Alan</th>
                      <th className="px-3 py-2">Tür</th>
                      <th className="px-3 py-2">Hata Kodu</th>
                      <th className="px-3 py-2">Açıklama</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
                    {previewDto.issues.map((issue, idx) => {
                      const isError = issue.severity.toLowerCase() === 'error' || issue.severity === '1';
                      return (
                        <tr key={idx} className={isError ? 'bg-red-50/50 dark:bg-red-950/20' : 'bg-amber-50/50 dark:bg-amber-950/20'}>
                          <td className="px-3 py-2 font-mono font-semibold">{issue.rowNumber > 0 ? issue.rowNumber : '-'}</td>
                          <td className="px-3 py-2 text-gray-600 dark:text-gray-400">{issue.column || '-'}</td>
                          <td className="px-3 py-2">
                            {isError ? (
                              <Badge variant="danger" className="text-[10px]">Hata</Badge>
                            ) : (
                              <Badge variant="warning" className="text-[10px]">Uyarı</Badge>
                            )}
                          </td>
                          <td className="px-3 py-2 font-mono text-navy-600 dark:text-navy-400">{issue.code}</td>
                          <td className="px-3 py-2 font-medium text-gray-900 dark:text-white">
                            {translateIssueCode(issue.code, issue.message)}
                          </td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            </Card>
          )}

          {/* Row Data Previews */}
          <Card className="overflow-hidden border border-gray-200 dark:border-gray-800">
            <div className="p-4 bg-gray-50 dark:bg-navy-900 border-b border-gray-200 dark:border-gray-800 font-semibold text-xs text-gray-700 dark:text-gray-300">
              Öğrenci Önizleme Listesi ({previewDto.rows.length} Örnek Satır)
            </div>

            <div className="max-h-80 overflow-y-auto">
              <table className="w-full text-xs text-left">
                <thead className="bg-gray-100 dark:bg-navy-950 text-gray-600 dark:text-gray-400 uppercase">
                  <tr>
                    <th className="px-3 py-2">Satır</th>
                    <th className="px-3 py-2">Öğrenci Numarası / Adı</th>
                    <th className="px-3 py-2">Eşleşen Sistem Öğrencisi</th>
                    <th className="px-3 py-2 text-center">Net / Detay</th>
                    <th className="px-3 py-2 text-center">Durum</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 dark:divide-gray-800">
                  {previewDto.rows.map((row: ImportRowPreviewDto) => (
                    <React.Fragment key={row.rowNumber}>
                      <tr className="hover:bg-gray-50 dark:hover:bg-navy-900/40">
                        <td className="px-3 py-2 font-mono">{row.rowNumber}</td>
                        <td className="px-3 py-2 font-medium text-gray-900 dark:text-white">{row.studentIdentifier}</td>
                        <td className="px-3 py-2 text-gray-600 dark:text-gray-400">
                          {row.matchedStudent ? (
                            <span>{row.matchedStudent.fullName} ({row.matchedStudent.studentNumber || 'No yok'})</span>
                          ) : (
                            <span className="text-red-500 italic">Eşleşme Bulunamadı</span>
                          )}
                        </td>
                        <td className="px-3 py-2 text-center">
                          {importType === ImportType.QuestionAnswers ? (
                            <button
                              type="button"
                              onClick={() => toggleRowExpansion(row.rowNumber)}
                              className="text-navy-600 dark:text-navy-400 hover:underline inline-flex items-center gap-1 font-medium"
                            >
                              {expandedRows[row.rowNumber] ? <ChevronDown className="h-3 w-3" /> : <ChevronRight className="h-3 w-3" />}
                              {row.answers ? `${row.answers.length} Yanıt Detayı` : 'Detay'}
                            </button>
                          ) : (
                            <span className="font-bold text-navy-600 dark:text-navy-400">
                              {row.calculatedTotalNet !== null ? `${row.calculatedTotalNet.toFixed(2)} Net` : '-'}
                            </span>
                          )}
                        </td>
                        <td className="px-3 py-2 text-center">
                          {row.isValid ? (
                            <CheckCircle2 className="h-4 w-4 text-emerald-500 mx-auto" />
                          ) : (
                            <XCircle className="h-4 w-4 text-red-500 mx-auto" />
                          )}
                        </td>
                      </tr>

                      {/* Expandable Answers Details for QuestionAnswers */}
                      {importType === ImportType.QuestionAnswers && expandedRows[row.rowNumber] && row.answers && (
                        <tr className="bg-gray-50 dark:bg-navy-950/80">
                          <td colSpan={5} className="p-3">
                            <div className="flex flex-wrap gap-2 text-[11px]">
                              {row.answers.map((ans) => (
                                <span
                                  key={ans.examQuestionId || ans.questionNumber}
                                  className={`px-2 py-1 rounded border font-mono ${
                                    ans.isBlank
                                      ? 'bg-gray-100 text-gray-500 border-gray-300 dark:bg-navy-900 dark:text-gray-400'
                                      : 'bg-blue-50 text-blue-700 border-blue-200 dark:bg-blue-950 dark:text-blue-300 font-bold'
                                  }`}
                                >
                                  S{ans.questionNumber}: {ans.isBlank ? 'BOŞ' : ans.selectedOption}
                                </span>
                              ))}
                            </div>
                          </td>
                        </tr>
                      )}
                    </React.Fragment>
                  ))}
                </tbody>
              </table>
            </div>
          </Card>

          {/* Action Commit Controls */}
          <div className="flex items-center justify-between pt-4 border-t border-gray-200 dark:border-gray-800">
            <Button
              type="button"
              variant="outline"
              onClick={() => setPreviewDto(null)}
              disabled={isCommitting}
            >
              Farklı Dosya Yükle
            </Button>

            <Button
              type="button"
              variant="primary"
              onClick={handleCommit}
              disabled={!previewDto.canImport || isCommitting}
              isLoading={isCommitting}
              className="flex items-center gap-2 bg-emerald-600 hover:bg-emerald-700 text-white"
            >
              <CheckCircle2 className="h-4 w-4" /> {previewDto.validRowsCount} Öğrencinin Sonucunu İçe Aktar
            </Button>
          </div>
        </div>
      )}
    </div>
  );
};
