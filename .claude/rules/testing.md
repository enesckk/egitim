# Testing Kuralları

## Test Piramidi

- **Unit testler:** İş mantığı, domain kuralları, utility fonksiyonları. En geniş katman.
- **Integration testler:** API endpoint'leri, database işlemleri, servis entegrasyonu.
- **E2E testler:** Kritik kullanıcı akışları. En dar katman.

## Zorunlu Testler

- Her yeni iş mantığı fonksiyonu unit test ile gelmeli.
- Her API endpoint en az bir happy-path ve bir error-path testine sahip olmalı.
- Auth ve authorization akışları test edilmeli.
- Edge case'ler (boş input, boundary değerler, yetkisiz erişim) test edilmeli.

## Test Disiplini

- Testler deterministic olmalı; flaky test kabul edilmez.
- Test verisi production verisinden izole olmalı.
- Testler birbirine bağımlı olmamalı; her test bağımsız çalışabilmeli.
- Mock kullanımı minimize edilmeli; gerçek bağımlılık tercih edilmeli.

## Coverage

- Coverage metrik değil araçtır. %100 coverage hedefleme, kritik yolları test et.
- Coverage raporları izlenmeli; düşüş fark edildiğinde değerlendirilmeli.

## Browser Testi

- UI doğrulaması için `Playwright MCP` kullanılmalı.
- E2E test senaryoları kritik kullanıcı akışlarını kapsamalı.

## Verification

- Bir iş bitmeden önce build, typecheck, lint ve ilgili testler çalıştırılmalı.
- Test başarısızlıkları araştırılmalı; tahmini fix uygulanmamalı.
- CI ortamında test geçişi production deployment ön koşulu.
