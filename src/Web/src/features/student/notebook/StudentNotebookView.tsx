import React, { useState, useEffect, useCallback } from 'react';
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
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { Badge } from '@/components/ui/Badge';
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
  const [searchTerm, setSearchTerm] = useState('');
  const [onlyPinned, setOnlyPinned] = useState(false);

  // Modal State
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [editingEntry, setEditingEntry] = useState<NotebookEntryDto | null>(null);
  const [formTitle, setFormTitle] = useState('');
  const [formContent, setFormContent] = useState('');
  const [formTags, setFormTags] = useState('');
  const [formIsPinned, setFormIsPinned] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const fetchEntries = useCallback(async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);
      const res = await notebookService.getEntries({
        searchTerm: searchTerm.trim() || undefined,
        isPinned: onlyPinned ? true : undefined,
        pageSize: 50,
      });
      setEntries(res.items || []);
    } catch (err) {
      console.error('Failed to load notebook entries:', err);
      setErrorMessage('Defter notları yüklenirken bir hata oluştu.');
    } finally {
      setIsLoading(false);
    }
  }, [searchTerm, onlyPinned]);

  useEffect(() => {
    fetchEntries();
  }, [fetchEntries]);

  const handleOpenCreateModal = () => {
    setEditingEntry(null);
    setFormTitle('');
    setFormContent('');
    setFormTags('');
    setFormIsPinned(false);
    setIsModalOpen(true);
  };

  const handleOpenEditModal = (entry: NotebookEntryDto) => {
    setEditingEntry(entry);
    setFormTitle(entry.title);
    setFormContent(entry.content);
    setFormTags(entry.tags || '');
    setFormIsPinned(entry.isPinned);
    setIsModalOpen(true);
  };

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formTitle.trim() || !formContent.trim() || isSubmitting) return;

    try {
      setIsSubmitting(true);
      const payload: CreateNotebookEntryPayload = {
        title: formTitle.trim(),
        content: formContent.trim(),
        tags: formTags.trim() || null,
        isPinned: formIsPinned,
      };

      if (editingEntry) {
        const updated = await notebookService.updateEntry(editingEntry.id, payload);
        setEntries((prev) =>
          prev.map((item) => (item.id === updated.id ? updated : item))
        );
      } else {
        const created = await notebookService.createEntry(payload);
        setEntries((prev) => [created, ...prev]);
      }

      setIsModalOpen(false);
    } catch (err) {
      console.error('Failed to save notebook entry:', err);
      alert('Not kaydedilirken bir hata oluştu. Lütfen tekrar deneyin.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (id: string) => {
    if (deletingId) return;
    try {
      setDeletingId(id);
      await notebookService.deleteEntry(id);
      setEntries((prev) => prev.filter((item) => item.id !== id));
    } catch (err) {
      console.error('Failed to delete notebook entry:', err);
      alert('Not silinirken bir hata oluştu.');
    } finally {
      setDeletingId(null);
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
            return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
          })
      );
    } catch (err) {
      console.error('Failed to toggle pin:', err);
    }
  };

  return (
    <div className="max-w-6xl mx-auto space-y-6 select-none p-4 sm:p-6" data-testid="student-notebook-view">
      {/* Page Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <div className="flex items-center gap-2">
            <div className="w-9 h-9 rounded-xl bg-primary-100 text-primary-700 flex items-center justify-center">
              <BookOpen className="h-5 w-5" />
            </div>
            <h1 className="text-xl sm:text-2xl font-bold text-neutral-900 tracking-tight">
              Öğrenci Defteri
            </h1>
          </div>
          <p className="text-xs sm:text-sm text-neutral-500 mt-1">
            Ders notlarınız, formülleriniz ve önemli sınav hatırlatmalarınız
          </p>
        </div>

        <Button
          variant="primary"
          onClick={handleOpenCreateModal}
          leftIcon={<Plus className="h-4 w-4" />}
          className="shadow-soft-sm"
        >
          Yeni Not Ekle
        </Button>
      </div>

      {/* Filter and Search Bar */}
      <div className="bg-white rounded-2xl border border-neutral-100 p-4 shadow-soft-sm flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-neutral-400" />
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder="Not başlığı veya içeriğinde ara..."
            className="w-full text-xs sm:text-sm bg-neutral-50 border border-neutral-200 rounded-xl pl-10 pr-4 py-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500 transition-all"
          />
        </div>

        <button
          type="button"
          onClick={() => setOnlyPinned((prev) => !prev)}
          className={`flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-xs font-semibold border transition-all ${
            onlyPinned
              ? 'bg-amber-50 text-amber-800 border-amber-300'
              : 'bg-neutral-50 text-neutral-600 border-neutral-200 hover:bg-neutral-100'
          }`}
        >
          <Pin className={`h-4 w-4 ${onlyPinned ? 'fill-amber-600 text-amber-600' : ''}`} />
          <span>Yalnızca Sabitlenenler</span>
        </button>
      </div>

      {/* Error Alert */}
      {errorMessage && (
        <Alert
          variant="danger"
          title="Bir hata oluştu"
        >
          <div className="flex items-center justify-between gap-2 mt-1">
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

      {/* Content Area */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="bg-white rounded-2xl border border-neutral-100 p-5 space-y-3">
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
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {entries.map((entry) => (
            <div
              key={entry.id}
              className={`bg-white rounded-2xl border transition-all duration-200 p-5 flex flex-col justify-between shadow-soft-sm hover:shadow-soft-md relative ${
                entry.isPinned ? 'border-amber-300 bg-amber-50/20' : 'border-neutral-100'
              }`}
            >
              <div>
                {/* Header: Title & Actions */}
                <div className="flex items-start justify-between gap-2 mb-2">
                  <h3 className="text-sm font-bold text-neutral-900 leading-snug break-words flex-1">
                    {entry.title}
                  </h3>
                  <div className="flex items-center gap-1 flex-shrink-0">
                    <button
                      type="button"
                      onClick={() => handleTogglePin(entry)}
                      title={entry.isPinned ? 'Sabitlemeyi kaldır' : 'Sabitle'}
                      className={`p-1.5 rounded-lg transition-colors ${
                        entry.isPinned
                          ? 'text-amber-600 bg-amber-100 hover:bg-amber-200'
                          : 'text-neutral-400 hover:text-amber-600 hover:bg-neutral-100'
                      }`}
                    >
                      <Pin className={`h-4 w-4 ${entry.isPinned ? 'fill-amber-600' : ''}`} />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleOpenEditModal(entry)}
                      title="Düzenle"
                      className="p-1.5 text-neutral-400 hover:text-primary-600 hover:bg-neutral-100 rounded-lg transition-colors"
                    >
                      <Edit3 className="h-4 w-4" />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleDelete(entry.id)}
                      disabled={deletingId === entry.id}
                      title="Sil"
                      className="p-1.5 text-neutral-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors disabled:opacity-50"
                    >
                      {deletingId === entry.id ? (
                        <Loader2 className="h-4 w-4 animate-spin text-red-600" />
                      ) : (
                        <Trash2 className="h-4 w-4" />
                      )}
                    </button>
                  </div>
                </div>

                {/* Content */}
                <p className="text-xs sm:text-sm text-neutral-700 whitespace-pre-wrap leading-relaxed line-clamp-6 mb-4">
                  {entry.content}
                </p>
              </div>

              {/* Footer: Tags & Date */}
              <div className="pt-3 border-t border-neutral-100 flex items-center justify-between gap-2 mt-auto">
                <div className="flex flex-wrap gap-1">
                  {entry.tags ? (
                    entry.tags.split(',').map((tag, idx) => (
                      <Badge key={idx} variant="secondary" size="sm">
                        <Tag className="h-2.5 w-2.5 mr-1" />
                        {tag.trim()}
                      </Badge>
                    ))
                  ) : (
                    <span className="text-[11px] text-neutral-400 italic">Genel Not</span>
                  )}
                </div>

                <span className="text-[11px] text-neutral-400 font-mono flex items-center gap-1 flex-shrink-0">
                  <Calendar className="h-3 w-3" />
                  {new Date(entry.createdAt).toLocaleDateString('tr-TR')}
                </span>
              </div>
            </div>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<BookOpen className="h-8 w-8 text-neutral-400" />}
          title={searchTerm || onlyPinned ? 'Aramanıza uygun not bulunamadı' : 'Henüz not eklenmemiş'}
          description={
            searchTerm || onlyPinned
              ? 'Farklı bir arama terimi deneyebilir veya filtreyi kaldırabilirsiniz.'
              : 'Ders çalışırken öğrendiğiniz önemli kuralları ve taktikleri kaydetmeye başlayın.'
          }
          action={
            <Button
              variant="primary"
              onClick={handleOpenCreateModal}
              leftIcon={<Plus className="h-4 w-4" />}
            >
              İlk Notu Ekle
            </Button>
          }
        />
      )}

      {/* Add / Edit Note Modal */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => !isSubmitting && setIsModalOpen(false)}
        title={editingEntry ? 'Notu Düzenle' : 'Yeni Defter Notu'}
        subtitle="Yalnızca size özel kişisel çalışma notu"
        footer={
          <div className="flex justify-end gap-2 w-full">
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setIsModalOpen(false)}
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
                  <Loader2 className="h-3.5 w-3.5 animate-spin mr-1" /> Kaydediliyor...
                </>
              ) : (
                'Kaydet'
              )}
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSave} className="space-y-4">
          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1">
              Başlık <span className="text-red-500">*</span>
            </label>
            <input
              type="text"
              value={formTitle}
              onChange={(e) => setFormTitle(e.target.value)}
              disabled={isSubmitting}
              placeholder="Örn: Logaritma Taban Değiştirme Kuralı"
              className="w-full text-xs sm:text-sm bg-neutral-50 border border-neutral-200 rounded-xl px-3.5 py-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
          </div>

          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1">
              Etiketler (Virgülle ayırın)
            </label>
            <input
              type="text"
              value={formTags}
              onChange={(e) => setFormTags(e.target.value)}
              disabled={isSubmitting}
              placeholder="Matematik, AYT, Formül"
              className="w-full text-xs sm:text-sm bg-neutral-50 border border-neutral-200 rounded-xl px-3.5 py-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
          </div>

          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1">
              Not İçeriği <span className="text-red-500">*</span>
            </label>
            <textarea
              value={formContent}
              onChange={(e) => setFormContent(e.target.value)}
              rows={6}
              disabled={isSubmitting}
              placeholder="Notlarınızı, çözümleri ve formülleri detaylıca buraya yazın..."
              className="w-full text-xs sm:text-sm bg-neutral-50 border border-neutral-200 rounded-xl p-3 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
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
    </div>
  );
};
export default StudentNotebookView;
