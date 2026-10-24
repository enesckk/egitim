import React, { useState, useEffect, useCallback, useRef } from 'react';
import { Search, FileText, Share2, AlertCircle, RefreshCw, Plus, Upload, Download, CheckCircle2 } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Modal } from '@/components/ui/Modal';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { contentService, TeacherMaterialDto } from '@/services/contentService';
import { teacherService } from '@/services/teacherService';
import { ClassroomDto } from '@/services/adminService';

export const TeacherContentView: React.FC = () => {
  const [materials, setMaterials] = useState<TeacherMaterialDto[]>([]);
  const [classrooms, setClassrooms] = useState<ClassroomDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const [searchQuery, setSearchQuery] = useState('');
  const [subjectFilter, setSubjectFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');

  // Modals
  const [selectedMaterial, setSelectedMaterial] = useState<TeacherMaterialDto | null>(null);
  const [targetClassroomId, setTargetClassroomId] = useState('');
  const [teacherNote, setTeacherNote] = useState('');
  const [isAssigning, setIsAssigning] = useState(false);

  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
  const [uploadTitle, setUploadTitle] = useState('');
  const [uploadSubject, setUploadSubject] = useState('Matematik');
  const [uploadTopic, setUploadTopic] = useState('');
  const [uploadGrade] = useState('11. Sınıf');
  const [uploadType, setUploadType] = useState('fasikul');
  const [uploadDescription, setUploadDescription] = useState('');
  const [uploadSelectedFile, setUploadSelectedFile] = useState<File | null>(null);
  const [uploadTargetClass, setUploadTargetClass] = useState('');
  const [isUploading, setIsUploading] = useState(false);
  const [actionSuccess, setActionSuccess] = useState<string | null>(null);

  const fileInputRef = useRef<HTMLInputElement>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const [mats, classes] = await Promise.all([
        contentService.getTeacherMaterials(),
        teacherService.listClassrooms(),
      ]);
      setMaterials(mats);
      setClassrooms(classes);
      if (classes.length > 0 && !targetClassroomId) {
        setTargetClassroomId(classes[0].id);
      }
    } catch (err: any) {
      setErrorMessage(err.response?.data?.detail || err.message || 'Materyaller yüklenirken bir hata oluştu.');
    } finally {
      setIsLoading(false);
    }
  }, [targetClassroomId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const handleAssign = async () => {
    if (!selectedMaterial || !targetClassroomId) return;
    setIsAssigning(true);
    try {
      await contentService.assignToClassroom(selectedMaterial.id, targetClassroomId, teacherNote);
      setActionSuccess(`"${selectedMaterial.title}" şubeye başarıyla atandı.`);
      setSelectedMaterial(null);
      setTeacherNote('');
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.detail || err.message || 'Şubeye atama başarısız oldu.');
    } finally {
      setIsAssigning(false);
    }
  };

  const handleRevoke = async (materialId: string, classroomId: string) => {
    if (!confirm('Bu şubenin materyal erişimini sonlandırmak istediğinizden emin misiniz?')) return;
    try {
      await contentService.revokeAssignment(materialId, classroomId);
      setActionSuccess('Şube erişimi başarıyla geri alındı.');
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.detail || err.message || 'Erişim iptali başarısız oldu.');
    }
  };

  const handleUploadSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!uploadTitle.trim() || !uploadSelectedFile) {
      alert('Lütfen başlık girin ve bir dosya seçin.');
      return;
    }

    setIsUploading(true);
    try {
      const formData = new FormData();
      formData.append('Title', uploadTitle.trim());
      formData.append('Subject', uploadSubject);
      formData.append('Topic', uploadTopic.trim());
      formData.append('Grade', uploadGrade);
      formData.append('MaterialType', uploadType);
      formData.append('Description', uploadDescription.trim());
      formData.append('File', uploadSelectedFile);
      if (uploadTargetClass) {
        formData.append('AssignClassroomIds', uploadTargetClass);
      }

      await contentService.createMaterial(formData);
      setActionSuccess('Yeni materyal başarıyla yüklendi.');
      setIsUploadModalOpen(false);
      setUploadTitle('');
      setUploadTopic('');
      setUploadDescription('');
      setUploadSelectedFile(null);
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.detail || err.message || 'Materyal yüklenirken bir hata oluştu.');
    } finally {
      setIsUploading(false);
    }
  };

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
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
            Ders Materyalleri & Testler
          </h1>
          <p className="text-xs text-neutral-400 mt-0.5">
            Branşınıza ait müfredat fasikülleri, kazanım kavrama testleri ve soru bankaları
          </p>
        </div>
        <Button
          variant="primary"
          size="sm"
          onClick={() => setIsUploadModalOpen(true)}
          leftIcon={<Plus className="h-4 w-4" />}
          id="btn-new-material"
        >
          Yeni Materyal Yükle
        </Button>
      </div>

      {actionSuccess && (
        <Alert variant="success" icon={<CheckCircle2 className="h-5 w-5" />}>
          {actionSuccess}
        </Alert>
      )}

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
            { value: '', label: 'Tüm Branşlar' },
            ...subjects.map((s) => ({ value: s, label: s })),
          ]}
          value={subjectFilter}
          onChange={(e) => setSubjectFilter(e.target.value)}
          placeholder="Tüm Branşlar"
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

                {mat.topic && (
                  <p className="text-xs text-neutral-500">
                    Konu: <strong>{mat.topic}</strong>
                  </p>
                )}

                <div className="flex items-center gap-3 text-xs text-neutral-400 font-mono pt-1">
                  <span>{(mat.fileSizeBytes / (1024 * 1024)).toFixed(1)} MB</span>
                  <span className="truncate max-w-[120px]" title={mat.originalFileName}>
                    {mat.originalFileName}
                  </span>
                </div>

                {mat.assignments && mat.assignments.filter((a) => a.isActive).length > 0 && (
                  <div className="pt-2 border-t border-neutral-100">
                    <p className="text-[11px] font-medium text-neutral-500 mb-1">Atanan Şubeler:</p>
                    <div className="flex flex-wrap gap-1">
                      {mat.assignments
                        .filter((a) => a.isActive)
                        .map((a) => (
                          <span
                            key={a.id}
                            className="inline-flex items-center gap-1 text-[10px] bg-neutral-100 text-neutral-700 px-2 py-0.5 rounded-md"
                          >
                            <span>{a.classroomName || 'Şube'}</span>
                            <button
                              type="button"
                              onClick={() => handleRevoke(mat.id, a.classroomId)}
                              className="text-neutral-400 hover:text-red-500"
                              title="Erişimi İptal Et"
                            >
                              &times;
                            </button>
                          </span>
                        ))}
                    </div>
                  </div>
                )}
              </div>

              <div className="pt-3 mt-3 border-t border-neutral-100 flex items-center justify-between gap-2">
                <a
                  href={contentService.downloadMaterialUrl(mat.id)}
                  download={mat.originalFileName}
                  className="inline-flex items-center gap-1.5 text-xs text-neutral-600 hover:text-primary-600 font-medium"
                >
                  <Download className="h-3.5 w-3.5" />
                  İndir
                </a>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => {
                    setSelectedMaterial(mat);
                    if (classrooms.length > 0) setTargetClassroomId(classrooms[0].id);
                  }}
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
          description="Henüz yüklenmiş ders materyali veya arama kriterlerine uygun kayıt bulunmuyor."
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
                onClick={handleAssign}
                disabled={isAssigning || !targetClassroomId}
              >
                {isAssigning ? 'Atanıyor...' : 'Atamayı Onayla'}
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
                value={targetClassroomId}
                onChange={(e) => setTargetClassroomId(e.target.value)}
                className="w-full text-xs bg-white border border-neutral-200 rounded-xl px-3 py-2 text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
              >
                {classrooms.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.name} ({c.studentCount || 0} Öğrenci)
                  </option>
                ))}
              </select>
            </div>

            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">
                Öğretmen Açıklaması / Notu
              </label>
              <textarea
                rows={2}
                value={teacherNote}
                onChange={(e) => setTeacherNote(e.target.value)}
                placeholder="Örn: Bu fasikülü cuma gününe kadar bitirip yapamadığınız soruları işaretleyin."
                className="w-full text-xs bg-surface-alt border border-neutral-200 rounded-xl p-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
              />
            </div>
          </div>
        </Modal>
      )}

      {/* Upload New Material Modal */}
      {isUploadModalOpen && (
        <Modal
          isOpen={isUploadModalOpen}
          onClose={() => setIsUploadModalOpen(false)}
          title="Yeni Ders Materyali Yükle"
          subtitle="Öğrencileriniz için PDF fasikül, test veya konu anlatımı yükleyin"
          headerVariant="dark"
          footer={
            <div className="flex justify-end gap-2 w-full">
              <Button variant="secondary" size="sm" onClick={() => setIsUploadModalOpen(false)}>
                Vazgeç
              </Button>
              <Button
                variant="primary"
                size="sm"
                onClick={handleUploadSubmit}
                disabled={isUploading || !uploadTitle.trim() || !uploadSelectedFile}
                leftIcon={<Upload className="h-4 w-4" />}
                id="btn-submit-upload"
              >
                {isUploading ? 'Yükleniyor...' : 'Yükle ve Kaydet'}
              </Button>
            </div>
          }
        >
          <div className="space-y-3">
            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">
                Materyal Başlığı *
              </label>
              <Input
                placeholder="Örn: Limit ve Süreklilik Fasikülü"
                value={uploadTitle}
                onChange={(e) => setUploadTitle(e.target.value)}
                id="upload-title-input"
              />
            </div>

            <div className="grid grid-cols-2 gap-2">
              <div>
                <label className="text-xs font-semibold text-neutral-700 block mb-1">Ders / Branş</label>
                <select
                  value={uploadSubject}
                  onChange={(e) => setUploadSubject(e.target.value)}
                  className="w-full text-xs bg-white border border-neutral-200 rounded-xl px-3 py-2 text-neutral-800"
                >
                  <option value="Matematik">Matematik</option>
                  <option value="Fizik">Fizik</option>
                  <option value="Kimya">Kimya</option>
                  <option value="Biyoloji">Biyoloji</option>
                  <option value="Türkçe">Türkçe</option>
                </select>
              </div>

              <div>
                <label className="text-xs font-semibold text-neutral-700 block mb-1">Materyal Türü</label>
                <select
                  value={uploadType}
                  onChange={(e) => setUploadType(e.target.value)}
                  className="w-full text-xs bg-white border border-neutral-200 rounded-xl px-3 py-2 text-neutral-800"
                >
                  <option value="fasikul">Konu Fasikülü</option>
                  <option value="test">Kazanım Testi</option>
                  <option value="soru_bankasi">Soru Bankası</option>
                  <option value="deneme">Deneme Sınavı</option>
                </select>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-2">
              <div>
                <label className="text-xs font-semibold text-neutral-700 block mb-1">Konu</label>
                <Input
                  placeholder="Örn: Fonksiyonlarda Limit"
                  value={uploadTopic}
                  onChange={(e) => setUploadTopic(e.target.value)}
                />
              </div>
              <div>
                <label className="text-xs font-semibold text-neutral-700 block mb-1">Hedef Şube (İsteğe bağlı)</label>
                <select
                  value={uploadTargetClass}
                  onChange={(e) => setUploadTargetClass(e.target.value)}
                  className="w-full text-xs bg-white border border-neutral-200 rounded-xl px-3 py-2 text-neutral-800"
                >
                  <option value="">Doğrudan Atama Yapma</option>
                  {classrooms.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
              </div>
            </div>

            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">
                Dosya Seçimi * (Maks 25 MB: PDF, Word, Excel, PPTX, Resim)
              </label>
              <input
                type="file"
                ref={fileInputRef}
                onChange={(e) => {
                  if (e.target.files && e.target.files[0]) {
                    setUploadSelectedFile(e.target.files[0]);
                  }
                }}
                className="w-full text-xs text-neutral-600 file:mr-3 file:py-1.5 file:px-3 file:rounded-xl file:border-0 file:text-xs file:font-semibold file:bg-primary-50 file:text-primary-700 hover:file:bg-primary-100"
                id="upload-file-input"
              />
              {uploadSelectedFile && (
                <p className="text-[11px] text-neutral-500 mt-1">
                  Seçilen: {uploadSelectedFile.name} ({(uploadSelectedFile.size / (1024 * 1024)).toFixed(2)} MB)
                </p>
              )}
            </div>

            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">Açıklama (İsteğe bağlı)</label>
              <textarea
                rows={2}
                value={uploadDescription}
                onChange={(e) => setUploadDescription(e.target.value)}
                placeholder="Materyal içeriği hakkında kısa not..."
                className="w-full text-xs bg-surface-alt border border-neutral-200 rounded-xl p-2.5 text-neutral-900"
              />
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};
