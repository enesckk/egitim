import React, { useState } from 'react';
import { Send } from 'lucide-react';
import { Button } from '@/components/ui/Button';

export interface MessageComposerProps {
  onSendMessage: (content: string) => void;
  disabled?: boolean;
}

export const MessageComposer: React.FC<MessageComposerProps> = ({ onSendMessage, disabled }) => {
  const [content, setContent] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!content.trim()) return;
    onSendMessage(content.trim());
    setContent('');
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSubmit(e);
    }
  };

  return (
    <form
      onSubmit={handleSubmit}
      className="bg-white rounded-2xl border border-neutral-200 p-2 sm:p-2.5 flex items-end gap-2 shadow-soft-sm"
    >
      <textarea
        value={content}
        onChange={(e) => setContent(e.target.value)}
        onKeyDown={handleKeyDown}
        placeholder="Koçunuza mesaj yazın... (Enter ile gönder)"
        disabled={disabled}
        rows={1}
        className="flex-1 resize-none bg-transparent px-3 py-2 text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none max-h-32 min-h-[40px]"
      />

      <Button
        type="submit"
        variant="primary"
        size="sm"
        disabled={!content.trim() || disabled}
        className="min-h-[40px] px-3.5"
      >
        <Send className="h-4 w-4" />
        <span className="hidden sm:inline ml-1">Gönder</span>
      </Button>
    </form>
  );
};
