import React, { useState, useEffect, useCallback } from 'react';
import { Search, FileText, Download, AlertCircle, RefreshCw } from 'lucide-react';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { contentService, StudentMaterialDto } from '@/services/contentService';

export const StudentMaterialsView: React.FC = () => {
  const [materials, setMaterials] = useState<StudentMaterialDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState('');
  const [subjectFilter, setSubjectFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const data = await contentService.getStudentMaterials();
      setMaterials(data);
    } catch (err: any) {
      setErrorMessage(err.response?.data?.detail || err.message || 'Ders materyalleri yüklenemedi.');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadData();
  }, [loadData]);

  if (isLoading) {
    return (
      <div className="max-w-5xl mx-auto space-y-4 select-none">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <Skeleton className="h-44 w-full rounded-2xl" />
          <Skeleton className="h-44 w-full rounded-2xl" />
          <Skeleton className="h-44 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4">
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

  const subjects = Array.from(new Set(materials.map((m) => m.subject).filter(Boolean)));

  const filteredMaterials = materials.filter((m) => {
    const matchesSearch =
      m.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      m.topic.toLowerCase().includes(searchQuery.toLowerCase());

    const matchesSubject = !subjectFilter || m.subject === subjectFilter;
    const matchesType = !typeFilter || m.materialType === typeFilter;

    return matchesSearch && matchesSubject && matchesType;
  });

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* Header */}
      <div>
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
          Ders Materyallerim
        </h1>
        <p className="text-xs text-neutral-400 mt-0.5">
          Öğretmenleriniz tarafından sınıfınıza atanan konu anlatımları, fasiküller ve testler
        </p>
      </div>

      {/* Filter Bar */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-2.5">
        <div className="sm:col-span-2">
          <Input
            placeholder="Materyal veya konu ara..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            leftIcon={<Search className="h-4 w-4 text-neutral-400" />}
          />
        </div>

        <Select
          options={[
            { value: '', label: 'Tüm Dersler' },
            ...subjects.map((s) => ({ value: s, label: s })),
          ]}
          value={subjectFilter}
          onChange={(e) => setSubjectFilter(e.target.value)}
          placeholder="Tüm Dersler"
        />

        <Select
          options={[
            { value: '', label: 'Tüm Türler' },
            { value: 'fasikul', label: 'Konu Fasikülü' },
            { value: 'test', label: 'Kazanım Testi' },
            { value: 'soru_bankasi', label: 'Soru Bankası' },
            { value: 'deneme', label: 'Deneme' },
          ]}
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value)}
          placeholder="Tüm Türler"
        />
      </div>

      {/* Materials List */}
      {filteredMaterials.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4" id="student-materials-grid">
          {filteredMaterials.map((mat) => (
            <div
              key={mat.id}
              className="bg-white rounded-2xl border border-neutral-100 p-4 sm:p-5 shadow-soft-sm hover:shadow-soft-md transition-all flex flex-col justify-between"
            >
              <div className="space-y-2.5">
                <div className="flex items-center justify-between gap-1">
                  <span className="text-[11px] font-bold text-primary-700 bg-primary-50 px-2 py-0.5 rounded-full">
                    {mat.subject}
                  </span>
                  <span className="text-xs text-neutral-400 font-mono">
                    {mat.classroomName}
                  </span>
                </div>

                <h3 className="font-semibold text-sm text-neutral-900 leading-snug">
                  {mat.title}
                </h3>

                {mat.topic && (
                  <p className="text-xs text-neutral-500">
                    Konu: <strong>{mat.topic}</strong>
                  </p>
                )}

                {mat.teacherNote && (
                  <div className="text-xs bg-amber-50 text-amber-800 p-2 rounded-xl border border-amber-200">
                    <span className="font-semibold block mb-0.5">Öğretmen Notu:</span>
                    {mat.teacherNote}
                  </div>
                )}

                <div className="flex items-center gap-3 text-xs text-neutral-400 font-mono pt-1">
                  <span>{(mat.fileSizeBytes / (1024 * 1024)).toFixed(1)} MB</span>
                  <span className="truncate max-w-[140px]" title={mat.originalFileName}>
                    {mat.originalFileName}
                  </span>
                </div>
              </div>

              <div className="pt-3 mt-3 border-t border-neutral-100 flex items-center justify-between">
                <span className="text-[11px] text-neutral-400">
                  {new Date(mat.assignedAt).toLocaleDateString('tr-TR')}
                </span>
                <a
                  href={contentService.downloadMaterialUrl(mat.id)}
                  download={mat.originalFileName}
                  className="inline-flex items-center gap-1 text-xs bg-primary-600 text-white px-3 py-1.5 rounded-xl font-medium hover:bg-primary-700 transition-colors"
                >
                  <Download className="h-3.5 w-3.5" />
                  İndir
                </a>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<FileText className="h-6 w-6 text-neutral-400" />}
          title="Henüz Materyal Yok"
          description="Sınıfınıza atanmış herhangi bir ders materyali veya test bulunmuyor."
        />
      )}
    </div>
  );
};
