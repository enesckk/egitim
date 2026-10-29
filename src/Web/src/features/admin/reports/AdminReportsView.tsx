import React, { useState, useEffect, useCallback, useMemo } from 'react';
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
  Calendar,
  RotateCcw,
  Clock,
  TrendingUp,
} from 'lucide-react';
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

  // Filter Select Options
  const [classrooms, setClassrooms] = useState<ClassroomDto[]>([]);
  const [exams, setExams] = useState<ExamDto[]>([]);

  // Filter Form State
  const [selectedClassroomId, setSelectedClassroomId] = useState<string>('');
  const [selectedExamId, setSelectedExamId] = useState<string>('');
  const [startDate, setStartDate] = useState<string>('');
  const [endDate, setEndDate] = useState<string>('');
  const [dateError, setDateError] = useState<string | null>(null);

  // Active Applied Filters (used for exports and badge displays)
  const [appliedFilters, setAppliedFilters] = useState<ReportFilterParams>({});

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
      setAppliedFilters(filters || {});
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

  const validateDates = (start: string, end: string): boolean => {
    if (start && end && new Date(start) > new Date(end)) {
      setDateError('Başlangıç tarihi bitiş tarihinden sonra olamaz.');
      return false;
    }
    setDateError(null);
    return true;
  };

  const handleApplyFilter = () => {
    if (!validateDates(startDate, endDate)) return;

    loadReport({
      classroomId: selectedClassroomId || undefined,
      examId: selectedExamId || undefined,
      startDate: startDate ? new Date(startDate).toISOString() : undefined,
      endDate: endDate ? new Date(endDate).toISOString() : undefined,
    });
  };

  const handleResetFilters = () => {
    setSelectedClassroomId('');
    setSelectedExamId('');
    setStartDate('');
    setEndDate('');
    setDateError(null);
    loadReport({});
  };

  const hasActiveFilters = Boolean(
    selectedClassroomId || selectedExamId || startDate || endDate || appliedFilters.classroomId || appliedFilters.examId || appliedFilters.startDate || appliedFilters.endDate
  );

  const handleExport = (format: 'xlsx' | 'csv') => {
    const token = authService.getAccessToken();
    const url = reportingService.getExportUrl(format, appliedFilters);

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

  // Active study plan total calculation to distinguish registered students from students with plans
  const totalActivePlanStudents = useMemo(() => {
    if (!data?.adherenceDistribution) return 0;
    return data.adherenceDistribution.reduce((acc, curr) => acc + curr.studentCount, 0);
  }, [data]);

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto space-y-5 select-none p-4 sm:p-6" data-testid="admin-reports-loading">
        <Skeleton className="h-10 w-64 rounded-xl" />
        <Skeleton className="h-14 w-full rounded-2xl" />
        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
          <Skeleton className="h-28 w-full rounded-2xl" />
          <Skeleton className="h-28 w-full rounded-2xl" />
          <Skeleton className="h-28 w-full rounded-2xl" />
          <Skeleton className="h-28 w-full rounded-2xl" />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
          <Skeleton className="h-72 w-full rounded-2xl" />
          <Skeleton className="h-72 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-7xl mx-auto py-10 space-y-4 select-none p-4 sm:p-6" data-testid="admin-reports-error">
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
            onClick={propOnRetry || (() => loadReport(appliedFilters))}
            leftIcon={<RefreshCw className="h-4 w-4" />}
            className="bg-[#315FE8] hover:bg-blue-700 text-white rounded-xl"
          >
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  if (!data) return null;

  return (
    <div className="max-w-7xl mx-auto select-none space-y-5 sm:space-y-6 p-4 sm:p-6" data-testid="admin-reports-view">
      {/* 1. Header & Actions with Ivory/Navy/Blue tokens */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-navy-200/70 pb-5">
        <div>
          <div className="flex items-center gap-2.5 flex-wrap">
            <div className="p-2 rounded-xl bg-blue-50 text-[#315FE8]">
              <BarChart3 className="h-6 w-6" />
            </div>
            <h1 className="font-serif text-2xl sm:text-3xl font-bold text-[#14233E] tracking-tight leading-tight">
              {data.institutionName}
            </h1>
            <span className="text-xs font-bold text-[#315FE8] bg-blue-50 px-2.5 py-0.5 rounded-full border border-blue-100">
              Kurum Analitiği
            </span>
          </div>
          <p className="text-xs sm:text-sm text-navy-600 mt-1 pl-1">
            Gerçek MSSQL deneme sonuçları, şubeler arası başarı ve plan uyumu analizi.
          </p>
        </div>

        <div className="flex items-center gap-2 flex-wrap">
          <Button
            variant="outline"
            size="sm"
            onClick={() => handleExport('csv')}
            leftIcon={<Download className="h-4 w-4 text-[#315FE8]" />}
            id="btn-export-csv"
            data-testid="btn-export-csv"
            className="bg-white border-navy-200/80 text-[#14233E] hover:bg-neutral-50 rounded-xl font-semibold shadow-2xs"
          >
            CSV İndir
          </Button>
          <Button
            variant="primary"
            size="sm"
            onClick={() => handleExport('xlsx')}
            leftIcon={<FileSpreadsheet className="h-4 w-4 text-white" />}
            id="btn-export-xlsx"
            data-testid="btn-export-xlsx"
            className="bg-[#315FE8] hover:bg-blue-700 text-white rounded-xl font-semibold shadow-soft-xs"
          >
            Excel İndir
          </Button>
        </div>
      </div>

      {/* 2. Filters Toolbar with Dates & Reset Button */}
      <div className="bg-[#F8F7F3] rounded-2xl border border-navy-200/80 p-4 sm:p-5 shadow-soft-xs space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2 text-xs font-bold text-[#14233E]">
            <Filter className="h-4 w-4 text-[#315FE8]" />
            <span>Rapor Filtreleri:</span>
          </div>

          {hasActiveFilters && (
            <Button
              variant="ghost"
              size="sm"
              onClick={handleResetFilters}
              id="btn-reset-filters"
              data-testid="btn-reset-filters"
              className="text-xs text-navy-500 hover:text-[#14233E] h-8 px-2.5"
            >
              <RotateCcw className="h-3.5 w-3.5 mr-1" /> Filtreleri Temizle
            </Button>
          )}
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3 pt-1">
          {/* Branch / Classroom Filter */}
          <div>
            <label htmlFor="select-filter-classroom" className="text-[11px] font-bold text-navy-700 block mb-1">
              Şube / Sınıf
            </label>
            <select
              id="select-filter-classroom"
              data-testid="select-filter-classroom"
              value={selectedClassroomId}
              onChange={(e) => setSelectedClassroomId(e.target.value)}
              className="w-full text-xs bg-white border border-navy-200/80 rounded-xl px-3 py-2 text-[#14233E] focus:outline-none focus:ring-2 focus:ring-[#315FE8]/20 focus:border-[#315FE8]"
            >
              <option value="">Tüm Şubeler</option>
              {classrooms.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </div>

          {/* Exam Filter */}
          <div>
            <label htmlFor="select-filter-exam" className="text-[11px] font-bold text-navy-700 block mb-1">
              Sınav
            </label>
            <select
              id="select-filter-exam"
              data-testid="select-filter-exam"
              value={selectedExamId}
              onChange={(e) => setSelectedExamId(e.target.value)}
              className="w-full text-xs bg-white border border-navy-200/80 rounded-xl px-3 py-2 text-[#14233E] focus:outline-none focus:ring-2 focus:ring-[#315FE8]/20 focus:border-[#315FE8]"
            >
              <option value="">Tüm Sınavlar</option>
              {exams.map((e) => (
                <option key={e.id} value={e.id}>
                  {e.title}
                </option>
              ))}
            </select>
          </div>

          {/* Start Date */}
          <div>
            <label htmlFor="input-filter-start-date" className="text-[11px] font-bold text-navy-700 block mb-1">
              Başlangıç Tarihi
            </label>
            <div className="relative">
              <Calendar className="h-3.5 w-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-navy-400 pointer-events-none" />
              <input
                type="date"
                id="input-filter-start-date"
                data-testid="input-filter-start-date"
                value={startDate}
                onChange={(e) => {
                  setStartDate(e.target.value);
                  validateDates(e.target.value, endDate);
                }}
                className="w-full pl-8 pr-3 py-2 text-xs bg-white border border-navy-200/80 rounded-xl text-[#14233E] focus:outline-none focus:ring-2 focus:ring-[#315FE8]/20 focus:border-[#315FE8]"
              />
            </div>
          </div>

          {/* End Date */}
          <div>
            <label htmlFor="input-filter-end-date" className="text-[11px] font-bold text-navy-700 block mb-1">
              Bitiş Tarihi
            </label>
            <div className="relative">
              <Calendar className="h-3.5 w-3.5 absolute left-3 top-1/2 -translate-y-1/2 text-navy-400 pointer-events-none" />
              <input
                type="date"
                id="input-filter-end-date"
                data-testid="input-filter-end-date"
                value={endDate}
                onChange={(e) => {
                  setEndDate(e.target.value);
                  validateDates(startDate, e.target.value);
                }}
                className="w-full pl-8 pr-3 py-2 text-xs bg-white border border-navy-200/80 rounded-xl text-[#14233E] focus:outline-none focus:ring-2 focus:ring-[#315FE8]/20 focus:border-[#315FE8]"
              />
            </div>
          </div>

          {/* Apply Filter Button */}
          <div className="flex items-end">
            <Button
              variant="primary"
              size="sm"
              onClick={handleApplyFilter}
              id="btn-apply-filter"
              data-testid="btn-apply-filter"
              className="w-full h-[38px] bg-[#315FE8] hover:bg-blue-700 text-white rounded-xl font-semibold shadow-soft-xs text-xs"
            >
              Uygula
            </Button>
          </div>
        </div>

        {dateError && (
          <p className="text-xs font-semibold text-rose-600 pt-1" data-testid="date-filter-error">
            {dateError}
          </p>
        )}
      </div>

      {/* 3. Summary KPIs: 2 cols on mobile/tablet, 4 on desktop */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4" data-testid="reports-kpi-grid">
        {/* KPI 1: Registered Students */}
        <div className="bg-white rounded-2xl border border-navy-200/80 p-4 sm:p-5 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-xs text-navy-500">
            <span className="font-semibold text-navy-700">Kayıtlı Öğrenci</span>
            <div className="p-1.5 rounded-lg bg-blue-50 text-[#315FE8]">
              <Users className="h-4 w-4" />
            </div>
          </div>
          <div className="mt-2.5">
            <p className="text-xl sm:text-2xl font-bold font-mono text-[#14233E]" data-testid="kpi-total-students">
              {data.totalStudents}
            </p>
            <p className="text-[11px] text-navy-400 mt-0.5">
              Aktif planlı: <strong className="text-navy-700 font-semibold">{totalActivePlanStudents}</strong>
            </p>
          </div>
        </div>

        {/* KPI 2: Total Exam Attempts */}
        <div className="bg-white rounded-2xl border border-navy-200/80 p-4 sm:p-5 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-xs text-navy-500">
            <span className="font-semibold text-navy-700">Toplam Giriş</span>
            <div className="p-1.5 rounded-lg bg-indigo-50 text-indigo-600">
              <GraduationCap className="h-4 w-4" />
            </div>
          </div>
          <div className="mt-2.5">
            <p className="text-xl sm:text-2xl font-bold font-mono text-[#14233E]" data-testid="kpi-total-attempts">
              {data.totalAttempts}
            </p>
            <p className="text-[11px] text-navy-400 mt-0.5">
              İncelenen sınav: <strong className="text-navy-700 font-semibold">{data.totalExams}</strong>
            </p>
          </div>
        </div>

        {/* KPI 3: Average Net */}
        <div className="bg-white rounded-2xl border border-navy-200/80 p-4 sm:p-5 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-xs text-navy-500">
            <span className="font-semibold text-navy-700">Ortalama Net</span>
            <div className="p-1.5 rounded-lg bg-emerald-50 text-emerald-600">
              <Target className="h-4 w-4" />
            </div>
          </div>
          <div className="mt-2.5">
            <p className="text-xl sm:text-2xl font-bold font-mono text-[#14233E]" data-testid="kpi-average-net">
              {data.overallAverageNet.toFixed(2)}
            </p>
            <p className="text-[11px] text-emerald-600 font-medium mt-0.5 flex items-center gap-1">
              <TrendingUp className="h-3 w-3" /> Kurum Genel Ortalaması
            </p>
          </div>
        </div>

        {/* KPI 4: Plan Adherence */}
        <div className="bg-white rounded-2xl border border-navy-200/80 p-4 sm:p-5 shadow-soft-xs flex flex-col justify-between">
          <div className="flex items-center justify-between text-xs text-navy-500">
            <span className="font-semibold text-navy-700">Ort. Plan Uyumu</span>
            <div className="p-1.5 rounded-lg bg-amber-50 text-amber-600">
              <BarChart3 className="h-4 w-4" />
            </div>
          </div>
          <div className="mt-2.5">
            <p className="text-xl sm:text-2xl font-bold font-mono text-[#14233E]" data-testid="kpi-average-adherence">
              %{data.overallAverageAdherence.toFixed(1)}
            </p>
            <p className="text-[11px] text-navy-400 mt-0.5">
              {totalActivePlanStudents > 0 ? `${totalActivePlanStudents} aktif plandan` : 'Aktif plan bulunamadı'}
            </p>
          </div>
        </div>
      </div>

      {/* 4. Subject Averages Section */}
      <div className="bg-white rounded-2xl border border-navy-200/80 p-4 sm:p-5 shadow-soft-xs space-y-4">
        <div className="flex items-center justify-between border-b border-navy-100 pb-3">
          <h2 className="font-bold text-[#14233E] text-sm sm:text-base flex items-center gap-2">
            <BarChart3 className="h-4 w-4 text-[#315FE8]" />
            Ders Bazlı Başarı & Net Ortalamaları
          </h2>
          <span className="text-xs text-navy-500 font-medium">
            {data.subjectAverages.length} Ders Kaydı
          </span>
        </div>

        {data.subjectAverages.length === 0 ? (
          <div className="p-6 text-center bg-[#F8F7F3] rounded-xl border border-navy-200/60 text-navy-500 text-xs font-medium">
            Seçili filtrelere uygun ders sınav sonucu bulunamadı.
          </div>
        ) : (
          <div className="grid grid-cols-2 sm:grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-3.5">
            {data.subjectAverages.map((sa) => (
              <div
                key={sa.subjectName}
                className="p-3.5 rounded-xl bg-[#F8F7F3] border border-navy-200/70 space-y-1.5 hover:border-[#315FE8]/30 transition-colors"
              >
                <span className="text-xs font-bold text-[#14233E] block truncate">
                  {sa.subjectName}
                </span>
                <span className="text-lg font-bold font-mono text-[#315FE8] block">
                  {sa.averageNet.toFixed(2)} Net
                </span>
                <div className="text-[11px] text-navy-500 flex items-center justify-between font-mono pt-0.5">
                  <span className="text-emerald-700 font-semibold">{sa.averageCorrect.toFixed(1)} D</span>
                  <span className="text-rose-700 font-semibold">{sa.averageWrong.toFixed(1)} Y</span>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {/* 5. Grid: Adherence Distribution & Branch Comparisons */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 sm:gap-6">
        {/* Adherence Distribution */}
        <div className="bg-white rounded-2xl border border-navy-200/80 p-4 sm:p-5 shadow-soft-xs space-y-4">
          <div className="flex items-center justify-between pb-3 border-b border-navy-100">
            <div className="flex items-center gap-2">
              <BarChart3 className="h-4 w-4 text-[#315FE8]" />
              <h2 className="font-bold text-[#14233E] text-sm sm:text-base">
                Öğrenci Plan Uyumu Dağılımı
              </h2>
            </div>
            <span className="text-xs text-navy-500 font-medium">
              Toplam {totalActivePlanStudents} Plan
            </span>
          </div>

          {data.adherenceDistribution.length === 0 || totalActivePlanStudents === 0 ? (
            <div className="p-6 text-center bg-[#F8F7F3] rounded-xl border border-navy-200/60 text-navy-500 text-xs font-medium">
              Kurumda aktif çalışma planı kaydı bulunmuyor.
            </div>
          ) : (
            <div className="space-y-4 pt-1">
              {data.adherenceDistribution.map((item) => {
                let barColor = 'bg-rose-500';
                if (item.range.includes('80') || item.range.includes('Yüksek')) {
                  barColor = 'bg-emerald-500';
                } else if (item.range.includes('50') || item.range.includes('Orta')) {
                  barColor = 'bg-amber-500';
                }

                return (
                  <div key={item.range} className="space-y-1.5">
                    <div className="flex items-center justify-between text-xs">
                      <span className="font-semibold text-navy-800">{item.range}</span>
                      <span className="font-mono text-[#14233E] font-bold">
                        {item.studentCount} Öğrenci (%{item.percentage.toFixed(1)})
                      </span>
                    </div>
                    <div className="h-2.5 w-full bg-[#F8F7F3] border border-navy-100 rounded-full overflow-hidden">
                      <div
                        className={`h-full rounded-full transition-all duration-500 ${barColor}`}
                        style={{ width: `${Math.min(100, item.percentage)}%` }}
                      />
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        {/* Branch Comparisons */}
        <div className="bg-white rounded-2xl border border-navy-200/80 p-4 sm:p-5 shadow-soft-xs space-y-4">
          <div className="flex items-center justify-between pb-3 border-b border-navy-100">
            <div className="flex items-center gap-2">
              <Building2 className="h-4 w-4 text-[#315FE8]" />
              <h2 className="font-bold text-[#14233E] text-sm sm:text-base">
                Şubeler Arası Başarı Karşılaştırması
              </h2>
            </div>
            <span className="text-xs text-navy-500 font-medium">
              {data.branchComparisons.length} Şube
            </span>
          </div>

          {data.branchComparisons.length === 0 ? (
            <div className="p-6 text-center bg-[#F8F7F3] rounded-xl border border-navy-200/60 text-navy-500 text-xs font-medium">
              Kuruma kayıtlı şube bulunamadı.
            </div>
          ) : (
            <div className="divide-y divide-navy-100 max-h-72 overflow-y-auto pr-1">
              {data.branchComparisons.map((branch) => (
                <div
                  key={branch.classroomId}
                  className="py-3 flex items-center justify-between gap-3 hover:bg-[#F8F7F3]/70 px-2 rounded-xl transition-colors"
                >
                  <div>
                    <h4 className="text-xs sm:text-sm font-bold text-[#14233E]">
                      {branch.branchName}
                    </h4>
                    <p className="text-[11px] text-navy-500 mt-0.5">
                      {branch.studentCount} Öğrenci · {branch.coachCount} Öğretmen/Koç
                    </p>
                  </div>
                  <div className="text-right font-mono text-xs space-y-0.5 shrink-0">
                    <span className="block font-bold text-[#315FE8]">
                      {branch.averageNet.toFixed(2)} Net Ort.
                    </span>
                    <span className="text-emerald-700 font-semibold block text-[11px]">
                      %{branch.averageAdherence.toFixed(1)} Uyum
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* 6. Recent Exam Results Section (Table on >=1024px, Cards on <1024px) */}
      <div className="bg-white rounded-2xl border border-navy-200/80 p-4 sm:p-5 shadow-soft-xs space-y-4">
        <div className="flex items-center justify-between pb-3 border-b border-navy-100">
          <h2 className="font-bold text-[#14233E] text-sm sm:text-base flex items-center gap-2">
            <GraduationCap className="h-4 w-4 text-[#315FE8]" />
            Son Sınav Sonuçları
          </h2>
          <span className="text-xs text-navy-500 font-medium">
            {data.examPerformances.length > 0 ? `İlk ${Math.min(10, data.examPerformances.length)} Sonuç Gösteriliyor` : 'Kayıt Yok'}
          </span>
        </div>

        {data.examPerformances.length === 0 ? (
          <div className="p-8 text-center bg-[#F8F7F3] rounded-xl border border-navy-200/60 text-navy-500 text-xs font-medium">
            Görüntülenecek sınav sonucu bulunamadı. Filtre parametrelerini kontrol ediniz.
          </div>
        ) : (
          <>
            {/* Desktop Table View (>= 1024px) */}
            <div className="hidden lg:block overflow-x-auto">
              <table className="w-full text-left text-xs" id="table-exam-performances" data-testid="table-exam-performances">
                <thead>
                  <tr className="border-b border-navy-200/80 text-navy-500 font-bold uppercase tracking-wider text-[11px]">
                    <th className="pb-3">Sınav Adı</th>
                    <th className="pb-3">Şube</th>
                    <th className="pb-3">Öğrenci</th>
                    <th className="pb-3 text-right">Net</th>
                    <th className="pb-3 text-right">Puan</th>
                    <th className="pb-3 text-right">Tarih</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-navy-100/70 font-mono">
                  {data.examPerformances.slice(0, 10).map((row) => (
                    <tr key={row.attemptId} className="hover:bg-[#F8F7F3]/70 transition-colors">
                      <td className="py-3 font-sans font-semibold text-[#14233E]">{row.examTitle}</td>
                      <td className="py-3 text-navy-600 font-sans">{row.classroomName}</td>
                      <td className="py-3 text-[#14233E] font-sans font-medium">{row.studentName}</td>
                      <td className="py-3 text-right font-bold text-[#315FE8]">{row.totalNet.toFixed(2)}</td>
                      <td className="py-3 text-right font-bold text-navy-800">{row.score.toFixed(2)}</td>
                      <td className="py-3 text-right text-navy-500 font-sans">
                        {new Date(row.takenAt).toLocaleDateString('tr-TR')}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Tablet & Mobile Cards View (< 1024px) with zero horizontal overflow */}
            <div className="block lg:hidden space-y-3" data-testid="mobile-exam-performances-cards">

              {data.examPerformances.slice(0, 10).map((row) => (
                <div
                  key={row.attemptId}
                  className="p-3.5 rounded-xl bg-[#F8F7F3] border border-navy-200/80 space-y-2"
                >
                  <div className="flex items-start justify-between gap-2">
                    <div>
                      <h4 className="text-xs font-bold text-[#14233E]">{row.examTitle}</h4>
                      <p className="text-[11px] text-navy-500 font-sans">
                        {row.classroomName} · {row.studentName}
                      </p>
                    </div>
                    <span className="text-[11px] font-mono text-navy-400 shrink-0 flex items-center gap-1">
                      <Clock className="h-3 w-3" />
                      {new Date(row.takenAt).toLocaleDateString('tr-TR')}
                    </span>
                  </div>

                  <div className="flex items-center justify-between pt-2 border-t border-navy-200/50 text-xs font-mono">
                    <span className="text-navy-600">
                      Puan: <strong className="text-navy-900 font-bold">{row.score.toFixed(2)}</strong>
                    </span>
                    <span className="text-sm font-bold text-[#315FE8]">
                      {row.totalNet.toFixed(2)} Net
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </>
        )}
      </div>
    </div>
  );
};
