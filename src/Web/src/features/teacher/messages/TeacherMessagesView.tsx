import React, { useState, useEffect, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  Send,
  MessageSquare,
  AlertCircle,
  RefreshCw,
  Search,
  ArrowLeft,
  CheckCircle2,
  X,
} from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { messagingService, ConversationSummaryDto, MessageDto } from '@/services/messagingService';

export const TeacherMessagesView: React.FC = () => {
  const [searchParams] = useSearchParams();
  const targetStudentId = searchParams.get('studentId');

  const [conversations, setConversations] = useState<ConversationSummaryDto[]>([]);
  const [selectedConvId, setSelectedConvId] = useState<string | null>(null);
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMessages, setIsLoadingMessages] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [statusFeedback, setStatusFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null);
  const [inputText, setInputText] = useState('');
  const [isSending, setIsSending] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');

  // Responsive mobile navigation state: 'list' or 'thread'
  const [mobileView, setMobileView] = useState<'list' | 'thread'>('list');

  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const loadConversations = async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);
      const list = await messagingService.listConversations();
      setConversations(list || []);
      if (list && list.length > 0) {
        if (targetStudentId) {
          const match = list.find((c) => c.otherUserId === targetStudentId);
          if (match) {
            setSelectedConvId(match.id);
            setMobileView('thread');
          } else {
            setSelectedConvId(list[0].id);
          }
        } else {
          setSelectedConvId((prev) => prev || list[0].id);
        }
      }
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Mesajlar yüklenirken hata oluştu.';
      setErrorMessage(msg);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadConversations();
  }, [targetStudentId]);

  useEffect(() => {
    if (!selectedConvId) {
      setMessages([]);
      return;
    }

    let active = true;
    setIsLoadingMessages(true);

    messagingService
      .getConversationMessages(selectedConvId)
      .then((msgs) => {
        if (!active) return;
        setMessages(msgs || []);
        messagingService.markConversationRead(selectedConvId).catch(() => {});
        // Update unread count locally in conversation list
        setConversations((prev) =>
          prev.map((c) => (c.id === selectedConvId ? { ...c, unreadCount: 0 } : c))
        );
      })
      .catch((err) => {
        if (!active) return;
        console.error('Mesajlar yüklenemedi', err);
      })
      .finally(() => {
        if (active) {
          setIsLoadingMessages(false);
          setTimeout(scrollToBottom, 100);
        }
      });

    return () => {
      active = false;
    };
  }, [selectedConvId]);

  useEffect(() => {
    if (messages.length > 0) {
      scrollToBottom();
    }
  }, [messages.length]);

  const handleSelectConversation = (convId: string) => {
    setSelectedConvId(convId);
    setMobileView('thread');
  };

  const handleBackToList = () => {
    setMobileView('list');
  };

  const handleSendMessage = async (e?: React.FormEvent) => {
    if (e) e.preventDefault();
    if (!inputText.trim() || isSending) return;

    try {
      setIsSending(true);
      setStatusFeedback(null);
      const activeConv = conversations.find((c) => c.id === selectedConvId);
      const sent = await messagingService.sendMessage({
        conversationId: selectedConvId || undefined,
        recipientUserId: activeConv ? activeConv.otherUserId : undefined,
        content: inputText.trim(),
      });

      setMessages((prev) => [...prev, sent]);
      setInputText('');
      if (textareaRef.current) {
        textareaRef.current.style.height = 'auto';
      }

      setConversations((prev) =>
        prev.map((c) =>
          c.id === sent.conversationId
            ? { ...c, lastMessageSnippet: sent.content, lastMessageAt: sent.createdAt }
            : c
        )
      );
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Mesaj gönderilemedi. Lütfen bağlantınızı kontrol edin.';
      setStatusFeedback({ type: 'error', message: msg });
    } finally {
      setIsSending(false);
      setTimeout(scrollToBottom, 100);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const handleTextChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setInputText(e.target.value);
    const el = e.target;
    el.style.height = 'auto';
    el.style.height = `${Math.min(el.scrollHeight, 120)}px`;
  };

  const filteredConversations = conversations.filter((c) => {
    if (!searchQuery.trim()) return true;
    const query = searchQuery.toLowerCase();
    return (
      c.otherUserName.toLowerCase().includes(query) ||
      (c.otherUserRole && c.otherUserRole.toLowerCase().includes(query)) ||
      (c.lastMessageSnippet && c.lastMessageSnippet.toLowerCase().includes(query))
    );
  });

  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="teacher-messages-loading">
        <div className="border-b border-neutral-200/60 pb-3.5 sm:pb-4">
          <Skeleton className="h-8 w-48 rounded-xl" />
          <Skeleton className="h-4 w-72 rounded-lg mt-1" />
        </div>
        <div className="flex gap-4 h-[560px]">
          <Skeleton className="w-full lg:w-80 h-full rounded-2xl" />
          <Skeleton className="hidden lg:block flex-1 h-full rounded-2xl" />
        </div>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4 select-none p-4" data-testid="teacher-messages-error">
        <div className="border-b border-neutral-200/60 pb-3.5 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Mesajlar
          </h1>
          <p className="text-xs sm:text-sm text-neutral-500 mt-0.5">
            Sınıflarınızdaki öğrenciler, veliler ve danışman koçlar ile iletişim
          </p>
        </div>
        <Alert variant="danger" icon={<AlertCircle className="h-5 w-5" />} title="Mesajlar Yüklenemedi">
          {errorMessage}
        </Alert>
        <div className="text-center pt-2">
          <Button variant="primary" size="sm" onClick={loadConversations} leftIcon={<RefreshCw className="h-4 w-4" />}>
            Tekrar Dene
          </Button>
        </div>
      </div>
    );
  }

  if (conversations.length === 0) {
    return (
      <div className="max-w-6xl mx-auto select-none space-y-4 p-4" data-testid="teacher-messages-view">
        <div className="border-b border-neutral-200/60 pb-3.5 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Mesajlar
          </h1>
          <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
            Sınıflarınızdaki öğrenciler, veliler ve danışman koçlar ile iletişim
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-200/80 p-8 sm:p-12 shadow-soft-sm text-center">
          <div className="w-14 h-14 rounded-2xl bg-[#F8F7F3] text-navy-900 flex items-center justify-center mx-auto mb-4 border border-neutral-200">
            <MessageSquare className="h-7 w-7 text-primary-600" />
          </div>
          <h2 className="text-lg font-bold text-neutral-900">
            Henüz Konuşma Bulunmuyor
          </h2>
          <p className="text-xs sm:text-sm text-neutral-600 max-w-md mx-auto mt-2 leading-relaxed">
            Sınıflarınızdaki öğrencileriniz veya paydaşlar mesaj gönderdiğinde konuşmalarınız burada listelenecektir.
          </p>
        </div>
      </div>
    );
  }

  const activeConv = conversations.find((c) => c.id === selectedConvId) || conversations[0];

  return (
    <div className="max-w-6xl mx-auto select-none space-y-3 sm:space-y-4 p-4" data-testid="teacher-messages-view">
      {/* 1. Header (visible on desktop or when on mobile/tablet list view) */}
      <div className={`border-b border-neutral-200/60 pb-3.5 sm:pb-4 ${mobileView === 'thread' ? 'hidden lg:block' : 'block'}`}>
        <div className="flex items-center justify-between gap-3 flex-wrap">
          <div>
            <div className="flex items-center gap-2.5">
              <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
                Mesajlar
              </h1>
              <span className="text-xs font-bold text-primary-700 bg-primary-50 px-2.5 py-0.5 rounded-full border border-primary-100">
                {conversations.length} Konuşma
              </span>
            </div>
            <p className="text-xs sm:text-sm text-neutral-500 mt-0.5">
              Sınıflarınızdaki öğrenciler, veliler ve danışman koçlar ile yetkili iletişim
            </p>
          </div>
        </div>
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
            className="p-1 hover:bg-black/5 rounded text-neutral-500 cursor-pointer"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        </div>
      )}

      {/* 2. Responsive Two-Column / Master-Detail Container */}
      <div className="flex flex-col lg:flex-row gap-3 sm:gap-4 h-[calc(100vh-14rem)] min-h-[520px] max-h-[760px]">
        {/* Master: Conversation List (Visible on desktop OR mobile/tablet 'list' view) */}
        <div
          className={`w-full lg:w-80 xl:w-88 flex flex-col bg-white rounded-2xl border border-neutral-200/80 shadow-soft-xs overflow-hidden shrink-0 ${
            mobileView === 'thread' ? 'hidden lg:flex' : 'flex'
          }`}
        >
          {/* Header & Search */}
          <div className="p-3 sm:p-3.5 border-b border-neutral-100 bg-neutral-50/80 space-y-2.5">
            <div className="flex items-center justify-between">
              <h2 className="font-bold text-neutral-900 text-xs uppercase tracking-wider">Gelen Kutusu</h2>
              <span className="text-xs text-neutral-500 font-mono font-medium">{conversations.length} Sohbet</span>
            </div>
            <div className="relative">
              <Search className="h-3.5 w-3.5 text-neutral-400 absolute left-3 top-1/2 -translate-y-1/2" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Kişi veya mesaj ara..."
                className="w-full pl-8 pr-3 py-1.5 text-xs bg-white border border-neutral-200 rounded-xl placeholder:text-neutral-400 text-neutral-900 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500"
              />
            </div>
          </div>

          {/* List items */}
          <div className="flex-1 overflow-y-auto divide-y divide-neutral-100/80">
            {filteredConversations.length === 0 ? (
              <div className="p-6 text-center text-neutral-400 text-xs">
                Aramanızla eşleşen konuşma bulunamadı.
              </div>
            ) : (
              filteredConversations.map((c) => {
                const isSelected = c.id === activeConv?.id;
                const initials = c.otherUserName
                  ? c.otherUserName
                      .split(' ')
                      .map((w) => w[0])
                      .join('')
                      .substring(0, 2)
                      .toUpperCase()
                  : 'KLL';

                return (
                  <button
                    key={c.id}
                    type="button"
                    onClick={() => handleSelectConversation(c.id)}
                    className={`w-full text-left p-3 sm:p-3.5 transition-all flex items-start gap-3 cursor-pointer select-none ${
                      isSelected ? 'bg-primary-50/60 border-l-4 border-primary-600' : 'hover:bg-neutral-50/80'
                    }`}
                  >
                    <div className="w-10 h-10 rounded-xl bg-[#14233E] text-white font-bold text-xs flex items-center justify-center shrink-0 shadow-soft-xs">
                      {initials}
                    </div>

                    <div className="flex-1 min-w-0">
                      <div className="flex justify-between items-baseline mb-0.5">
                        <span className="text-xs sm:text-sm font-bold text-neutral-900 truncate">
                          {c.otherUserName}
                        </span>
                        <span className="text-[10px] text-neutral-400 font-mono shrink-0 ml-1">
                          {c.lastMessageAt
                            ? new Date(c.lastMessageAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
                            : ''}
                        </span>
                      </div>

                      <div className="text-[11px] font-medium text-primary-700 truncate mb-1">
                        {c.otherUserRole}
                      </div>

                      {c.lastMessageSnippet && (
                        <p className="text-xs text-neutral-500 truncate font-normal">
                          {c.lastMessageSnippet}
                        </p>
                      )}
                    </div>

                    {c.unreadCount > 0 && (
                      <span className="px-2 py-0.5 text-[10px] font-bold bg-primary-600 text-white rounded-full shadow-soft-xs shrink-0 self-center">
                        {c.unreadCount}
                      </span>
                    )}
                  </button>
                );
              })
            )}
          </div>
        </div>

        {/* Detail: Active Chat Pane (Visible on desktop OR mobile/tablet 'thread' view) */}
        <div
          className={`flex-1 flex flex-col bg-white rounded-2xl border border-neutral-200/80 shadow-soft-xs overflow-hidden ${
            mobileView === 'list' ? 'hidden lg:flex' : 'flex'
          }`}
        >
          {/* Thread Header */}
          <div className="p-3 sm:p-3.5 border-b border-neutral-100 flex items-center justify-between bg-neutral-50/80">
            <div className="flex items-center gap-2.5">
              {/* Back to conversations button on mobile/tablet */}
              <button
                type="button"
                onClick={handleBackToList}
                className="lg:hidden p-1.5 -ml-1 text-neutral-600 hover:text-neutral-900 hover:bg-neutral-200/50 rounded-lg transition-colors cursor-pointer"
                title="Gelen Kutusuna Dön"
              >
                <ArrowLeft className="h-4 w-4" />
              </button>

              <div className="w-9 h-9 rounded-xl bg-[#14233E] text-white font-bold text-xs flex items-center justify-center shrink-0 shadow-soft-xs">
                {activeConv?.otherUserName
                  ? activeConv.otherUserName
                      .split(' ')
                      .map((w) => w[0])
                      .join('')
                      .substring(0, 2)
                      .toUpperCase()
                  : 'KLL'}
              </div>

              <div>
                <h3 className="font-bold text-neutral-900 text-xs sm:text-sm leading-tight">
                  {activeConv?.otherUserName}
                </h3>
                <p className="text-[11px] text-primary-700 font-medium">{activeConv?.otherUserRole}</p>
              </div>
            </div>
          </div>

          {/* Thread Message Stream */}
          <div className="flex-1 overflow-y-auto p-4 space-y-3 bg-[#F8F7F3]/70">
            {isLoadingMessages ? (
              <div className="space-y-3 py-4">
                <Skeleton className="h-12 w-2/3 rounded-2xl" />
                <Skeleton className="h-12 w-1/2 ml-auto rounded-2xl" />
                <Skeleton className="h-12 w-3/4 rounded-2xl" />
              </div>
            ) : messages.length === 0 ? (
              <div className="h-full flex items-center justify-center text-center p-6 text-neutral-400">
                <EmptyState
                  icon={<MessageSquare className="h-6 w-6 text-neutral-400" />}
                  title="Henüz Mesaj Yok"
                  description="İletişimi başlatmak için aşağıdaki alandan mesajınızı yazıp gönderebilirsiniz."
                />
              </div>
            ) : (
              messages.map((m) => {
                const isMine = m.senderRole?.toLowerCase() === 'teacher';
                return (
                  <div
                    key={m.id}
                    className={`flex flex-col ${isMine ? 'items-end' : 'items-start'}`}
                  >
                    <div
                      className={`max-w-[85%] sm:max-w-[75%] rounded-2xl px-4 py-2.5 text-xs sm:text-sm leading-relaxed shadow-soft-xs ${
                        isMine
                          ? 'bg-[#315FE8] text-white rounded-br-xs'
                          : 'bg-white text-neutral-900 border border-neutral-200/80 rounded-bl-xs'
                      }`}
                    >
                      <p className="whitespace-pre-wrap">{m.content}</p>
                    </div>
                    <span className="text-[10px] text-neutral-400 font-mono mt-1 px-1">
                      {new Date(m.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                );
              })
            )}
            <div ref={messagesEndRef} />
          </div>

          {/* Thread Input Composer */}
          <form
            onSubmit={handleSendMessage}
            className="p-3 border-t border-neutral-200/80 bg-white flex items-end gap-2"
          >
            <div className="flex-1 relative">
              <textarea
                ref={textareaRef}
                id="message-content-input"
                rows={1}
                value={inputText}
                onChange={handleTextChange}
                onKeyDown={handleKeyDown}
                disabled={isSending}
                placeholder={`${activeConv?.otherUserName || 'Kullanıcıya'} cevap yazın... (Enter ile gönder, Shift+Enter ile alt satır)`}
                className="w-full text-xs sm:text-sm bg-neutral-50 border border-neutral-200/80 rounded-xl px-3.5 py-2.5 text-neutral-900 placeholder:text-neutral-400 focus:outline-none focus:ring-2 focus:ring-primary-500/20 focus:border-primary-500 transition-all font-medium resize-none leading-relaxed"
                style={{ minHeight: '40px', maxHeight: '120px' }}
              />
            </div>

            <Button
              type="submit"
              variant="primary"
              size="sm"
              disabled={!inputText.trim() || isSending}
              leftIcon={<Send className="h-3.5 w-3.5" />}
              className="h-10 px-4 shrink-0 rounded-xl shadow-soft-xs bg-[#315FE8] hover:bg-blue-700"
            >
              Gönder
            </Button>
          </form>
        </div>
      </div>
    </div>
  );
};

export default TeacherMessagesView;
