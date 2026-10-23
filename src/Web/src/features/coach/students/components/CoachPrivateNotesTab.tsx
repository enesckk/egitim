import React, { useState } from 'react';
import { Lock, Plus, Calendar, ShieldCheck, Tag, Trash2, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { Modal } from '@/components/ui/Modal';
import { Badge } from '@/components/ui/Badge';
import { EmptyState } from '@/components/ui/EmptyState';
import { CoachPrivateNote } from '../types';

export interface CoachPrivateNotesTabProps {
  notes: CoachPrivateNote[];
  studentName: string;
  onAddNote: (note: Omit<CoachPrivateNote, 'id' | 'createdAt'>) => Promise<void> | void;
  onDeleteNote?: (noteId: string) => Promise<void> | void;
  isLoading?: boolean;
}

export const CoachPrivateNotesTab: React.FC<CoachPrivateNotesTabProps> = ({
  notes,
  studentName,
  onAddNote,
  onDeleteNote,
  isLoading = false,
}) => {
  const [isAddModalOpen, setIsAddModalOpen] = useState(false);
  const [newContent, setNewContent] = useState('');
  const [newCategory, setNewCategory] = useState<CoachPrivateNote['category']>('Akademik');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const handleSave = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!newContent.trim() || isSubmitting) return;

    try {
      setIsSubmitting(true);
      await onAddNote({
        category: newCategory,
        content: newContent.trim(),
      });
      setNewContent('');
      setIsAddModalOpen(false);
    } catch {
      // Hata üst bileşende veya bildirimde ele alınır
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleDelete = async (noteId: string) => {
    if (!onDeleteNote || deletingId) return;
    try {
      setDeletingId(noteId);
      await onDeleteNote(noteId);
    } finally {
      setDeletingId(null);
    }
  };

  return (
    <div className="space-y-4 select-none">
      {/* Privacy Notice Banner */}
      <div className="bg-attention-light border border-purple-200 rounded-2xl p-3.5 sm:p-4 flex items-start gap-3">
        <div className="w-8 h-8 rounded-xl bg-purple-100 text-attention-dark flex items-center justify-center flex-shrink-0 mt-0.5">
          <Lock className="h-4 w-4" />
        </div>
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2">
            <h4 className="text-xs sm:text-sm font-semibold text-purple-950">
              Koça Özel Gizli Not Alanı
            </h4>
            <span className="inline-flex items-center gap-0.5 text-[10px] font-bold text-attention-dark bg-purple-100/80 px-2 py-0.5 rounded-full">
              <ShieldCheck className="h-3 w-3" /> Gizli
            </span>
          </div>
          <p className="text-xs text-purple-800 leading-relaxed mt-0.5">
            Bu alanda tutulan notlar yalnızca size (Koç) özeldir. Öğrenci, veli veya öğretmenler bu notları kesinlikle görüntüleyemez.
          </p>
        </div>
        <Button
          variant="subtle"
          size="sm"
          onClick={() => setIsAddModalOpen(true)}
          leftIcon={<Plus className="h-3.5 w-3.5" />}
          className="flex-shrink-0 bg-white border border-purple-200 text-attention-dark hover:bg-purple-50"
        >
          Not Ekle
        </Button>
      </div>

      {/* Notes List */}
      {isLoading ? (
        <div className="p-8 flex justify-center items-center text-neutral-400 gap-2">
          <Loader2 className="h-5 w-5 animate-spin" />
          <span className="text-xs">Notlar yükleniyor...</span>
        </div>
      ) : notes.length > 0 ? (
        <div className="space-y-3">
          {notes.map((note) => (
            <div
              key={note.id}
              className="bg-white rounded-2xl border border-neutral-100 p-4 space-y-2 shadow-soft-sm relative group"
            >
              <div className="flex items-center justify-between gap-2">
                <div className="flex items-center gap-2">
                  <Badge variant="attention" size="sm">
                    <Tag className="h-2.5 w-2.5 mr-1" />
                    {note.category}
                  </Badge>
                </div>
                <div className="flex items-center gap-3">
                  <span className="text-xs text-neutral-400 font-mono flex items-center gap-1">
                    <Calendar className="h-3 w-3" />
                    {note.createdAt}
                  </span>
                  {onDeleteNote && (
                    <button
                      type="button"
                      onClick={() => handleDelete(note.id)}
                      disabled={deletingId === note.id}
                      aria-label="Notu sil"
                      className="text-neutral-400 hover:text-red-500 transition-colors p-1 rounded-lg hover:bg-red-50 disabled:opacity-50"
                    >
                      {deletingId === note.id ? (
                        <Loader2 className="h-3.5 w-3.5 animate-spin text-red-500" />
                      ) : (
                        <Trash2 className="h-3.5 w-3.5" />
                      )}
                    </button>
                  )}
                </div>
              </div>

              <p className="text-sm text-neutral-800 leading-relaxed whitespace-pre-wrap">
                {note.content}
              </p>
            </div>
          ))}
        </div>
      ) : (
        <EmptyState
          icon={<Lock className="h-6 w-6 text-neutral-400" />}
          title="Henüz Özel Not Eklenmemiş"
          description={`${studentName} için görüşme, motivasyon veya akademik takip notlarınızı güvenle kaydedebilirsiniz.`}
          action={
            <Button
              size="sm"
              variant="primary"
              onClick={() => setIsAddModalOpen(true)}
              leftIcon={<Plus className="h-4 w-4" />}
            >
              İlk Notu Ekle
            </Button>
          }
        />
      )}

      {/* Add Note Modal */}
      <Modal
        isOpen={isAddModalOpen}
        onClose={() => !isSubmitting && setIsAddModalOpen(false)}
        title="Yeni Özel Koç Notu"
        subtitle={`${studentName} için yalnızca koça özel kayıt`}
        headerVariant="dark"
        footer={
          <div className="flex justify-end gap-2 w-full">
            <Button
              variant="secondary"
              size="sm"
              onClick={() => setIsAddModalOpen(false)}
              disabled={isSubmitting}
            >
              İptal
            </Button>
            <Button
              variant="primary"
              size="sm"
              onClick={handleSave}
              disabled={!newContent.trim() || isSubmitting}
            >
              {isSubmitting ? (
                <>
                  <Loader2 className="h-3.5 w-3.5 animate-spin mr-1" /> Kaydediliyor...
                </>
              ) : (
                'Notu Kaydet'
              )}
            </Button>
          </div>
        }
      >
        <form onSubmit={handleSave} className="space-y-3 select-none">
          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1">
              Kategori
            </label>
            <select
              value={newCategory}
              onChange={(e) => setNewCategory(e.target.value as CoachPrivateNote['category'])}
              disabled={isSubmitting}
              className="w-full text-xs bg-white border border-neutral-200 rounded-xl px-3 py-2 text-neutral-800 focus:outline-none focus:ring-2 focus:ring-primary-500"
            >
              <option value="Akademik">Akademik Değerlendirme</option>
              <option value="Motivasyon">Motivasyon & Psikolojik Takip</option>
              <option value="Veli Görüşmesi">Veli Görüşmesi Notu</option>
              <option value="Genel">Genel Koçluk Gözlemi</option>
            </select>
          </div>

          <div>
            <label className="text-xs font-semibold text-neutral-700 block mb-1">
              Not İçeriği
            </label>
            <textarea
              value={newContent}
              onChange={(e) => setNewContent(e.target.value)}
              rows={4}
              disabled={isSubmitting}
              placeholder="Öğrenciye dair özel gözlemlerinizi ve takip edilecek noktaları yazın..."
              className="w-full text-sm bg-surface-alt border border-neutral-200 rounded-xl p-3 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
            />
          </div>
        </form>
      </Modal>
    </div>
  );
};
