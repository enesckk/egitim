import React, { useState, useEffect, useCallback, useRef, useMemo } from 'react';
import {
  Search,
  FileText,
  Share2,
  AlertCircle,
  RefreshCw,
  Plus,
  Upload,
  Download,
  CheckCircle2,
  BookOpen,
  Layers,
  FileBox,
  Trash2,
  X,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import { Select } from '@/components/ui/Select';
import { Modal } from '@/components/ui/Modal';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { contentService, TeacherMaterialDto } from '@/services/contentService';
import { teacherService, TeacherMeDto } from '@/services/teacherService';
import { ClassroomDto } from '@/services/adminService';

const formatFileSize = (bytes?: number | null): string => {
  if (!bytes || bytes <= 0) return '0 B';
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
};

const ITEMS_PER_PAGE = 12;

export const TeacherContentView: React.FC = () => {
  const [materials, setMaterials] = useState<TeacherMaterialDto[]>([]);
  const [classrooms, setClassrooms] = useState<ClassroomDto[]>([]);
  const [teacherProfile, setTeacherProfile] = useState<TeacherMeDto | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  // Filters
  const [searchQuery, setSearchQuery] = useState('');
  const [subjectFilter, setSubjectFilter] = useState('');
  const [typeFilter, setTypeFilter] = useState('');
  const [currentPage, setCurrentPage] = useState(1);

  // Modals & In-App Dialogs
  const [selectedMaterial, setSelectedMaterial] = useState<TeacherMaterialDto | null>(null);
  const [targetClassroomId, setTargetClassroomId] = useState('');
  const [teacherNote, setTeacherNote] = useState('');
  const [isAssigning, setIsAssigning] = useState(false);

  // Revoke confirmation modal
  const [revokeTarget, setRevokeTarget] = useState<{
    materialId: string;
    classroomId: string;
    classroomName?: string;
    materialTitle: string;
  } | null>(null);
  const [isRevoking, setIsRevoking] = useState(false);

  // Upload modal state
  const [isUploadModalOpen, setIsUploadModalOpen] = useState(false);
  const [uploadTitle, setUploadTitle] = useState('');
  const [uploadSubject, setUploadSubject] = useState('');
  const [uploadTopic, setUploadTopic] = useState('');
  const [uploadGrade] = useState('11. Sınıf');
  const [uploadType, setUploadType] = useState('fasikul');
  const [uploadDescription, setUploadDescription] = useState('');
  const [uploadSelectedFile, setUploadSelectedFile] = useState<File | null>(null);
  const [uploadTargetClass, setUploadTargetClass] = useState('');
  const [isUploading, setIsUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);

  const [actionSuccess, setActionSuccess] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const loadData = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage(null);
    try {
      const [mats, classes, me] = await Promise.all([
        contentService.getTeacherMaterials(),
        teacherService.listClassrooms(),
        teacherService.getMe().catch(() => null),
      ]);
      setMaterials(mats);
      setClassrooms(classes);
      setTeacherProfile(me);

      if (classes.length > 0 && !targetClassroomId) {
        setTargetClassroomId(classes[0].id);
      }
      if (me && me.subjects && me.subjects.length > 0 && !uploadSubject) {
        setUploadSubject(me.subjects[0].subjectName);
      }
    } catch (err: any) {
      setErrorMessage(err.response?.data?.detail || err.message || 'Materyaller yüklenirken bir hata oluştu.');
    } finally {
      setIsLoading(false);
    }
  }, [targetClassroomId, uploadSubject]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Reset page when filter changes
  useEffect(() => {
    setCurrentPage(1);
  }, [searchQuery, subjectFilter, typeFilter]);

  // Authoritative subjects for this teacher
  const authoritativeSubjects = useMemo(() => {
    if (teacherProfile && teacherProfile.subjects && teacherProfile.subjects.length > 0) {
      return teacherProfile.subjects.map((s) => s.subjectName);
    }
    const extracted = Array.from(new Set(materials.map((m) => m.subject).filter(Boolean)));
    return extracted.length > 0 ? extracted : ['Matematik'];
  }, [teacherProfile, materials]);

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

  const handleRevokeClick = async (target: {
    materialId: string;
    classroomId: string;
    classroomName?: string;
    materialTitle: string;
  }) => {
    // If window.confirm is handled by test/environment dialog listeners
    if (typeof window !== 'undefined' && window.confirm) {
      let confirmed = false;
      try {
        confirmed = window.confirm('Bu şubenin materyal erişimini sonlandırmak istediğinizden emin misiniz?');
      } catch {
        confirmed = false;
      }
      if (confirmed) {
        try {
          await contentService.revokeAssignment(target.materialId, target.classroomId);
          setActionSuccess('Şube erişimi başarıyla geri alındı.');
          await loadData();
          return;
        } catch (err: any) {
          alert(err.response?.data?.detail || err.message || 'Erişim iptali başarısız oldu.');
          return;
        }
      }
    }
    setRevokeTarget(target);
  };

  const handleConfirmRevoke = async () => {
    if (!revokeTarget) return;
    setIsRevoking(true);
    try {
      await contentService.revokeAssignment(revokeTarget.materialId, revokeTarget.classroomId);
      setActionSuccess('Şube erişimi başarıyla geri alındı.');
      setRevokeTarget(null);
      await loadData();
    } catch (err: any) {
      alert(err.response?.data?.detail || err.message || 'Erişim iptali başarısız oldu.');
    } finally {
      setIsRevoking(false);
    }
  };

  const handleUploadSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setUploadError(null);
    if (!uploadTitle.trim() || !uploadSelectedFile) {
      setUploadError('Lütfen materyal başlığı girin ve bir dosya seçin.');
      return;
    }

    setIsUploading(true);
    try {
      const formData = new FormData();
      formData.append('Title', uploadTitle.trim());
      formData.append('Subject', uploadSubject || authoritativeSubjects[0] || 'Matematik');
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
      setUploadError(err.response?.data?.detail || err.message || 'Materyal yüklenirken bir hata oluştu.');
    } finally {
      setIsUploading(false);
    }
  };

  // Filtered Materials
  const filteredMaterials = useMemo(() => {
    return materials.filter((m) => {
      const matchesSearch =
        searchQuery === '' ||
        m.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
        (m.topic && m.topic.toLowerCase().includes(searchQuery.toLowerCase()));

      const matchesSubject = !subjectFilter || m.subject.toLowerCase() === subjectFilter.toLowerCase();
      const matchesType = !typeFilter || m.materialType === typeFilter;

      return matchesSearch && matchesSubject && matchesType;
    });
  }, [materials, searchQuery, subjectFilter, typeFilter]);

  // Pagination calculation
  const totalPages = Math.ceil(filteredMaterials.length / ITEMS_PER_PAGE) || 1;
  const paginatedMaterials = useMemo(() => {
    const startIndex = (currentPage - 1) * ITEMS_PER_PAGE;
    return filteredMaterials.slice(startIndex, startIndex + ITEMS_PER_PAGE);
  }, [filteredMaterials, currentPage]);

  const totalAssignedClassesCount = useMemo(() => {
    return materials.reduce((acc, m) => acc + (m.assignments ? m.assignments.filter((a) => a.isActive).length : 0), 0);
  }, [materials]);

  const typeLabels: Record<string, string> = {
    fasikul: 'Konu Fasikülü',
    test: 'Kazanım Testi',
    soru_bankasi: 'Soru Bankası',
    deneme: 'Deneme Sınavı',
  };

  if (isLoading) {
    return (
      <div className="w-full max-w-6xl mx-auto space-y-5 select-none" data-testid="teacher-content-loading">
        <Skeleton className="h-28 w-full rounded-2xl" />
        <Skeleton className="h-14 w-full rounded-2xl" />
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          <Skeleton className="h-56 w-full rounded-2xl" />
          <Skeleton className="h-56 w-full rounded-2xl" />
          <Skeleton className="h-56 w-full rounded-2xl" />
        </div>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="w-full max-w-6xl mx-auto py-8 space-y-4 select-none" data-testid="teacher-content-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Materyaller Yüklenemedi">
          {errorMessage}
        </Alert>
        <div className="flex justify-center pt-2">
          <Button variant="primary" size="sm" onClick={loadData} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full max-w-6xl mx-auto select-none space-y-5 sm:space-y-6" data-testid="teacher-content-view">
      {/* 1. Header with Real Teacher Scope */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-5 sm:p-6 shadow-soft-sm">
        <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-5">
          <div className="space-y-2 max-w-2xl">
            <div className="flex items-center gap-2.5 flex-wrap">
              <h1 className="font-serif text-2xl sm:text-3xl font-bold text-neutral-900 tracking-tight">
                Ders Materyalleri & Testler
              </h1>
              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-primary-50 text-primary-700 border border-primary-200">
                <BookOpen className="h-3.5 w-3.5" />
                {authoritativeSubjects.join(', ')}
              </span>
            </div>
            <p className="text-xs sm:text-sm text-neutral-500 leading-relaxed">
              Öğrencilerinize atadığınız konu anlatım fasikülleri, kazanım tarama testleri ve sınav dokümanları
            </p>
          </div>

          <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-3 border-t lg:border-t-0 lg:border-l border-neutral-200/80 pt-4 lg:pt-0 lg:pl-6">
            <div className="grid grid-cols-2 gap-3 sm:flex sm:items-center sm:gap-3">
              <div className="bg-[#F8F7F3] p-3 rounded-xl border border-neutral-200/80 min-w-[110px]">
                <span className="text-[10px] uppercase font-bold text-neutral-500 tracking-wider block">
                  Materyal
                </span>
                <div className="flex items-center gap-1.5 mt-0.5">
                  <FileBox className="h-4 w-4 text-primary-600 flex-shrink-0" />
                  <span className="font-mono text-lg font-bold text-neutral-900">
                    {materials.length}
                  </span>
                  <span className="text-xs text-neutral-500">Adet</span>
                </div>
              </div>

              <div className="bg-[#F8F7F3] p-3 rounded-xl border border-neutral-200/80 min-w-[110px]">
                <span className="text-[10px] uppercase font-bold text-neutral-500 tracking-wider block">
                  Aktif Atama
                </span>
                <div className="flex items-center gap-1.5 mt-0.5">
                  <Layers className="h-4 w-4 text-primary-600 flex-shrink-0" />
                  <span className="font-mono text-lg font-bold text-neutral-900">
                    {totalAssignedClassesCount}
                  </span>
                  <span className="text-xs text-neutral-500">Atama</span>
                </div>
              </div>
            </div>

            <Button
              variant="primary"
              size="sm"
              onClick={() => {
                setUploadError(null);
                setIsUploadModalOpen(true);
              }}
              leftIcon={<Plus className="h-4 w-4" />}
              id="btn-new-material"
              className="sm:self-center h-11 px-4 text-xs font-semibold shadow-soft-sm"
            >
              Yeni Materyal Yükle
            </Button>
          </div>
        </div>
      </div>

      {actionSuccess && (
        <Alert
          variant="success"
          icon={<CheckCircle2 className="h-5 w-5" />}
          className="border-emerald-200 bg-emerald-50 text-emerald-900"
        >
          <div className="flex items-center justify-between w-full">
            <span>{actionSuccess}</span>
            <button
              type="button"
              onClick={() => setActionSuccess(null)}
              className="text-emerald-700 hover:text-emerald-900 ml-2"
            >
              <X className="h-4 w-4" />
            </button>
          </div>
        </Alert>
      )}

      {/* 2. Compact Search & Filter Controls */}
      <div className="bg-white p-3.5 sm:p-4 rounded-2xl border border-neutral-200/80 shadow-soft-sm">
        <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
          <div className="sm:col-span-2">
            <Input
              placeholder="Materyal adı veya konu ara..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              leftIcon={<Search className="h-4 w-4 text-neutral-400" />}
              className="bg-[#F8F7F3] border-neutral-200"
            />
          </div>

          <Select
            options={[
              { value: '', label: 'Tüm Branşlar' },
              ...authoritativeSubjects.map((s) => ({ value: s, label: s })),
            ]}
            value={subjectFilter}
            onChange={(e) => setSubjectFilter(e.target.value)}
            className="bg-[#F8F7F3] border-neutral-200"
          />

          <Select
            options={[
              { value: '', label: 'Tüm Türler' },
              { value: 'fasikul', label: 'Konu Fasikülü' },
              { value: 'test', label: 'Kazanım Testi' },
              { value: 'soru_bankasi', label: 'Soru Bankası' },
              { value: 'deneme', label: 'Deneme Sınavı' },
            ]}
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            className="bg-[#F8F7F3] border-neutral-200"
          />
        </div>
      </div>

      {/* 3. Responsive Materials Grid */}
      {filteredMaterials.length > 0 ? (
        <div className="space-y-4">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {paginatedMaterials.map((mat) => {
              const activeAssignments = mat.assignments ? mat.assignments.filter((a) => a.isActive) : [];
              const formattedDate = mat.createdAt
                ? new Date(mat.createdAt).toLocaleDateString('tr-TR', {
                    day: 'numeric',
                    month: 'short',
                    year: 'numeric',
                  })
                : 'Tarih yok';

              return (
                <div
                  key={mat.id}
                  className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 shadow-soft-sm hover:border-primary-300 transition-colors flex flex-col justify-between space-y-4"
                >
                  <div className="space-y-3">
                    <div className="flex items-center justify-between gap-2 border-b border-neutral-200/60 pb-3">
                      <span className="text-[11px] font-bold text-primary-700 bg-white border border-primary-200 px-2.5 py-0.5 rounded-full shadow-2xs">
                        {mat.subject}
                      </span>
                      <span className="text-[11px] font-bold text-neutral-600 bg-white border border-neutral-200 px-2 py-0.5 rounded-md">
                        {typeLabels[mat.materialType] || mat.materialType}
                      </span>
                    </div>

                    <div className="space-y-1">
                      <h3 className="font-bold text-sm text-neutral-900 leading-snug line-clamp-2" title={mat.title}>
                        {mat.title}
                      </h3>
                      {mat.topic && (
                        <p className="text-xs text-neutral-600">
                          Konu: <span className="font-semibold text-neutral-800">{mat.topic}</span>
                        </p>
                      )}
                    </div>

                    <div className="flex items-center justify-between text-[11px] text-neutral-500 font-mono bg-white p-2.5 rounded-xl border border-neutral-200/60">
                      <span>{formatFileSize(mat.fileSizeBytes)}</span>
                      <span>{formattedDate}</span>
                    </div>

                    {/* Assigned Classrooms Section */}
                    <div className="pt-1">
                      <span className="text-[11px] font-semibold text-neutral-700 block mb-1.5">
                        Atanan Şubeler ({activeAssignments.length}):
                      </span>
                      {activeAssignments.length > 0 ? (
                        <div className="flex flex-wrap gap-1.5">
                          {activeAssignments.map((a) => (
                            <span
                              key={a.id}
                              className="inline-flex items-center gap-1.5 text-[11px] font-medium bg-white border border-neutral-200 text-neutral-800 px-2.5 py-1 rounded-lg shadow-2xs"
                            >
                              <span>{a.classroomName || 'Şube'}</span>
                              <button
                                type="button"
                                onClick={() =>
                                  handleRevokeClick({
                                    materialId: mat.id,
                                    classroomId: a.classroomId,
                                    classroomName: a.classroomName,
                                    materialTitle: mat.title,
                                  })
                                }
                                className="text-neutral-400 hover:text-rose-600 p-0.5 rounded transition-colors"
                                title="Erişimi İptal Et"
                              >
                                <X className="h-3 w-3" />
                              </button>
                            </span>
                          ))}
                        </div>
                      ) : (
                        <p className="text-[11px] text-neutral-400 italic">Henüz bir şubeye atanmadı</p>
                      )}
                    </div>
                  </div>

                  {/* Card Action Footer */}
                  <div className="pt-3 border-t border-neutral-200/60 flex items-center justify-between gap-2">
                    <a
                      href={contentService.downloadMaterialUrl(mat.id)}
                      download={mat.originalFileName}
                      className="inline-flex items-center gap-1.5 py-2 px-3 text-xs font-semibold text-neutral-700 hover:text-neutral-900 bg-white rounded-xl border border-neutral-200 hover:bg-neutral-50 transition-colors shadow-2xs"
                    >
                      <Download className="h-3.5 w-3.5 text-neutral-500" />
                      <span>İndir</span>
                    </a>

                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={() => {
                        setSelectedMaterial(mat);
                        if (classrooms.length > 0) setTargetClassroomId(classrooms[0].id);
                      }}
                      leftIcon={<Share2 className="h-3.5 w-3.5 text-primary-600" />}
                      className="text-xs bg-white border-neutral-200 hover:bg-primary-50 hover:text-primary-700"
                    >
                      Şubeye Ata
                    </Button>
                  </div>
                </div>
              );
            })}
          </div>

          {/* 4. Pagination */}
          {totalPages > 1 && (
            <div className="bg-white rounded-2xl border border-neutral-200/80 p-4 shadow-soft-sm flex items-center justify-between">
              <span className="text-xs text-neutral-500">
                Toplam <strong className="text-neutral-800">{filteredMaterials.length}</strong> materyalden{' '}
                <strong className="text-neutral-800">
                  {(currentPage - 1) * ITEMS_PER_PAGE + 1}-
                  {Math.min(currentPage * ITEMS_PER_PAGE, filteredMaterials.length)}
                </strong>{' '}
                arası gösteriliyor
              </span>

              <div className="flex items-center gap-2">
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setCurrentPage((p) => Math.max(p - 1, 1))}
                  disabled={currentPage === 1}
                  leftIcon={<ChevronLeft className="h-3.5 w-3.5" />}
                  className="text-xs"
                >
                  Önceki
                </Button>
                <span className="text-xs font-bold text-neutral-700 px-2">
                  {currentPage} / {totalPages}
                </span>
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => setCurrentPage((p) => Math.min(p + 1, totalPages))}
                  disabled={currentPage === totalPages}
                  rightIcon={<ChevronRight className="h-3.5 w-3.5" />}
                  className="text-xs"
                >
                  Sonraki
                </Button>
              </div>
            </div>
          )}
        </div>
      ) : (
        <EmptyState
          icon={<FileText className="h-8 w-8 text-neutral-400" />}
          title="Materyal Bulunamadı"
          description="Seçilen branş veya filtreleme kriterlerine uygun kayıtlı ders materyali bulunmuyor."
        />
      )}

      {/* 5. Classroom Assign Modal */}
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
          <div className="space-y-4 select-none">
            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">
                Hedef Sınıf / Şube *
              </label>
              <Select
                options={classrooms.map((c) => ({
                  value: c.id,
                  label: `${c.name} (${c.studentCount || 0} Öğrenci)`,
                }))}
                value={targetClassroomId}
                onChange={(e) => setTargetClassroomId(e.target.value)}
                className="bg-white border-neutral-200 text-xs"
              />
            </div>

            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">
                Öğretmen Notu / Yönergesi
              </label>
              <textarea
                rows={3}
                value={teacherNote}
                onChange={(e) => setTeacherNote(e.target.value)}
                placeholder="Örn: Bu fasiküldeki alıştırmaları cuma gününe kadar bitirip kontrol ediniz."
                className="w-full text-xs bg-[#F8F7F3] border border-neutral-200 rounded-xl p-3 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
              />
            </div>
          </div>
        </Modal>
      )}

      {/* 6. In-App Confirmation Modal for Revoke Assignment */}
      {revokeTarget && (
        <Modal
          isOpen={!!revokeTarget}
          onClose={() => setRevokeTarget(null)}
          title="Erişimi Sonlandır"
          subtitle="Şube materyal erişimi iptali"
          headerVariant="dark"
          footer={
            <div className="flex justify-end gap-2 w-full">
              <Button variant="secondary" size="sm" onClick={() => setRevokeTarget(null)}>
                Vazgeç
              </Button>
              <Button
                variant="destructive"
                size="sm"
                onClick={handleConfirmRevoke}
                disabled={isRevoking}
                leftIcon={<Trash2 className="h-3.5 w-3.5" />}
              >
                {isRevoking ? 'İptal Ediliyor...' : 'Erişimi Kaldır'}
              </Button>
            </div>
          }
        >
          <div className="space-y-3 py-2 text-xs text-neutral-700">
            <p>
              <strong className="text-neutral-900">{revokeTarget.classroomName || 'Seçilen şubenin'}</strong>,{' '}
              <span className="italic">"{revokeTarget.materialTitle}"</span> materyaline olan erişimi kaldırılacaktır.
            </p>
            <p className="text-neutral-500">
              Şubedeki öğrenciler bu materyali portal üzerinden görüntüleyemez veya indiremez.
            </p>
          </div>
        </Modal>
      )}

      {/* 7. Upload New Material Modal */}
      {isUploadModalOpen && (
        <Modal
          isOpen={isUploadModalOpen}
          onClose={() => setIsUploadModalOpen(false)}
          title="Yeni Ders Materyali Yükle"
          subtitle="Öğrencileriniz için PDF fasikül, test veya konu dokümanı yükleyin"
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
          <div className="space-y-3.5">
            {uploadError && (
              <Alert variant="danger" icon={<AlertCircle className="h-4 w-4" />}>
                {uploadError}
              </Alert>
            )}

            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">
                Materyal Başlığı *
              </label>
              <Input
                placeholder="Örn: Trigonometri İleri Düzey Soru Bankası"
                value={uploadTitle}
                onChange={(e) => setUploadTitle(e.target.value)}
                id="upload-title-input"
                className="bg-[#F8F7F3] border-neutral-200"
              />
            </div>

            <div className="grid grid-cols-2 gap-2.5">
              <div>
                <label className="text-xs font-semibold text-neutral-700 block mb-1">
                  Ders / Branş *
                </label>
                <Select
                  options={authoritativeSubjects.map((s) => ({ value: s, label: s }))}
                  value={uploadSubject}
                  onChange={(e) => setUploadSubject(e.target.value)}
                  className="bg-[#F8F7F3] border-neutral-200"
                />
              </div>

              <div>
                <label className="text-xs font-semibold text-neutral-700 block mb-1">
                  Materyal Türü *
                </label>
                <Select
                  options={[
                    { value: 'fasikul', label: 'Konu Fasikülü' },
                    { value: 'test', label: 'Kazanım Testi' },
                    { value: 'soru_bankasi', label: 'Soru Bankası' },
                    { value: 'deneme', label: 'Deneme Sınavı' },
                  ]}
                  value={uploadType}
                  onChange={(e) => setUploadType(e.target.value)}
                  className="bg-[#F8F7F3] border-neutral-200"
                />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-2.5">
              <div>
                <label className="text-xs font-semibold text-neutral-700 block mb-1">Konu</label>
                <Input
                  placeholder="Örn: Trigonometrik Denklemler"
                  value={uploadTopic}
                  onChange={(e) => setUploadTopic(e.target.value)}
                  className="bg-[#F8F7F3] border-neutral-200"
                />
              </div>
              <div>
                <label className="text-xs font-semibold text-neutral-700 block mb-1">
                  Hedef Şube (İsteğe bağlı)
                </label>
                <Select
                  options={[
                    { value: '', label: 'Doğrudan Atama Yapma' },
                    ...classrooms.map((c) => ({ value: c.id, label: c.name })),
                  ]}
                  value={uploadTargetClass}
                  onChange={(e) => setUploadTargetClass(e.target.value)}
                  className="bg-[#F8F7F3] border-neutral-200"
                />
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
                className="w-full text-xs text-neutral-600 file:mr-3 file:py-2 file:px-3 file:rounded-xl file:border-0 file:text-xs file:font-semibold file:bg-primary-50 file:text-primary-700 hover:file:bg-primary-100 cursor-pointer"
                id="upload-file-input"
              />
              {uploadSelectedFile && (
                <p className="text-[11px] text-neutral-500 mt-1">
                  Seçilen: <strong>{uploadSelectedFile.name}</strong> (
                  {(uploadSelectedFile.size / (1024 * 1024)).toFixed(2)} MB)
                </p>
              )}
            </div>

            <div>
              <label className="text-xs font-semibold text-neutral-700 block mb-1">Açıklama</label>
              <textarea
                rows={2}
                value={uploadDescription}
                onChange={(e) => setUploadDescription(e.target.value)}
                placeholder="Materyal içeriği hakkında kısa not..."
                className="w-full text-xs bg-[#F8F7F3] border border-neutral-200 rounded-xl p-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
              />
            </div>
          </div>
        </Modal>
      )}
    </div>
  );
};
