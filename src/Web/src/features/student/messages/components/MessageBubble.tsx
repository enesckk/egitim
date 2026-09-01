import React from 'react';
import { cn } from '@/lib/utils';
import { MessageItem } from '../types';

export interface MessageBubbleProps {
  message: MessageItem;
}

export const MessageBubble: React.FC<MessageBubbleProps> = ({ message }) => {
  const isStudent = message.senderRole === 'student';

  return (
    <div className={cn('flex flex-col', isStudent ? 'items-end' : 'items-start')}>
      <div
        className={cn(
          'max-w-[85%] sm:max-w-[75%] rounded-2xl px-4 py-2.5 text-sm select-none transition-all',
          isStudent
            ? 'bg-primary-600 text-white rounded-br-sm shadow-soft-sm'
            : 'bg-white border border-neutral-200 text-neutral-800 rounded-bl-sm'
        )}
      >
        <p className="leading-relaxed whitespace-pre-wrap">{message.content}</p>
      </div>

      <span className="font-mono text-[10px] text-neutral-400 mt-1 px-1">
        {message.timestamp}
      </span>
    </div>
  );
};
