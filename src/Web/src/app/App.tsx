import React, { useState } from 'react';
import {
  LayoutDashboard,
  CalendarDays,
  FileText,
  MessageSquare,
  User,
  Search,
  CheckCircle2,
  AlertTriangle,
  Info,
  Sparkles,
  RefreshCw,
} from 'lucide-react';
import { AppShell } from '@/components/layout/AppShell';
import { NavItem } from '@/components/layout/BottomNav';
import {
  Button,
  Card,
  CardHeader,
  CardTitle,
  CardDescription,
  CardContent,
  CardFooter,
  Badge,
  Input,
  Select,
  Progress,
  Avatar,
  Tabs,
  Alert,
  EmptyState,
  Modal,
  WeeklyStreak,
} from '@/components/ui';

export const App: React.FC = () => {
  const [activeTab, setActiveTab] = useState<string>('overview');
  const [subTab, setSubTab] = useState<string>('today');
  const [searchQuery, setSearchQuery] = useState<string>('');
  const [filterValue, setFilterValue] = useState<string>('');
  const [isModalOpen, setIsModalOpen] = useState<boolean>(false);

  const studentNavItems: NavItem[] = [
    {
      id: 'overview',
      label: 'Bugün',
      icon: <LayoutDashboard className="h-5 w-5" />,
    },
    {
      id: 'schedule',
      label: 'Planlar',
      icon: <CalendarDays className="h-5 w-5" />,
      badge: 3,
    },
    {
      id: 'exams',
      label: 'Denemeler',
      icon: <FileText className="h-5 w-5" />,
    },
    {
      id: 'messages',
      label: 'Mesajlar',
      icon: <MessageSquare className="h-5 w-5" />,
    },
    {
      id: 'profile',
      label: 'Profil',
      icon: <User className="h-5 w-5" />,
    },
  ];

  const streakDays = [
    { dayLabel: 'Pzt', isCompleted: true, hours: 3.5 },
    { dayLabel: 'Sal', isCompleted: true, hours: 2.5 },
    { dayLabel: 'Çar', isCompleted: true, hours: 4 },
    { dayLabel: 'Per', isCompleted: false, hours: 0 },
    { dayLabel: 'Cum', isCompleted: true, hours: 3 },
    { dayLabel: 'Cmt', isCompleted: true, hours: 3.5 },
    { dayLabel: 'Paz', isCompleted: false, isToday: true, hours: 0 },
  ];

  return (
    <AppShell
      role="student"
      navItems={studentNavItems}
      activeNavId={activeTab}
      onNavChange={setActiveTab}
      headerProps={{
        title: 'Bilim Akademi',
        subtitle: 'Öğrenci Portalı',
        institutionName: 'Merkez Şube',
        userName: 'Ayşe Kaya',
        userRole: '11. Sınıf • Sayısal',
        onProfileClick: () => setIsModalOpen(true),
      }}
    >
      <div className="space-y-6">
        {/* Hero Greeting — DM Serif Display */}
        <div className="flex flex-col sm:flex-row sm:items-baseline justify-between gap-2 border-b border-neutral-100 pb-4">
          <div>
            <p className="text-neutral-400 text-xs sm:text-sm capitalize font-medium">
              1 Eylül 2026, Salı
            </p>
            <h1 className="font-serif text-3xl sm:text-4xl text-neutral-900 tracking-tight mt-0.5">
              Günaydın, Ayşe
            </h1>
            <p className="text-xs sm:text-sm text-neutral-500 mt-1">
              Bugün için planlanmış 4 çalışma görevin ve 1 deneme analizin var.
            </p>
          </div>
          <div className="flex items-center gap-2 pt-1 sm:pt-0">
            <Badge variant="success" size="md">
              <CheckCircle2 className="h-3 w-3 mr-1" />
              Tasarım Sistemi Hizalandı
            </Badge>
            <Badge variant="attention" size="md">
              Figma Tokens v1.0
            </Badge>
          </div>
        </div>

        {/* Action & Filter Bar */}
        <div className="grid grid-cols-1 sm:grid-cols-4 gap-3">
          <div className="sm:col-span-2">
            <Input
              placeholder="Konu, ders veya hedef ara..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              leftIcon={<Search className="h-4 w-4" />}
            />
          </div>
          <div>
            <Select
              options={[
                { value: 'math', label: 'Matematik' },
                { value: 'physics', label: 'Fizik' },
                { value: 'chemistry', label: 'Kimya' },
                { value: 'turkish', label: 'Türkçe' },
              ]}
              value={filterValue}
              onChange={(e) => setFilterValue(e.target.value)}
              placeholder="Ders Seçin"
            />
          </div>
          <div className="flex items-center gap-2">
            <Button
              variant="primary"
              className="flex-1"
              leftIcon={<Sparkles className="h-4 w-4" />}
              onClick={() => setIsModalOpen(true)}
            >
              Hızlı Detay
            </Button>
          </div>
        </div>

        {/* Underline Tabs */}
        <Tabs
          tabs={[
            { id: 'today', label: 'Bugünün Planı', count: 4 },
            { id: 'weekly', label: 'Haftalık Seri' },
            { id: 'stats', label: 'Akademik Netler' },
          ]}
          activeId={subTab}
          onChange={setSubTab}
        />

        {/* Grid Cards Showcase */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          {/* Card 1: Dark Hero Card (Figma Navy-900) */}
          <Card variant="dark">
            <CardHeader>
              <div className="flex items-center justify-between">
                <span className="text-xs font-semibold uppercase tracking-wider text-navy-300">
                  Sonraki Çalışma
                </span>
                <span className="font-mono text-xs text-primary-400">14:00 - 15:30</span>
              </div>
              <CardTitle className="text-white text-lg font-serif mt-1">
                Türev Uygulamaları
              </CardTitle>
              <CardDescription className="text-navy-200">
                Matematik • 45 Soru Çözümü & Formül Tekrarı
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
              <div className="flex items-center justify-between text-xs text-navy-300">
                <span>Hedef İlerleme</span>
                <span className="font-mono font-bold text-white">%75</span>
              </div>
              <Progress value={75} variant="primary" />
            </CardContent>
            <CardFooter className="border-navy-800 flex justify-between items-center">
              <div className="flex items-center gap-2">
                <Avatar name="Hasan Yılmaz" size="sm" variant="navy" />
                <span className="text-xs text-navy-200">Koç: Hasan Yılmaz</span>
              </div>
              <Button size="sm" variant="subtle" className="text-xs">
                Başlat
              </Button>
            </CardFooter>
          </Card>

          {/* Card 2: Tinted Contextual Recommendation Card (Figma Attention/Purple) */}
          <div className="bg-attention-light border border-purple-200 rounded-2xl p-5 flex flex-col justify-between">
            <div>
              <div className="flex items-center gap-2 mb-2">
                <div className="w-7 h-7 rounded-lg bg-attention flex items-center justify-center text-white flex-shrink-0">
                  <Info className="h-4 w-4" />
                </div>
                <h3 className="font-semibold text-sm text-attention-dark">
                  Kişisel Koçluk Önerisi
                </h3>
              </div>
              <p className="text-xs text-purple-900 leading-relaxed mt-2">
                Limit & Süreklilik konusunda son 2 denemede tekrarlayan soru kaçırma tespit edildi. Bu konuyu bugün 30 dakika pekiştirmeni öneririz.
              </p>
            </div>
            <div className="pt-4 border-t border-purple-200/60 mt-4 flex justify-between items-center">
              <span className="text-[11px] font-medium text-purple-700">Koç Algoritması</span>
              <button
                type="button"
                className="text-xs font-semibold text-attention hover:text-attention-dark flex items-center gap-1 transition-colors"
              >
                Çalışmayı Başlat →
              </button>
            </div>
          </div>

          {/* Card 3: Flat Weekly Streak Card */}
          <Card variant="default">
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle>Haftalık Çalışma Serisi</CardTitle>
                <span className="font-mono text-xs font-semibold text-success">5 / 7 Gün</span>
              </div>
              <CardDescription>
                Bu hafta toplam 18.5 saat tamamlandı.
              </CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              <WeeklyStreak days={streakDays} />
              <div className="pt-2">
                <div className="flex justify-between text-xs text-neutral-600 mb-1">
                  <span>Haftalık Hedef (21 Saat)</span>
                  <span className="font-mono font-semibold text-neutral-800">%88</span>
                </div>
                <Progress value={88} variant="success" />
              </div>
            </CardContent>
            <CardFooter>
              <span className="text-xs text-neutral-400">Hedefe 2.5 saat kaldı</span>
            </CardFooter>
          </Card>
        </div>

        {/* Semantic Alerts Row */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <Alert
            variant="warning"
            icon={<AlertTriangle className="h-4 w-4" />}
            title="Dikkat Gerektiren Konu"
          >
            Fizik Optik konusunda eksik kazanımlar mevcut. Ek etüt planlayabilirsiniz.
          </Alert>
          <Alert
            variant="success"
            icon={<CheckCircle2 className="h-4 w-4" />}
            title="Hedef Tamamlandı"
          >
            Türkçe Paragraf haftalık soru çözümü hedefi başarıyla tamamlandı.
          </Alert>
        </div>

        {/* Empty State Showcase */}
        <EmptyState
          icon={<RefreshCw className="h-5 w-5 animate-spin text-neutral-400" />}
          title="Henüz Tamamlanmış Sınav Yok"
          description="Bu hafta katıldığınız deneme sınavı sonuçları açıklandığında burada listelenecektir."
          action={
            <Button size="sm" variant="secondary">
              Deneme Takvimine Git
            </Button>
          }
        />
      </div>

      {/* Modal / BottomSheet Demonstration */}
      <Modal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        title="Öğrenci Hızlı Detay"
        subtitle="11. Sınıf • Sayısal • YKS 2027"
        headerVariant="dark"
        footer={
          <>
            <Button variant="secondary" size="sm" onClick={() => setIsModalOpen(false)}>
              Kapat
            </Button>
            <Button variant="primary" size="sm" onClick={() => setIsModalOpen(false)}>
              Detaylı Profile Git
            </Button>
          </>
        }
      >
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-3">
            <div className="bg-surface-alt rounded-xl p-3">
              <p className="text-xs text-neutral-400 mb-1">Plan Uyumu</p>
              <p className="font-mono text-xl font-semibold text-success">%88</p>
            </div>
            <div className="bg-surface-alt rounded-xl p-3">
              <p className="text-xs text-neutral-400 mb-1">Son Aktivite</p>
              <p className="text-sm font-semibold text-neutral-700">Bugün, 09:30</p>
            </div>
          </div>
          <p className="text-xs text-neutral-500 leading-relaxed">
            Koç Hasan Yılmaz ile bir sonraki birebir görüşmeniz <strong>Cuma, 16:00</strong> için planlanmıştır.
          </p>
        </div>
      </Modal>
    </AppShell>
  );
};
