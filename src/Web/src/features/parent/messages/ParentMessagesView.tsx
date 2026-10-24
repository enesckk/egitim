import React, { useState, useEffect } from 'react';
import { MessageSquare, AlertCircle, RefreshCw, Send } from 'lucide-react';
import { Button } from '@/components/ui/Button';
import { EmptyState } from '@/components/ui/EmptyState';
import { Alert } from '@/components/ui/Alert';
import { Skeleton } from '@/components/ui/Skeleton';
import { messagingService, ConversationSummaryDto, MessageDto } from '@/services/messagingService';

export const ParentMessagesView: React.FC = () => {
  const [conversations, setConversations] = useState<ConversationSummaryDto[]>([]);
  const [selectedConvId, setSelectedConvId] = useState<string | null>(null);
  const [messages, setMessages] = useState<MessageDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isLoadingMessages, setIsLoadingMessages] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [inputText, setInputText] = useState('');
  const [isSending, setIsSending] = useState(false);

  const loadConversations = async () => {
    try {
      setIsLoading(true);
      setErrorMessage(null);
      const list = await messagingService.listConversations();
      setConversations(list);
      if (list.length > 0) {
        setSelectedConvId((prev) => prev || list[0].id);
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
  }, []);

  useEffect(() => {
    if (!selectedConvId) {
      setMessages([]);
      return;
    }

    let active = true;
    setIsLoadingMessages(true);

    messagingService.getConversationMessages(selectedConvId)
      .then((msgs) => {
        if (!active) return;
        setMessages(msgs);
        messagingService.markConversationRead(selectedConvId).catch(() => {});
      })
      .catch((err) => {
        if (!active) return;
        console.error('Mesajlar yüklenemedi', err);
      })
      .finally(() => {
        if (active) setIsLoadingMessages(false);
      });

    return () => {
      active = false;
    };
  }, [selectedConvId]);

  const handleSendMessage = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!inputText.trim() || isSending) return;

    try {
      setIsSending(true);
      const activeConv = conversations.find((c) => c.id === selectedConvId);
      const sent = await messagingService.sendMessage({
        conversationId: selectedConvId || undefined,
        recipientUserId: activeConv ? activeConv.otherUserId : undefined,
        content: inputText.trim(),
      });

      setMessages((prev) => [...prev, sent]);
      setInputText('');
      setConversations((prev) =>
        prev.map((c) =>
          c.id === sent.conversationId
            ? { ...c, lastMessageSnippet: sent.content, lastMessageAt: sent.createdAt }
            : c
        )
      );
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Mesaj gönderilemedi.';
      alert(msg);
    } finally {
      setIsSending(false);
    }
  };

  if (isLoading) {
    return (
      <div className="max-w-6xl mx-auto space-y-4 select-none p-4" data-testid="parent-messages-loading">
        <Skeleton className="h-16 w-1/3 rounded-xl" />
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 h-[550px]">
          <Skeleton className="h-full w-full rounded-2xl" />
          <Skeleton className="h-full w-full md:col-span-2 rounded-2xl" />
        </div>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="max-w-6xl mx-auto py-8 space-y-4 p-4" data-testid="parent-messages-error">
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
      <div className="max-w-6xl mx-auto select-none space-y-4 p-4" data-testid="parent-messages-view">
        <div className="border-b border-neutral-100 pb-3 sm:pb-4">
          <h1 className="font-serif text-2xl sm:text-3xl text-neutral-900 tracking-tight leading-tight">
            Mesajlar
          </h1>
          <p className="text-neutral-500 text-xs sm:text-sm mt-0.5">
            Öğrencinizin danışman koçu ve öğretmenleri ile güvenli iletişim
          </p>
        </div>

        <div className="bg-white rounded-2xl border border-neutral-200/80 p-6 sm:p-8 shadow-soft-sm text-center">
          <div className="w-12 h-12 rounded-2xl bg-navy-50 text-navy-800 flex items-center justify-center mx-auto mb-4 border border-navy-100">
            <MessageSquare className="h-6 w-6 text-primary-600" />
          </div>
          <h2 className="text-lg font-semibold text-neutral-900">
            Henüz Konuşma Bulunmuyor
          </h2>
          <p className="text-xs sm:text-sm text-neutral-600 max-w-md mx-auto mt-2 leading-relaxed">
            Öğrencinizin danışman koçu veya ders öğretmenleri mesaj gönderdiğinde konuşmalarınız burada listelenecektir.
          </p>
        </div>
      </div>
    );
  }

  const activeConv = conversations.find((c) => c.id === selectedConvId) || conversations[0];

  return (
    <div className="max-w-6xl mx-auto p-4 flex flex-col md:flex-row gap-4 h-[calc(100vh-8rem)] min-h-[550px]" data-testid="parent-messages-view">
      {/* Sidebar: Conversation List */}
      <div className="w-full md:w-80 flex flex-col bg-white rounded-2xl border border-neutral-200 shadow-sm overflow-hidden">
        <div className="p-4 border-b border-neutral-100 bg-neutral-50/50 flex justify-between items-center">
          <div>
            <h2 className="font-semibold text-neutral-900 text-sm">Gelen Kutusu</h2>
            <span className="text-xs text-neutral-500">{conversations.length} konuşma</span>
          </div>
        </div>
        <div className="flex-1 overflow-y-auto divide-y divide-neutral-100">
          {conversations.map((c) => {
            const isSelected = c.id === activeConv?.id;
            return (
              <button
                key={c.id}
                onClick={() => setSelectedConvId(c.id)}
                className={`w-full text-left p-3.5 transition-colors flex items-start gap-3 ${
                  isSelected ? 'bg-primary-50/60' : 'hover:bg-neutral-50'
                }`}
              >
                <div className="w-9 h-9 rounded-xl bg-primary-100 text-primary-700 font-semibold text-xs flex items-center justify-center shrink-0">
                  {c.otherUserName.slice(0, 2).toUpperCase()}
                </div>
                <div className="flex-1 min-w-0">
                  <div className="flex justify-between items-baseline mb-0.5">
                    <span className="text-xs font-semibold text-neutral-900 truncate">
                      {c.otherUserName}
                    </span>
                    <span className="text-[10px] text-neutral-400">
                      {new Date(c.lastMessageAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                  <div className="text-[11px] text-neutral-500 truncate mb-1">
                    {c.otherUserRole}
                  </div>
                  {c.lastMessageSnippet && (
                    <p className="text-xs text-neutral-600 truncate">
                      {c.lastMessageSnippet}
                    </p>
                  )}
                </div>
                {c.unreadCount > 0 && (
                  <span className="px-1.5 py-0.5 text-[10px] font-bold bg-primary-600 text-white rounded-full">
                    {c.unreadCount}
                  </span>
                )}
              </button>
            );
          })}
        </div>
      </div>

      {/* Main Conversation & Messages */}
      <div className="flex-1 flex flex-col bg-white rounded-2xl border border-neutral-200 shadow-sm overflow-hidden">
        {/* Header */}
        <div className="p-4 border-b border-neutral-100 flex items-center justify-between bg-neutral-50/50">
          <div className="flex items-center gap-3">
            <div className="w-9 h-9 rounded-xl bg-primary-600 text-white font-semibold text-xs flex items-center justify-center">
              {activeConv?.otherUserName.slice(0, 2).toUpperCase()}
            </div>
            <div>
              <h3 className="font-semibold text-neutral-900 text-sm">{activeConv?.otherUserName}</h3>
              <p className="text-[11px] text-neutral-500">{activeConv?.otherUserRole}</p>
            </div>
          </div>
        </div>

        {/* Message Thread */}
        <div className="flex-1 overflow-y-auto p-4 space-y-3 bg-neutral-50/30">
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
                description="İletişimi başlatmak için aşağıdan mesajınızı yazın."
              />
            </div>
          ) : (
            messages.map((m) => {
              const isMine = m.senderRole.toLowerCase() === 'parent';
              return (
                <div
                  key={m.id}
                  className={`flex flex-col ${isMine ? 'items-end' : 'items-start'}`}
                >
                  <div
                    className={`max-w-[75%] rounded-2xl px-4 py-2.5 text-xs shadow-soft-sm ${
                      isMine
                        ? 'bg-primary-600 text-white rounded-br-xs'
                        : 'bg-white text-neutral-900 border border-neutral-200/80 rounded-bl-xs'
                    }`}
                  >
                    <p className="whitespace-pre-wrap leading-relaxed">{m.content}</p>
                  </div>
                  <span className="text-[10px] text-neutral-400 mt-1 px-1">
                    {new Date(m.createdAt).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                  </span>
                </div>
              );
            })
          )}
        </div>

        {/* Input Composer */}
        <form onSubmit={handleSendMessage} className="p-3 border-t border-neutral-200 bg-white flex gap-2">
          <input
            id="message-content-input"
            type="text"
            value={inputText}
            onChange={(e) => setInputText(e.target.value)}
            placeholder={`${activeConv?.otherUserName} adlı kullanıcıya mesaj yazın...`}
            className="flex-1 px-3.5 py-2 text-xs bg-neutral-50 border border-neutral-300 rounded-xl focus:outline-none focus:ring-2 focus:ring-primary-500"
          />
          <Button
            type="submit"
            variant="primary"
            size="sm"
            disabled={!inputText.trim() || isSending}
            leftIcon={<Send className="h-3.5 w-3.5" />}
          >
            Gönder
          </Button>
        </form>
      </div>
    </div>
  );
};
