# Eğitim Platformu — Proje Anayasası

## Durum

Bu proje uzun vadeli, production seviyesinde bir eğitim platformudur.
Uygulama kodu henüz yazılmamıştır. Teknoloji stack seçimi yapılmamıştır.

## Çalışma Prensipleri

1. **Önce analiz ve plan, sonra kod.** Büyük değişikliklerde doğrudan kod yazmaya başlama.
2. **YAGNI.** Gereksiz over-engineering yapma. En basit sürdürülebilir çözümü seç.
3. **Modüler mimari.** Sürdürülebilir ve ölçeklenebilir yapı kur.
4. **Güvenlik mimari düzeyde.** Sonradan eklenen değil, içten gelen bir özellik.
5. **Net sorumluluk ayrımı.** Backend, frontend ve database birbirine karışmasın.
6. **Database şema disiplini.** Migration gerektiren değişiklikleri önceden belirt.
7. **Auth zorunluluğu.** Authentication ve authorization backend tarafında zorunlu.
8. **Input validation.** Her trust boundary'dde uygula.
9. **Secret yönetimi.** Token, key, connection string kaynak koda yazma.
10. **Kod tekrarı azalt, gereksiz abstraction yapma.** Tekrar = sorun, abstraction = daha büyük sorun olabilir.
11. **Onay gerektiren kararlar.** Büyük mimari kararları kullanıcının onayı olmadan değiştirme.
12. **Doğrulama.** Bir işi bitmiş kabul etmeden önce build/test/verification yap.

## Detaylı Kurallar

Alan bazlı kurallar `.claude/rules/` altında ayrı dosyalardadır:

- `architecture.md` — Mimari kararlar, katmanlar arası bağımlılık, modülerlik
- `backend.md` — API tasarımı, auth, iş mantığı, validation
- `frontend.md` — UI/UX, component yapısı, state yönetimi, erişilebilirlik
- `database.md` — Schema tasarımı, migration, query disiplini
- `security.md` — Güvenlik katmanları, secret yönetimi, OWASP
- `testing.md` — Test stratejisi, coverage, verification
- `workflow.md` — İş akışı, plan-before-code, code review süreci

## Claude Code Araç Kullanımı

### Context7 MCP
- **Ne zaman:** Bir framework/library/API hakkında güncel dokümantasyon gerektiğinde.
- **Ne zaman değil:** Refactoring, sıfırdan script yazma, code review.
- Kullanıcı bir kütüphane adını veya API sorgusunu belirttiğinde, eğitim verisi eski olsa bile önce Context7'ye sor.

### Playwright MCP
- **Ne zaman:** Browser tabanlı UI doğrulaması, E2E test, görsel kontrol.
- **Ne zaman değil:** Backend mantığı, API testi (API için kendi test aracını kullan).

### Impeccable
- **Ne zaman:** Frontend arayüz tasarımı, UX eleştirisi, visual polish, tasarım sistemi.
- **Ne zaman değil:** Backend kodu, veritabanı, saf mantık.

### frontend-design
- **Ne zaman:** Yeni UI/ekran oluştururken estetik yön, tipografi, renk, layout kararı.
- **Ne zaman değil:** Var olan bir bug fix, backend işi.

### feature-dev
- **Ne zaman:** Yeni özellik geliştirme — codebase analizi, mimari plan, implementation blueprint.
- **Ne zaman değil:** Küçük bug fix, refactor, dokümantasyon.

### code-review
- **Ne zaman:** PR veya diff üzerinden kod kalitesi, bug, security, convention kontrolü.
- **Ne zaman değil:** Yeni özellik planlama (feature-dev kullan).

### security-guidance / security-review
- **Ne zaman:** Auth, authorization, ödeme, hassas veri, dışa açık API, kritik güvenlik değişikliği.
- **Ne zaman değil:** UI renk değişikliği, dokümantasyon düzeltmesi.

### simplify
- **Ne zaman:** Önemli kod değişikliklerinden sonra reuse, simplification, efficiency kontrolü.
- **Ne zaman değil:** İlk implementasyon sırasında (önce çalıştır, sonra sadeleştir).

### Ponytail
- **Ne zaman:** Her kod yazma kararında aktif. YAGNI, stdlib first, en kısa diff.
- **Ne zaman değil:** Güvenlik, input validation, veri kaybı önleme, erişilebilirlik — bunlarda sadelik arama, doğruluğu önceliklendir.

### run
- **Ne zaman:** Uygulamanın çalıştığını doğrulamak, değişiklikleri canlı görmek.
- **Ne zaman değil:** Henüz uygulama kodu yok.

## Başlamadan Önce

Teknoloji stack seçimi yapılmamıştır. Mimari aşamasında birlikte belirlenecektir.
Şu anda sadece Claude Code çalışma yapısı hazırdır.
