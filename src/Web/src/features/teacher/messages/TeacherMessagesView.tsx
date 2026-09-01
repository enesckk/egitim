import React, { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { Send, MessageSquare, AlertCircle, RefreshCw, ArrowLeft } from 'lucide-react';
import { cn } from '@/lib/utils';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { initialTeacherMessagesData } from './mockData';
import { TeacherConversation, TeacherMessageItem, TeacherMessagesViewModel } from './types';

export interface TeacherMessagesViewProps {
  initialData?: TeacherMessagesViewModel;
  isLoading?: boolean;
  errorMessage?: string;
  onRetry?: () => void;
}

export const TeacherMessagesView: React.FC<TeacherMessagesViewProps> = ({
  initialData = initialTeacherMessagesData,
  isLoading = false,
  errorMessage,
  onRetry,
}) => {
  const [searchParams] = useSearchParams();
  const targetStudentId = searchParams.get('studentId');

  const [conversations, setConversations] = useState<TeacherConversation[]>(initialData.conversations);
  const [selectedConvId, setSelectedConvId] = useState<string>(() => {
    if (targetStudentId) {
      const match = initialData.conversations.find((c) => c.studentId === targetStudentId);
      if (match) return match.id;
    }
    return initialData.conversations[0]?.id || '';
  });
  const [inputText, setInputText] = useState('');
  const [mobileView, setMobileView] = useState<'list' | 'thread'>('list');

  const activeConversation = conversations.find((c) => c.id === selectedConvId) || conversations[0];

  const handleSendMessage = (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputText.trim() || !activeConversation) return;

    const newMsg: TeacherMessageItem = {
      id: `tm-${Date.now()}`,
      senderId: 'teacher-1',
      senderName: 'Kemal Bey',
      senderRole: 'teacher',
      content: inputText.trim(),
      timestamp: 'Şimdi',
    };

    setConversations((prev) =>
      prev.map((c) => {
        if (c.id === activeConversation.id) {
          return {
            ...c,
            lastMessage: newMsg.content,
            lastMessageTime: 'Şimdi',
            messages: [...c.messages, newMsg],
          };
        }
        return c;
      })
    );

    setInputText('');
  };

  // Loading State
  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 h-[550px]">
          <Skeleton className="h-full w-full rounded-2xl" />
          <Skeleton className="h-full w-full md:col-span-2 rounded-2xl" />
        </div>
      </div>
    );
  }

  // Error State
  if (errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4">
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

  return (
    <div className="max-w-6xl mx-auto select-none space-y-4 sm:space-y-6">
      {/* Title */}
      <div>
        <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 leading-tight">
          Öğrenci Soru & İletişim
        </h1>
        <p className="text-xs text-neutral-400 mt-0.5">
          Atanmış şubelerinizdeki öğrencilerle branş bazlı soru çözümü ve akademik iletişim
        </p>
      </div>

      {/* Grid Layout */}
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 min-h-[520px] bg-white rounded-2xl border border-neutral-100 shadow-soft-sm overflow-hidden">
        {/* Left Column: Conversation List */}
        <div
          className={cn(
            'border-r border-neutral-100 flex flex-col md:block',
            mobileView === 'thread' ? 'hidden md:block' : 'block'
          )}
        >
          <div className="p-3.5 border-b border-neutral-100 bg-neutral-50/50">
            <span className="text-xs font-semibold uppercase tracking-wider text-neutral-500">
              Sohbetler ({conversations.length})
            </span>
          </div>

          <div className="divide-y divide-neutral-50 overflow-y-auto max-h-[500px]">
            {conversations.map((conv) => {
              const isSelected = conv.id === activeConversation?.id;

              return (
                <button
                  key={conv.id}
                  type="button"
                  onClick={() => {
                    setSelectedConvId(conv.id);
                    setMobileView('thread');
                  }}
                  className={cn(
                    'w-full text-left p-3.5 flex items-start gap-3 transition-colors',
                    isSelected ? 'bg-primary-50/70 border-l-4 border-primary-600' : 'hover:bg-neutral-50'
                  )}
                >
                  <div className="w-10 h-10 rounded-2xl bg-navy-900 text-white font-semibold text-xs flex items-center justify-center flex-shrink-0">
                    {conv.studentInitials}
                  </div>

                  <div className="min-w-0 flex-1">
                    <div className="flex items-center justify-between gap-1">
                      <span className="text-sm font-semibold text-neutral-900 truncate">
                        {conv.studentName}
                      </span>
                      <span className="font-mono text-[10px] text-neutral-400">
                        {conv.lastMessageTime}
                      </span>
                    </div>

                    <div className="flex items-center gap-1.5 mt-0.5">
                      <span className="text-xs text-neutral-400 truncate">
                        {conv.studentGrade}
                      </span>
                      <span className="text-[10px] font-bold text-primary-700 bg-primary-50 px-1.5 py-0.2 rounded">
                        {conv.subject}
                      </span>
                    </div>

                    <p className="text-xs text-neutral-600 truncate mt-1">
                      {conv.lastMessage}
                    </p>
                  </div>

                  {conv.unreadCount > 0 && (
                    <span className="w-4 h-4 rounded-full bg-primary-600 text-white text-[10px] font-bold flex items-center justify-center flex-shrink-0">
                      {conv.unreadCount}
                    </span>
                  )}
                </button>
              );
            })}
          </div>
        </div>

        {/* Right Column: Active Thread & Composer */}
        <div
          className={cn(
            'md:col-span-2 flex flex-col justify-between p-4',
            mobileView === 'list' ? 'hidden md:flex' : 'flex'
          )}
        >
          {activeConversation ? (
            <>
              {/* Thread Header */}
              <div className="flex items-center justify-between pb-3 border-b border-neutral-100 mb-3">
                <div className="flex items-center gap-3">
                  <button
                    type="button"
                    onClick={() => setMobileView('list')}
                    className="md:hidden p-1 text-neutral-500 hover:bg-neutral-100 rounded-lg"
                    aria-label="Listeye Dön"
                  >
                    <ArrowLeft className="h-4 w-4" />
                  </button>
                  <div className="w-8 h-8 rounded-xl bg-navy-900 text-white font-semibold text-xs flex items-center justify-center flex-shrink-0">
                    {activeConversation.studentInitials}
                  </div>
                  <div>
                    <h3 className="text-sm font-semibold text-neutral-900">
                      {activeConversation.studentName}
                    </h3>
                    <p className="text-[11px] text-neutral-400">
                      {activeConversation.studentGrade} • {activeConversation.subject}
                    </p>
                  </div>
                </div>
              </div>

              {/* Message History */}
              <div className="flex-1 space-y-3 overflow-y-auto py-2 pr-1 max-h-[360px]">
                {activeConversation.messages.map((msg) => {
                  const isTeacher = msg.senderRole === 'teacher';

                  return (
                    <div
                      key={msg.id}
                      className={cn('flex flex-col', isTeacher ? 'items-end' : 'items-start')}
                    >
                      <div
                        className={cn(
                          'max-w-[85%] sm:max-w-[75%] rounded-2xl px-4 py-2.5 text-sm transition-all',
                          isTeacher
                            ? 'bg-primary-600 text-white rounded-br-sm shadow-soft-sm'
                            : 'bg-surface-alt border border-neutral-200 text-neutral-800 rounded-bl-sm'
                        )}
                      >
                        <p className="leading-relaxed whitespace-pre-wrap">{msg.content}</p>
                      </div>
                      <span className="font-mono text-[10px] text-neutral-400 mt-1 px-1">
                        {msg.timestamp}
                      </span>
                    </div>
                  );
                })}
              </div>

              {/* Composer */}
              <form onSubmit={handleSendMessage} className="pt-3 border-t border-neutral-100 flex items-center gap-2">
                <input
                  type="text"
                  value={inputText}
                  onChange={(e) => setInputText(e.target.value)}
                  placeholder={`${activeConversation.studentName} adlı öğrenciye cevap yazın...`}
                  className="flex-1 bg-surface-alt border border-neutral-200 rounded-xl px-3.5 py-2.5 text-xs sm:text-sm text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500"
                />
                <Button
                  type="submit"
                  variant="primary"
                  size="sm"
                  disabled={!inputText.trim()}
                  className="min-h-[40px] px-3 sm:px-4"
                >
                  <Send className="h-4 w-4" />
                  <span className="hidden sm:inline ml-1.5">Gönder</span>
                </Button>
              </form>
            </>
          ) : (
            <EmptyState
              icon={<MessageSquare className="h-6 w-6 text-neutral-400" />}
              title="Sohbet Seçilmedi"
              description="Soru ve mesajları görüntülemek için sol menüden bir öğrenci seçin."
            />
          )}
        </div>
      </div>
    </div>
  );
};
