import React, { useState, useEffect, useCallback } from 'react';
import {
  BarChart3,
  AlertCircle,
  RefreshCw,
  Building2,
  Download,
  Filter,
  GraduationCap,
  FileSpreadsheet,
  Users,
  Target,
} from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { reportingService, InstitutionalReportDto, ReportFilterParams } from '@/services/reportingService';
import { examService } from '@/services/examService';
import type { ExamDto } from '@/types/exam';
import { adminService, ClassroomDto } from '@/services/adminService';
import { authService } from '@/services/auth';

export interface AdminReportsViewProps {
  initialData?: InstitutionalReportDto;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const AdminReportsView: React.FC<AdminReportsViewProps> = ({
  initialData: propInitialData,
  isLoading: propIsLoading = false,
  errorMessage: propErrorMessage,
  onRetry: propOnRetry,
}) => {
  const [data, setData] = useState<InstitutionalReportDto | null>(propInitialData || null);
  const [isLoading, setIsLoading] = useState<boolean>(propIsLoading || (!propInitialData && !propErrorMessage));
  const [errorMessage, setErrorMessage] = useState<string | null>(propErrorMessage || null);

  // Filters
  const [classrooms, setClassrooms] = useState<ClassroomDto[]>([]);
  const [exams, setExams] = useState<ExamDto[]>([]);
  const [selectedClassroomId, setSelectedClassroomId] = useState<string>('');
  const [selectedExamId, setSelectedExamId] = useState<string>('');

  const loadFilterOptions = async () => {
    try {
      const [classRes, examRes] = await Promise.all([
        adminService.listClassrooms(),
        examService.getExams({ page: 1, pageSize: 100 }),
      ]);
      setClassrooms(classRes || []);
      setExams(examRes?.items || []);
    } catch {
      // Non-critical, filter options can stay empty
    }
  };

  const loadReport = useCallback(async (filters?: ReportFilterParams) => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const report = await reportingService.getInstitutionalReport(filters);
      setData(report);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Kurum raporları yüklenemedi.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    if (!propInitialData && !propErrorMessage) {
      loadFilterOptions();
      loadReport();
    }
  }, [propInitialData, propErrorMessage, loadReport]);

  const handleApplyFilter = () => {
    loadReport({
      classroomId: selectedClassroomId || undefined,
      examId: selectedExamId || undefined,
    });
  };

  const handleExport = (format: 'xlsx' | 'csv') => {
    const token = authService.getAccessToken();
    const url = reportingService.getExportUrl(format, {
      classroomId: selectedClassroomId || undefined,
      examId: selectedExamId || undefined,
    });
    
    // Download authenticated file
    fetch(url, {
      headers: {
        Authorization: `Bearer ${token}`,
      },
    })
      .then((res) => {
        if (!res.ok) throw new Error('İndirme başarısız oldu.');
        return res.blob();
      })
      .then((blob) => {
        const downloadUrl = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = downloadUrl;
        a.download = `Kurum_Raporu_${Date.now()}.${format}`;
        document.body.appendChild(a);
        a.click();
        a.remove();
        window.URL.revokeObjectURL(downloadUrl);
      })
      .catch((err) => {
        alert(err.message);
      });
  };

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4">
        <Skeleton className="h-10 w-64 rounded-xl" />
        <div className="grid grid-cols-1 sm:grid-cols-4 gap-4">
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
          <Skeleton className="h-24 w-full rounded-2xl" />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <Skeleton className="h-64 w-full rounded-2xl" />
          <Skeleton className="h-64 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-10 space-y-4 select-none p-4">
        <Alert
          variant="danger"
          icon={<AlertCircle className="h-5 w-5" />}
          title="Raporlar Yüklenemedi"
        >
          {errorMessage}
        </Alert>
        <div className="text-center pt-2">
          <Button
            variant="primary"
            size="sm"
            onClick={propOnRetry || (() => loadReport())}
            leftIcon={<RefreshCw className="h-4 w-4" />}
          >
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  if (!data) return null;

  return (
    <div className="max-w-6xl mx-auto select-none space-y-6 p-4">
      {/* 1. Header & Actions */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            {data.institutionName}
          </h1>
          <p className="text-xs sm:text-sm text-neutral-500 mt-0.5">
            Gerçek MSSQL deneme sonuçları, şubeler arası başarı ve plan uyumu analizi
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport('csv')}
            leftIcon={<Download className="h-4 w-4" />}
            id="btn-export-csv"
          >
            CSV İndir
          </Button>
          <Button
            variant="primary"
            size="sm"
            onClick={() => handleExport('xlsx')}
            leftIcon={<FileSpreadsheet className="h-4 w-4" />}
            id="btn-export-xlsx"
          >
            Excel İndir
          </Button>
        </div>
      </div>

      {/* 2. Filters */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm flex flex-wrap items-center gap-3">
        <div className="flex items-center gap-1.5 text-xs font-semibold text-neutral-600">
          <Filter className="h-4 w-4 text-primary-600" />
          <span>Filtreler:</span>
        </div>

        <select
          id="select-filter-classroom"
          value={selectedClassroomId}
          onChange={(e) => setSelectedClassroomId(e.target.value)}
          className="text-xs bg-neutral-50 border border-neutral-200 rounded-xl px-3 py-2 text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500"
        >
          <option value="">Tüm Şubeler</option>
          {classrooms.map((c) => (
            <option key={c.id} value={c.id}>
              {c.name}
            </option>
          ))}
        </select>

        <select
          id="select-filter-exam"
          value={selectedExamId}
          onChange={(e) => setSelectedExamId(e.target.value)}
          className="text-xs bg-neutral-50 border border-neutral-200 rounded-xl px-3 py-2 text-neutral-700 focus:outline-none focus:ring-2 focus:ring-primary-500"
        >
          <option value="">Tüm Sınavlar</option>
          {exams.map((e) => (
            <option key={e.id} value={e.id}>
              {e.title}
            </option>
          ))}
        </select>

        <Button
          variant="secondary"
          size="sm"
          onClick={handleApplyFilter}
          id="btn-apply-filter"
        >
          Uygula
        </Button>
      </div>

      {/* 3. Summary KPIs */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm">
          <div className="flex items-center gap-2 text-xs text-neutral-500">
            <Users className="h-4 w-4 text-primary-600" />
            <span>Kayıtlı Öğrenci</span>
          </div>
          <p className="text-xl sm:text-2xl font-bold font-mono text-neutral-900 mt-2">
            {data.totalStudents}
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm">
          <div className="flex items-center gap-2 text-xs text-neutral-500">
            <GraduationCap className="h-4 w-4 text-navy-600" />
            <span>Toplam Sınav Girişi</span>
          </div>
          <p className="text-xl sm:text-2xl font-bold font-mono text-neutral-900 mt-2">
            {data.totalAttempts}
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm">
          <div className="flex items-center gap-2 text-xs text-neutral-500">
            <Target className="h-4 w-4 text-success" />
            <span>Ortalama Net</span>
          </div>
          <p className="text-xl sm:text-2xl font-bold font-mono text-neutral-900 mt-2">
            {data.overallAverageNet}
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm">
          <div className="flex items-center gap-2 text-xs text-neutral-500">
            <BarChart3 className="h-4 w-4 text-warning" />
            <span>Ort. Plan Uyumu</span>
          </div>
          <p className="text-xl sm:text-2xl font-bold font-mono text-neutral-900 mt-2">
            %{data.overallAverageAdherence}
          </p>
        </div>
      </div>

      {/* 4. Subject Averages */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm space-y-4">
        <h2 className="font-semibold text-neutral-800 text-sm flex items-center gap-2">
          <BarChart3 className="h-4 w-4 text-primary-600" />
          Ders Bazlı Başarı & Net Ortalamaları
        </h2>

        {data.subjectAverages.length === 0 ? (
          <p className="text-xs text-neutral-400 py-4 text-center">
            Seçili filtrelere uygun sınav sonucu bulunamadı.
          </p>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
            {data.subjectAverages.map((sa) => (
              <div
                key={sa.subjectName}
                className="p-3 rounded-xl bg-neutral-50 border border-neutral-100 space-y-1"
              >
                <span className="text-xs font-semibold text-neutral-700 block">
                  {sa.subjectName}
                </span>
                <span className="text-lg font-bold font-mono text-primary-700 block">
                  {sa.averageNet} Net
                </span>
                <span className="text-[10px] text-neutral-500">
                  {sa.averageCorrect} Doğru · {sa.averageWrong} Yanlış
                </span>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* 5. Grid: Adherence Distribution & Branch Comparisons */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Adherence Distribution */}
        <div className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm space-y-4">
          <div className="flex items-center gap-2 pb-2 border-b border-neutral-100">
            <BarChart3 className="h-4 w-4 text-primary-600" />
            <h2 className="font-semibold text-neutral-800 text-sm">
              Öğrenci Plan Uyumu Dağılımı
            </h2>
          </div>

          <div className="space-y-4">
            {data.adherenceDistribution.map((item) => (
              <div key={item.range} className="space-y-1.5">
                <div className="flex items-center justify-between text-xs">
                  <span className="font-medium text-neutral-700">{item.range}</span>
                  <span className="font-mono text-neutral-900 font-bold">
                    {item.studentCount} Öğrenci (%{item.percentage})
                  </span>
                </div>
                <div className="h-2.5 w-full bg-neutral-100 rounded-full overflow-hidden">
                  <div
                    className={cn('h-full rounded-full transition-all duration-500', item.color)}
                    style={{ width: `${Math.min(100, item.percentage)}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Branch Comparisons */}
        <div className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm space-y-4">
          <div className="flex items-center gap-2 pb-2 border-b border-neutral-100">
            <Building2 className="h-4 w-4 text-primary-600" />
            <h2 className="font-semibold text-neutral-800 text-sm">
              Şubeler Arası Başarı Karşılaştırması
            </h2>
          </div>

          <div className="divide-y divide-neutral-100 max-h-72 overflow-y-auto">
            {data.branchComparisons.map((branch) => (
              <div
                key={branch.branchName}
                className="py-3 flex items-center justify-between gap-2"
              >
                <div>
                  <h4 className="text-sm font-semibold text-neutral-900">
                    {branch.branchName}
                  </h4>
                  <p className="text-xs text-neutral-400 mt-0.5">
                    {branch.studentCount} Öğrenci · {branch.coachCount} Öğretmen/Koç
                  </p>
                </div>
                <div className="text-right font-mono text-xs space-y-0.5">
                  <span className="block font-bold text-primary-700">
                    {branch.averageNet} Net Ort.
                  </span>
                  <span className="text-success font-semibold">
                    %{branch.averageAdherence} Uyum
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* 6. Exam Performances Table */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-5 shadow-soft-sm space-y-4">
        <h2 className="font-semibold text-neutral-800 text-sm flex items-center gap-2">
          <GraduationCap className="h-4 w-4 text-primary-600" />
          Son Sınav Sonuçları
        </h2>

        {data.examPerformances.length === 0 ? (
          <p className="text-xs text-neutral-400 py-6 text-center">
            Görüntülenecek sınav sonucu bulunamadı.
          </p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs" id="table-exam-performances">
              <thead>
                <tr className="border-b border-neutral-100 text-neutral-400 font-semibold">
                  <th className="pb-2">Sınav Adı</th>
                  <th className="pb-2">Şube</th>
                  <th className="pb-2">Öğrenci</th>
                  <th className="pb-2 text-right">Net</th>
                  <th className="pb-2 text-right">Puan</th>
                  <th className="pb-2 text-right">Tarih</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-neutral-50 font-mono">
                {data.examPerformances.slice(0, 10).map((row) => (
                  <tr key={row.attemptId} className="hover:bg-neutral-50 transition-colors">
                    <td className="py-2.5 font-sans font-medium text-neutral-900">{row.examTitle}</td>
                    <td className="py-2.5 text-neutral-600 font-sans">{row.classroomName}</td>
                    <td className="py-2.5 text-neutral-900 font-sans">{row.studentName}</td>
                    <td className="py-2.5 text-right font-bold text-primary-700">{row.totalNet.toFixed(2)}</td>
                    <td className="py-2.5 text-right text-neutral-700">{row.score.toFixed(2)}</td>
                    <td className="py-2.5 text-right text-neutral-400 font-sans">
                      {new Date(row.takenAt).toLocaleDateString('tr-TR')}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
};
