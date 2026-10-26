import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  Search,
  Download,
  AlertCircle,
  RefreshCw,
  FolderOpen,
  Calendar,
  Layers,
  FileCheck,
  CheckCircle2,
  X,
  Loader2,
} from 'lucide-react';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { contentService, StudentMaterialDto } from '@/services/contentService';

const MATERIAL_TYPE_LABELS: Record<string, string> = {
  fasikul: 'Konu Fasikülü',
  test: 'Kazanım Testi',
  soru_bankasi: 'Soru Bankası',
  deneme: 'Deneme Sınavı',
  ozet: 'Ders Özeti',
  calisma_yapraki: 'Çalışma Yaprağı',
};

const formatFileSize = (bytes?: number): string | null => {
  if (bytes === undefined || bytes === null || isNaN(bytes) || bytes <= 0) {
    return null;
  }
  if (bytes < 1024) {
    return `${bytes} B`;
  }
  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

export const StudentMaterialsView: React.FC = () => {
  const [materials, setMaterials] = useState<StudentMaterialDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);
  const [statusFeedback, setStatusFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);

  const [searchQuery, setSearchQuery] = useState('');
  const [selectedSubject, setSelectedSubject] = useState<string>('all');
  const [typeFilter, setTypeFilter] = useState('');

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const data = await contentService.getStudentMaterials();
      setMaterials(data || []);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Ders materyalleri yüklenemedi.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Extract unique subjects from actual assigned materials
  const availableSubjects = useMemo(() => {
    const list = Array.from(new Set(materials.map((m) => m.subject?.trim()).filter(Boolean)));
    return list.sort((a, b) => a.localeCompare(b, 'tr-TR'));
  }, [materials]);

  // Extract unique material types actually present in loaded list
  const availableTypes = useMemo(() => {
    return Array.from(new Set(materials.map((m) => m.materialType?.trim()).filter(Boolean)));
  }, [materials]);

  const filteredMaterials = useMemo(() => {
    const q = searchQuery.toLowerCase().trim();
    return materials.filter((m) => {
      const titleMatch = m.title?.toLowerCase().includes(q) ?? false;
      const topicMatch = m.topic?.toLowerCase().includes(q) ?? false;
      const subjectMatch = m.subject?.toLowerCase().includes(q) ?? false;
      const matchesSearch = !q || titleMatch || topicMatch || subjectMatch;

      const matchesSubject = selectedSubject === 'all' || m.subject === selectedSubject;
      const matchesType = !typeFilter || m.materialType === typeFilter;

      return matchesSearch && matchesSubject && matchesType;
    });
  }, [materials, searchQuery, selectedSubject, typeFilter]);

  const handleDownload = async (mat: StudentMaterialDto) => {
    if (downloadingId === mat.id) return;
    try {
      setDownloadingId(mat.id);
      setStatusFeedback(null);
      await contentService.downloadMaterial(mat.id, mat.originalFileName || `${mat.title}.pdf`);
      setStatusFeedback({
        type: 'success',
        message: `"${mat.title}" başarıyla indirildi.`,
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Materyal indirilemedi veya erişiminiz kısıtlanmış olabilir.';
      setStatusFeedback({
        type: 'error',
        message: msg,
      });
    } finally {
      setDownloadingId(null);
    }
  };

  // Loading skeleton state
  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none" data-testid="student-materials-loading">
        <div className="border-b border-neutral-200/60 pb-3.5 sm:pb-4">
          <Skeleton className="h-8 w-60 rounded-xl" />
          <Skeleton className="h-4 w-96 rounded-lg mt-1" />
        </div>
        <div className="flex gap-2">
          <Skeleton className="h-9 w-24 rounded-full" />
          <Skeleton className="h-9 w-24 rounded-full" />
          <Skeleton className="h-9 w-24 rounded-full" />
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3.5 sm:gap-4">
          <Skeleton className="h-48 w-full rounded-2xl" />
          <Skeleton className="h-48 w-full rounded-2xl" />
          <Skeleton className="h-48 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error alert state
  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4 select-none" data-testid="student-materials-error">
        <div className="border-b border-neutral-200/60 pb-3.5 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Ders Materyallerim
          </h1>
          <p className="text-xs sm:text-sm text-neutral-500 mt-0.5">
            Sınıfınıza öğretmenleriniz tarafından atanan konu anlatımları ve testler
          </p>
        </div>
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Materyaller Yüklenemedi">
          {errorMessage}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={loadData} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto space-y-4 sm:space-y-5 select-none" data-testid="student-materials-view">
      {/* 1. Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-neutral-200/60 pb-3.5 sm:pb-4">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
              Ders Materyallerim
            </h1>
            <span className="text-xs font-bold text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-full border border-primary-100">
              {materials.length} Materyal
            </span>
          </div>
          <p className="text-xs sm:text-sm text-neutral-500 mt-0.5">
            Sınıfınıza öğretmenleriniz tarafından atanan konu anlatımları, fasiküller ve çalışma yaprakları
          </p>
        </div>
      </div>

      {/* Inline Feedback Toast */}
      {statusFeedback && (
        <div
          className={`flex items-center justify-between p-3 rounded-xl border text-xs font-medium animate-fadeIn ${
            statusFeedback.type === 'success'
              ? 'bg-emerald-50 text-emerald-800 border-emerald-200'
              : 'bg-rose-50 text-rose-800 border-rose-200'
          }`}
        >
          <div className="flex items-center gap-2">
            {statusFeedback.type === 'success' ? (
              <CheckCircle2 className="h-4 w-4 text-emerald-600 shrink-0" />
            ) : (
              <AlertCircle className="h-4 w-4 text-rose-600 shrink-0" />
            )}
            <span>{statusFeedback.message}</span>
          </div>
          <button
            type="button"
            onClick={() => setStatusFeedback(null)}
            className="p-1 hover:bg-black/5 rounded text-neutral-500"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        </div>
      )}

      {/* 2. Filter Ribbon: Search Bar & Type Dropdown */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-3 sm:p-3.5 shadow-soft-xs flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-2.5">
        <div className="relative flex-1">
          <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-neutral-400" />
          <input
            type="text"
            placeholder="Materyal, konu veya ders ara..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full text-xs sm:text-sm bg-neutral-50 border border-neutral-200/80 rounded-xl pl-9 pr-4 py-2 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all font-medium"
          />
        </div>

        <div className="w-full sm:w-56 shrink-0">
          <Select
            options={[
              { value: '', label: 'Tüm Materyal Türleri' },
              ...availableTypes.map((typeKey) => ({
                value: typeKey,
                label: MATERIAL_TYPE_LABELS[typeKey] || typeKey,
              })),
            ]}
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            className="text-xs min-h-[38px] py-1 pl-3 pr-7 font-medium"
          />
        </div>
      </div>

      {/* 3. Horizontally Scrollable Real Subject Chips */}
      {availableSubjects.length > 0 && (
        <div className="flex items-center gap-1.5 overflow-x-auto pb-1 scrollbar-none">
          <button
            type="button"
            onClick={() => setSelectedSubject('all')}
            className={`px-3 py-1.5 rounded-full text-xs font-semibold whitespace-nowrap transition-all ${
              selectedSubject === 'all'
                ? 'bg-navy-900 text-white shadow-soft-xs'
                : 'bg-white border border-neutral-200/80 text-neutral-600 hover:bg-neutral-50'
            }`}
          >
            Tümü ({materials.length})
          </button>
          {availableSubjects.map((sub) => {
            const count = materials.filter((m) => m.subject === sub).length;
            const isSelected = selectedSubject === sub;
            return (
              <button
                key={sub}
                type="button"
                onClick={() => setSelectedSubject(sub)}
                className={`px-3 py-1.5 rounded-full text-xs font-semibold whitespace-nowrap transition-all ${
                  isSelected
                    ? 'bg-navy-900 text-white shadow-soft-xs'
                    : 'bg-white border border-neutral-200/80 text-neutral-600 hover:bg-neutral-50'
                }`}
              >
                {sub} ({count})
              </button>
            );
          })}
        </div>
      )}

      {/* 4. Materials Grid */}
      {filteredMaterials.length > 0 ? (
        <div
          className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3.5 sm:gap-4"
          id="student-materials-grid"
        >
          {filteredMaterials.map((mat) => {
            const fileSizeStr = formatFileSize(mat.fileSizeBytes);
            const typeLabel = MATERIAL_TYPE_LABELS[mat.materialType] || mat.materialType;
            const isDownloading = downloadingId === mat.id;

            return (
              <div
                key={mat.id}
                data-testid="student-material-card"
                className="bg-white rounded-2xl border border-neutral-200/80 hover:border-neutral-300 p-4 sm:p-5 shadow-soft-xs hover:shadow-soft-md transition-all flex flex-col justify-between group"
              >
                <div className="space-y-3">
                  {/* Top Bar: Subject Badge & Classroom Sınıf Info */}
                  <div className="flex items-center justify-between gap-2">
                    <span className="text-[11px] font-bold text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-md border border-primary-100">
                      {mat.subject}
                    </span>
                    <span className="text-xs text-neutral-500 font-mono font-medium flex items-center gap-1">
                      <Layers className="h-3 w-3 text-neutral-400" />
                      {mat.classroomName}
                    </span>
                  </div>

                  {/* Title */}
                  <h3 className="font-bold text-sm sm:text-base text-neutral-900 leading-snug group-hover:text-primary-700 transition-colors line-clamp-2">
                    {mat.title}
                  </h3>

                  {/* Topic / Subtitle */}
                  {mat.topic && (
                    <p className="text-xs text-neutral-500 flex items-center gap-1">
                      <FileCheck className="h-3.5 w-3.5 text-neutral-400 shrink-0" />
                      <span className="truncate">Konu: <strong className="font-medium text-neutral-700">{mat.topic}</strong></span>
                    </p>
                  )}

                  {/* Teacher Note Banner */}
                  {mat.teacherNote && (
                    <div className="text-xs bg-amber-50/80 text-amber-900 p-2.5 rounded-xl border border-amber-200/80">
                      <span className="font-bold block mb-0.5 text-[11px] text-amber-800">Öğretmen Notu:</span>
                      <p className="line-clamp-2 leading-relaxed">{mat.teacherNote}</p>
                    </div>
                  )}

                  {/* Metadata: Type Badge & File Size */}
                  <div className="flex flex-wrap items-center gap-2 text-xs text-neutral-400 font-mono pt-1">
                    <span className="bg-neutral-100/90 text-neutral-600 px-2 py-0.5 rounded text-[11px] font-medium border border-neutral-200/50">
                      {typeLabel}
                    </span>
                    {fileSizeStr && (
                      <span className="text-neutral-500 text-[11px] font-medium">
                        {fileSizeStr}
                      </span>
                    )}
                  </div>
                </div>

                {/* Bottom Footer: Date & Download Button */}
                <div className="pt-3 mt-3.5 border-t border-neutral-100 flex items-center justify-between">
                  <span className="text-[11px] text-neutral-400 font-mono flex items-center gap-1">
                    <Calendar className="h-3 w-3" />
                    {new Date(mat.assignedAt).toLocaleDateString('tr-TR')}
                  </span>

                  <button
                    type="button"
                    onClick={() => handleDownload(mat)}
                    disabled={isDownloading}
                    className="inline-flex items-center gap-1.5 text-xs bg-primary-600 hover:bg-primary-700 active:scale-95 text-white px-3.5 py-1.5 rounded-xl font-semibold shadow-soft-xs transition-all disabled:opacity-60 cursor-pointer"
                  >
                    {isDownloading ? (
                      <>
                        <Loader2 className="h-3.5 w-3.5 animate-spin" />
                        İndiriliyor...
                      </>
                    ) : (
                      <>
                        <Download className="h-3.5 w-3.5" />
                        İndir
                      </>
                    )}
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <EmptyState
          icon={<FolderOpen className="h-8 w-8 text-neutral-400" />}
          title={searchQuery || selectedSubject !== 'all' || typeFilter ? 'Aramanıza uygun materyal bulunamadı' : 'Henüz Materyal Yok'}
          description={
            searchQuery || selectedSubject !== 'all' || typeFilter
              ? 'Farklı bir arama terimi deneyebilir veya filtreleri temizleyebilirsiniz.'
              : 'Sınıfınıza öğretmenleriniz tarafından henüz herhangi bir ders materyali veya test atanmamış.'
          }
          action={
            searchQuery || selectedSubject !== 'all' || typeFilter ? (
              <Button
                variant="secondary"
                size="sm"
                onClick={() => {
                  setSearchQuery('');
                  setSelectedSubject('all');
                  setTypeFilter('');
                }}
              >
                Filtreleri Temizle
              </Button>
            ) : undefined
          }
        />
      )}
    </div>
  );
};

export default StudentMaterialsView;
