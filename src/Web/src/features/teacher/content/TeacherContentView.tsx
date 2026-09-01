import React, { useState } from 'react';
import { Search, FileText, Share2, AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Modal } from '@/components/ui/Modal';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { initialTeacherContentData } from './mockData';
import { TeacherContentItem, TeacherContentViewModel } from './types';

export interface TeacherContentViewProps {
  initialData?: TeacherContentViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherContentView: React.FC<TeacherContentViewProps> = ({
  initialData = initialTeacherContentData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const [searchQuery, setSearchQuery] = useState('');
  const [subjectFilter, setSubjectFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [selectedMaterial, setSelectedMaterial] = useState<TeacherContentItem | null>(null);
  const [targetClass, setTargetClass] = useState('11-A Sayısal');

  // Loading State
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

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-5xl mx-auto py-8 space-y-4">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Materyaller Yüklenemedi">
          {errorMessage}
        </Alert>
        {onRetry && (
          <div className="text-center pt-2">
            <Button variant="primary" size="sm" onClick={onRetry} leftIcon={<RefreshCw className="h-4 w-4" />}>
              Tekrar Dene
            </Button>
          </div>
        )}
      </div>
    );
  }

  const filteredMaterials = initialData.materials.filter((m) => {
    const matchesSearch =
      m.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
      m.topic.toLowerCase().includes(searchQuery.toLowerCase());

    const matchesSubject = !subjectFilter || m.subject === subjectFilter;
    const matchesType = !typeFilter || m.type === typeFilter;

    return matchesSearch && matchesSubject && matchesType;
  });

  return (
    <div className="max-w-5xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* Header */}
      <div>
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
          Ders Materyalleri & Testler
        </h1>
        <p className="text-xs text-neutral-400 mt-0.5">
          Branşınıza ait müfredat fasikülleri, kazanım kavrama testleri ve soru bankaları
        </p>
      </div>

      {/* Filter Bar */}
      <div className="grid grid-cols-1 sm:grid-cols-4 gap-2.5">
        <div className="sm:col-span-2">
          <Input
            placeholder="Materyal adı veya konu ara..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            leftIcon={<Search className="h-4 w-4 text-neutral-400" />}
          />
        </div>

        <Select
          options={[
            { value: 'Matematik', label: 'Matematik' },
            { value: 'Fizik', label: 'Fizik' },
          ]}
          value={subjectFilter}
          onChange={(e) => setSubjectFilter(e.target.value)}
          placeholder="Tüm Branşlar"
        />

        <Select
          options={[
            { value: 'fasikul', label: 'Konu Fasikülü' },
            { value: 'test', label: 'Kazanım Testi' },
            { value: 'soru_bankasi', label: 'Soru Bankası' },
          ]}
          value={typeFilter}
          onChange={(e) => setTypeFilter(e.target.value)}
          placeholder="Tüm Türler"
        />
      </div>

      {/* Materials Grid */}
      {filteredMaterials.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
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
                    {mat.grade}
                  </span>
                </div>

                <h3 className="font-semibold text-sm text-neutral-900 leading-snug">
                  {mat.title}
                </h3>

                <p className="text-xs text-neutral-500">
                  Konu: <strong>{mat.topic}</strong>
                </p>

                <div className="flex items-center gap-3 text-xs text-neutral-400 font-mono pt-1">
                  {mat.pageCount && <span>{mat.pageCount} Sayfa</span>}
                  {mat.questionCount && <span>{mat.questionCount} Soru</span>}
                  {mat.durationMinutes && <span>~{mat.durationMinutes} dk</span>}
                </div>
              </div>

              <div className="pt-3 mt-3 border-t border-neutral-100 flex items-center justify-between">
                <span className="text-[11px] text-neutral-400">
                  {mat.assignedClassesCount} Şubede Aktif
                </span>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setSelectedMaterial(mat)}
                  leftIcon={<Share2 className="h-3.5 w-3.5" />}
                  className="text-xs"
                >
                  Şubeye Ata
                </Button>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<FileText className="h-6 w-6 text-neutral-400" />}
          title="Materyal Bulunamadı"
          description="Seçilen filtrelere uygun ders materyali veya test kaydı bulunmuyor."
        />
      )}

      {/* Share / Assign Modal */}
      {selectedMaterial && (
        <Modal
          isOpen={!!selectedMaterial}
          onClose={() => setSelectedMaterial(null)}
          title="Materyali Şubeye Ata"
          subtitle={selectedMaterial.title}
          headerVariant="dark"
          footer={
            <div className="flex justify-end gap-2 w-full">
              <Button variant="secondary" size="sm" onClick={() => setSelectedMaterial(null)}>
                İptal
              </Button>
              <Button
                variant="primary"
                size="sm"
                onClick={() => setSelectedMaterial(null)}
              >
                Atamayı Onayla
              </Button>
            </div>
          }
        >
          <div className="space-y-3 select-none">
            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">
                Hedef Sınıf / Şube
              </label>
              <select
                value={targetClass}
                onChange={(e) => setTargetClass(e.target.value)}
                className="w-full text-xs bg-white border border-neutral-200 rounded-xl px-3 py-2 text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                <option value="11-A Sayısal">11-A Sayısal (22 Öğrenci)</option>
                <option value="12-B Sayısal">12-B Sayısal (18 Öğrenci)</option>
                <option value="10-C Karma">10-C Karma (24 Öğrenci)</option>
              </select>
            </div>

            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">
                Öğretmen Açıklaması
              </label>
              <textarea
                rows={2}
                placeholder="Örn: Bu fasikülü cuma gününe kadar bitirip yapamadığınız soruları işaretleyin."
                className="w-full text-xs bg-surface-alt border border-neutral-200 rounded-xl p-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
              />
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};
