import React, { useState, useEffect, useCallback, useMemo } from 'react';
import {
  BookOpen,
  Pin,
  Plus,
  Search,
  Trash2,
  Edit3,
  Tag,
  Calendar,
  Loader2,
  RefreshCw,
  AlertCircle,
  CheckCircle2,
  X,
  Clock,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import {
  notebookService,
  NotebookEntryDto,
  CreateNotebookEntryPayload,
} from '@/services/notebookService';

export const StudentNotebookView: React.FC = () => {
  const [entries, setEntries] = useState<NotebookEntryDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [statusFeedback, setStatusFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [onlyPinned, setOnlyPinned] = useState(false);

  // Edit/Create Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingEntry, setEditingEntry] = useState<NotebookEntryDto | null>(null);
  const [formTitle, setFormTitle] = useState('');
  const [formContent, setFormContent] = useState('');
  const [formTags, setFormTags] = useState('');
  const [formIsPinned, setFormIsPinned] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [modalError, setModalError] = useState<string | null>(null);

  // Unsaved changes confirmation
  const [isDiscardConfirmOpen, setIsDiscardConfirmOpen] = useState(false);

  // Delete Confirmation Modal State
  const [deleteTarget, setDeleteTarget] = useState<NotebookEntryDto | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const fetchEntries = useCallback(async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);
      const res = await notebookService.getEntries({
        searchTerm: searchTerm.trim() || undefined,
        isPinned: onlyPinned ? true : undefined,
        pageSize: 100,
      });

      // Sort deterministically: Pinned first, then newest createdAt / updatedAt
      const sorted = (res.items || []).sort((a, b) => {
        if (a.isPinned !== b.isPinned) return a.isPinned ? -1 : 1;
        const dateA = new Date(a.updatedAt || a.createdAt).getTime();
        const dateB = new Date(b.updatedAt || b.createdAt).getTime();
        if (dateB !== dateA) return dateB - dateA;
        return b.id.localeCompare(a.id);
      });

      setEntries(sorted);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Defter notları yüklenirken bir hata oluştu.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  }, [searchTerm, onlyPinned]);

  useEffect(() => {
    fetchEntries();
  }, [fetchEntries]);

  // Check if modal has unsaved changes compared to initial state
  const isDirty = useMemo(() => {
    if (!isModalOpen) return false;
    if (editingEntry) {
      return (
        formTitle.trim() !== editingEntry.title.trim() ||
        formContent.trim() !== editingEntry.content.trim() ||
        formTags.trim() !== (editingEntry.tags || '').trim() ||
        formIsPinned !== editingEntry.isPinned
      );
    }
    return formTitle.trim().length > 0 || formContent.trim().length > 0 || formTags.trim().length > 0;
  }, [isModalOpen, editingEntry, formTitle, formContent, formTags, formIsPinned]);

  const handleOpenCreateModal = () => {
    setEditingEntry(null);
    setFormTitle('');
    setFormContent('');
    setFormTags('');
    setFormIsPinned(false);
    setModalError(null);
    setIsModalOpen(true);
  };

  const handleOpenEditModal = (entry: NotebookEntryDto) => {
    setEditingEntry(entry);
    setFormTitle(entry.title);
    setFormContent(entry.content);
    setFormTags(entry.tags || '');
    setFormIsPinned(entry.isPinned);
    setModalError(null);
    setIsModalOpen(true);
  };

  const handleRequestCloseModal = () => {
    if (isSubmitting) return;
    if (isDirty) {
      setIsDiscardConfirmOpen(true);
    } else {
      setIsModalOpen(false);
    }
  };

  const handleConfirmDiscard = () => {
    setIsDiscardConfirmOpen(false);
    setIsModalOpen(false);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formTitle.trim() || !formContent.trim() || isSubmitting) return;

    try {
      setIsSubmitting(true);
      setModalError(null);
      const payload: CreateNotebookEntryPayload = {
        title: formTitle.trim(),
        content: formContent.trim(),
        tags: formTags.trim() || null,
        isPinned: formIsPinned,
      };

      if (editingEntry) {
        const updated = await notebookService.updateEntry(editingEntry.id, payload);
        setEntries((prev) =>
          prev
            .map((item) => (item.id === updated.id ? updated : item))
            .sort((a, b) => {
              if (a.isPinned !== b.isPinned) return a.isPinned ? -1 : 1;
              return new Date(b.updatedAt || b.createdAt).getTime() - new Date(a.updatedAt || a.createdAt).getTime();
            })
        );
        setStatusFeedback({ type: 'success', message: 'Not başarıyla güncellendi.' });
      } else {
        const created = await notebookService.createEntry(payload);
        setEntries((prev) => [created, ...prev]);
        setStatusFeedback({ type: 'success', message: 'Yeni not deftere eklendi.' });
      }

      setIsModalOpen(false);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Not kaydedilirken bir hata oluştu.';
      setModalError(msg);
    } finally {
      setIsSubmitting(false);
    }
  };

  const handlePromptDelete = (entry: NotebookEntryDto) => {
    setDeleteTarget(entry);
  };

  const handleConfirmDelete = async () => {
    if (!deleteTarget || isDeleting) return;
    try {
      setIsDeleting(true);
      await notebookService.deleteEntry(deleteTarget.id);
      setEntries((prev) => prev.filter((item) => item.id !== deleteTarget.id));
      setStatusFeedback({ type: 'success', message: 'Not başarıyla silindi.' });
      setDeleteTarget(null);
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Not silinirken bir hata oluştu.';
      setStatusFeedback({ type: 'error', message: msg });
    } finally {
      setIsDeleting(false);
    }
  };

  const handleTogglePin = async (entry: NotebookEntryDto) => {
    try {
      const updated = await notebookService.updateEntry(entry.id, {
        title: entry.title,
        content: entry.content,
        tags: entry.tags,
        subjectId: entry.subjectId,
        topicId: entry.topicId,
        isPinned: !entry.isPinned,
      });

      setEntries((prev) =>
        prev
          .map((item) => (item.id === updated.id ? updated : item))
          .sort((a, b) => {
            if (a.isPinned !== b.isPinned) return a.isPinned ? -1 : 1;
            return new Date(b.updatedAt || b.createdAt).getTime() - new Date(a.updatedAt || a.createdAt).getTime();
          })
      );
      setStatusFeedback({
        type: 'success',
        message: updated.isPinned ? 'Not en üste sabitlendi.' : 'Not sabitlemesi kaldırıldı.',
      });
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Sabitleme durumu güncellenemedi.';
      setStatusFeedback({ type: 'error', message: msg });
    }
  };

  return (
    <div className="max-w-5xl mx-auto space-y-4 sm:space-y-5 select-none" data-testid="student-notebook-view">
      {/* 1. Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-neutral-200/60 pb-3.5 sm:pb-4">
        <div>
          <div className="flex items-center gap-2">
            <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
              Defterim
            </h1>
            <span className="text-xs font-bold text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-full border border-primary-100">
              {entries.length} Not
            </span>
          </div>
          <p className="text-xs sm:text-sm text-neutral-500 mt-0.5">
            Ders formülleri, soru çözüm ipuçları ve kişisel çalışma özetleriniz
          </p>
        </div>

        <Button
          variant="primary"
          onClick={handleOpenCreateModal}
          leftIcon={<Plus className="h-4 w-4" />}
          className="shadow-soft-xs w-full sm:w-auto"
        >
          Yeni Not Ekle
        </Button>
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

      {/* 2. Filter and Search Bar */}
      <div className="bg-white rounded-2xl border border-neutral-200/80 p-3 sm:p-3.5 shadow-soft-xs flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-2.5">
        <div className="relative flex-1">
          <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-neutral-400" />
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Not başlığı veya içeriğinde ara..."
            className="w-full text-xs sm:text-sm bg-neutral-50 border border-neutral-200/80 rounded-xl pl-9 pr-4 py-2 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all font-medium"
          />
        </div>

        <button
          type="button"
          onClick={() => setOnlyPinned((prev) => !prev)}
          className={`flex items-center justify-center gap-2 px-3.5 py-2 rounded-xl text-xs font-semibold border transition-all ${
            onlyPinned
              ? 'bg-amber-50 text-amber-900 border-amber-300 shadow-soft-xs'
              : 'bg-neutral-50 text-neutral-600 border-neutral-200/80 hover:bg-neutral-100/70'
          }`}
        >
          <Pin className={`h-3.5 w-3.5 ${onlyPinned ? 'fill-amber-600 text-amber-600' : 'text-neutral-400'}`} />
          <span>Yalnızca Sabitlenenler</span>
        </button>
      </div>

      {/* Error Alert */}
      {errorMessage && (
        <Alert variant="danger" title="Notlar Yüklenemedi">
          <div className="flex items-center justify-between gap-2 mt-1 text-xs">
            <span>{errorMessage}</span>
            <Button
              variant="subtle"
              size="sm"
              onClick={fetchEntries}
              leftIcon={<RefreshCw className="h-3.5 w-3.5" />}
            >
              Tekrar Dene
            </Button>
          </div>
        </Alert>
      )}

      {/* 3. Note Cards Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3.5 sm:gap-4">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="bg-white rounded-2xl border border-neutral-200/80 p-4 sm:p-5 space-y-3">
              <Skeleton className="h-5 w-2/3 rounded-lg" />
              <Skeleton className="h-16 w-full rounded-xl" />
              <div className="flex justify-between items-center pt-2">
                <Skeleton className="h-4 w-20 rounded" />
                <Skeleton className="h-6 w-16 rounded-full" />
              </div>
            </div>
          ))}
        </div>
      ) : entries.length > 0 ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3.5 sm:gap-4">
          {entries.map((entry) => {
            const formattedDate = new Date(entry.updatedAt || entry.createdAt).toLocaleDateString('tr-TR');
            const isUpdated = Boolean(entry.updatedAt && entry.updatedAt !== entry.createdAt);

            return (
              <div
                key={entry.id}
                data-testid="notebook-entry-card"
                className={`bg-white rounded-2xl border transition-all duration-200 p-4 sm:p-5 flex flex-col justify-between shadow-soft-xs hover:shadow-soft-md relative group ${
                  entry.isPinned ? 'border-amber-300/80 bg-amber-50/20' : 'border-neutral-200/80 hover:border-neutral-300'
                }`}
              >
                {/* Header: Title & Action Icons */}
                <div className="flex items-start justify-between gap-2 mb-2.5">
                  <div className="flex items-center gap-1.5 min-w-0 flex-1">
                    {entry.isPinned && (
                      <span className="shrink-0 p-1 rounded-md bg-amber-100 text-amber-700" title="Sabitlenmiş Not">
                        <Pin className="h-3 w-3 fill-amber-600" />
                      </span>
                    )}
                    <h3 className="text-sm sm:text-base font-bold text-neutral-900 leading-snug break-words truncate">
                      {entry.title}
                    </h3>
                  </div>

                  <div className="flex items-center gap-0.5 shrink-0 opacity-90 group-hover:opacity-100 transition-opacity">
                    <button
                      type="button"
                      onClick={() => handleTogglePin(entry)}
                      title={entry.isPinned ? 'Sabitlemeyi kaldır' : 'Sabitle'}
                      className={`p-1.5 rounded-lg transition-colors ${
                        entry.isPinned
                          ? 'text-amber-700 hover:bg-amber-100'
                          : 'text-neutral-400 hover:text-amber-600 hover:bg-neutral-100'
                      }`}
                    >
                      <Pin className={`h-3.5 w-3.5 ${entry.isPinned ? 'fill-amber-600' : ''}`} />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleOpenEditModal(entry)}
                      title="Düzenle"
                      className="p-1.5 text-neutral-400 hover:text-primary-700 hover:bg-primary-50 rounded-lg transition-colors"
                    >
                      <Edit3 className="h-3.5 w-3.5" />
                    </button>
                    <button
                      type="button"
                      onClick={() => handlePromptDelete(entry)}
                      title="Sil"
                      className="p-1.5 text-neutral-400 hover:text-rose-700 hover:bg-rose-50 rounded-lg transition-colors"
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                    </button>
                  </div>
                </div>

                {/* Note Content Preview */}
                <p className="text-xs sm:text-sm text-neutral-600 whitespace-pre-wrap leading-relaxed line-clamp-5 mb-4 font-normal">
                  {entry.content}
                </p>

                {/* Footer: Tags & Date */}
                <div className="pt-3 border-t border-neutral-100 flex items-center justify-between gap-2 mt-auto">
                  <div className="flex flex-wrap gap-1 min-w-0 flex-1">
                    {entry.tags ? (
                      entry.tags.split(',').map((tag, idx) => (
                        <span
                          key={idx}
                          className="inline-flex items-center text-[10px] font-semibold text-neutral-600 bg-neutral-100 px-2 py-0.5 rounded-md border border-neutral-200/50 truncate max-w-[120px]"
                        >
                          <Tag className="h-2.5 w-2.5 mr-1 text-neutral-400 shrink-0" />
                          {tag.trim()}
                        </span>
                      ))
                    ) : (
                      <span className="text-[11px] text-neutral-400 font-medium">Genel Not</span>
                    )}
                  </div>

                  <span
                    className="text-[11px] text-neutral-400 font-mono flex items-center gap-1 shrink-0"
                    title={isUpdated ? 'Son güncelleme tarihi' : 'Oluşturulma tarihi'}
                  >
                    {isUpdated ? <Clock className="h-3 w-3" /> : <Calendar className="h-3 w-3" />}
                    {formattedDate}
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      ) : (
        <EmptyState
          icon={<BookOpen className="h-8 w-8 text-neutral-400" />}
          title={searchTerm || onlyPinned ? 'Aramanıza uygun not bulunamadı' : 'Defterinizde Henüz Not Yok'}
          description={
            searchTerm || onlyPinned
              ? 'Farklı bir arama terimi deneyebilir veya filtreyi temizleyebilirsiniz.'
              : 'Ders çalışırken öğrendiğiniz önemli formülleri, kuralları ve çalışma taktiklerini kaydetmeye başlayın.'
          }
          action={
            searchTerm || onlyPinned ? (
              <Button
                variant="secondary"
                size="sm"
                onClick={() => {
                  setSearchTerm('');
                  setOnlyPinned(false);
                }}
              >
                Filtreleri Temizle
              </Button>
            ) : (
              <Button
                variant="primary"
                onClick={handleOpenCreateModal}
                leftIcon={<Plus className="h-4 w-4" />}
              >
                İlk Notumu Ekle
              </Button>
            )
          }
        />
      )}

      {/* 4. Add / Edit Note Modal with Draft Protection */}
      <Modal
        isOpen={isModalOpen}
        onClose={handleRequestCloseModal}
        title={editingEntry ? 'Notu Düzenle' : 'Yeni Defter Notu'}
        subtitle="Yalnızca size özel kişisel çalışma notu"
        headerVariant="dark"
        footer={
          <div className="flex justify-end gap-2 w-full">
            <Button
              variant="secondary"
              size="sm"
              onClick={handleRequestCloseModal}
              disabled={isSubmitting}
            >
              İptal
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={handleSave}
              disabled={!formTitle.trim() || !formContent.trim() || isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-3.5 w-3.5 animate-spin mr-1.5" /> Kaydediliyor...
                </>
              ) : (
                'Kaydet'
              )}
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSave} className="space-y-4">
          {modalError && (
            <Alert variant="danger" title="Hata">
              {modalError}
            </Alert>
          )}

          <div>
            <label className="text-xs font-bold text-neutral-800 block mb-1">
              Başlık <span className="text-rose-500">*</span>
            </label>
            <input
              type="text"
              value={formTitle}
              onChange={(e) => setFormTitle(e.target.value)}
              disabled={isSubmitting}
              placeholder="Örn: Logaritma Taban Değiştirme Kuralı"
              className="w-full text-xs sm:text-sm bg-neutral-50 border border-neutral-200/80 rounded-xl px-3.5 py-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 font-medium"
            />
          </div>

          <div>
            <label className="text-xs font-bold text-neutral-800 block mb-1">
              Etiketler (Virgülle ayırın)
            </label>
            <input
              type="text"
              value={formTags}
              onChange={(e) => setFormTags(e.target.value)}
              disabled={isSubmitting}
              placeholder="Matematik, AYT, Formül"
              className="w-full text-xs sm:text-sm bg-neutral-50 border border-neutral-200/80 rounded-xl px-3.5 py-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 font-medium"
            />
          </div>

          <div>
            <label className="text-xs font-bold text-neutral-800 block mb-1">
              Not İçeriği <span className="text-rose-500">*</span>
            </label>
            <textarea
              value={formContent}
              onChange={(e) => setFormContent(e.target.value)}
              rows={6}
              disabled={isSubmitting}
              placeholder="Notlarınızı, çözümleri ve formülleri detaylıca buraya yazın..."
              className="w-full text-xs sm:text-sm bg-neutral-50 border border-neutral-200/80 rounded-xl p-3 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 font-medium"
            />
          </div>

          <div className="flex items-center gap-2 pt-1">
            <input
              type="checkbox"
              id="formIsPinned"
              checked={formIsPinned}
              onChange={(e) => setFormIsPinned(e.target.checked)}
              disabled={isSubmitting}
              className="h-4 w-4 rounded border-neutral-300 text-primary-600 focus:ring-primary-500"
            />
            <label htmlFor="formIsPinned" className="text-xs font-medium text-neutral-700 cursor-pointer">
              Bu notu en üste sabitle
            </label>
          </div>
        </form>
      </Modal>

      {/* 5. Unsaved Changes Discard Confirmation Modal */}
      <Modal
        isOpen={isDiscardConfirmOpen}
        onClose={() => setIsDiscardConfirmOpen(false)}
        title="Kaydedilmemiş Değişiklikler Var"
        subtitle="Yaptığınız değişiklikler kaydedilmeden kaybolacak."
        maxWidth="sm"
        footer={
          <div className="flex justify-end gap-2 w-full">
            <Button variant="secondary" size="sm" onClick={() => setIsDiscardConfirmOpen(false)}>
              Düzenlemeye Devam Et
            </Button>
            <Button variant="destructive" size="sm" onClick={handleConfirmDiscard}>
              Vazgeç ve Kapat
            </Button>
          </div>
        }
      >
        <p className="text-xs sm:text-sm text-neutral-600 leading-relaxed">
          Not üzerinde yaptığınız değişiklikleri kaydetmediniz. Pencereyi kapatırsanız bu değişiklikler silinecektir. Kapatmak istediğinizden emin misiniz?
        </p>
      </Modal>

      {/* 6. Accessible Delete Confirmation Modal */}
      <Modal
        isOpen={Boolean(deleteTarget)}
        onClose={() => !isDeleting && setDeleteTarget(null)}
        title="Notu Sil"
        subtitle="Bu işlem notu defterinizden kaldıracaktır."
        maxWidth="sm"
        footer={
          <div className="flex justify-end gap-2 w-full">
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setDeleteTarget(null)}
              disabled={isDeleting}
            >
              Vazgeç
            </Button>
            <Button
              variant="destructive"
              size="sm"
              onClick={handleConfirmDelete}
              disabled={isDeleting}
            >
              {isDeleting ? (
                <>
                  <Loader2 className="h-3.5 w-3.5 animate-spin mr-1.5" /> Siliniyor...
                </>
              ) : (
                'Evet, Sil'
              )}
            </Button>
          </div>
        }
      >
        <div className="space-y-2">
          <p className="text-xs sm:text-sm text-neutral-600 leading-relaxed">
            <strong className="text-neutral-900 font-bold">&quot;{deleteTarget?.title}&quot;</strong> başlıklı notu silmek istediğinize emin misiniz?
          </p>
          <p className="text-[11px] text-neutral-400">
            Silinen not defter listenizden hemen kaldırılacaktır.
          </p>
        </div>
      </Modal>
    </div>
  );
};

export default StudentNotebookView;
