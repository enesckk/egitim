import React, { useState } from 'react';
import { MessageSquare, AlertCircle, RefreshCw } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { CoachInfoBanner } from './components/CoachInfoBanner';
import { MessageBubble } from './components/MessageBubble';
import { MessageComposer } from './components/MessageComposer';
import { MessageItem, StudentMessagesViewModel } from './types';

export interface StudentMessagesViewProps {
  initialData?: StudentMessagesViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const StudentMessagesView: React.FC<StudentMessagesViewProps> = ({
  initialData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const [messages, setMessages] = useState<MessageItem[]>(initialData?.messages || []);

  const handleSendMessage = (text: string) => {
    const newMessage: MessageItem = {
      id: `msg-${Date.now()}`,
      senderId: 'student-current',
      senderName: 'Öğrenci',
      senderRole: 'student',
      content: text,
      timestamp: 'Şimdi',
      isRead: true,
    };
    setMessages((prev) => [...prev, newMessage]);
  };

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-3xl mx-auto space-y-4 select-none" data-testid="student-messages-loading">
        <Skeleton className="h-20 w-full rounded-2xl" />
        <div className="space-y-3 py-4">
          <Skeleton className="h-16 w-3/4 rounded-2xl" />
          <Skeleton className="h-16 w-2/3 ml-auto rounded-2xl" />
          <Skeleton className="h-16 w-3/4 rounded-2xl" />
        </div>
        <Skeleton className="h-14 w-full rounded-2xl" />
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-3xl mx-auto py-8 space-y-4" data-testid="student-messages-error">
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Mesajlar Yüklenemedi">
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

  const hasMessages = initialData && initialData.messages && initialData.messages.length > 0;

  if (!hasMessages) {
    return (
      <div className="max-w-3xl mx-auto select-none space-y-4" data-testid="student-messages-view">
        <div className="border-b border-neutral-100 pb-3 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Mesajlar
          </h1>
          <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
            Koç ve öğretmen iletişimi
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-200/80 p-6 sm:p-8 shadow-soft-sm text-center">
          <div className="w-12 h-12 rounded-2xl bg-navy-50 text-navy-800 flex items-center justify-center mx-auto mb-4 border border-navy-100">
            <MessageSquare className="h-6 w-6 text-primary-600" />
          </div>
          <h2 className="text-lg font-semibold text-neutral-900">
            Henüz Mesajlaşma Başlatılmadı
          </h2>
          <p className="text-xs sm:text-sm text-neutral-600 max-w-md mx-auto mt-2 leading-relaxed">
            Danışman koçunuz veya öğretmenlerinizle mesajlaşma kanalı kurumunuz tarafından yetkilendirildiğinde aktif hale gelecektir.
          </p>
        </div>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto flex flex-col min-h-[calc(100vh-14rem)] md:min-h-[calc(100vh-10rem)] justify-between space-y-4" data-testid="student-messages-view">
      {/* 1. Coach Info Header */}
      {initialData?.coach && <CoachInfoBanner coach={initialData.coach} />}

      {/* 2. Message Thread */}
      <div className="flex-1 space-y-3.5 overflow-y-auto py-2 px-1">
        {messages.length > 0 ? (
          messages.map((msg) => (
            <MessageBubble key={msg.id} message={msg} />
          ))
        ) : (
          <EmptyState
            icon={<MessageSquare className="h-6 w-6 text-neutral-400" />}
            title="Henüz Mesaj Yok"
            description="İlk sorunuzu yazarak iletişimi başlatabilirsiniz."
          />
        )}
      </div>

      {/* 3. Message Composer (Bottom) */}
      <div className="sticky bottom-0 bg-surface/95 backdrop-blur-sm pt-2">
        <MessageComposer onSendMessage={handleSendMessage} />
      </div>
    </div>
  );
};
