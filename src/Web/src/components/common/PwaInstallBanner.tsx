import React, { useState, useEffect } from 'react';
import { WifiOff, Download, X, Share } from 'lucide-react';

interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

export const PwaInstallBanner: React.FC = () => {
  const [deferredPrompt, setDeferredPrompt] = useState<BeforeInstallPromptEvent | null>(null);
  const [isOffline, setIsOffline] = useState(!navigator.onLine);
  const [showIosGuide, setShowIosGuide] = useState(false);
  const [isDismissed, setIsDismissed] = useState(false);

  useEffect(() => {
    const handleOnline = () => setIsOffline(false);
    const handleOffline = () => setIsOffline(true);

    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    const handleBeforeInstall = (e: Event) => {
      e.preventDefault();
      setDeferredPrompt(e as BeforeInstallPromptEvent);
    };

    window.addEventListener('beforeinstallprompt', handleBeforeInstall);

    return () => {
      window.removeEventListener('online', handleOnline);
      window.removeEventListener('offline', handleOffline);
      window.removeEventListener('beforeinstallprompt', handleBeforeInstall);
    };
  }, []);

  const handleInstallClick = async () => {
    if (deferredPrompt) {
      await deferredPrompt.prompt();
      const choice = await deferredPrompt.userChoice;
      if (choice.outcome === 'accepted') {
        setDeferredPrompt(null);
      }
    } else {
      // Check iOS Safari
      const isIos = /iPad|iPhone|iPod/.test(navigator.userAgent) && !(window as any).MSStream;
      const isStandalone = window.matchMedia('(display-mode: standalone)').matches;
      if (isIos && !isStandalone) {
        setShowIosGuide(true);
      }
    }
  };

  const isIos = typeof navigator !== 'undefined' && /iPad|iPhone|iPod/.test(navigator.userAgent) && !(window as any).MSStream;
  const isStandalone = typeof window !== 'undefined' && window.matchMedia('(display-mode: standalone)').matches;
  const canShowInstall = (deferredPrompt || (isIos && !isStandalone)) && !isDismissed;

  return (
    <>
      {/* Offline Toast Banner */}
      {isOffline && (
        <div
          role="status"
          aria-live="polite"
          className="fixed top-0 inset-x-0 z-50 bg-amber-500 text-neutral-900 px-4 py-2 text-center text-xs sm:text-sm font-medium flex items-center justify-center gap-2 shadow-md transition-all animate-in fade-in"
        >
          <WifiOff className="w-4 h-4 shrink-0 text-neutral-900" />
          <span>Çevrimdışısınız. Önceden yüklenmiş sayfalara erişebilirsiniz, ancak verileriniz güncellenemeyebilir.</span>
        </div>
      )}

      {/* Unobtrusive Install Banner */}
      {canShowInstall && (
        <div className="fixed bottom-16 md:bottom-4 right-4 z-40 max-w-sm bg-neutral-900 text-white p-3.5 rounded-xl shadow-xl border border-neutral-700 flex items-center justify-between gap-3 text-xs sm:text-sm">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-primary-600 flex items-center justify-center text-white shrink-0 font-bold text-sm">
              BA
            </div>
            <div>
              <p className="font-semibold text-white">Bilim Akademi Uygulaması</p>
              <p className="text-neutral-300 text-2xs sm:text-xs">Daha hızlı erişim için ana ekrana ekleyin</p>
            </div>
          </div>
          <div className="flex items-center gap-1.5 shrink-0">
            <button
              onClick={handleInstallClick}
              className="px-2.5 py-1.5 bg-primary-500 hover:bg-primary-600 active:scale-95 text-white font-medium rounded-lg text-xs transition flex items-center gap-1"
            >
              <Download className="w-3.5 h-3.5" />
              Yükle
            </button>
            <button
              onClick={() => setIsDismissed(true)}
              aria-label="Kapat"
              className="p-1 text-neutral-400 hover:text-white rounded transition"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* iOS Manual Install Instructions Modal */}
      {showIosGuide && (
        <div className="fixed inset-0 z-50 bg-neutral-900/60 backdrop-blur-xs flex items-end sm:items-center justify-center p-4">
          <div className="bg-surface rounded-2xl max-w-sm w-full p-5 shadow-2xl border border-neutral-200 animate-in fade-in slide-in-from-bottom">
            <div className="flex justify-between items-center mb-3">
              <h3 className="font-semibold text-neutral-900 text-sm">iOS için Ana Ekrana Ekle</h3>
              <button
                onClick={() => setShowIosGuide(false)}
                className="text-neutral-400 hover:text-neutral-700 p-1"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
            <p className="text-xs text-neutral-600 mb-3">
              Bilim Akademi uygulamasını iPhone veya iPad ana ekranınıza eklemek için:
            </p>
            <ol className="text-xs text-neutral-700 space-y-2 list-decimal list-inside bg-neutral-50 p-3 rounded-lg border border-neutral-200">
              <li className="flex items-start gap-1.5">
                <span>1. Safari alt çubuğundaki</span>
                <span className="inline-flex items-center gap-1 font-medium bg-neutral-200 px-1.5 py-0.5 rounded text-2xs">
                  <Share className="w-3 h-3" /> Paylaş
                </span>
                <span>butonuna dokunun.</span>
              </li>
              <li>2. Açılan menüden <strong>"Ana Ekrana Ekle"</strong> seçeneğini seçin.</li>
              <li>3. Sağ üstteki <strong>"Ekle"</strong> butonuna basın.</li>
            </ol>
            <button
              onClick={() => setShowIosGuide(false)}
              className="w-full mt-4 py-2 bg-primary-600 text-white rounded-lg text-xs font-medium hover:bg-primary-700 transition"
            >
              Anladım
            </button>
          </div>
        </div>
      )}
    </>
  );
};
