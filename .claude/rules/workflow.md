# İş Akışı Kuralları

## Plan-Before-Code

- Büyük değişikliklerde önce analiz ve plan yap.
- Plan kullanıcının onayından geçmeden implementasyona başlama.
- Plan aşamasında mevcut kod tabanını incele, pattern ve convention'ları anla.
- Mimari kararlar belgelenmeli; karara varmadan kod yazma.

## Küçük Adımlar

- Değişiklikleri küçük ve reviewable parçalara böl.
- Her adım çalışır durumda olmalı; bozuk ara durum commit etme.
- Bağımsız işleri paralel yürütebilirsin.

## Code Review

- Önemli değişikliklerden sonra `code-review` skill kullan.
- Code review'da: correctness, security, performance, convention, readability.
- Review bulguları düzeltilmeden işi bitmiş sayma.
- `simplify` skill ile gereksiz karmaşıklığı kontrol et.

## Over-engineering Koruması

- `Ponytail` prensiplerini her kod kararında uygula.
- Yeni abstraction eklemeden önce: stdlib yeterli mi, mevcut helper var mı, gerçekten ihtiyaç var mı?
- Tek implementasyonlu interface, tek ürünlü factory, hiç değişmeyen config ekleme.

## Dokümantasyon

- Kararlar ve mimari değişiklikler belgelenmeli.
- README güncel tutulmalı.
- Public API değişikliklerinde backward compatibility değerlendirilmeli.

## Commit ve Versiyon Kontrolü

- Anlamlı commit mesajları yaz; ne ve neden değiştiğini açıkla.
- Secret, credential, API key commit etme.
- Migration dosyaları versiyon kontrolünde tutulmalı.

## Ortam Ayrımı

- Development, staging, production ortam ayrımı uygulanmalı.
- Environment-specific config environment variable ile yönetilmeli.
- Local development ortamı production'a bağımlı olmamalı.

## Güvenlik Review

- Auth, authorization, ödeme, hassas veri değişikliklerinde `security-review` kullan.
- Dışa açık API değişikliklerinde `security-guidance` kullan.

## Feature Geliştirme

- Yeni özellik için `feature-dev` skill kullan: codebase analizi → mimari plan → implementasyon.
- UI/UX özelliği ise önce `frontend-design` ve `Impeccable` ile tasarla.
- Güncel dokümantasyon için `Context7` kullan.
