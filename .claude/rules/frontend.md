# Frontend Kuralları

## UI/UX Prenspleri

- Erişilebilirlik (a11y) opsiyonel değil, zorunlu.
- Responsive tasarım temel kabul edilir.
- Loading, error, empty state'leri her ekran için tasarlanmalı.
- Formlarda inline validation, başarılı submit feedback'i sağlanmalı.
- Dark mode desteği mimari aşamasında değerlendirilecek.

## Component Yapısı

- Component'ler tek sorumluluk taşımalı.
- Presentational ve container (logic) ayrımı uygulanmalı.
- Reusable component'ler kendi içinde izole ve test edilebilir olmalı.
- Component library baştan şişirilmemeli; ihtiyaç oldukça oluşturulmalı.

## State Yönetimi

- Local state tercih edilmeli; global state ancak gerçekten gerektiğinde.
- Server state (API verisi) ve client state (UI state) ayrımı net olmalı.
- State management çözümü mimari aşamasında seçilecek.

## Routing

- Route yapısı kullanıcı rolleri ve yetkilendirme ile uyumlu olmalı.
- Protected route'lar hem client hem server tarafında korunmalı.
- Deep link desteği düşünülmeli.

## Performans

- Code splitting ve lazy loading varsayılan.
- Gereksiz re-render'ları önle, ama premature optimization yapma.
- Asset optimizasyonu (image, font) production build'te zorunlu.

## Tasarım Kararları

- Yeni UI tasarımı için `frontend-design` skill ve `Impeccable` kullan.
- Browser doğrulaması için `Playwright MCP` kullan.
- Tasarım token'ları (renk, spacing, typography) merkezi yönetilmeli.
